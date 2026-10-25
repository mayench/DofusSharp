using DofusSharp.Combat.Pathfinding;
using DofusSharp.Combat.Spells;
using DofusSharp.Domain;

namespace DofusSharp.Combat.AI;

/// <summary>Decisión táctica de movimiento calculada para el turno de un enemigo.</summary>
public sealed record EnemyTurnPlan(Fighter Actor, Fighter Target, PathResult? MovementPath)
{
    public bool HasMovement => MovementPath is { MovementCost: > 0 };
}

/// <summary>Resultado de resolver las acciones ofensivas del plan.</summary>
public sealed record EnemyTurnResult(Fighter Target, Spell? Spell, SpellCastResult? CastResult)
{
    public bool CastSpell => Spell is not null && CastResult == SpellCastResult.Success;
}
