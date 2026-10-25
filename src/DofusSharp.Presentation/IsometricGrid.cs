using System.Numerics;
using DofusSharp.Domain;
using Raylib_cs;

namespace DofusSharp.Presentation;

/// <summary>Dibuja el tablero lógico como rombos y convierte el puntero a celdas de la cuadrícula.</summary>
internal sealed class IsometricGrid : IDisposable
{
    private readonly float _originX;
    private readonly float _originY;
    private readonly float _tileWidth;
    private readonly float _tileHeight;
    private readonly Texture2D _yopukaSprite;
    private readonly Texture2D _ocraSprite;

    public IsometricGrid(float originX, float originY, float tileWidth, float tileHeight)
    {
        _originX = originX;
        _originY = originY;
        _tileWidth = tileWidth;
        _tileHeight = tileHeight;
        var assets = Path.Combine(AppContext.BaseDirectory, "Assets");
        _yopukaSprite = Raylib.LoadTexture(Path.Combine(assets, "yopuka.png"));
        _ocraSprite = Raylib.LoadTexture(Path.Combine(assets, "ocra.png"));
    }

    /// <summary>Proyecta índices lógicos a centros de rombos en pantalla.</summary>
    public Vector2 GridToScreen(int x, int y) => new(
        _originX + (x - y) * _tileWidth / 2f,
        _originY + (x + y) * _tileHeight / 2f);

    /// <summary>Calcula la celda bajo el puntero y valida que esté dentro de su rombo y del tablero.</summary>
    public bool TryGetCellAt(GameGrid grid, Vector2 pointer, out int cellX, out int cellY)
    {
        var relativeX = (pointer.X - _originX) / (_tileWidth / 2f);
        var relativeY = (pointer.Y - _originY) / (_tileHeight / 2f);
        cellX = (int)MathF.Round((relativeX + relativeY) / 2f, MidpointRounding.AwayFromZero);
        cellY = (int)MathF.Round((relativeY - relativeX) / 2f, MidpointRounding.AwayFromZero);

        if (!grid.Contains(cellX, cellY)) return false;
        var center = GridToScreen(cellX, cellY);
        var diamondDistance = MathF.Abs(pointer.X - center.X) / (_tileWidth / 2f)
                            + MathF.Abs(pointer.Y - center.Y) / (_tileHeight / 2f);
        return diamondDistance <= 1f;
    }

    public void Draw(GameGrid grid, int highlightedX, int highlightedY,
        IReadOnlySet<(int X, int Y)>? movementPath = null,
        IReadOnlySet<(int X, int Y)>? spellTargets = null)
    {
        // Dibujar de atrás hacia delante por suma de coordenadas evita que las filas se solapen mal.
        for (var diagonal = 0; diagonal < grid.Width + grid.Height - 1; diagonal++)
        {
            var minX = Math.Max(0, diagonal - (grid.Height - 1));
            var maxX = Math.Min(grid.Width - 1, diagonal);
            for (var x = minX; x <= maxX; x++)
            {
                var y = diagonal - x;
                var cell = grid.GetCell(x, y);
                var center = GridToScreen(x, y);
                var fill = GetCellColor(cell, x, y);
                if (movementPath?.Contains((x, y)) == true)
                    fill = new Color(54, 145, 164, 255);
                if (spellTargets?.Contains((x, y)) == true)
                    fill = new Color(201, 102, 227, 255);
                if (x == highlightedX && y == highlightedY)
                    fill = new Color(236, 190, 73, 255);

                DrawDiamond(center, fill, new Color(24, 35, 33, 255));
            }
        }
    }

    public void DrawFighter(Vector2 center, Fighter fighter, bool isActive)
    {
        var sprite = fighter.Class == PlayerClass.Ocra ? _ocraSprite : _yopukaSprite;
        var spriteScale = 0.24f;
        var spriteWidth = sprite.Width * spriteScale;
        var spriteHeight = sprite.Height * spriteScale;
        Raylib.DrawEllipse((int)center.X, (int)center.Y + 7, 20, 8, new Color(62, 66, 39, 115));
        if (isActive)
        {
            Raylib.DrawEllipseLines((int)center.X, (int)center.Y + 7, 23, 9, new Color(199, 148, 54, 255));
            Raylib.DrawCircle((int)center.X, (int)(center.Y - spriteHeight - 8), 4,
                new Color(199, 148, 54, 255));
        }
        Raylib.DrawTextureEx(sprite,
            new Vector2(center.X - spriteWidth / 2f, center.Y - spriteHeight + 5), 0, spriteScale,
            fighter.IsAlive ? Color.White : new Color(140, 135, 125, 180));
        Raylib.DrawRectangle((int)center.X - 34, (int)(center.Y - spriteHeight - 32), 68, 18,
            new Color(236, 223, 182, 235));
        Raylib.DrawRectangleLines((int)center.X - 34, (int)(center.Y - spriteHeight - 32), 68, 18,
            new Color(150, 113, 57, 255));
        Raylib.DrawText(fighter.Name, (int)center.X - 29, (int)(center.Y - spriteHeight - 29), 11,
            fighter.IsAlive ? new Color(83, 53, 34, 255) : new Color(124, 117, 101, 255));
        Raylib.DrawRectangle((int)center.X - 19, (int)center.Y + 16, 38, 5, new Color(115, 72, 54, 255));
        var healthRatio = fighter.MaxHP == 0 ? 0f : (float)fighter.HP / fighter.MaxHP;
        Raylib.DrawRectangle((int)center.X - 19, (int)center.Y + 17, (int)(38 * healthRatio), 5,
            new Color(85, 190, 100, 255));
    }

    public void Dispose()
    {
        Raylib.UnloadTexture(_yopukaSprite);
        Raylib.UnloadTexture(_ocraSprite);
    }

    private void DrawDiamond(Vector2 center, Color fill, Color outline)
    {
        var halfWidth = _tileWidth / 2f;
        var halfHeight = _tileHeight / 2f;
        var top = new Vector2(center.X, center.Y - halfHeight);
        var right = new Vector2(center.X + halfWidth, center.Y);
        var bottom = new Vector2(center.X, center.Y + halfHeight);
        var left = new Vector2(center.X - halfWidth, center.Y);

        Raylib.DrawTriangle(top, left, right, fill);
        Raylib.DrawTriangle(bottom, right, left, fill);
        Raylib.DrawLineV(top, right, outline);
        Raylib.DrawLineV(right, bottom, outline);
        Raylib.DrawLineV(bottom, left, outline);
        Raylib.DrawLineV(left, top, outline);
    }

    private static Color GetCellColor(GridCell cell, int x, int y)
    {
        if (!cell.IsWalkable && !cell.LineOfSight) return new Color(126, 88, 67, 255);
        if (!cell.IsWalkable) return new Color(132, 117, 84, 255);
        if (!cell.LineOfSight) return new Color(126, 139, 104, 255);
        return (x + y) % 2 == 0
            ? new Color(197, 174, 119, 255)
            : new Color(210, 187, 131, 255);
    }
}
