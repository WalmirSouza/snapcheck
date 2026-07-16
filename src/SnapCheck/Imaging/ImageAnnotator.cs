using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SnapCheck.Face.Models;
using Img = SixLabors.ImageSharp.Image;

namespace SnapCheck.Imaging;

public interface IImageAnnotator
{
    byte[] AnotarRostos(byte[] imageBytes, IReadOnlyList<FaceMatch> matches);
}

public sealed class ImageAnnotator : IImageAnnotator
{
    public byte[] AnotarRostos(byte[] imageBytes, IReadOnlyList<FaceMatch> matches)
    {
        using var image = Img.Load<Rgb24>(imageBytes);

        var font = FontHelper.ObterFonteRotulo();

        foreach (var match in matches)
        {
            var face = match.Face;
            var rect = new RectangleF(face.X, face.Y, face.Width, face.Height);
            var cor = match.Reconhecido ? Color.LimeGreen : Color.OrangeRed;
            var label = match.Reconhecido ? match.Nome! : "Desconhecido";

            image.Mutate(ctx =>
            {
                ctx.Draw(cor, 3, rect);
                var textY = Math.Max(0, face.Y - 24);
                var textRect = new RectangleF(face.X, textY, Math.Min(face.Width + 40, image.Width - face.X), 22);
                ctx.Fill(Color.FromRgba(0, 0, 0, 160), textRect);
                ctx.DrawText(label, font, Color.White, new PointF(face.X + 4, textY + 2));
            });
        }

        using var ms = new MemoryStream();
        image.Save(ms, new JpegEncoder());
        return ms.ToArray();
    }
}
