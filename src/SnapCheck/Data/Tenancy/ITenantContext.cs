namespace SnapCheck.Data.Tenancy;

/// <summary>
/// Contexto ambiente do tenant resolvido para o update do Telegram em processamento.
/// Baseado em AsyncLocal: só é seguro dentro da mesma cadeia de await (não atravessa
/// a fila em memória entre FotoHandler e PipelineService — para esse trecho o tenant
/// precisa ser copiado explicitamente para MensagemProcessamento.TenantId).
/// </summary>
public interface ITenantContext
{
    int? TenantId { get; }

    IDisposable BeginScope(int tenantId);
}

public sealed class TenantContext : ITenantContext
{
    private static readonly AsyncLocal<int?> Current = new();

    public int? TenantId => Current.Value;

    public IDisposable BeginScope(int tenantId)
    {
        var anterior = Current.Value;
        Current.Value = tenantId;
        return new Escopo(anterior);
    }

    private sealed class Escopo(int? anterior) : IDisposable
    {
        public void Dispose() => Current.Value = anterior;
    }
}
