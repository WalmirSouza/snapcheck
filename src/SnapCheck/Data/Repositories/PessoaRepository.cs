using Dapper;
using SnapCheck.Data.Models;

namespace SnapCheck.Data.Repositories;

public interface IPessoaRepository
{
    Task<IReadOnlyList<Pessoa>> ListarAtivasAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<Pessoa?> ObterPorNomeAsync(int tenantId, string nome, CancellationToken cancellationToken = default);
    Task<int> InserirAsync(int tenantId, string nome, byte[] embedding, CancellationToken cancellationToken = default);
    Task<bool> RemoverPorNomeAsync(int tenantId, string nome, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Pessoa>> ListarSumidosAsync(int tenantId, int dias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Contagem agregada entre todos os tenants — métrica operacional do painel
    /// (BotController), não expõe dado de negócio de nenhum tenant específico.
    /// Ver item 01.8 para quando o painel ganhar contexto de tenant.
    /// </summary>
    Task<int> ContarAtivasAsync(CancellationToken cancellationToken = default);
}

public sealed class PessoaRepository(IDbConnectionFactory connectionFactory) : IPessoaRepository
{
    public async Task<IReadOnlyList<Pessoa>> ListarAtivasAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, nome, embedding, data_cadastro AS DataCadastro, ativo AS Ativo
            FROM pessoas
            WHERE tenant_id = @tenantId AND ativo = TRUE
            ORDER BY nome
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var result = await connection.QueryAsync<Pessoa>(
            new CommandDefinition(sql, new { tenantId }, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<Pessoa?> ObterPorNomeAsync(int tenantId, string nome, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, nome, embedding, data_cadastro AS DataCadastro, ativo AS Ativo
            FROM pessoas
            WHERE tenant_id = @tenantId AND ativo = TRUE AND LOWER(nome) = LOWER(@nome)
            LIMIT 1
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<Pessoa>(
            new CommandDefinition(sql, new { tenantId, nome }, cancellationToken: cancellationToken));
    }

    public async Task<int> InserirAsync(
        int tenantId, string nome, byte[] embedding, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO pessoas (tenant_id, nome, embedding, data_cadastro, ativo)
            VALUES (@tenantId, @nome, @embedding, NOW(), TRUE)
            RETURNING id
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { tenantId, nome, embedding }, cancellationToken: cancellationToken));
    }

    public async Task<bool> RemoverPorNomeAsync(int tenantId, string nome, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE pessoas
            SET ativo = FALSE
            WHERE tenant_id = @tenantId AND ativo = TRUE AND LOWER(nome) = LOWER(@nome)
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { tenantId, nome }, cancellationToken: cancellationToken));
        return rows > 0;
    }

    public async Task<int> ContarAtivasAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(*) FROM pessoas WHERE ativo = TRUE";

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Pessoa>> ListarSumidosAsync(
        int tenantId, int dias, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT p.id, p.nome, p.embedding, p.data_cadastro AS DataCadastro, p.ativo AS Ativo
            FROM pessoas p
            WHERE p.tenant_id = @tenantId
              AND p.ativo = TRUE
              AND NOT EXISTS (
                  SELECT 1
                  FROM presencas pr
                  WHERE pr.pessoa_id = p.id
                    AND pr.tenant_id = @tenantId
                    AND pr.data_hora >= NOW() - (@dias || ' days')::INTERVAL
              )
            ORDER BY p.nome
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var result = await connection.QueryAsync<Pessoa>(
            new CommandDefinition(sql, new { tenantId, dias }, cancellationToken: cancellationToken));
        return result.AsList();
    }
}
