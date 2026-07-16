using SnapCheck.Bot.Models;
using SnapCheck.Data;
using SnapCheck.Data.Repositories;
using SnapCheck.Face;

namespace SnapCheck.Bot.Pipeline.Etapas;

public sealed class DetectarRostosEtapa(IFaceService faceService) : IPipelineEtapa
{
    public string Nome => "DetectarRostos";

    public Task ExecutarAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        if (context.ImagemBytes is null)
        {
            throw new InvalidOperationException("Imagem não disponível para detecção.");
        }

        var rostos = faceService.DetectarRostos(context.ImagemBytes);
        if (rostos.Count == 0)
        {
            context.Erros.Add("Nenhum rosto detectado na imagem.");
        }

        return Task.CompletedTask;
    }
}

public sealed class CompararRostosEtapa(
    IFaceService faceService,
    IPessoaRepository pessoaRepository) : IPipelineEtapa
{
    public string Nome => "CompararRostos";

    public async Task ExecutarAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        if (context.ImagemBytes is null)
        {
            throw new InvalidOperationException("Imagem não disponível para comparação.");
        }

        var pessoas = await pessoaRepository.ListarAtivasAsync(cancellationToken);
        var cadastro = pessoas
            .Select(p => (p.Id, p.Nome, EmbeddingHelper.FromBytes(p.Embedding)))
            .ToList();

        context.Matches = faceService.CompararComCadastro(context.ImagemBytes, cadastro);
    }
}
