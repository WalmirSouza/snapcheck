using Dapper;

namespace SnapCheck.Data.Repositories;

public enum TenantVinculacaoResultado
{
    Sucesso,
    CodigoInvalido,
    ChatJaVinculado
}

public interface ITenantRepository
{
    Task<int?> ObterTenantIdPorChatAsync(long chatId, CancellationToken cancellationToken = default);

    Task<TenantVinculacaoResultado> VincularChatAsync(
        long chatId, string codigoAtivacao, CancellationToken cancellationToken = default);

    /// <summary>
    /// Contagem de tenants ativos — indicador somente-leitura do painel de
    /// instância (item 01.8). Não expõe dado de nenhum tenant específico.
    /// </summary>
    Task<int> ContarAtivosAsync(CancellationToken cancellationToken = default);
}

public sealed class TenantRepository(IDbConnectionFactory connectionFactory) : ITenantRepository
{
    public async Task<int?> ObterTenantIdPorChatAsync(long chatId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT tenant_id FROM tenant_chat_telegram WHERE chat_id = @chatId LIMIT 1";

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(sql, new { chatId }, cancellationToken: cancellationToken));
    }

    public async Task<TenantVinculacaoResultado> VincularChatAsync(
        long chatId, string codigoAtivacao, CancellationToken cancellationToken = default)
    {
        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);

        var jaVinculado = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(
                "SELECT tenant_id FROM tenant_chat_telegram WHERE chat_id = @chatId LIMIT 1",
                new { chatId },
                cancellationToken: cancellationToken));

        if (jaVinculado is not null)
        {
            return TenantVinculacaoResultado.ChatJaVinculado;
        }

        var tenantId = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(
                "SELECT id FROM tenants WHERE codigo_ativacao = @codigoAtivacao AND status = 'ativo' LIMIT 1",
                new { codigoAtivacao },
                cancellationToken: cancellationToken));

        if (tenantId is null)
        {
            return TenantVinculacaoResultado.CodigoInvalido;
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                "INSERT INTO tenant_chat_telegram (tenant_id, chat_id) VALUES (@tenantId, @chatId)",
                new { tenantId, chatId },
                cancellationToken: cancellationToken));

        return TenantVinculacaoResultado.Sucesso;
    }

    public async Task<int> ContarAtivosAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(*) FROM tenants WHERE status = 'ativo'";

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }
}
