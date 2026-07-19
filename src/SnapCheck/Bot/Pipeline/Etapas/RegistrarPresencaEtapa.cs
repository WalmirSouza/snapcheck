using SnapCheck.Bot.Models;
using SnapCheck.Data;
using SnapCheck.Data.Repositories;

namespace SnapCheck.Bot.Pipeline.Etapas;

public sealed class RegistrarPresencaEtapa(
    IPresencaRepository presencaRepository,
    IRevisaoPresencaRepository revisaoPresencaRepository) : IPipelineEtapa
{
    // Faixa "incerto" (ADR 0005, Decisão 1) — abaixo disso é desconhecido demais
    // pra valer revisão (docs/requisitos/qualidade-biometrica.md). O teto (0.42)
    // é o mesmo default de FaceService.CompararComCadastro.
    private const float LimiarInferiorRevisao = 0.30f;

    public string Nome => "RegistrarPresenca";

    public async Task ExecutarAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        if (context.StatusJanela is StatusJanelaPresenca.SemTurmaVinculada or StatusJanelaPresenca.ForaDaJanela)
        {
            // ValidarJanelaPresencaEtapa já preencheu context.MensagemJanela — não
            // registra presença nem cria revisão, e não silencia (regra do item 02.1).
            return;
        }

        var revisaoPorMultiplosRostos = context.Matches.Count > 1;
        var revisaoPorBaixaConfianca = context.Matches.Count == 1 &&
            !context.Matches[0].Reconhecido &&
            context.Matches[0].Similaridade >= LimiarInferiorRevisao;

        if (revisaoPorMultiplosRostos || revisaoPorBaixaConfianca)
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
                        Confianca = m.Similaridade,
                        Embedding = m.Face.Embedding.Length > 0 ? EmbeddingHelper.ToBytes(m.Face.Embedding) : null
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

            var statusPresenca = (context.StatusJanela ?? StatusJanelaPresenca.Completa)
                .ToString()
                .ToLowerInvariant();

            var resultado = await presencaRepository.RegistrarAsync(
                context.Mensagem.TenantId,
                pessoaId,
                context.Mensagem.Turma,
                statusPresenca,
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
