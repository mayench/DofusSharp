using DofusSharp.Combat.Spells;
using DofusSharp.Domain;

namespace DofusSharp.Tests;

public sealed class CharacterStatsTests
{
    [Fact]
    public void Fighter_AssignsClassAndAddsVitalityToMaximumHealth()
    {
        var stats = new Stats(vitality: 30, strength: 20, intelligence: 12, agility: 8, luck: 5);
        var fighter = CreateFighter("Ocra", stats, PlayerClass.Ocra);

        Assert.Equal(PlayerClass.Ocra, fighter.Class);
        Assert.Same(stats, fighter.Stats);
        Assert.Equal(130, fighter.MaxHP);
        Assert.Equal(130, fighter.HP);
    }

    [Fact]
    public void SpellCasting_ScalesPhysicalDamageWithStrength()
    {
        var grid = new GameGrid(4, 2);
        var caster = CreateFighter("Yopuka", new Stats(strength: 50), PlayerClass.Yopuka);
        var target = new Fighter(Guid.NewGuid(), "Objetivo", 2, 0, 6, 3, 100);
        var spell = new Spell("earth-hit", "Golpe de tierra", 2, 1, 3, 20, 0,
            SpellScalingAttribute.Strength);

        var result = new SpellCastingService(grid).TryCast(caster, target, spell);

        Assert.Equal(SpellCastResult.Success, result);
        Assert.Equal(70, target.HP); // 20 base + 50 % de Fuerza.
    }

    [Fact]
    public void Spell_CalculatesDamageFromItsConfiguredAttribute()
    {
        var stats = new Stats(strength: 80, intelligence: 25, agility: 10, luck: 40);
        var fireSpell = new Spell("fire", "Fuego", 2, 1, 4, 40, 0,
            SpellScalingAttribute.Intelligence);
        var neutralSpell = new Spell("neutral", "Neutral", 2, 1, 4, 40, 0,
            SpellScalingAttribute.None);

        Assert.Equal(50, fireSpell.CalculateDamage(stats));
        Assert.Equal(40, neutralSpell.CalculateDamage(stats));
    }

    private static Fighter CreateFighter(string name, Stats stats, PlayerClass playerClass) =>
        new(Guid.NewGuid(), name, 0, 0, 6, 3, 100, teamId: "blue", playerClass: playerClass, stats: stats);
}
