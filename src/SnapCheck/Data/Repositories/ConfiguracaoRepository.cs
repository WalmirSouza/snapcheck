using Dapper;
using SnapCheck.Data.Models;

namespace SnapCheck.Data.Repositories;

public interface IConfiguracaoRepository
{
    Task<string?> ObterValorAsync(string chave, CancellationToken cancellationToken = default);
    Task SalvarAsync(string chave, string valor, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, string>> ObterTodasAsync(CancellationToken cancellationToken = default);
}

public sealed class ConfiguracaoRepository(IDbConnectionFactory connectionFactory) : IConfiguracaoRepository
{
    public async Task<string?> ObterValorAsync(string chave, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT valor FROM configuracoes WHERE chave = @chave LIMIT 1";

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<string?>(
            new CommandDefinition(sql, new { chave }, cancellationToken: cancellationToken));
    }

    public async Task SalvarAsync(string chave, string valor, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO configuracoes (chave, valor)
            VALUES (@chave, @valor)
            ON CONFLICT (chave) DO UPDATE SET valor = EXCLUDED.valor
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { chave, valor }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyDictionary<string, string>> ObterTodasAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT chave, valor FROM configuracoes";

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<Configuracao>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.ToDictionary(x => x.Chave, x => x.Valor);
    }
}
