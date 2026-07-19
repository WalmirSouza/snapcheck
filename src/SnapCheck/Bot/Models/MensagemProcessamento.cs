namespace SnapCheck.Bot.Models;

public enum TipoMensagem
{
    FotoPresenca
}

public sealed class MensagemProcessamento
{
    public TipoMensagem Tipo { get; init; }
    public required int TenantId { get; init; }
    public long ChatId { get; init; }
    public int MessageId { get; init; }
    public string FileId { get; init; } = string.Empty;
    public string? Turma { get; init; }
    public bool NotificarDuplicidadeAoSolicitante { get; init; } = true;
}

public enum StatusJanelaPresenca
{
    SemTurmaVinculada,
    ForaDaJanela,
    Completa,
    Atrasado,
    Parcial
}

public sealed class PipelineContext
{
    public MensagemProcessamento Mensagem { get; init; } = null!;
    public byte[]? ImagemBytes { get; set; }
    public IReadOnlyList<Face.Models.FaceMatch> Matches { get; set; } = [];
    public byte[]? ImagemAnotada { get; set; }
    public int? RevisaoPresencaId { get; set; }
    public bool RegistroPorRevisao { get; set; }
    public StatusJanelaPresenca? StatusJanela { get; set; }
    public string? MensagemJanela { get; set; }
    public List<string> PresencasRegistradas { get; } = [];
    public List<string> PresencasJaRegistradas { get; } = [];
    public List<string> Erros { get; } = [];
}
