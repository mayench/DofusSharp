using DofusSharp.Domain;

namespace DofusSharp.Combat.Pathfinding;

/// <summary>Resultado del cálculo. Path incluye origen y destino; MovementCost cuenta solo los pasos.</summary>
public sealed record PathResult(IReadOnlyList<GridCell> Path)
{
    public int MovementCost => Math.Max(0, Path.Count - 1);
}
