namespace SnapCheck.Face.Models;

public sealed class FaceResult
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public float[] Embedding { get; init; } = [];
}

public sealed class FaceMatch
{
    public FaceResult Face { get; init; } = new();
    public int? PessoaId { get; init; }
    public string? Nome { get; init; }
    public float Similaridade { get; init; }
    public bool Reconhecido => PessoaId.HasValue;
}
