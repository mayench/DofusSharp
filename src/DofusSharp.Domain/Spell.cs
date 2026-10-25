namespace DofusSharp.Domain;

/// <summary>Definición inmutable de un hechizo; su enfriamiento se administra por lanzador.</summary>
public sealed class Spell
{
    public string Id { get; }
    public string Name { get; }
    public int ActionPointCost { get; }
    public int MinimumRange { get; }
    public int MaximumRange { get; }
    public int Damage { get; }
    public int CooldownTurns { get; }
    public SpellScalingAttribute ScalingAttribute { get; }
    public SpellEffects Effects { get; }
    public int RequiredCharacterLevel { get; }

    public Spell(string id, string name, int actionPointCost, int minimumRange, int maximumRange, int damage,
        int cooldownTurns, SpellScalingAttribute scalingAttribute = SpellScalingAttribute.Strength,
        SpellEffects? effects = null, int requiredCharacterLevel = 1)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("El identificador es obligatorio.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El nombre es obligatorio.", nameof(name));
        if (actionPointCost <= 0) throw new ArgumentOutOfRangeException(nameof(actionPointCost), "El coste en PA debe ser mayor que cero.");
        if (minimumRange < 0) throw new ArgumentOutOfRangeException(nameof(minimumRange));
        if (maximumRange < minimumRange) throw new ArgumentOutOfRangeException(nameof(maximumRange));
        if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
        if (cooldownTurns < 0) throw new ArgumentOutOfRangeException(nameof(cooldownTurns));
        if (requiredCharacterLevel < 1) throw new ArgumentOutOfRangeException(nameof(requiredCharacterLevel));

        Id = id;
        Name = name;
        ActionPointCost = actionPointCost;
        MinimumRange = minimumRange;
        MaximumRange = maximumRange;
        Damage = damage;
        CooldownTurns = cooldownTurns;
        ScalingAttribute = scalingAttribute;
        Effects = effects ?? SpellEffects.None;
        RequiredCharacterLevel = requiredCharacterLevel;
        if (damage > 0 && Effects.HealingAmount > 0)
            throw new ArgumentException("Un hechizo no puede combinar daño directo y curación directa al objetivo.", nameof(effects));
    }

    /// <summary>Aplica +1% de daño base por cada punto del atributo asociado (redondeo hacia abajo).</summary>
    public int CalculateDamage(Stats casterStats)
    {
        ArgumentNullException.ThrowIfNull(casterStats);
        var bonus = (long)Damage * casterStats.GetValue(ScalingAttribute) / 100;
        return (int)Math.Min(int.MaxValue, Damage + bonus);
    }
}
