namespace DofusSharp.Domain;

/// <summary>Tablero rectangular en coordenadas lógicas; la proyección isométrica es responsabilidad de presentación.</summary>
public sealed class GameGrid
{
    private readonly GridCell[,] _cells;
    public int Width { get; }
    public int Height { get; }

    public GameGrid(int width = 14, int height = 20)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        Width = width;
        Height = height;
        _cells = new GridCell[width, height];
        for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                _cells[x, y] = new GridCell(x, y);
    }

    public bool Contains(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    public GridCell GetCell(int x, int y)
    {
        if (!Contains(x, y)) throw new ArgumentOutOfRangeException(nameof(x), $"La casilla ({x}, {y}) queda fuera del tablero.");
        return _cells[x, y];
    }

    /// <summary>Distancia Manhattan, que equivale al mínimo de pasos sin diagonales en un grid vacío.</summary>
    public static int ManhattanDistance(int x1, int y1, int x2, int y2) =>
        Math.Abs(x1 - x2) + Math.Abs(y1 - y2);

    /// <summary>Distancia euclídea en coordenadas lógicas; no tiene en cuenta obstáculos.</summary>
    public static double StraightLineDistance(int x1, int y1, int x2, int y2)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        return Math.Sqrt((double)dx * dx + (double)dy * dy);
    }
}
