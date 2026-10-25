using DofusSharp.Combat.Spells;
using DofusSharp.Domain;
using DofusSharp.GameState.Turns;

namespace DofusSharp.Tests;

public sealed class SpellCastingTests
{
    [Fact]
    public void TryCast_ConsumesActionPointsAndAppliesDamage()
    {
        var caster = CreateFighter("Cra", 0, 0, actionPoints: 6, initiative: 20);
        var target = CreateFighter("Iop", 2, 0, hp: 100, initiative: 10);
        var spell = CreateSpell(cost: 3, minRange: 1, maxRange: 3, damage: 25);
        var service = new SpellCastingService(new GameGrid());

        var result = service.TryCast(caster, target, spell);

        Assert.Equal(SpellCastResult.Success, result);
        Assert.Equal(3, caster.ActionPoints);
        Assert.Equal(75, target.HP);
    }

    [Fact]
    public void CanCastAt_ValidatesTargetWithoutConsumingActionPoints()
    {
        var grid = new GameGrid(5, 3);
        var caster = CreateFighter("Cra", 0, 1, actionPoints: 6);
        var spell = CreateSpell(cost: 3, minRange: 1, maxRange: 4);
        var service = new SpellCastingService(grid);

        var result = service.CanCastAt(caster, 3, 1, spell);

        Assert.Equal(SpellCastResult.Success, result);
        Assert.Equal(6, caster.ActionPoints);
    }

    [Fact]
    public void TryCast_RejectsTargetOutsideRangeWithoutSpendingResources()
    {
        var caster = CreateFighter("Cra", 0, 0, actionPoints: 6);
        var target = CreateFighter("Iop", 5, 0, hp: 100);
        var spell = CreateSpell(cost: 3, minRange: 1, maxRange: 3, damage: 25);

        var result = new SpellCastingService(new GameGrid()).TryCast(caster, target, spell);

        Assert.Equal(SpellCastResult.TargetOutOfRange, result);
        Assert.Equal(6, caster.ActionPoints);
        Assert.Equal(100, target.HP);
    }

    [Fact]
    public void TryCast_RejectsInsufficientActionPoints()
    {
        var caster = CreateFighter("Cra", 0, 0, actionPoints: 2);
        var target = CreateFighter("Iop", 1, 0, hp: 100);

        var result = new SpellCastingService(new GameGrid()).TryCast(caster, target, CreateSpell(cost: 3));

        Assert.Equal(SpellCastResult.InsufficientActionPoints, result);
        Assert.Equal(2, caster.ActionPoints);
        Assert.Equal(100, target.HP);
    }

    [Fact]
    public void Cooldown_IsPerCasterAndExpiresAtStartOfCasterNextTurn()
    {
        var caster = CreateFighter("Cra", 0, 0, actionPoints: 6, initiative: 20);
        var opponent = CreateFighter("Iop", 1, 0, actionPoints: 6, initiative: 10);
        var spell = CreateSpell(cost: 2, cooldown: 1);
        var turns = new TurnManager([caster, opponent]);
        var service = new SpellCastingService(new GameGrid());
        turns.TurnStarted += service.NotifyTurnStarted;

        Assert.Equal(SpellCastResult.Success, service.TryCast(caster, opponent, spell));
        Assert.Equal(1, service.GetRemainingCooldownTurns(caster, spell));
        Assert.Equal(SpellCastResult.SpellOnCooldown, service.TryCast(caster, opponent, spell));

        turns.PassTurn(); // El turno del rival no reduce el enfriamiento de Cra.
        Assert.Equal(1, service.GetRemainingCooldownTurns(caster, spell));
        Assert.Equal(SpellCastResult.SpellOnCooldown, service.TryCast(caster, opponent, spell));
        turns.PassTurn(); // Al volver el turno de Cra, el enfriamiento se reduce.
        Assert.Equal(0, service.GetRemainingCooldownTurns(caster, spell));
        Assert.Equal(SpellCastResult.Success, service.TryCast(caster, opponent, spell));
    }

