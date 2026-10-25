using DofusSharp.Combat.Spells;
using DofusSharp.Domain;

namespace DofusSharp.Tests;

public sealed class AdvancedSpellEffectsTests
{
    [Fact]
    public void TryCast_PushesTargetAlongDominantAxisWithoutSpendingTargetMovementPoints()
    {
        var grid = new GameGrid(6, 4);
        var caster = CreateFighter("Cra", 0, 1, "blue");
        var target = CreateFighter("Iop", 2, 1, "red", movementPoints: 4);
        var spell = CreateSpell(damage: 0, effects: new SpellEffects(pushbackCells: 2));

        var result = new SpellCastingService(grid).TryCast(caster, target, spell, [caster, target]);

        Assert.Equal(SpellCastResult.Success, result);
        Assert.Equal((4, 1), (target.X, target.Y));
        Assert.Equal(4, target.MovementPoints);
    }

    [Fact]
    public void TryCast_PushbackStopsBeforeTerrainObstacleOrOccupiedCell()
    {
        var grid = new GameGrid(6, 4);
        grid.GetCell(3, 1).IsWalkable = false;
        var caster = CreateFighter("Cra", 0, 1, "blue");
        var target = CreateFighter("Iop", 2, 1, "red");
        var bystander = CreateFighter("Aniripsa", 4, 1, "blue");
        var spell = CreateSpell(damage: 0, effects: new SpellEffects(pushbackCells: 3));

        var result = new SpellCastingService(grid).TryCast(caster, target, spell, [caster, target, bystander]);

        Assert.Equal(SpellCastResult.Success, result);
        Assert.Equal((2, 1), (target.X, target.Y));
    }

    [Fact]
    public void TryCast_PushbackStopsAtMapBoundary()
    {
        var grid = new GameGrid(3, 2);
        var caster = CreateFighter("Cra", 0, 0, "blue");
        var target = CreateFighter("Iop", 2, 0, "red");
        var spell = CreateSpell(damage: 0, effects: new SpellEffects(pushbackCells: 4));

        var result = new SpellCastingService(grid).TryCast(caster, target, spell);

        Assert.Equal(SpellCastResult.Success, result);
        Assert.Equal((2, 0), (target.X, target.Y));
    }

    [Fact]
    public void TryCast_PushbackStopsBeforeAnotherLivingFighter()
    {
        var grid = new GameGrid(6, 3);
        var caster = CreateFighter("Cra", 0, 1, "blue");
        var target = CreateFighter("Iop", 2, 1, "red");
        var blocker = CreateFighter("Sadida", 3, 1, "red");
        var spell = CreateSpell(damage: 0, effects: new SpellEffects(pushbackCells: 2));

        var result = new SpellCastingService(grid).TryCast(caster, target, spell, [caster, target, blocker]);

        Assert.Equal(SpellCastResult.Success, result);
        Assert.Equal((2, 1), (target.X, target.Y));
        Assert.Equal((3, 1), (blocker.X, blocker.Y));
    }

    [Fact]
    public void TryCast_HealsAllyUpToMaximumHealth()
    {
        var caster = CreateFighter("Aniripsa", 0, 0, "blue");
        var ally = CreateFighter("Cra", 1, 0, "blue");
        ally.TakeDamage(30);
        var spell = CreateSpell(damage: 0, effects: new SpellEffects(healingAmount: 50));

        var result = new SpellCastingService(new GameGrid(3, 2)).TryCast(caster, ally, spell);

        Assert.Equal(SpellCastResult.Success, result);
        Assert.Equal(100, ally.HP);
        Assert.Equal(4, caster.ActionPoints);
    }

    [Fact]
    public void TryCast_RejectsHealingAnEnemyWithoutSpendingActionPoints()
    {
        var caster = CreateFighter("Aniripsa", 0, 0, "blue");
        var enemy = CreateFighter("Iop", 1, 0, "red");
        enemy.TakeDamage(20);
        var spell = CreateSpell(damage: 0, effects: new SpellEffects(healingAmount: 20));

        var result = new SpellCastingService(new GameGrid(3, 2)).TryCast(caster, enemy, spell);

        Assert.Equal(SpellCastResult.InvalidTarget, result);
        Assert.Equal(6, caster.ActionPoints);
        Assert.Equal(80, enemy.HP);
    }

    [Fact]
    public void TryCast_LifeStealHealsCasterFromDamageActuallyDealt()
    {
        var caster = CreateFighter("Sram", 0, 0, "blue");
        var target = CreateFighter("Iop", 1, 0, "red");
        caster.TakeDamage(50);
        var spell = CreateSpell(damage: 20, effects: new SpellEffects(lifeStealPercent: 50));

        var result = new SpellCastingService(new GameGrid(3, 2)).TryCast(caster, target, spell);

        Assert.Equal(SpellCastResult.Success, result);
        Assert.Equal(80, target.HP);
        Assert.Equal(60, caster.HP);
    }

    [Fact]
    public void TryCast_ReducesActionAndMovementPointsWithoutGoingBelowZero()
    {
        var caster = CreateFighter("Xelor", 0, 0, "blue");
        var target = CreateFighter("Iop", 1, 0, "red", actionPoints: 5, movementPoints: 3);
        var spell = CreateSpell(damage: 0, effects: new SpellEffects(
            actionPointReduction: 4, movementPointReduction: 8));

        var result = new SpellCastingService(new GameGrid(3, 2)).TryCast(caster, target, spell);

        Assert.Equal(SpellCastResult.Success, result);
        Assert.Equal(1, target.ActionPoints);
        Assert.Equal(0, target.MovementPoints);
        Assert.Equal(5, target.MaxActionPoints);
        Assert.Equal(3, target.MaxMovementPoints);
    }

    private static Spell CreateSpell(int damage, SpellEffects effects) =>
        new("advanced", "Hechizo táctico", 2, 1, 5, damage, 0, effects: effects);

    private static Fighter CreateFighter(string name, int x, int y, string team,
        int actionPoints = 6, int movementPoints = 3) =>
        new(Guid.NewGuid(), name, x, y, actionPoints, movementPoints, 100, teamId: team);
}
