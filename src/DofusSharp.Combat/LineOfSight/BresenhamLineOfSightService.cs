using DofusSharp.Domain;

namespace DofusSharp.Combat.LineOfSight;

/// <summary>
/// Recorre las celdas de un rayo Bresenham en coordenadas lógicas. La transformación isométrica
/// es afín, por lo que no cambia qué celdas atraviesa el segmento entre los centros de dos casillas.
/// </summary>
public sealed class BresenhamLineOfSightService : ILineOfSightService
{
    public bool HasLineOfSight(GameGrid grid, int startX, int startY, int endX, int endY)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (!grid.Contains(startX, startY) || !grid.Contains(endX, endY)) return false;
        if (startX == endX && startY == endY) return true;

        var x = startX;
        var y = startY;
        var deltaX = Math.Abs(endX - startX);
        var stepX = startX < endX ? 1 : -1;
        var deltaY = -Math.Abs(endY - startY);
        var stepY = startY < endY ? 1 : -1;
        var error = deltaX + deltaY;

        while (x != endX || y != endY)
        {
            var doubledError = 2 * error;
            if (doubledError >= deltaY)
            {
                error += deltaY;
                x += stepX;
            }
            if (doubledError <= deltaX)
            {
                error += deltaX;
                y += stepY;
            }

            // Los extremos representan al lanzador y al objetivo; solo bloquean las celdas intermedias.
            if (x == endX && y == endY) break;
            var cell = grid.GetCell(x, y);
            if (!cell.IsWalkable || !cell.LineOfSight) return false;
        }

        return true;
    }
}
