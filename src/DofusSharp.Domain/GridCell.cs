namespace DofusSharp.Domain;

/// <summary>Casilla lógica del mapa. Las coordenadas son índices de la cuadrícula lógica.</summary>
public sealed class GridCell
{
    public int X { get; }
    public int Y { get; }
    public bool IsWalkable { get; set; }
    public bool LineOfSight { get; set; }

    public GridCell(int x, int y, bool isWalkable = true, bool lineOfSight = true)
    {
        X = x;
        Y = y;
        IsWalkable = isWalkable;
        LineOfSight = lineOfSight;
    }
}
