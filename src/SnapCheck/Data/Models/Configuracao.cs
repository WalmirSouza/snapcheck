namespace SnapCheck.Data.Models;

public sealed class Configuracao
{
    public int Id { get; set; }
    public string Chave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
}
