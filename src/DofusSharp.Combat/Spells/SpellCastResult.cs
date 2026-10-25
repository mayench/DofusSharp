namespace DofusSharp.Combat.Spells;

public enum SpellCastResult
{
    Success,
    InsufficientActionPoints,
    TargetOutOfRange,
    LineOfSightBlocked,
    SpellOnCooldown,
    CasterIsDead,
    TargetIsDead
}
