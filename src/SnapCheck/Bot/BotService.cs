using Microsoft.Extensions.Logging;
using SnapCheck.Bot.Handlers;
using SnapCheck.Bot.Services;
using SnapCheck.Data;
using SnapCheck.Data.Repositories;
using SnapCheck.Data.Tenancy;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SnapCheck.Bot;

public interface IBotManager
{
    bool IsRunning { get; }
    Task<bool> StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

public sealed class BotManager(
    IConfiguracaoRepository configuracaoRepository,
    IDbConnectionFactory connectionFactory,
    IBotClientProvider botClientProvider,
    ITenantRepository tenantRepository,
    ITenantContext tenantContext,
    VincularHandler vincularHandler,
    VincularTurmaHandler vincularTurmaHandler,
    StartHandler startHandler,
    CadastroHandler cadastroHandler,
    FotoHandler fotoHandler,
    ConsultaHandler consultaHandler,
    IBotMetricsService metricsService,
    IActivityLog activityLog,
    ILogger<BotManager> logger) : IBotManager
{
    private CancellationTokenSource? _cts;

    public bool IsRunning => _cts is not null;

    public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            return true;
        }

        var token = await configuracaoRepository.ObterValorAsync(ConfigKeys.TelegramToken, cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            activityLog.Error("Token do Telegram não configurado.");
            return false;
        }

        var connectionString = await configuracaoRepository.ObterValorAsync(
            ConfigKeys.PostgresConnectionString, cancellationToken);

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            connectionFactory.SetConnectionString(connectionString);
        }

        var client = new TelegramBotClient(token);
        botClientProvider.SetClient(client);

        await ConfigurarMenuBotAsync(client, cancellationToken);

        _cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message]
        };

        client.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: _cts.Token);

        metricsService.SetOnline(true);
        activityLog.Info("Bot do Telegram iniciado.");
        logger.LogInformation("Bot do Telegram iniciado.");
        return true;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            return Task.CompletedTask;
        }

        _cts!.Cancel();
        _cts.Dispose();
        _cts = null;
        botClientProvider.SetClient(null);

        metricsService.SetOnline(false);
        activityLog.Info("Bot do Telegram parado.");
        logger.LogInformation("Bot do Telegram parado.");
        return Task.CompletedTask;
    }

    private static async Task ConfigurarMenuBotAsync(ITelegramBotClient client, CancellationToken cancellationToken)
    {
        await client.SetMyCommands(
        [
            new BotCommand { Command = "vincular", Description = "Vincular este chat a um cliente" },
            new BotCommand { Command = "turma", Description = "Vincular este chat a uma turma" },
            new BotCommand { Command = "start", Description = "Abrir menu com botões" },
            new BotCommand { Command = "cadastrar", Description = "Cadastrar nova pessoa" },
            new BotCommand { Command = "listar", Description = "Listar pessoas cadastradas" },
            new BotCommand { Command = "sumidos", Description = "Quem não aparece há X dias" },
            new BotCommand { Command = "frequencia", Description = "Consultar presença de alguém" },
            new BotCommand { Command = "remover", Description = "Descadastrar pessoa" }
        ], cancellationToken: cancellationToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is null)
        {
            return;
        }

        var chatId = update.Message.Chat.Id;

        try
        {
            var texto = string.IsNullOrWhiteSpace(update.Message.Text) ? null : update.Message.Text.Trim();
            var comando = texto is null ? null : BotKeyboard.ParaComando(texto) ?? texto;

            string? resposta;

            if (comando is not null && comando.StartsWith("/vincular", StringComparison.OrdinalIgnoreCase))
            {
                resposta = await vincularHandler.HandleAsync(chatId, comando, cancellationToken);
            }
            else
            {
                var tenantId = await tenantRepository.ObterTenantIdPorChatAsync(chatId, cancellationToken);
                if (tenantId is null)
                {
                    resposta = "🔒 Este chat ainda não está vinculado a nenhum cliente.\n" +
                        "Use */vincular CODIGO* com o código de ativação enviado pelo SnapCheck.";
                }
                else
                {
                    using var _ = tenantContext.BeginScope(tenantId.Value);
                    resposta = await ProcessarUpdateAsync(bot, update, chatId, texto, comando, cancellationToken);
                }
            }

            if (!string.IsNullOrWhiteSpace(resposta))
            {
                await EnviarComMenuAsync(bot, chatId, resposta, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar update {UpdateId}", update.Id);
            metricsService.RegistrarErro(ex.Message);
            activityLog.Error($"Erro no handler: {ex.Message}");

            await EnviarComMenuAsync(
                bot,
                chatId,
                "❌ Ocorreu um erro ao processar sua mensagem. Tente novamente.",
                cancellationToken);
        }
    }

    private async Task<string?> ProcessarUpdateAsync(
        ITelegramBotClient bot,
        Update update,
        long chatId,
        string? texto,
        string? comando,
        CancellationToken cancellationToken)
    {
        string? resposta = null;

        if (texto is not null)
        {
            if (texto == BotKeyboard.RegistrarPresenca)
            {
                resposta = BotKeyboard.InstrucaoEnviarFoto;
            }
            else if (comando!.StartsWith("/turma", StringComparison.OrdinalIgnoreCase))
            {
                resposta = await vincularTurmaHandler.HandleAsync(chatId, comando, cancellationToken);
            }
            else if (comando.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
            {
                resposta = await startHandler.HandleAsync(update, cancellationToken);
            }
            else if (comando.StartsWith("/cadastrar", StringComparison.OrdinalIgnoreCase))
            {
                resposta = await cadastroHandler.HandleAsync(CriarUpdateComTexto(update, comando), cancellationToken);
            }
            else
            {
                var updateComando = CriarUpdateComTexto(update, comando);
                resposta = await cadastroHandler.HandleAsync(updateComando, cancellationToken)
                    ?? await consultaHandler.HandleAsync(updateComando, cancellationToken);
            }
        }

        if (update.Message!.Photo?.Length > 0)
        {
            if (cadastroHandler.EstaEmCadastro(chatId))
            {
                var fotoBytes = await BaixarFotoAsync(bot, update, cancellationToken);
                resposta = await cadastroHandler.HandleFotoAsync(update, fotoBytes, cancellationToken);
            }
            else
            {
                resposta = await fotoHandler.HandleAsync(update, cancellationToken);
            }
        }

        return resposta;
    }

    internal static async Task EnviarComMenuAsync(
        ITelegramBotClient bot,
        long chatId,
        string texto,
        CancellationToken cancellationToken,
        ParseMode parseMode = ParseMode.Markdown)
    {
        await bot.SendMessage(
            chatId,
            texto,
            parseMode: parseMode,
            replyMarkup: BotKeyboard.MenuPrincipal,
            cancellationToken: cancellationToken);
    }

    private static Update CriarUpdateComTexto(Update update, string texto)
    {
        update.Message!.Text = texto;
        return update;
    }

    private static async Task<byte[]> BaixarFotoAsync(
        ITelegramBotClient bot,
        Update update,
        CancellationToken cancellationToken)
    {
        var photo = update.Message!.Photo!.OrderByDescending(p => p.FileSize).First();
        var file = await bot.GetFile(photo.FileId, cancellationToken);
        await using var stream = new MemoryStream();
        await bot.DownloadFile(file.FilePath!, stream, cancellationToken);
        return stream.ToArray();
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Erro no polling do Telegram");
        metricsService.RegistrarErro(exception.Message);
        activityLog.Error($"Erro no polling: {exception.Message}");
        return Task.CompletedTask;
    }
}
