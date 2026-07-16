using Dapper;
using SnapCheck.Data.Models;

namespace SnapCheck.Data.Repositories;

public interface IPessoaRepository
{
    Task<IReadOnlyList<Pessoa>> ListarAtivasAsync(CancellationToken cancellationToken = default);
    Task<Pessoa?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default);
    Task<int> InserirAsync(string nome, byte[] embedding, CancellationToken cancellationToken = default);
    Task<bool> RemoverPorNomeAsync(string nome, CancellationToken cancellationToken = default);
    Task<int> ContarAtivasAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Pessoa>> ListarSumidosAsync(int dias, CancellationToken cancellationToken = default);
}

public sealed class PessoaRepository(IDbConnectionFactory connectionFactory) : IPessoaRepository
{
    public async Task<IReadOnlyList<Pessoa>> ListarAtivasAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, nome, embedding, data_cadastro AS DataCadastro, ativo AS Ativo
            FROM pessoas
            WHERE ativo = TRUE
            ORDER BY nome
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var result = await connection.QueryAsync<Pessoa>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<Pessoa?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, nome, embedding, data_cadastro AS DataCadastro, ativo AS Ativo
            FROM pessoas
            WHERE ativo = TRUE AND LOWER(nome) = LOWER(@nome)
            LIMIT 1
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<Pessoa>(
            new CommandDefinition(sql, new { nome }, cancellationToken: cancellationToken));
    }

    public async Task<int> InserirAsync(string nome, byte[] embedding, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO pessoas (nome, embedding, data_cadastro, ativo)
            VALUES (@nome, @embedding, NOW(), TRUE)
            RETURNING id
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { nome, embedding }, cancellationToken: cancellationToken));
    }

    public async Task<bool> RemoverPorNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE pessoas
            SET ativo = FALSE
            WHERE ativo = TRUE AND LOWER(nome) = LOWER(@nome)
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, new { nome }, cancellationToken: cancellationToken));
        return rows > 0;
    }

    public async Task<int> ContarAtivasAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(*) FROM pessoas WHERE ativo = TRUE";

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Pessoa>> ListarSumidosAsync(int dias, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT p.id, p.nome, p.embedding, p.data_cadastro AS DataCadastro, p.ativo AS Ativo
            FROM pessoas p
            WHERE p.ativo = TRUE
              AND NOT EXISTS (
                  SELECT 1
                  FROM presencas pr
                  WHERE pr.pessoa_id = p.id
                    AND pr.data_hora >= NOW() - (@dias || ' days')::INTERVAL
              )
            ORDER BY p.nome
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var result = await connection.QueryAsync<Pessoa>(
            new CommandDefinition(sql, new { dias }, cancellationToken: cancellationToken));
        return result.AsList();
    }
}
