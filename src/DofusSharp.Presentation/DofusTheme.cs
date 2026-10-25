using System.Numerics;
using Raylib_cs;

namespace DofusSharp.Presentation;

/// <summary>Tipografía de lectura y paleta inspiradas en un RPG de fantasía clásica.</summary>
internal static class DofusTheme
{
    private static Font _font;
    private static bool _ownsFont;

    public static void Initialize()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "DofusRetro.ttf"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "georgiab.ttf"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "georgia.ttf"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "BKNTA.ttf")
        };

        var fontPath = candidates.FirstOrDefault(File.Exists);
        if (fontPath is null)
        {
            _font = Raylib.GetFontDefault();
            return;
        }

        _font = Raylib.LoadFontEx(fontPath, 48, null, 0);
        _ownsFont = true;
        Raylib.SetTextureFilter(_font.Texture, TextureFilter.Bilinear);
    }

    public static void DrawText(string text, int x, int y, int size, Color color)
    {
        if (string.IsNullOrEmpty(text)) return;
        Raylib.DrawTextEx(_font, text, new Vector2(x, y), size, Math.Max(0.5f, size * 0.025f), color);
    }

    public static void Unload()
    {
        if (_ownsFont) Raylib.UnloadFont(_font);
        _ownsFont = false;
    }
}
