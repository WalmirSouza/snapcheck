using System.Reflection;
using Dapper;
using Microsoft.Extensions.Logging;

namespace SnapCheck.Data;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseInitializer(
    IDbConnectionFactory connectionFactory,
    ILogger<DatabaseInitializer> logger) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionFactory.ConnectionString))
        {
            logger.LogWarning("Banco não inicializado: connection string ausente.");
            return;
        }

        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream("SnapCheck.Data.Scripts.init.sql")
            ?? throw new InvalidOperationException("Script init.sql não encontrado.");

        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync(cancellationToken);

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
        logger.LogInformation("Banco de dados inicializado com sucesso.");
    }
}
