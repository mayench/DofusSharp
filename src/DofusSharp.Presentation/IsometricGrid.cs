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

    public void Draw(GameGrid grid, int highlightedX, int highlightedY)
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
                var fill = GetCellColor(cell);
                if (x == highlightedX && y == highlightedY)
                    fill = new Color(236, 190, 73, 255);

                DrawDiamond(center, fill, new Color(24, 35, 33, 255));
            }
        }
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

    private static Color GetCellColor(GridCell cell)
    {
        if (!cell.IsWalkable && !cell.LineOfSight) return new Color(91, 45, 91, 255);
        if (!cell.IsWalkable) return new Color(145, 62, 54, 255);
        if (!cell.LineOfSight) return new Color(65, 83, 143, 255);
        return new Color(66, 111, 81, 255);
    }
}
