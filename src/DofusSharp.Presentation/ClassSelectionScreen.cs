using System.Numerics;
using DofusSharp.Domain;
using Raylib_cs;

namespace DofusSharp.Presentation;

/// <summary>Selector inicial de clase con retratos y una presentación cálida de fantasía.</summary>
internal static class ClassSelectionScreen
{
    private static readonly Color Parchment = new(232, 215, 165, 255);
    private static readonly Color Paper = new(246, 232, 190, 255);
    private static readonly Color Ink = new(75, 48, 31, 255);
    private static readonly Color Gold = new(177, 113, 35, 255);

    public static PlayerClass Choose()
    {
        var assets = Path.Combine(AppContext.BaseDirectory, "Assets");
        var yopuka = Raylib.LoadTexture(Path.Combine(assets, "yopuka.png"));
        var ocra = Raylib.LoadTexture(Path.Combine(assets, "ocra.png"));
        var selected = 0;

        try
        {
            while (!Raylib.WindowShouldClose())
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Left) || Raylib.IsKeyPressed(KeyboardKey.A)) selected = 0;
                if (Raylib.IsKeyPressed(KeyboardKey.Right) || Raylib.IsKeyPressed(KeyboardKey.D)) selected = 1;
                if (Raylib.IsKeyPressed(KeyboardKey.Enter)) break;

                var mouse = Raylib.GetMousePosition();
                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    if (Contains(205, 176, 390, 492, mouse)) selected = 0;
                    else if (Contains(685, 176, 390, 492, mouse)) selected = 1;
                    else continue;
                    break;
                }

                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(126, 135, 83, 255));
                DrawBackdrop();
                DofusTheme.DrawText("Elige tu camino", 445, 43, 34, Ink);
                DofusTheme.DrawText("Dos escuelas. Dos formas de dominar el combate.", 396, 91, 17, Ink);
                DrawClassCard(205, 176, selected == 0, "Yopuka", "Guerrero de fuerza y combate cercano",
                    "TIERRA · FUERZA", "Presión · Intimidación", yopuka);
                DrawClassCard(685, 176, selected == 1, "Ocra", "Arquera de precisión y largo alcance",
                    "AIRE · AGILIDAD", "Flecha Mágica · Flecha de Retroceso", ocra);
                DofusTheme.DrawText("← / → para elegir   ·   Intro o clic para comenzar", 376, 700, 16, Ink);
                Raylib.EndDrawing();
            }

            return selected == 0 ? PlayerClass.Yopuka : PlayerClass.Ocra;
        }
        finally
        {
            Raylib.UnloadTexture(yopuka);
            Raylib.UnloadTexture(ocra);
        }
    }

    private static void DrawBackdrop()
    {
        // Marco inspirado en madera y pergamino, compuesto con primitivas para evitar dependencias gráficas extra.
        Raylib.DrawRectangle(34, 24, 1212, 744, new Color(89, 62, 39, 255));
        Raylib.DrawRectangle(42, 32, 1196, 728, Parchment);
        Raylib.DrawRectangleLines(49, 39, 1182, 714, Gold);
        for (var x = 60; x < 1230; x += 38)
            Raylib.DrawCircle(x, 50, 2, new Color(193, 157, 91, 255));
    }

    private static void DrawClassCard(int x, int y, bool selected, string title, string subtitle,
        string attributes, string spells, Texture2D portrait)
    {
        var fill = selected ? Paper : new Color(219, 202, 155, 255);
        Raylib.DrawRectangle(x + 5, y + 7, 390, 492, new Color(104, 78, 47, 75));
        Raylib.DrawRectangle(x, y, 390, 492, fill);
        Raylib.DrawRectangleLines(x, y, 390, 492, selected ? Gold : new Color(161, 136, 86, 255));
        Raylib.DrawRectangle(x + 10, y + 10, 370, 5, selected ? Gold : new Color(191, 173, 127, 255));
        DofusTheme.DrawText(title, x + 28, y + 24, 31, Ink);
        DofusTheme.DrawText(subtitle, x + 28, y + 61, 14, Ink);

        var scale = Math.Min(230f / portrait.Width, 290f / portrait.Height);
        var size = new Vector2(portrait.Width * scale, portrait.Height * scale);
        Raylib.DrawTextureEx(portrait, new Vector2(x + (390 - size.X) / 2, y + 82), 0, scale, Color.White);

        Raylib.DrawLine(x + 26, y + 383, x + 364, y + 383, new Color(185, 157, 104, 255));
        DofusTheme.DrawText(attributes, x + 28, y + 397, 15, new Color(125, 72, 36, 255));
        DofusTheme.DrawText(spells, x + 28, y + 425, 12, Ink);
        if (selected)
        {
            Raylib.DrawRectangle(x + 112, y + 455, 166, 27, new Color(121, 91, 51, 255));
            DofusTheme.DrawText("SELECCIONADO", x + 127, y + 462, 12, Paper);
        }
    }

    private static bool Contains(int x, int y, int width, int height, Vector2 point) =>
        point.X >= x && point.X <= x + width && point.Y >= y && point.Y <= y + height;
}
