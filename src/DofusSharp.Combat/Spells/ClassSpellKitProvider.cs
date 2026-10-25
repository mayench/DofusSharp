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
                new Spell("yopuka-sword", "Espada de Yopuka", 4, 1, 1, 28, 0,
                    SpellScalingAttribute.Strength),
                new Spell("yopuka-pressure", "Presión", 3, 1, 2, 18, 1,
                    SpellScalingAttribute.Strength)
            }),
            [PlayerClass.Ocra] = Array.AsReadOnly(new[]
            {
                new Spell("ocra-pursuit", "Flecha perseguidora", 3, 2, 7, 19, 0,
                    SpellScalingAttribute.Agility),
                new Spell("ocra-slowing", "Flecha ralentizante", 4, 3, 8, 23, 1,
                    SpellScalingAttribute.Luck)
            })
        };

    private static readonly IReadOnlyList<Spell> GenericKit = Array.AsReadOnly(new[]
    {
        new Spell("basic-strike", "Golpe básico", 3, 1, 1, 15, 0, SpellScalingAttribute.Strength),
        new Spell("basic-shot", "Disparo básico", 3, 2, 4, 12, 0, SpellScalingAttribute.Agility)
    });

    /// <summary>Devuelve hechizos inmutables para la clase; las clases aún no diseñadas usan kit genérico.</summary>
    public static IReadOnlyList<Spell> GetKit(PlayerClass playerClass) =>
        Kits.GetValueOrDefault(playerClass, GenericKit);
}
