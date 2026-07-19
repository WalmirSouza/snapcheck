using Microsoft.AspNetCore.Mvc;
using SnapCheck.Bot.Services;
using SnapCheck.Data.Repositories;
using SnapCheck.Web.Models;

namespace SnapCheck.Web.Controllers;

[ApiController]
[Route("api/presencas")]
public sealed class PresencasController(
    IPresencaRepository presencaRepository,
    IActivityLog activityLog) : ControllerBase
{
    [HttpPost("manual")]
    public async Task<ActionResult<RegistrarPresencaManualResponse>> RegistrarManual(
        [FromBody] RegistrarPresencaManualRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId <= 0)
        {
            return BadRequest("TenantId inválido.");
        }

        if (request.PessoaId <= 0)
        {
            return BadRequest("PessoaId inválido.");
        }

        if (string.IsNullOrWhiteSpace(request.Responsavel))
        {
            return BadRequest("Responsavel é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.Motivo))
        {
            return BadRequest("Motivo é obrigatório.");
        }

        var resultado = await presencaRepository.RegistrarManualAsync(
            request.TenantId,
            request.PessoaId,
            request.Turma,
            request.Responsavel.Trim(),
            request.Motivo.Trim(),
            cancellationToken);

        // Trilha de auditoria hoje é o log operacional (IActivityLog, em memória).
        // Auditoria imutável de verdade é o item 04.5 — este é o mesmo tipo de
        // stopgap documentado no item 01.9 para autenticação.
        activityLog.Info(
            $"Presença manual: tenant {request.TenantId}, pessoa {request.PessoaId}, " +
            $"responsável {request.Responsavel.Trim()}, motivo \"{request.Motivo.Trim()}\" " +
            $"({(resultado == RegistroPresencaResultado.Registrada ? "registrada" : "já existia")}).");

        return Ok(new RegistrarPresencaManualResponse
        {
            Sucesso = true,
            Status = resultado.ToString(),
            Mensagem = resultado == RegistroPresencaResultado.Registrada
                ? "Presença manual registrada com sucesso."
                : "Já existia presença registrada para esta pessoa nesta turma hoje."
        });
    }
}
