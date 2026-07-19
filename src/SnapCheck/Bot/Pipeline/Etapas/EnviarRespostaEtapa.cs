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

        if (context.StatusJanela is StatusJanelaPresenca.SemTurmaVinculada or StatusJanelaPresenca.ForaDaJanela)
        {
            await BotManager.EnviarComMenuAsync(
                botClient,
                context.Mensagem.ChatId,
                context.MensagemJanela ?? "Presença não registrada.",
                cancellationToken);
            activityLog.Info($"Foto rejeitada por janela ({context.StatusJanela}) no chat {context.Mensagem.ChatId}");
            return;
        }

        if (context.RegistroPorRevisao)
        {
            var totalFaces = context.Matches.Count;
            var reconhecidosSugestao = context.Matches.Count(m => m.Reconhecido);
            var resumoRevisao = new List<string>
            {
                $"📝 Revisão de presença criada (#{context.RevisaoPresencaId}).",
                $"Faces detectadas: {totalFaces}. Sugestões automáticas: {reconhecidosSugestao}.",
                "Confirme no painel web em até 24 horas."
            };

            if (context.Erros.Count > 0)
            {
                resumoRevisao.AddRange(context.Erros.Select(e => $"⚠️ {e}"));
            }

            await BotManager.EnviarComMenuAsync(
                botClient,
                context.Mensagem.ChatId,
                string.Join("\n", resumoRevisao),
                cancellationToken);

            if (context.ImagemAnotada is not null)
            {
                using var stream = new MemoryStream(context.ImagemAnotada);
                await botClient.SendPhoto(
                    context.Mensagem.ChatId,
                    InputFile.FromStream(stream, "revisao-presenca.jpg"),
                    caption: "Foto enviada para revisão de presença",
                    replyMarkup: BotKeyboard.MenuPrincipal,
                    cancellationToken: cancellationToken);
            }

            activityLog.Info($"Revisão de presença #{context.RevisaoPresencaId} enviada ao chat {context.Mensagem.ChatId}");
            return;
        }

        var reconhecidos = context.PresencasRegistradas.Distinct().ToList();
        var duplicados = context.PresencasJaRegistradas.Distinct().ToList();
        var desconhecidos = context.Matches.Count(m => !m.Reconhecido);

        var resumo = new List<string>();
        if (reconhecidos.Count > 0)
        {
            var rotuloStatus = context.StatusJanela switch
            {
                StatusJanelaPresenca.Atrasado => " (atrasado)",
                StatusJanelaPresenca.Parcial => " (presença parcial)",
                _ => string.Empty
            };
            resumo.Add($"✅ Presença registrada{rotuloStatus}: {string.Join(", ", reconhecidos)}");
        }

        if (desconhecidos > 0)
        {
            resumo.Add($"⚠️ {desconhecidos} rosto(s) não reconhecido(s). Use o botão *Cadastrar Pessoa* para incluir.");
        }

        if (context.Mensagem.NotificarDuplicidadeAoSolicitante && duplicados.Count > 0)
        {
            resumo.Add($"ℹ️ Presença já registrada para: {string.Join(", ", duplicados)}");
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
