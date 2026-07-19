using Microsoft.AspNetCore.Mvc;
using SnapCheck.Bot.Services;
using SnapCheck.Data.Models;
using SnapCheck.Data.Repositories;
using SnapCheck.Web.Models;

namespace SnapCheck.Web.Controllers;

[ApiController]
[Route("api/revisoes-presenca")]
public sealed class RevisoesPresencaController(
    IRevisaoPresencaRepository revisaoRepository,
    IActivityLog activityLog) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CriarRevisaoPresencaResponse>> Criar(
        [FromBody] CriarRevisaoPresencaRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId <= 0)
        {
            return BadRequest("TenantId inválido.");
        }

        if (string.IsNullOrWhiteSpace(request.CriadoPor))
        {
            return BadRequest("CriadoPor é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.ReferenciaArquivo))
        {
            return BadRequest("ReferenciaArquivo é obrigatória.");
        }

        if (request.Itens.Count == 0)
        {
            return BadRequest("A revisão deve conter ao menos um item.");
        }

        var expiraEm = DateTime.UtcNow.AddHours(24);
        var revisaoId = await revisaoRepository.CriarAsync(
            new RevisaoCriacaoInput
            {
                TenantId = request.TenantId,
                Turma = request.Turma,
                ChatId = request.ChatId,
                CriadoPor = request.CriadoPor.Trim(),
                ExpiraEm = expiraEm,
                Origem = string.IsNullOrWhiteSpace(request.Origem) ? "telegram" : request.Origem.Trim(),
                ReferenciaArquivo = request.ReferenciaArquivo.Trim(),
                Itens = request.Itens.Select(i => new RevisaoCriacaoItemInput
                {
                    PessoaSugeridaId = i.PessoaSugeridaId,
                    NomeSugerido = i.NomeSugerido,
                    Confianca = i.Confianca
                }).ToList()
            },
            cancellationToken);

        activityLog.Info($"Revisão de presença em grupo criada (tenant {request.TenantId}, revisão {revisaoId}).");

        return Ok(new CriarRevisaoPresencaResponse
        {
            RevisaoId = revisaoId,
            Status = RevisaoStatus.Pendente,
            ExpiraEm = expiraEm,
            TotalItens = request.Itens.Count
        });
    }

    [HttpGet("pendentes")]
    public async Task<ActionResult<IReadOnlyList<RevisaoResumo>>> ListarPendentes(
        [FromQuery] int tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId <= 0)
        {
            return BadRequest("TenantId inválido.");
        }

        var revisoes = await revisaoRepository.ListarPendentesAsync(tenantId, cancellationToken);
        return Ok(revisoes);
    }

    [HttpGet("{revisaoId:int}")]
    public async Task<ActionResult<RevisaoDetalhe>> ObterDetalhe(
        [FromRoute] int revisaoId,
        [FromQuery] int tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId <= 0)
        {
            return BadRequest("TenantId inválido.");
        }

        var revisao = await revisaoRepository.ObterDetalheAsync(tenantId, revisaoId, cancellationToken);
        if (revisao is null)
        {
            return NotFound("Revisão não encontrada.");
        }

        return Ok(revisao);
    }

    [HttpPost("{revisaoId:int}/confirmar")]
    public async Task<ActionResult<ConfirmarRevisaoPresencaResponse>> Confirmar(
        [FromRoute] int revisaoId,
        [FromBody] ConfirmarRevisaoPresencaRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId <= 0)
        {
            return BadRequest("TenantId inválido.");
        }

        if (string.IsNullOrWhiteSpace(request.ConfirmadoPor))
        {
            return BadRequest("ConfirmadoPor é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.PerfilConfirmador))
        {
            return BadRequest("PerfilConfirmador é obrigatório.");
        }

        if (request.Itens.Any(i => !DecisaoValida(i.Decisao)))
        {
            return BadRequest("Decisão inválida. Use: aprovada, rejeitada ou reatribuida.");
        }

        var resultado = await revisaoRepository.ConfirmarAsync(
            new RevisaoConfirmacaoInput
            {
                RevisaoId = revisaoId,
                TenantId = request.TenantId,
                ConfirmadoPor = request.ConfirmadoPor.Trim(),
                PerfilConfirmador = request.PerfilConfirmador.Trim(),
                Itens = request.Itens.Select(i => new RevisaoConfirmacaoItemInput
                {
                    ItemId = i.ItemId,
                    Decisao = i.Decisao.Trim().ToLowerInvariant(),
                    PessoaFinalId = i.PessoaFinalId,
                    Motivo = i.Motivo
                }).ToList()
            },
            cancellationToken);

        var response = new ConfirmarRevisaoPresencaResponse
        {
            Sucesso = resultado.Status == ConfirmacaoRevisaoStatus.Confirmada,
            Status = resultado.Status.ToString(),
            Mensagem = resultado.Mensagem,
            PresencasEfetivadas = resultado.PresencasEfetivadas
        };

        if (resultado.Status == ConfirmacaoRevisaoStatus.NaoEncontrada)
        {
            return NotFound(response);
        }

        if (resultado.Status != ConfirmacaoRevisaoStatus.Confirmada)
        {
            return BadRequest(response);
        }

        activityLog.Info($"Revisão {revisaoId} confirmada no tenant {request.TenantId}.");
        return Ok(response);
    }

    private static bool DecisaoValida(string? decisao) =>
        string.Equals(decisao, RevisaoDecisao.Aprovada, StringComparison.OrdinalIgnoreCase)
        || string.Equals(decisao, RevisaoDecisao.Rejeitada, StringComparison.OrdinalIgnoreCase)
        || string.Equals(decisao, RevisaoDecisao.Reatribuida, StringComparison.OrdinalIgnoreCase);
}
