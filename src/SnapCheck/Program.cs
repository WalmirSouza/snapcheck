using SnapCheck.Bot;
using SnapCheck.Data;
using SnapCheck.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddSnapCheckBot();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.MapRazorPages();
app.MapGet("/", () => Results.Redirect("/Index"));

using (var scope = app.Services.CreateScope())
{
    var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
    var configuracaoRepository = scope.ServiceProvider.GetRequiredService<IConfiguracaoRepository>();
    var databaseInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var connectionString = builder.Configuration.GetConnectionString("PostgreSQL")
            ?? await configuracaoRepository.ObterValorAsync(ConfigKeys.PostgresConnectionString);

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            connectionFactory.SetConnectionString(connectionString);
            await databaseInitializer.InitializeAsync();
        }
        else
        {
            logger.LogWarning("Connection string não configurada. Configure pela interface web.");
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Não foi possível inicializar o banco na startup.");
    }
}

app.Run();
