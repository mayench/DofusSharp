using DofusSharp.Domain;
using DofusSharp.Combat.LineOfSight;
using DofusSharp.Combat.State;

namespace DofusSharp.Combat.Spells;

/// <summary>Valida y resuelve lanzamientos básicos; el enfriamiento se cuenta en turnos del lanzador.</summary>
public sealed class SpellCastingService
{
    private readonly GameGrid _grid;
    private readonly ILineOfSightService _lineOfSight;
    private readonly BattleStateService _battleState;
    private readonly Dictionary<(Guid CasterId, string SpellId), int> _remainingCooldowns = new();

    public SpellCastingService(GameGrid grid, ILineOfSightService? lineOfSight = null,
        BattleStateService? battleState = null)
    {
        _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        _lineOfSight = lineOfSight ?? new BresenhamLineOfSightService();
        _battleState = battleState ?? new BattleStateService(grid);
        if (!ReferenceEquals(_battleState.Grid, _grid))
            throw new ArgumentException("El servicio de batalla debe usar el mismo tablero que el lanzador de hechizos.", nameof(battleState));
    }

    public SpellCastResult TryCast(Fighter caster, Fighter target, Spell spell,
        IEnumerable<Fighter>? combatants = null)
    {
        ArgumentNullException.ThrowIfNull(caster);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(spell);

        if (!target.IsAlive) return SpellCastResult.TargetIsDead;
        if (spell.Effects.HealingAmount > 0 && caster.TeamId != target.TeamId)
            return SpellCastResult.InvalidTarget;

        var cooldownKey = (caster.Id, spell.Id);
        var validation = CanCastAt(caster, target.X, target.Y, spell);
        if (validation != SpellCastResult.Success) return validation;

        // Primero se validan todas las condiciones; los cambios de estado solo ocurren al lanzar correctamente.
        if (!caster.TrySpendActionPoints(spell.ActionPointCost))
            return SpellCastResult.InsufficientActionPoints;
        var actualDamage = 0;
        if (spell.Damage > 0)
        {
            var hpBefore = target.HP;
            _battleState.ApplyDamage(target, spell.CalculateDamage(caster.Stats));
            actualDamage = hpBefore - target.HP;
        }
        else if (spell.Effects.HealingAmount > 0)
        {
            target.Heal(spell.Effects.HealingAmount);
        }

        if (actualDamage > 0 && spell.Effects.LifeStealPercent > 0)
        {
            var lifeSteal = (int)((long)actualDamage * spell.Effects.LifeStealPercent / 100);
            caster.Heal(lifeSteal);
        }

        if (target.IsAlive)
        {
            ApplyPushback(caster, target, spell.Effects.PushbackCells, combatants);
            target.ReduceActionPoints(spell.Effects.ActionPointReduction);
            target.ReduceMovementPoints(spell.Effects.MovementPointReduction);
        }

        if (spell.CooldownTurns > 0)
            _remainingCooldowns[cooldownKey] = spell.CooldownTurns;

        return SpellCastResult.Success;
    }

    /// <summary>Empuja en el eje lógico dominante alejándose del lanzador; bloqueos detienen el empuje.</summary>
    private void ApplyPushback(Fighter caster, Fighter target, int cells, IEnumerable<Fighter>? combatants)
    {
        if (cells == 0) return;

        var deltaX = target.X - caster.X;
        var deltaY = target.Y - caster.Y;
        var stepX = 0;
        var stepY = 0;
        if (Math.Abs(deltaX) >= Math.Abs(deltaY)) stepX = Math.Sign(deltaX);
        else stepY = Math.Sign(deltaY);
        if (stepX == 0 && stepY == 0) return;

        var occupiedCells = (combatants ?? [caster, target])
            .Where(fighter => fighter.IsAlive && fighter != target)
            .Select(fighter => (fighter.X, fighter.Y))
            .ToHashSet();

        for (var pushed = 0; pushed < cells; pushed++)
        {
            var nextX = target.X + stepX;
            var nextY = target.Y + stepY;
            if (!_grid.Contains(nextX, nextY) || !_grid.GetCell(nextX, nextY).IsWalkable ||
                occupiedCells.Contains((nextX, nextY)))
                break;

            target.DisplaceTo(nextX, nextY);
        }
    }

    /// <summary>Valida alcance, LoS, PA y cooldown para una celda sin modificar el estado del combate.</summary>
    public SpellCastResult CanCastAt(Fighter caster, int targetX, int targetY, Spell spell)
    {
        ArgumentNullException.ThrowIfNull(caster);
        ArgumentNullException.ThrowIfNull(spell);

        if (!caster.IsAlive) return SpellCastResult.CasterIsDead;

        var distance = GameGrid.ManhattanDistance(caster.X, caster.Y, targetX, targetY);
        if (distance < spell.MinimumRange || distance > spell.MaximumRange)
            return SpellCastResult.TargetOutOfRange;
        if (!_lineOfSight.HasLineOfSight(_grid, caster.X, caster.Y, targetX, targetY))
            return SpellCastResult.LineOfSightBlocked;

        if (GetRemainingCooldownTurns(caster, spell) > 0)
            return SpellCastResult.SpellOnCooldown;
        if (caster.ActionPoints < spell.ActionPointCost)
            return SpellCastResult.InsufficientActionPoints;

        return SpellCastResult.Success;
    }

    /// <summary>Consulta LoS para presentar las celdas alcanzables sin intentar lanzar el hechizo.</summary>
    public bool HasLineOfSight(int sourceX, int sourceY, int targetX, int targetY) =>
        _lineOfSight.HasLineOfSight(_grid, sourceX, sourceY, targetX, targetY);

    public int GetRemainingCooldownTurns(Fighter caster, Spell spell)
    {
        ArgumentNullException.ThrowIfNull(caster);
        ArgumentNullException.ThrowIfNull(spell);
        return _remainingCooldowns.GetValueOrDefault((caster.Id, spell.Id));
    }

    /// <summary>Llamar desde el coordinador de la partida al comenzar el turno de una entidad.</summary>
    public void NotifyTurnStarted(Fighter fighter)
    {
        ArgumentNullException.ThrowIfNull(fighter);
        foreach (var key in _remainingCooldowns.Keys.Where(key => key.CasterId == fighter.Id).ToArray())
        {
            var remaining = _remainingCooldowns[key] - 1;
            if (remaining <= 0) _remainingCooldowns.Remove(key);
            else _remainingCooldowns[key] = remaining;
        }
    }
}