    [Fact]
    public void TryCast_RejectsDeadCasterOrTarget()
    {
        var caster = CreateFighter("Cra", 0, 0, hp: 0);
        var target = CreateFighter("Iop", 1, 0, hp: 100);
        var service = new SpellCastingService(new GameGrid());
        var spell = CreateSpell();

        Assert.Equal(SpellCastResult.CasterIsDead, service.TryCast(caster, target, spell));
        caster = CreateFighter("Cra", 0, 0);
        target.TakeDamage(100);
        Assert.Equal(SpellCastResult.TargetIsDead, service.TryCast(caster, target, spell));
    }

    [Fact]
    public void TryCast_LethalDamageMarksTargetDeadAndReleasesItsCell()
    {
        var grid = new GameGrid(3, 3);
        grid.GetCell(1, 0).IsWalkable = false;
        grid.GetCell(1, 0).LineOfSight = false;
        var caster = CreateFighter("Cra", 0, 0, actionPoints: 6);
        var target = CreateFighter("Iop", 1, 0, hp: 10);
        var lethalSpell = CreateSpell(cost: 2, minRange: 1, maxRange: 2, damage: 10);

        var result = new SpellCastingService(grid).TryCast(caster, target, lethalSpell);

        Assert.Equal(SpellCastResult.Success, result);
        Assert.False(target.IsAlive);
        Assert.True(grid.GetCell(1, 0).IsWalkable);
        Assert.True(grid.GetCell(1, 0).LineOfSight);
    }

    [Fact]
    public void TryCast_SucceedsWhenLineOfSightIsClear()
    {
        var grid = new GameGrid(5, 3);
        var caster = CreateFighter("Cra", 0, 1);
        var target = CreateFighter("Iop", 4, 1);
        var spell = CreateSpell(minRange: 1, maxRange: 5, damage: 15);
        var service = new SpellCastingService(grid);

        var result = service.TryCast(caster, target, spell);

        Assert.Equal(SpellCastResult.Success, result);
        Assert.Equal(85, target.HP);
    }

    [Fact]
    public void TryCast_RejectsSpellWhenIntermediateCellBlocksLineOfSight()
    {
        var grid = new GameGrid(5, 3);
        grid.GetCell(2, 1).IsWalkable = false;
        var caster = CreateFighter("Cra", 0, 1);
        var target = CreateFighter("Iop", 4, 1);
        var spell = CreateSpell(minRange: 1, maxRange: 5, damage: 15);
        var service = new SpellCastingService(grid);

        var result = service.TryCast(caster, target, spell);

        Assert.Equal(SpellCastResult.LineOfSightBlocked, result);
        Assert.Equal(6, caster.ActionPoints);
        Assert.Equal(100, target.HP);
    }

    [Fact]
    public void TryCast_RejectsSpellWhenIntermediateCellIsOpaque()
    {
        var grid = new GameGrid(5, 3);
        grid.GetCell(2, 1).LineOfSight = false;
        var caster = CreateFighter("Cra", 0, 1);
        var target = CreateFighter("Iop", 4, 1);
        var spell = CreateSpell(minRange: 1, maxRange: 5);

        var result = new SpellCastingService(grid).TryCast(caster, target, spell);

        Assert.Equal(SpellCastResult.LineOfSightBlocked, result);
    }

    private static Fighter CreateFighter(string name, int x = 0, int y = 0, int actionPoints = 6,
        int hp = 100, int initiative = 0) =>
        new(Guid.NewGuid(), name, x, y, actionPoints, movementPoints: 3, hp, initiative);

    private static Spell CreateSpell(int cost = 2, int minRange = 0, int maxRange = 3,
        int damage = 10, int cooldown = 0) =>
        new("basic-shot", "Disparo básico", cost, minRange, maxRange, damage, cooldown);
}
