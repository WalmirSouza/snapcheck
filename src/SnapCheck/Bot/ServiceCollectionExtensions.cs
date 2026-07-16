using Microsoft.Extensions.DependencyInjection;
using SnapCheck.Bot.Handlers;
using SnapCheck.Bot.Pipeline;
using SnapCheck.Bot.Pipeline.Etapas;
using SnapCheck.Bot.Services;
using SnapCheck.Data;
using SnapCheck.Data.Repositories;
using SnapCheck.Face;
using SnapCheck.Imaging;

namespace SnapCheck.Bot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSnapCheckBot(this IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
        services.AddSingleton<IConfiguracaoRepository, ConfiguracaoRepository>();
        services.AddSingleton<IPessoaRepository, PessoaRepository>();
        services.AddSingleton<IPresencaRepository, PresencaRepository>();

        services.AddSingleton<IFaceService, FaceService>();
        services.AddSingleton<IImageAnnotator, ImageAnnotator>();
        services.AddSingleton<IBotClientProvider, BotClientProvider>();
        services.AddSingleton<IActivityLog, ActivityLog>();
        services.AddSingleton<IBotMetricsService, BotMetricsService>();
        services.AddSingleton<IMessageChannel, MessageChannel>();
        services.AddSingleton<IBotManager, BotManager>();

        services.AddSingleton<StartHandler>();
        services.AddSingleton<CadastroHandler>();
        services.AddSingleton<FotoHandler>();
        services.AddSingleton<ConsultaHandler>();

        services.AddSingleton<IPipelineEtapa, DownloadFotoEtapa>();
        services.AddSingleton<IPipelineEtapa, DetectarRostosEtapa>();
        services.AddSingleton<IPipelineEtapa, CompararRostosEtapa>();
        services.AddSingleton<IPipelineEtapa, RegistrarPresencaEtapa>();
        services.AddSingleton<IPipelineEtapa, AnotarImagemEtapa>();
        services.AddSingleton<IPipelineEtapa, EnviarRespostaEtapa>();

        services.AddHostedService<PipelineService>();

        return services;
    }
}
