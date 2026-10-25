using DofusSharp.Domain;

namespace DofusSharp.Combat.Pathfinding;

/// <summary>A* para movimiento ortogonal de coste uniforme, sin diagonales.</summary>
public sealed class AStarPathfinder
{
    private static readonly (int X, int Y)[] Directions = [(1, 0), (-1, 0), (0, 1), (0, -1)];

    public PathResult? FindPath(GameGrid grid, int startX, int startY, int goalX, int goalY,
        IReadOnlySet<(int X, int Y)>? blockedCells = null)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (!grid.Contains(startX, startY) || !grid.Contains(goalX, goalY)) return null;
        if (!grid.GetCell(goalX, goalY).IsWalkable) return null;

        var start = (X: startX, Y: startY);
        var goal = (X: goalX, Y: goalY);
        if (start == goal) return new PathResult([grid.GetCell(startX, startY)]);

        // Prioridad lexicográfica: menor f primero y luego menor h para desempates estables.
        var open = new PriorityQueue<(int X, int Y), (int F, int H, int Sequence)>();
        var cameFrom = new Dictionary<(int X, int Y), (int X, int Y)>();
        var gScore = new Dictionary<(int X, int Y), int> { [start] = 0 };
        var closed = new HashSet<(int X, int Y)>();
        var sequence = 0;
        var initialH = GameGrid.ManhattanDistance(startX, startY, goalX, goalY);
        open.Enqueue(start, (initialH, initialH, sequence++));

        while (open.TryDequeue(out var current, out _))
        {
            if (!closed.Add(current)) continue;
            if (current == goal) return new PathResult(Reconstruct(grid, cameFrom, current));

            foreach (var direction in Directions)
            {
                var next = (X: current.X + direction.X, Y: current.Y + direction.Y);
                if (!grid.Contains(next.X, next.Y) || !grid.GetCell(next.X, next.Y).IsWalkable ||
                    blockedCells?.Contains(next) == true || closed.Contains(next)) continue;
                var tentativeG = gScore[current] + 1;
                if (gScore.TryGetValue(next, out var previousG) && tentativeG >= previousG) continue;

                cameFrom[next] = current;
                gScore[next] = tentativeG;
                var h = GameGrid.ManhattanDistance(next.X, next.Y, goalX, goalY);
                open.Enqueue(next, (tentativeG + h, h, sequence++));
            }
        }

        return null;
    }

    /// <summary>Comprueba puntos de movimiento sin alterar la entidad; el gestor de estado confirma el movimiento.</summary>
    public bool CanAfford(Fighter fighter, PathResult path)
    {
        ArgumentNullException.ThrowIfNull(fighter);
        ArgumentNullException.ThrowIfNull(path);
        return path.MovementCost <= fighter.MovementPoints;
    }

    private static IReadOnlyList<GridCell> Reconstruct(GameGrid grid,
        IReadOnlyDictionary<(int X, int Y), (int X, int Y)> cameFrom, (int X, int Y) current)
    {
        var coordinates = new List<(int X, int Y)> { current };
        while (cameFrom.TryGetValue(current, out var previous))
        {
            current = previous;
            coordinates.Add(current);
        }
        coordinates.Reverse();
        return coordinates.Select(point => grid.GetCell(point.X, point.Y)).ToArray();
    }
}
