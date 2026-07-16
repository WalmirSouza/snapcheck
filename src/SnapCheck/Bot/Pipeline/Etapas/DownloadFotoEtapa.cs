using SnapCheck.Bot.Models;
using SnapCheck.Bot.Services;
using Telegram.Bot;

namespace SnapCheck.Bot.Pipeline.Etapas;

public interface IPipelineEtapa
{
    string Nome { get; }
    Task ExecutarAsync(PipelineContext context, CancellationToken cancellationToken = default);
}

public sealed class DownloadFotoEtapa(IBotClientProvider botClientProvider) : IPipelineEtapa
{
    public string Nome => "DownloadFoto";

    public async Task ExecutarAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        var botClient = botClientProvider.Client
            ?? throw new InvalidOperationException("Bot do Telegram não está em execução.");

        var file = await botClient.GetFile(context.Mensagem.FileId, cancellationToken);
        await using var stream = new MemoryStream();
        await botClient.DownloadFile(file.FilePath!, stream, cancellationToken);
        context.ImagemBytes = stream.ToArray();
    }
}
