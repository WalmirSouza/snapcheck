using SnapCheck.Bot.Models;
using SnapCheck.Data.Repositories;

namespace SnapCheck.Bot.Pipeline.Etapas;

public sealed class RegistrarPresencaEtapa(
    IPresencaRepository presencaRepository,
    IRevisaoPresencaRepository revisaoPresencaRepository) : IPipelineEtapa
{
    public string Nome => "RegistrarPresenca";

    public async Task ExecutarAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        if (context.Matches.Count > 1)
        {
            var revisaoId = await revisaoPresencaRepository.CriarAsync(
                new RevisaoCriacaoInput
                {
                    TenantId = context.Mensagem.TenantId,
                    Turma = context.Mensagem.Turma,
                    ChatId = context.Mensagem.ChatId,
                    CriadoPor = $"chat:{context.Mensagem.ChatId}",
                    ExpiraEm = DateTime.UtcNow.AddHours(24),
                    Origem = "telegram",
                    ReferenciaArquivo = context.Mensagem.FileId,
                    Itens = context.Matches.Select(m => new RevisaoCriacaoItemInput
                    {
                        PessoaSugeridaId = m.PessoaId,
                        NomeSugerido = m.Nome,
                        Confianca = m.Similaridade
                    }).ToList()
                },
                cancellationToken);

            context.RevisaoPresencaId = revisaoId;
            context.RegistroPorRevisao = true;
            return;
        }

        var processados = new HashSet<int>();

        foreach (var match in context.Matches.Where(m => m.Reconhecido))
        {
            var pessoaId = match.PessoaId!.Value;
            if (!processados.Add(pessoaId))
            {
                continue;
            }

            var resultado = await presencaRepository.RegistrarAsync(
                context.Mensagem.TenantId,
                pessoaId,
                context.Mensagem.Turma,
                cancellationToken);

            if (resultado == RegistroPresencaResultado.Registrada)
            {
                context.PresencasRegistradas.Add(match.Nome ?? $"Pessoa #{pessoaId}");
                continue;
            }

            context.PresencasJaRegistradas.Add(match.Nome ?? $"Pessoa #{pessoaId}");
        }
    }
}
