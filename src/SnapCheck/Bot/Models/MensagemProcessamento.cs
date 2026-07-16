namespace SnapCheck.Bot.Models;

public enum TipoMensagem
{
    FotoPresenca
}

public sealed class MensagemProcessamento
{
    public TipoMensagem Tipo { get; init; }
    public long ChatId { get; init; }
    public int MessageId { get; init; }
    public string FileId { get; init; } = string.Empty;
    public string? Turma { get; init; }
}

public sealed class PipelineContext
{
    public MensagemProcessamento Mensagem { get; init; } = null!;
    public byte[]? ImagemBytes { get; set; }
    public IReadOnlyList<Face.Models.FaceMatch> Matches { get; set; } = [];
    public byte[]? ImagemAnotada { get; set; }
    public List<string> Erros { get; } = [];
}
