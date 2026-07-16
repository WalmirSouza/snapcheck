using SnapCheck.Bot.Models;
using SnapCheck.Data.Repositories;

namespace SnapCheck.Bot.Pipeline.Etapas;

public sealed class RegistrarPresencaEtapa(IPresencaRepository presencaRepository) : IPipelineEtapa
{
    public string Nome => "RegistrarPresenca";

    public async Task ExecutarAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        foreach (var match in context.Matches.Where(m => m.Reconhecido))
        {
            await presencaRepository.RegistrarAsync(
                match.PessoaId!.Value,
                context.Mensagem.Turma,
                cancellationToken);
        }
    }
}
