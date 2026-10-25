using System.Numerics;
using DofusSharp.Combat.Pathfinding;
using DofusSharp.Domain;

namespace DofusSharp.Presentation;

/// <summary>Interpola visualmente un camino A* y descuenta un PM cada vez que alcanza la celda siguiente.</summary>
internal sealed class FighterMovementAnimation
{
    private const float SecondsPerCell = 0.14f;
    private readonly Fighter _fighter;
    private float _segmentElapsed;
    private int _nextCellIndex = 1;

    public PathResult Result { get; }
    public bool IsComplete => _nextCellIndex >= Result.Path.Count;
    public IReadOnlySet<(int X, int Y)> HighlightedCells { get; }

    public FighterMovementAnimation(Fighter fighter, PathResult result)
    {
        _fighter = fighter;
        Result = result;
        HighlightedCells = result.Path.Skip(1).Select(cell => (cell.X, cell.Y)).ToHashSet();
    }

    public Vector2 GetVisualPosition(IsometricGrid projection)
    {
        if (IsComplete)
            return projection.GridToScreen(_fighter.X, _fighter.Y);

        var from = Result.Path[_nextCellIndex - 1];
        var to = Result.Path[_nextCellIndex];
        var start = projection.GridToScreen(from.X, from.Y);
        var end = projection.GridToScreen(to.X, to.Y);
        return Vector2.Lerp(start, end, Math.Clamp(_segmentElapsed / SecondsPerCell, 0f, 1f));
    }

    public void Update(float elapsedSeconds)
    {
        if (elapsedSeconds <= 0f || IsComplete) return;
        _segmentElapsed += elapsedSeconds;

        while (_segmentElapsed >= SecondsPerCell && !IsComplete)
        {
            var nextCell = Result.Path[_nextCellIndex];
            if (!_fighter.TryMoveTo(nextCell.X, nextCell.Y, movementCost: 1))
                throw new InvalidOperationException("El combatiente se quedó sin PM durante un camino previamente validado.");

            _segmentElapsed -= SecondsPerCell;
            _nextCellIndex++;
        }
    }
}
