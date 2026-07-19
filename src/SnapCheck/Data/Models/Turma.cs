namespace SnapCheck.Data.Models;

public sealed class Turma
{
    public int Id { get; init; }
    public int TenantId { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string CodigoVinculacao { get; init; } = string.Empty;
    public bool Ativa { get; init; }
    public DateTime CriadoEm { get; init; }
}

public sealed class TurmaJanela
{
    public int Id { get; init; }
    public int TurmaId { get; init; }
    public short[] DiasSemana { get; init; } = [];
    public TimeSpan HoraInicio { get; init; }
    public TimeSpan HoraFim { get; init; }
    public int ToleranciaAtrasoMinutos { get; init; }
    public short CortePresencaParcialPercentual { get; init; }
    public bool Ativa { get; init; }
}
