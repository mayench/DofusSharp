using DofusSharp.Combat.Spells;
using DofusSharp.Domain;

namespace DofusSharp.Tests;

public sealed class ClassSpellKitTests
{
    [Fact]
    public void GetKit_ReturnsCanonicalClassSpellNamesAndElementalKits()
    {
        var yopukaKit = ClassSpellKitProvider.GetKit(PlayerClass.Yopuka);
        var ocraKit = ClassSpellKitProvider.GetKit(PlayerClass.Ocra);

        Assert.Equal(4, yopukaKit.Count);
        Assert.Contains(yopukaKit, spell => spell.Name == "Presión" && spell.ScalingAttribute == SpellScalingAttribute.Strength);
        Assert.Contains(yopukaKit, spell => spell.Name == "Intimidación" && spell.Effects.PushbackCells > 0);
        Assert.Contains(yopukaKit, spell => spell.Name == "Espada Divina" && spell.ScalingAttribute == SpellScalingAttribute.Agility);
        Assert.Contains(yopukaKit, spell => spell.Name == "Espada de Yopuka");

        Assert.Equal(4, ocraKit.Count);
        Assert.Contains(ocraKit, spell => spell.Name == "Flecha Mágica" && spell.ScalingAttribute == SpellScalingAttribute.Intelligence);
        Assert.Contains(ocraKit, spell => spell.Name == "Flecha de Retroceso" && spell.Effects.PushbackCells == 3);
        Assert.Contains(ocraKit, spell => spell.Name == "Flecha Helada" && spell.Effects.ActionPointReduction > 0);
        Assert.Contains(ocraKit, spell => spell.Name == "Flecha Flamígera");
        Assert.NotEqual(yopukaKit[0].Id, ocraKit[0].Id);
    }

    [Fact]
    public void OcraKit_CastsItsRangedFireSpellWithIntelligenceScaling()
    {
        var caster = new Fighter(Guid.NewGuid(), "Ocra", 0, 0, 6, 3, 100, teamId: "blue",
            playerClass: PlayerClass.Ocra, stats: new Stats(intelligence: 100));
        var target = new Fighter(Guid.NewGuid(), "Objetivo", 4, 0, 6, 3, 100, teamId: "red");
        var spell = ClassSpellKitProvider.GetKit(caster.Class)[0];
        var result = new SpellCastingService(new GameGrid(5, 2)).TryCast(caster, target, spell);

        Assert.Equal(SpellCastResult.Success, result);
        Assert.Equal(80, target.HP); // 10 base + 100 % de Inteligencia.
    }

    [Fact]
    public void ClassKits_UnlockSpellsAtTheirClassLevel()
    {
        Assert.Equal(2, ClassSpellKitProvider.GetAvailableKit(PlayerClass.Yopuka, 1).Count);
        Assert.Equal(3, ClassSpellKitProvider.GetAvailableKit(PlayerClass.Yopuka, 6).Count);
        Assert.Equal(4, ClassSpellKitProvider.GetAvailableKit(PlayerClass.Yopuka, 90).Count);
        Assert.Equal(2, ClassSpellKitProvider.GetAvailableKit(PlayerClass.Ocra, 1).Count);
        Assert.Equal(3, ClassSpellKitProvider.GetAvailableKit(PlayerClass.Ocra, 3).Count);
        Assert.Equal(4, ClassSpellKitProvider.GetAvailableKit(PlayerClass.Ocra, 6).Count);
    }
}
