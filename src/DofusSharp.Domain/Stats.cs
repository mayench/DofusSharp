namespace DofusSharp.Domain;

/// <summary>Atributos base inmutables del combatiente. Vitalidad suma directamente al HP máximo.</summary>
public sealed record Stats
{
    public int Vitality { get; }
    public int Strength { get; }
    public int Intelligence { get; }
    public int Agility { get; }
    public int Luck { get; }

    public static Stats Zero { get; } = new();

    public Stats(int vitality = 0, int strength = 0, int intelligence = 0, int agility = 0, int luck = 0)
    {
        if (vitality < 0) throw new ArgumentOutOfRangeException(nameof(vitality));
        if (strength < 0) throw new ArgumentOutOfRangeException(nameof(strength));
        if (intelligence < 0) throw new ArgumentOutOfRangeException(nameof(intelligence));
        if (agility < 0) throw new ArgumentOutOfRangeException(nameof(agility));
        if (luck < 0) throw new ArgumentOutOfRangeException(nameof(luck));

        Vitality = vitality;
        Strength = strength;
        Intelligence = intelligence;
        Agility = agility;
        Luck = luck;
    }

    /// <summary>Devuelve el atributo usado como porcentaje de bonificación del hechizo.</summary>
    public int GetValue(SpellScalingAttribute attribute) => attribute switch
    {
        SpellScalingAttribute.Strength => Strength,
        SpellScalingAttribute.Intelligence => Intelligence,
        SpellScalingAttribute.Agility => Agility,
        SpellScalingAttribute.Luck => Luck,
        _ => 0
    };
}
