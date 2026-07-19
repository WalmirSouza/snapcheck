namespace SnapCheck.Data.Models;

public class RevisaoPresencaGrupo
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string? Turma { get; set; }
    public long? ChatId { get; set; }
    public string Status { get; set; } = RevisaoStatus.Pendente;
    public DateTime ExpiraEm { get; set; }
    public string CriadoPor { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
    public string? ConfirmadoPor { get; set; }
    public DateTime? ConfirmadoEm { get; set; }
}

public sealed class RevisaoFaceItem
{
    public int Id { get; set; }
    public int RevisaoId { get; set; }
    public int TenantId { get; set; }
    public int? PessoaSugeridaId { get; set; }
    public string? NomeSugerido { get; set; }
    public float? Confianca { get; set; }
    public string Decisao { get; set; } = RevisaoDecisao.Pendente;
    public int? PessoaFinalId { get; set; }
    public string? DecididoPor { get; set; }
    public DateTime? DecididoEm { get; set; }
    public string? Motivo { get; set; }
}

public static class RevisaoStatus
{
    public const string Pendente = "pendente";
    public const string Confirmada = "confirmada";
    public const string Cancelada = "cancelada";
    public const string Expirada = "expirada";
}

public static class RevisaoDecisao
{
    public const string Pendente = "pendente";
    public const string Aprovada = "aprovada";
    public const string Rejeitada = "rejeitada";
    public const string Reatribuida = "reatribuida";
}
