using Microsoft.AspNetCore.Mvc;
using SnapCheck.Bot;
using SnapCheck.Bot.Services;
using SnapCheck.Data;
using SnapCheck.Data.Repositories;
using SnapCheck.Web.Models;

namespace SnapCheck.Web.Controllers;

[ApiController]
[Route("api/bot")]
public sealed class BotController(
    IBotManager botManager,
    IConfiguracaoRepository configuracaoRepository,
    IDbConnectionFactory connectionFactory,
    IDatabaseInitializer databaseInitializer,
    IPessoaRepository pessoaRepository,
    IPresencaRepository presencaRepository,
    IBotMetricsService metricsService,
    IActivityLog activityLog) : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<StatusResponse>> ObterStatus(CancellationToken cancellationToken)
    {
        var bancoConectado = await connectionFactory.TestConnectionAsync(cancellationToken);
        var metrics = metricsService.Metrics;

        int totalPessoas = 0;
        int presencasHoje = 0;

        if (bancoConectado)
        {
            try
            {
                totalPessoas = await pessoaRepository.ContarAtivasAsync(cancellationToken);
                presencasHoje = await presencaRepository.ContarHojeAsync(cancellationToken);
            }
            catch
            {
                bancoConectado = false;
            }
        }

        return Ok(new StatusResponse
        {
            BotOnline = metrics.Online,
            BancoConectado = bancoConectado,
            TotalPessoas = totalPessoas,
            PresencasHoje = presencasHoje,
            FotosProcessadasHoje = metrics.FotosProcessadasHoje,
            MensagensProcessadas = metrics.MensagensProcessadas,
            Erros = metrics.Erros,
            Uptime = metrics.Uptime?.ToString(@"dd\.hh\:mm\:ss"),
            UltimaAtividade = metrics.UltimaAtividade?.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"),
            Logs = activityLog.ObterUltimas(10).Select(l => new LogItem
            {
                Timestamp = l.Timestamp.ToLocalTime().ToString("HH:mm:ss"),
                Mensagem = l.Mensagem,
                Nivel = l.Nivel
            }).ToList()
        });
    }

    [HttpPost("configuracoes")]
    public async Task<ActionResult<OperacaoResponse>> SalvarConfiguracoes(
        [FromBody] ConfiguracaoRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(request.ConnectionString))
            {
                connectionFactory.SetConnectionString(request.ConnectionString);
                await databaseInitializer.InitializeAsync(cancellationToken);
                await configuracaoRepository.SalvarAsync(
                    ConfigKeys.PostgresConnectionString,
                    request.ConnectionString,
                    cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(request.TelegramToken))
            {
                await configuracaoRepository.SalvarAsync(
                    ConfigKeys.TelegramToken,
                    request.TelegramToken,
                    cancellationToken);
            }

            activityLog.Info("Configurações salvas pela interface web.");

            return Ok(new OperacaoResponse
            {
                Sucesso = true,
                Mensagem = "Configurações salvas com sucesso."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new OperacaoResponse
            {
                Sucesso = false,
                Mensagem = ex.Message
            });
        }
    }

    [HttpPost("iniciar")]
    public async Task<ActionResult<OperacaoResponse>> Iniciar(CancellationToken cancellationToken)
    {
        var iniciado = await botManager.StartAsync(cancellationToken);
        return Ok(new OperacaoResponse
        {
            Sucesso = iniciado,
            Mensagem = iniciado
                ? "Bot iniciado com sucesso."
                : "Não foi possível iniciar o bot. Verifique o token e a conexão com o banco."
        });
    }

    [HttpPost("parar")]
    public async Task<ActionResult<OperacaoResponse>> Parar(CancellationToken cancellationToken)
    {
        await botManager.StopAsync(cancellationToken);
        return Ok(new OperacaoResponse
        {
            Sucesso = true,
            Mensagem = "Bot parado com sucesso."
        });
    }
}
