using SnapCheck.Bot.Models;
using SnapCheck.Imaging;

namespace SnapCheck.Bot.Pipeline.Etapas;

public sealed class AnotarImagemEtapa(IImageAnnotator imageAnnotator) : IPipelineEtapa
{
    public string Nome => "AnotarImagem";

    public Task ExecutarAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        if (context.ImagemBytes is null)
        {
            throw new InvalidOperationException("Imagem não disponível para anotação.");
        }

        context.ImagemAnotada = imageAnnotator.AnotarRostos(context.ImagemBytes, context.Matches);
        return Task.CompletedTask;
    }
}
