namespace SnapCheck.Data.Models;

public sealed class Pessoa
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public byte[] Embedding { get; set; } = [];
    public DateTime DataCadastro { get; set; }
    public bool Ativo { get; set; } = true;
}
