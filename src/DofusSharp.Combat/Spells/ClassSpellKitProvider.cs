using DofusSharp.Domain;

namespace DofusSharp.Combat.Spells;

/// <summary>Proporciona la baraja inicial correspondiente a la clase del combatiente.</summary>
public static class ClassSpellKitProvider
{
    private static readonly IReadOnlyDictionary<PlayerClass, IReadOnlyList<Spell>> Kits =
        new Dictionary<PlayerClass, IReadOnlyList<Spell>>
        {
            [PlayerClass.Yopuka] = Array.AsReadOnly(new[]
            {
                new Spell("yopuka-pressure", "Presión", 3, 1, 3, 18, 0,
                    SpellScalingAttribute.Strength),
                new Spell("yopuka-intimidation", "Intimidación", 3, 1, 1, 10, 0,
                    SpellScalingAttribute.None, new SpellEffects(pushbackCells: 2)),
                new Spell("yopuka-divine-sword", "Espada Divina", 3, 1, 1, 16, 0,
                    SpellScalingAttribute.Agility, requiredCharacterLevel: 6),
                new Spell("yopuka-sword-of-yopuka", "Espada de Yopuka", 4, 1, 4, 26, 0,
                    SpellScalingAttribute.Strength, requiredCharacterLevel: 90)
            }),
            [PlayerClass.Ocra] = Array.AsReadOnly(new[]
            {
                new Spell("ocra-magic-arrow", "Flecha Mágica", 4, 1, 8, 10, 0,
                    SpellScalingAttribute.Intelligence),
                new Spell("ocra-retreat-arrow", "Flecha de Retroceso", 4, 1, 8, 12, 0,
                    SpellScalingAttribute.Agility, new SpellEffects(pushbackCells: 3)),
                new Spell("ocra-icy-arrow", "Flecha Helada", 4, 1, 5, 8, 0,
                    SpellScalingAttribute.Intelligence, new SpellEffects(actionPointReduction: 1), 3),
                new Spell("ocra-burning-arrow", "Flecha Flamígera", 4, 1, 5, 14, 0,
                    SpellScalingAttribute.Intelligence, new SpellEffects(pushbackCells: 1), 6)
            })
        };

    private static readonly IReadOnlyList<Spell> GenericKit = Array.AsReadOnly(new[]
    {
        new Spell("basic-strike", "Golpe básico", 3, 1, 1, 15, 0, SpellScalingAttribute.Strength),
        new Spell("basic-shot", "Disparo básico", 3, 2, 4, 12, 0, SpellScalingAttribute.Agility),
        new Spell("basic-fire", "Llamilla", 3, 1, 5, 12, 0, SpellScalingAttribute.Intelligence),
        new Spell("basic-water", "Agua Turbia", 3, 1, 5, 10, 0, SpellScalingAttribute.Luck)
    });

    /// <summary>Devuelve hechizos inmutables para la clase; las clases aún no diseñadas usan kit genérico.</summary>
    public static IReadOnlyList<Spell> GetKit(PlayerClass playerClass) =>
        Kits.GetValueOrDefault(playerClass, GenericKit);

    /// <summary>Filtra el kit por el nivel de personaje necesario para aprender cada hechizo.</summary>
    public static IReadOnlyList<Spell> GetAvailableKit(PlayerClass playerClass, int characterLevel)
    {
        if (characterLevel < 1) throw new ArgumentOutOfRangeException(nameof(characterLevel));
        return GetKit(playerClass).Where(spell => spell.RequiredCharacterLevel <= characterLevel).ToArray();
    }
}
