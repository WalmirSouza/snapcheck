using SnapCheck.Bot.Models;
using SnapCheck.Bot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SnapCheck.Bot.Pipeline.Etapas;

public sealed class EnviarRespostaEtapa(
    IBotClientProvider botClientProvider,
    IActivityLog activityLog) : IPipelineEtapa
{
    public string Nome => "EnviarResposta";

    public async Task ExecutarAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        var botClient = botClientProvider.Client
            ?? throw new InvalidOperationException("Bot do Telegram não está em execução.");

        var reconhecidos = context.Matches.Where(m => m.Reconhecido).Select(m => m.Nome!).Distinct().ToList();
        var desconhecidos = context.Matches.Count(m => !m.Reconhecido);

        var resumo = new List<string>();
        if (reconhecidos.Count > 0)
        {
            resumo.Add($"✅ Presença registrada: {string.Join(", ", reconhecidos)}");
        }

        if (desconhecidos > 0)
        {
            resumo.Add($"⚠️ {desconhecidos} rosto(s) não reconhecido(s). Use o botão *Cadastrar Pessoa* para incluir.");
        }

        if (context.Erros.Count > 0)
        {
            resumo.AddRange(context.Erros.Select(e => $"⚠️ {e}"));
        }

        if (resumo.Count == 0)
        {
            resumo.Add("Nenhum rosto processado.");
        }

        await BotManager.EnviarComMenuAsync(
            botClient,
            context.Mensagem.ChatId,
            string.Join("\n", resumo),
            cancellationToken);

        if (context.ImagemAnotada is not null)
        {
            using var stream = new MemoryStream(context.ImagemAnotada);
            await botClient.SendPhoto(
                context.Mensagem.ChatId,
                InputFile.FromStream(stream, "presenca.jpg"),
                caption: "Foto processada pelo SnapCheck",
                replyMarkup: BotKeyboard.MenuPrincipal,
                cancellationToken: cancellationToken);
        }

        activityLog.Info($"Resposta enviada ao chat {context.Mensagem.ChatId}");
    }
}
