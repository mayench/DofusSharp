using DofusSharp.Combat.Spells;
using DofusSharp.Domain;

namespace DofusSharp.Tests;

public sealed class ClassSpellKitTests
{
    [Fact]
    public void GetKit_ReturnsDistinctMeleeAndRangedKitsForYopukaAndOcra()
    {
        var yopukaKit = ClassSpellKitProvider.GetKit(PlayerClass.Yopuka);
        var ocraKit = ClassSpellKitProvider.GetKit(PlayerClass.Ocra);

        Assert.Equal(2, yopukaKit.Count);
        Assert.All(yopukaKit, spell => Assert.Equal(SpellScalingAttribute.Strength, spell.ScalingAttribute));
        Assert.Contains(yopukaKit, spell => spell.MaximumRange == 1);

        Assert.Equal(2, ocraKit.Count);
        Assert.Contains(ocraKit, spell => spell.MinimumRange >= 2 && spell.MaximumRange >= 7);
        Assert.Contains(ocraKit, spell => spell.ScalingAttribute == SpellScalingAttribute.Agility);
        Assert.Contains(ocraKit, spell => spell.ScalingAttribute == SpellScalingAttribute.Luck);
        Assert.NotEqual(yopukaKit[0].Id, ocraKit[0].Id);
    }

    [Fact]
    public void OcraKit_CastsItsRangedAgilitySpellWithClassScaling()
    {
        var caster = new Fighter(Guid.NewGuid(), "Ocra", 0, 0, 6, 3, 100, teamId: "blue",
            playerClass: PlayerClass.Ocra, stats: new Stats(agility: 100));
        var target = new Fighter(Guid.NewGuid(), "Objetivo", 4, 0, 6, 3, 100, teamId: "red");
        var spell = ClassSpellKitProvider.GetKit(caster.Class)[0];
        var result = new SpellCastingService(new GameGrid(5, 2)).TryCast(caster, target, spell);

        Assert.Equal(SpellCastResult.Success, result);
        Assert.Equal(62, target.HP); // 19 base + 100 % de Agilidad.
    }
}
