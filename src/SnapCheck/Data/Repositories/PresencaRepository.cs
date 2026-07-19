using Dapper;
using SnapCheck.Data.Models;

namespace SnapCheck.Data.Repositories;

public interface IPresencaRepository
{
    Task<RegistroPresencaResultado> RegistrarAsync(
        int tenantId,
        int pessoaId,
        string? turma,
        string statusPresenca = "completa",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Presença manual (item 02.5) — exige responsável e motivo, sempre marcada
    /// com status "manual". Trilha de auditoria hoje é o log operacional
    /// (IActivityLog, em memória); auditoria imutável de verdade é o item 04.5.
    /// </summary>
    Task<RegistroPresencaResultado> RegistrarManualAsync(
        int tenantId,
        int pessoaId,
        string? turma,
        string responsavel,
        string motivo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Presenca>> ListarPorPessoaAsync(int tenantId, string nome, CancellationToken cancellationToken = default);

    /// <summary>
    /// Contagem agregada entre todos os tenants — métrica operacional do painel
    /// (BotController), não expõe dado de negócio de nenhum tenant específico.
    /// Ver item 01.8 para quando o painel ganhar contexto de tenant.
    /// </summary>
    Task<int> ContarHojeAsync(CancellationToken cancellationToken = default);
}

public enum RegistroPresencaResultado
{
    Registrada = 1,
    Duplicada = 2
}

public sealed class PresencaRepository(IDbConnectionFactory connectionFactory) : IPresencaRepository
{
    public async Task<RegistroPresencaResultado> RegistrarAsync(
        int tenantId, int pessoaId, string? turma, string statusPresenca = "completa", CancellationToken cancellationToken = default)
    {
        var turmaNormalizada = (turma ?? string.Empty).Trim().ToLowerInvariant();

        const string sql = """
            INSERT INTO presencas (tenant_id, pessoa_id, data_hora, turma, data_dia, turma_normalizada, status_presenca)
            VALUES (@tenantId, @pessoaId, NOW(), @turma, CURRENT_DATE, @turmaNormalizada, @statusPresenca)
            ON CONFLICT (tenant_id, pessoa_id, turma_normalizada, data_dia) DO NOTHING
            RETURNING id
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var insertedId = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                sql,
                new
                {
                    tenantId,
                    pessoaId,
                    turma,
                    turmaNormalizada,
                    statusPresenca
                },
                cancellationToken: cancellationToken));

        return insertedId.HasValue
            ? RegistroPresencaResultado.Registrada
            : RegistroPresencaResultado.Duplicada;
    }

    public async Task<RegistroPresencaResultado> RegistrarManualAsync(
        int tenantId,
        int pessoaId,
        string? turma,
        string responsavel,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        var turmaNormalizada = (turma ?? string.Empty).Trim().ToLowerInvariant();

        const string sql = """
            INSERT INTO presencas
                (tenant_id, pessoa_id, data_hora, turma, data_dia, turma_normalizada, status_presenca, responsavel_manual, motivo_manual)
            VALUES
                (@tenantId, @pessoaId, NOW(), @turma, CURRENT_DATE, @turmaNormalizada, 'manual', @responsavel, @motivo)
            ON CONFLICT (tenant_id, pessoa_id, turma_normalizada, data_dia) DO NOTHING
            RETURNING id
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var insertedId = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                sql,
                new
                {
                    tenantId,
                    pessoaId,
                    turma,
                    turmaNormalizada,
                    responsavel,
                    motivo
                },
                cancellationToken: cancellationToken));

        return insertedId.HasValue
            ? RegistroPresencaResultado.Registrada
            : RegistroPresencaResultado.Duplicada;
    }

    public async Task<IReadOnlyList<Presenca>> ListarPorPessoaAsync(
        int tenantId, string nome, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT pr.id,
                   pr.pessoa_id AS PessoaId,
                   pr.data_hora AS DataHora,
                   pr.turma AS Turma,
                   p.nome AS NomePessoa
            FROM presencas pr
            INNER JOIN pessoas p ON p.id = pr.pessoa_id
            WHERE pr.tenant_id = @tenantId
              AND p.tenant_id = @tenantId
              AND p.ativo = TRUE
              AND LOWER(p.nome) = LOWER(@nome)
            ORDER BY pr.data_hora DESC
            LIMIT 30
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var result = await connection.QueryAsync<Presenca>(
            new CommandDefinition(sql, new { tenantId, nome }, cancellationToken: cancellationToken));
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
