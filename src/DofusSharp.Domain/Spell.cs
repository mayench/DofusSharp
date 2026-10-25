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

    public Spell(string id, string name, int actionPointCost, int minimumRange, int maximumRange, int damage, int cooldownTurns)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("El identificador es obligatorio.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El nombre es obligatorio.", nameof(name));
        if (actionPointCost <= 0) throw new ArgumentOutOfRangeException(nameof(actionPointCost), "El coste en PA debe ser mayor que cero.");
        if (minimumRange < 0) throw new ArgumentOutOfRangeException(nameof(minimumRange));
        if (maximumRange < minimumRange) throw new ArgumentOutOfRangeException(nameof(maximumRange));
        if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
        if (cooldownTurns < 0) throw new ArgumentOutOfRangeException(nameof(cooldownTurns));

        Id = id;
        Name = name;
        ActionPointCost = actionPointCost;
        MinimumRange = minimumRange;
        MaximumRange = maximumRange;
        Damage = damage;
        CooldownTurns = cooldownTurns;
    }
}
