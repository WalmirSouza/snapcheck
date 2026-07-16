using FaceONNX;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SnapCheck.Face.Models;

namespace SnapCheck.Face;

public interface IFaceService
{
    IReadOnlyList<FaceResult> DetectarRostos(byte[] imageBytes);
    float[] ExtrairEmbedding(byte[] imageBytes);
    IReadOnlyList<FaceMatch> CompararComCadastro(
        byte[] imageBytes,
        IReadOnlyList<(int Id, string Nome, float[] Embedding)> cadastro,
        float limiarSimilaridade = 0.42f);
}

public sealed class FaceService : IFaceService, IDisposable
{
    private readonly FaceDetector _faceDetector = new();
    private readonly Face68LandmarksExtractor _landmarksExtractor = new();
    private readonly FaceEmbedder _faceEmbedder = new();

    public IReadOnlyList<FaceResult> DetectarRostos(byte[] imageBytes)
    {
        using var image = Image.Load<Rgb24>(imageBytes);
        var array = ToFloatArray(image);
        var detections = _faceDetector.Forward(array);

        return detections
            .Where(d => !d.Box.IsEmpty)
            .Select(d => new FaceResult
            {
                X = d.Box.X,
                Y = d.Box.Y,
                Width = d.Box.Width,
                Height = d.Box.Height,
                Embedding = ExtrairEmbedding(array, d.Box)
            })
            .ToList();
    }

    public float[] ExtrairEmbedding(byte[] imageBytes)
    {
        using var image = Image.Load<Rgb24>(imageBytes);
        var array = ToFloatArray(image);
        var detection = _faceDetector.Forward(array).FirstOrDefault();
        if (detection is null || detection.Box.IsEmpty)
        {
            return [];
        }

        return ExtrairEmbedding(array, detection.Box);
    }

    public IReadOnlyList<FaceMatch> CompararComCadastro(
        byte[] imageBytes,
        IReadOnlyList<(int Id, string Nome, float[] Embedding)> cadastro,
        float limiarSimilaridade = 0.42f)
    {
        var rostos = DetectarRostos(imageBytes);
        var matches = new List<FaceMatch>();

        foreach (var rosto in rostos)
        {
            if (rosto.Embedding.Length == 0)
            {
                matches.Add(new FaceMatch { Face = rosto, Similaridade = 0 });
                continue;
            }

            var cadastrado = cadastro
                .Select(p => (p, score: CosineSimilarity(rosto.Embedding, p.Embedding)))
                .OrderByDescending(x => x.score)
                .FirstOrDefault();

            if (cadastrado.p.Id > 0 && cadastrado.score >= limiarSimilaridade)
            {
                matches.Add(new FaceMatch
                {
                    Face = rosto,
                    PessoaId = cadastrado.p.Id,
                    Nome = cadastrado.p.Nome,
                    Similaridade = cadastrado.score
                });
            }
            else
            {
                matches.Add(new FaceMatch
                {
                    Face = rosto,
                    Similaridade = cadastrado.score
                });
            }
        }

        return matches;
    }

    private float[] ExtrairEmbedding(float[][,] array, System.Drawing.Rectangle rectangle)
    {
        var points = _landmarksExtractor.Forward(array, rectangle);
        var angle = points.RotationAngle;
        var aligned = FaceProcessingExtensions.Align(array, rectangle, angle);
        return _faceEmbedder.Forward(aligned);
    }

    private static float[][,] ToFloatArray(Image<Rgb24> image)
    {
        var array = new[]
        {
            new float[image.Height, image.Width],
            new float[image.Height, image.Width],
            new float[image.Height, image.Width]
        };

        image.ProcessPixelRows(pixelAccessor =>
        {
            for (var y = 0; y < pixelAccessor.Height; y++)
            {
                var row = pixelAccessor.GetRowSpan(y);
                for (var x = 0; x < pixelAccessor.Width; x++)
                {
                    array[2][y, x] = row[x].R / 255.0F;
                    array[1][y, x] = row[x].G / 255.0F;
                    array[0][y, x] = row[x].B / 255.0F;
                }
            }
        });

        return array;
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length)
        {
            return 0;
        }

        float dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0)
        {
            return 0;
        }

        return dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }

    public void Dispose()
    {
        _faceDetector.Dispose();
        _landmarksExtractor.Dispose();
        _faceEmbedder.Dispose();
    }
}
