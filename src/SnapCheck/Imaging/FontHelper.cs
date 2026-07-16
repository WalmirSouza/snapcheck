using SixLabors.Fonts;

namespace SnapCheck.Imaging;

internal static class FontHelper
{
    private static Font? _fontRotulo;

    public static Font ObterFonteRotulo()
    {
        if (_fontRotulo is not null)
        {
            return _fontRotulo;
        }

        string[] preferidas =
        [
            "DejaVu Sans",
            "Liberation Sans",
            "FreeSans",
            "Arial",
            "Sans"
        ];

        foreach (var nome in preferidas)
        {
            if (SystemFonts.Collection.TryGet(nome, out FontFamily familia))
            {
                _fontRotulo = familia.CreateFont(18, FontStyle.Bold);
                return _fontRotulo;
            }
        }

        if (SystemFonts.Collection.Families.FirstOrDefault() is { } fallback)
        {
            _fontRotulo = fallback.CreateFont(18, FontStyle.Bold);
            return _fontRotulo;
        }

        throw new InvalidOperationException("Nenhuma fonte disponível no sistema.");
    }
}
