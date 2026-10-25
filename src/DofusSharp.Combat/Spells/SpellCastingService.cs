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

    public SpellCastResult TryCast(Fighter caster, Fighter target, Spell spell)
    {
        ArgumentNullException.ThrowIfNull(caster);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(spell);

        if (!target.IsAlive) return SpellCastResult.TargetIsDead;

        var cooldownKey = (caster.Id, spell.Id);
        var validation = CanCastAt(caster, target.X, target.Y, spell);
        if (validation != SpellCastResult.Success) return validation;

        // Primero se validan todas las condiciones; los cambios de estado solo ocurren al lanzar correctamente.
        if (!caster.TrySpendActionPoints(spell.ActionPointCost))
            return SpellCastResult.InsufficientActionPoints;
        _battleState.ApplyDamage(target, spell.Damage);
        if (spell.CooldownTurns > 0)
            _remainingCooldowns[cooldownKey] = spell.CooldownTurns;

        return SpellCastResult.Success;
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
