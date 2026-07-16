namespace SnapCheck.Data.Models;

public sealed class Presenca
{
    public int Id { get; set; }
    public int PessoaId { get; set; }
    public DateTime DataHora { get; set; }
    public string? Turma { get; set; }
    public string? NomePessoa { get; set; }
}
