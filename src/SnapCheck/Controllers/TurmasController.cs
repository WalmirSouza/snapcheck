using Microsoft.AspNetCore.Mvc;
using SnapCheck.Data.Repositories;
using SnapCheck.Web.Models;

namespace SnapCheck.Web.Controllers;

[ApiController]
[Route("api/turmas")]
public sealed class TurmasController(ITurmaRepository turmaRepository) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CriarTurmaResponse>> Criar(
        [FromBody] CriarTurmaRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId <= 0)
        {
            return BadRequest("TenantId inválido.");
        }

        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return BadRequest("Nome é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.CodigoVinculacao))
        {
            return BadRequest("CodigoVinculacao é obrigatório.");
        }

        var (resultado, turmaId) = await turmaRepository.CriarAsync(
            request.TenantId, request.Nome.Trim(), request.CodigoVinculacao.Trim(), cancellationToken);

        if (resultado == CriarTurmaResultado.CodigoJaExisteNoTenant)
        {
            return Conflict(new CriarTurmaResponse
            {
                Sucesso = false,
                Mensagem = "Já existe uma turma com esse código de vinculação neste tenant."
            });
        }

        return Ok(new CriarTurmaResponse
        {
            Sucesso = true,
            TurmaId = turmaId,
            Mensagem = "Turma criada com sucesso."
        });
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TurmaResponse>>> Listar(
        [FromQuery] int tenantId,
        CancellationToken cancellationToken)
    {
        if (tenantId <= 0)
        {
            return BadRequest("TenantId inválido.");
        }

        var turmas = await turmaRepository.ListarPorTenantAsync(tenantId, cancellationToken);
        return Ok(turmas.Select(t => new TurmaResponse
        {
            Id = t.Id,
            Nome = t.Nome,
            CodigoVinculacao = t.CodigoVinculacao,
            Ativa = t.Ativa
        }).ToList());
    }

    [HttpPost("{turmaId:int}/janelas")]
    public async Task<ActionResult<OperacaoResponse>> AdicionarJanela(
        [FromRoute] int turmaId,
        [FromBody] AdicionarJanelaRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId <= 0)
        {
            return BadRequest("TenantId inválido.");
        }

        if (request.DiasSemana.Length == 0)
        {
            return BadRequest("Informe ao menos um dia da semana (1=segunda ... 7=domingo).");
        }

        if (!TimeSpan.TryParse(request.HoraInicio, out var horaInicio) ||
            !TimeSpan.TryParse(request.HoraFim, out var horaFim))
        {
            return BadRequest("HoraInicio e HoraFim devem estar no formato HH:mm.");
        }

        if (horaFim <= horaInicio)
        {
            return BadRequest("HoraFim deve ser depois de HoraInicio.");
        }

        var turmas = await turmaRepository.ListarPorTenantAsync(request.TenantId, cancellationToken);
        if (!turmas.Any(t => t.Id == turmaId))
        {
            return NotFound("Turma não encontrada para este tenant.");
        }

        await turmaRepository.AdicionarJanelaAsync(
            turmaId,
            request.DiasSemana,
            horaInicio,
            horaFim,
            request.ToleranciaAtrasoMinutos,
            request.CortePresencaParcialPercentual,
            cancellationToken);

        return Ok(new OperacaoResponse
        {
            Sucesso = true,
            Mensagem = "Janela adicionada com sucesso."
        });
    }
}
