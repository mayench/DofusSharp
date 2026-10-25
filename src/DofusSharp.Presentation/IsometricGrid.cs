using System.Numerics;
using DofusSharp.Domain;
using Raylib_cs;

namespace DofusSharp.Presentation;

/// <summary>Dibuja el tablero lógico como rombos y convierte el puntero a celdas de la cuadrícula.</summary>
internal sealed class IsometricGrid
{
    private readonly float _originX;
    private readonly float _originY;
    private readonly float _tileWidth;
    private readonly float _tileHeight;

    public IsometricGrid(float originX, float originY, float tileWidth, float tileHeight)
    {
        _originX = originX;
        _originY = originY;
        _tileWidth = tileWidth;
        _tileHeight = tileHeight;
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
        var teamColor = fighter.TeamId == "blue"
            ? new Color(78, 144, 180, 255)
            : new Color(185, 83, 72, 255);
        var body = fighter.IsAlive ? teamColor : new Color(92, 94, 91, 255);
        Raylib.DrawEllipse((int)center.X, (int)center.Y + 8, 16, 7, new Color(20, 24, 23, 130));
        Raylib.DrawCircleV(center, isActive ? 17f : 15f, isActive ? new Color(238, 190, 72, 255) : new Color(40, 46, 44, 255));
        Raylib.DrawCircleV(center, isActive ? 12f : 11f, body);
        Raylib.DrawCircle((int)center.X - 3, (int)center.Y - 4, 3, new Color(255, 249, 222, 210));
        Raylib.DrawCircleLines((int)center.X, (int)center.Y, isActive ? 17 : 15,
            isActive ? new Color(255, 221, 128, 255) : new Color(26, 34, 34, 255));
        Raylib.DrawRectangle((int)center.X - 25, (int)center.Y - 35, 50, 16, new Color(28, 35, 38, 225));
        Raylib.DrawText(fighter.Name, (int)center.X - 22, (int)center.Y - 34, 11,
            fighter.IsAlive ? new Color(244, 240, 222, 255) : new Color(151, 152, 147, 255));
        Raylib.DrawRectangle((int)center.X - 19, (int)center.Y + 17, 38, 5, new Color(73, 41, 43, 255));
        var healthRatio = fighter.MaxHP == 0 ? 0f : (float)fighter.HP / fighter.MaxHP;
        Raylib.DrawRectangle((int)center.X - 19, (int)center.Y + 17, (int)(38 * healthRatio), 5,
            new Color(85, 190, 100, 255));
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
        if (!cell.IsWalkable && !cell.LineOfSight) return new Color(91, 63, 84, 255);
        if (!cell.IsWalkable) return new Color(103, 91, 77, 255);
        if (!cell.LineOfSight) return new Color(89, 105, 126, 255);
        return (x + y) % 2 == 0
            ? new Color(178, 158, 108, 255)
            : new Color(190, 170, 119, 255);
    }
}
