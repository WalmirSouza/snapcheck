using Dapper;
using SnapCheck.Data.Models;

namespace SnapCheck.Data.Repositories;

public enum TurmaVinculacaoResultado
{
    Sucesso,
    CodigoInvalido,
    ChatJaVinculado
}

public enum CriarTurmaResultado
{
    Sucesso,
    CodigoJaExisteNoTenant
}

public interface ITurmaRepository
{
    Task<(CriarTurmaResultado Resultado, int? TurmaId)> CriarAsync(
        int tenantId, string nome, string codigoVinculacao, CancellationToken cancellationToken = default);

    Task AdicionarJanelaAsync(
        int turmaId,
        short[] diasSemana,
        TimeSpan horaInicio,
        TimeSpan horaFim,
        int toleranciaAtrasoMinutos,
        short cortePresencaParcialPercentual,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Turma>> ListarPorTenantAsync(int tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TurmaJanela>> ObterJanelasAtivasAsync(int turmaId, CancellationToken cancellationToken = default);

    Task<int?> ObterTurmaIdPorChatAsync(long chatId, CancellationToken cancellationToken = default);

    Task<TurmaVinculacaoResultado> VincularChatAsync(
        int tenantId, long chatId, string codigoVinculacao, CancellationToken cancellationToken = default);
}

public sealed class TurmaRepository(IDbConnectionFactory connectionFactory) : ITurmaRepository
{
    public async Task<(CriarTurmaResultado Resultado, int? TurmaId)> CriarAsync(
        int tenantId, string nome, string codigoVinculacao, CancellationToken cancellationToken = default)
    {
        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existente = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(
                "SELECT id FROM turmas WHERE tenant_id = @tenantId AND codigo_vinculacao = @codigoVinculacao LIMIT 1",
                new { tenantId, codigoVinculacao },
                cancellationToken: cancellationToken));

        if (existente is not null)
        {
            return (CriarTurmaResultado.CodigoJaExisteNoTenant, null);
        }

        const string sql = """
            INSERT INTO turmas (tenant_id, nome, codigo_vinculacao, ativa)
            VALUES (@tenantId, @nome, @codigoVinculacao, TRUE)
            RETURNING id
            """;

        var turmaId = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { tenantId, nome, codigoVinculacao }, cancellationToken: cancellationToken));

        return (CriarTurmaResultado.Sucesso, turmaId);
    }

    public async Task AdicionarJanelaAsync(
        int turmaId,
        short[] diasSemana,
        TimeSpan horaInicio,
        TimeSpan horaFim,
        int toleranciaAtrasoMinutos,
        short cortePresencaParcialPercentual,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO turma_janelas
                (turma_id, dias_semana, hora_inicio, hora_fim, tolerancia_atraso_minutos, corte_presenca_parcial_percentual, ativa)
            VALUES
                (@turmaId, @diasSemana, @horaInicio, @horaFim, @toleranciaAtrasoMinutos, @cortePresencaParcialPercentual, TRUE)
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    turmaId,
                    diasSemana,
                    horaInicio,
                    horaFim,
                    toleranciaAtrasoMinutos,
                    cortePresencaParcialPercentual
                },
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Turma>> ListarPorTenantAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, tenant_id AS TenantId, nome, codigo_vinculacao AS CodigoVinculacao, ativa, criado_em AS CriadoEm
            FROM turmas
            WHERE tenant_id = @tenantId
            ORDER BY nome
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var result = await connection.QueryAsync<Turma>(
            new CommandDefinition(sql, new { tenantId }, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<IReadOnlyList<TurmaJanela>> ObterJanelasAtivasAsync(
        int turmaId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id,
                   turma_id AS TurmaId,
                   dias_semana AS DiasSemana,
                   hora_inicio AS HoraInicio,
                   hora_fim AS HoraFim,
                   tolerancia_atraso_minutos AS ToleranciaAtrasoMinutos,
                   corte_presenca_parcial_percentual AS CortePresencaParcialPercentual,
                   ativa
            FROM turma_janelas
            WHERE turma_id = @turmaId AND ativa = TRUE
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var result = await connection.QueryAsync<TurmaJanela>(
            new CommandDefinition(sql, new { turmaId }, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<int?> ObterTurmaIdPorChatAsync(long chatId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT turma_id FROM turma_chat_telegram WHERE chat_id = @chatId LIMIT 1";

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(sql, new { chatId }, cancellationToken: cancellationToken));
    }

    public async Task<TurmaVinculacaoResultado> VincularChatAsync(
        int tenantId, long chatId, string codigoVinculacao, CancellationToken cancellationToken = default)
    {
        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);

        var jaVinculado = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(
                "SELECT turma_id FROM turma_chat_telegram WHERE chat_id = @chatId LIMIT 1",
                new { chatId },
                cancellationToken: cancellationToken));

        if (jaVinculado is not null)
        {
            return TurmaVinculacaoResultado.ChatJaVinculado;
        }

        var turmaId = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(
                "SELECT id FROM turmas WHERE tenant_id = @tenantId AND codigo_vinculacao = @codigoVinculacao AND ativa = TRUE LIMIT 1",
                new { tenantId, codigoVinculacao },
                cancellationToken: cancellationToken));

        if (turmaId is null)
        {
            return TurmaVinculacaoResultado.CodigoInvalido;
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                "INSERT INTO turma_chat_telegram (turma_id, chat_id) VALUES (@turmaId, @chatId)",
                new { turmaId, chatId },
                cancellationToken: cancellationToken));

        return TurmaVinculacaoResultado.Sucesso;
    }
}
