namespace DofusSharp.Domain;

/// <summary>Paquete inmutable de efectos tácticos secundarios que se resuelven al lanzar un hechizo.</summary>
public sealed record SpellEffects
{
    public int PushbackCells { get; }
    public int HealingAmount { get; }
    public int LifeStealPercent { get; }
    public int ActionPointReduction { get; }
    public int MovementPointReduction { get; }

    public static SpellEffects None { get; } = new();

    public SpellEffects(int pushbackCells = 0, int healingAmount = 0, int lifeStealPercent = 0,
        int actionPointReduction = 0, int movementPointReduction = 0)
    {
        if (pushbackCells < 0) throw new ArgumentOutOfRangeException(nameof(pushbackCells));
        if (healingAmount < 0) throw new ArgumentOutOfRangeException(nameof(healingAmount));
        if (lifeStealPercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(lifeStealPercent), "El robo de vida debe estar entre 0 y 100 %.");
        if (actionPointReduction < 0) throw new ArgumentOutOfRangeException(nameof(actionPointReduction));
        if (movementPointReduction < 0) throw new ArgumentOutOfRangeException(nameof(movementPointReduction));

        PushbackCells = pushbackCells;
        HealingAmount = healingAmount;
        LifeStealPercent = lifeStealPercent;
        ActionPointReduction = actionPointReduction;
        MovementPointReduction = movementPointReduction;
    }
}
