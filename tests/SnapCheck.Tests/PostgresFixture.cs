using Microsoft.Extensions.Logging.Abstractions;
using SnapCheck.Data;

namespace SnapCheck.Tests;

/// <summary>
/// Testes de integração rodam contra o Postgres real do docker-compose local
/// (decisão do item 10.1 — sem Testcontainers para não adicionar infra nova).
/// Suba o banco antes de rodar: docker compose up -d postgres.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string ConnectionStringEnvVar = "SNAPCHECK_TEST_CONNECTION_STRING";
    // Porta 5439, não 5432 — evita conflito com outras instâncias de Postgres
    // já rodando na máquina de desenvolvimento (ver comentário em docker-compose.yml).
    private const string ConnectionStringPadrao =
        "Host=localhost;Port=5439;Database=snapcheck;Username=snapcheck;Password=snapcheck";

    public IDbConnectionFactory ConnectionFactory { get; } = new DbConnectionFactory();

    public async Task InitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvVar) ?? ConnectionStringPadrao;
        ConnectionFactory.SetConnectionString(connectionString);

        var initializer = new DatabaseInitializer(ConnectionFactory, NullLogger<DatabaseInitializer>.Instance);
        await initializer.InitializeAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
