namespace SnapCheck.Data.Models;

public sealed class PapelAcesso
{
    public int Id { get; set; }
    public int? TenantId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Ativo { get; set; }
    public string PermissoesCsv { get; set; } = string.Empty;
}