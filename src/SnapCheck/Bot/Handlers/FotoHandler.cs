using SnapCheck.Bot.Models;
using SnapCheck.Bot.Services;
using Telegram.Bot.Types;

namespace SnapCheck.Bot.Handlers;

public sealed class FotoHandler(IMessageChannel messageChannel, IActivityLog activityLog)
{
    public async Task<string?> HandleAsync(Update update, CancellationToken cancellationToken = default)
    {
        var message = update.Message!;
        var photo = message.Photo?.OrderByDescending(p => p.FileSize).FirstOrDefault();

        if (photo is null)
        {
            return "❌ Não foi possível ler a foto enviada.";
        }

        var turma = message.Chat.Title ?? message.Chat.Username ?? message.Chat.Id.ToString();

        await messageChannel.EnfileirarAsync(new MensagemProcessamento
        {
            Tipo = TipoMensagem.FotoPresenca,
            ChatId = message.Chat.Id,
            MessageId = message.MessageId,
            FileId = photo.FileId,
            Turma = turma
        }, cancellationToken);

        activityLog.Info($"Foto enfileirada para processamento (chat {message.Chat.Id})");

        return "⏳ Foto recebida! Processando presença...\n\n_Aguarde, em instantes devolvo a imagem com os nomes._";
    }
}
