using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnapCheck.Bot.Models;
using SnapCheck.Bot.Pipeline.Etapas;
using SnapCheck.Bot.Services;

namespace SnapCheck.Bot.Pipeline;

public sealed class PipelineService(
    IMessageChannel messageChannel,
    IEnumerable<IPipelineEtapa> etapas,
    IBotMetricsService metricsService,
    IActivityLog activityLog,
    ILogger<PipelineService> logger) : BackgroundService
{
    private readonly IReadOnlyList<IPipelineEtapa> _etapas = etapas.ToList();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Pipeline de processamento iniciado.");

        await foreach (var mensagem in messageChannel.Reader.ReadAllAsync(stoppingToken))
        {
            var context = new PipelineContext { Mensagem = mensagem };

            try
            {
                foreach (var etapa in _etapas)
                {
                    try
                    {
                        logger.LogDebug("Executando etapa {Etapa}", etapa.Nome);
                        await etapa.ExecutarAsync(context, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        var erro = $"Falha na etapa {etapa.Nome}: {ex.Message}";
                        context.Erros.Add(erro);
                        logger.LogError(ex, "Erro na etapa {Etapa}", etapa.Nome);
                        activityLog.Error(erro);
                        metricsService.RegistrarErro(erro);
                    }
                }

                metricsService.RegistrarMensagemProcessada();
                metricsService.RegistrarFotoProcessada();
            }
            catch (Exception ex)
            {
                var erro = $"Falha no pipeline: {ex.Message}";
                logger.LogError(ex, "Erro fatal no pipeline");
                activityLog.Error(erro);
                metricsService.RegistrarErro(erro);
            }
        }
    }
}
