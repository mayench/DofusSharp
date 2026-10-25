using DofusSharp.Domain;

namespace DofusSharp.Combat.Spells;

/// <summary>Valida y resuelve lanzamientos básicos; el enfriamiento se cuenta en turnos del lanzador.</summary>
public sealed class SpellCastingService
{
    private readonly Dictionary<(Guid CasterId, string SpellId), int> _remainingCooldowns = new();

    public SpellCastResult TryCast(Fighter caster, Fighter target, Spell spell)
    {
        ArgumentNullException.ThrowIfNull(caster);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(spell);

        if (!caster.IsAlive) return SpellCastResult.CasterIsDead;
        if (!target.IsAlive) return SpellCastResult.TargetIsDead;

        var distance = GameGrid.ManhattanDistance(caster.X, caster.Y, target.X, target.Y);
        if (distance < spell.MinimumRange || distance > spell.MaximumRange)
            return SpellCastResult.TargetOutOfRange;

        var cooldownKey = (caster.Id, spell.Id);
        if (_remainingCooldowns.TryGetValue(cooldownKey, out var remaining) && remaining > 0)
            return SpellCastResult.SpellOnCooldown;
        if (caster.ActionPoints < spell.ActionPointCost)
            return SpellCastResult.InsufficientActionPoints;

        // Primero se validan todas las condiciones; los cambios de estado solo ocurren al lanzar correctamente.
        if (!caster.TrySpendActionPoints(spell.ActionPointCost))
            return SpellCastResult.InsufficientActionPoints;
        target.TakeDamage(spell.Damage);
        if (spell.CooldownTurns > 0)
            _remainingCooldowns[cooldownKey] = spell.CooldownTurns;

        return SpellCastResult.Success;
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
