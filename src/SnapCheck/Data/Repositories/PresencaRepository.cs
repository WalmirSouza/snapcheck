using Dapper;
using SnapCheck.Data.Models;

namespace SnapCheck.Data.Repositories;

public interface IPresencaRepository
{
    Task RegistrarAsync(int pessoaId, string? turma, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Presenca>> ListarPorPessoaAsync(string nome, CancellationToken cancellationToken = default);
    Task<int> ContarHojeAsync(CancellationToken cancellationToken = default);
}

public sealed class PresencaRepository(IDbConnectionFactory connectionFactory) : IPresencaRepository
{
    public async Task RegistrarAsync(int pessoaId, string? turma, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO presencas (pessoa_id, data_hora, turma)
            VALUES (@pessoaId, NOW(), @turma)
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { pessoaId, turma }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Presenca>> ListarPorPessoaAsync(string nome, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT pr.id,
                   pr.pessoa_id AS PessoaId,
                   pr.data_hora AS DataHora,
                   pr.turma AS Turma,
                   p.nome AS NomePessoa
            FROM presencas pr
            INNER JOIN pessoas p ON p.id = pr.pessoa_id
            WHERE p.ativo = TRUE AND LOWER(p.nome) = LOWER(@nome)
            ORDER BY pr.data_hora DESC
            LIMIT 30
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var result = await connection.QueryAsync<Presenca>(
            new CommandDefinition(sql, new { nome }, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<int> ContarHojeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM presencas
            WHERE data_hora::date = CURRENT_DATE
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }
}
