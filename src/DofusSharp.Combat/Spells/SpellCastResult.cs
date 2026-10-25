namespace DofusSharp.Combat.Spells;

public enum SpellCastResult
{
    Success,
    InsufficientActionPoints,
    TargetOutOfRange,
    SpellOnCooldown,
    CasterIsDead,
    TargetIsDead
}
