namespace SnapCheck.Data.Models;

public sealed class UsuarioAcesso
{
    public int Id { get; set; }
    public int? TenantId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public string PapeisCsv { get; set; } = string.Empty;
}