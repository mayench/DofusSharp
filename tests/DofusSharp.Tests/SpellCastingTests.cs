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
        var service = new SpellCastingService();

        var result = service.TryCast(caster, target, spell);

        Assert.Equal(SpellCastResult.Success, result);
        Assert.Equal(3, caster.ActionPoints);
        Assert.Equal(75, target.HP);
    }

    [Fact]
    public void TryCast_RejectsTargetOutsideRangeWithoutSpendingResources()
    {
        var caster = CreateFighter("Cra", 0, 0, actionPoints: 6);
        var target = CreateFighter("Iop", 5, 0, hp: 100);
        var spell = CreateSpell(cost: 3, minRange: 1, maxRange: 3, damage: 25);

        var result = new SpellCastingService().TryCast(caster, target, spell);

        Assert.Equal(SpellCastResult.TargetOutOfRange, result);
        Assert.Equal(6, caster.ActionPoints);
        Assert.Equal(100, target.HP);
    }

    [Fact]
    public void TryCast_RejectsInsufficientActionPoints()
    {
        var caster = CreateFighter("Cra", 0, 0, actionPoints: 2);
        var target = CreateFighter("Iop", 1, 0, hp: 100);

        var result = new SpellCastingService().TryCast(caster, target, CreateSpell(cost: 3));

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
        var service = new SpellCastingService();
        turns.TurnStarted += service.NotifyTurnStarted;

        Assert.Equal(SpellCastResult.Success, service.TryCast(caster, opponent, spell));
        Assert.Equal(SpellCastResult.SpellOnCooldown, service.TryCast(caster, opponent, spell));

        turns.PassTurn(); // El turno del rival no reduce el enfriamiento de Cra.
        Assert.Equal(SpellCastResult.SpellOnCooldown, service.TryCast(caster, opponent, spell));
        turns.PassTurn(); // Al volver el turno de Cra, el enfriamiento se reduce.
        Assert.Equal(SpellCastResult.Success, service.TryCast(caster, opponent, spell));
    }

    [Fact]
    public void TryCast_RejectsDeadCasterOrTarget()
    {
        var caster = CreateFighter("Cra", 0, 0, hp: 0);
        var target = CreateFighter("Iop", 1, 0, hp: 100);
        var service = new SpellCastingService();
        var spell = CreateSpell();

        Assert.Equal(SpellCastResult.CasterIsDead, service.TryCast(caster, target, spell));
        caster = CreateFighter("Cra", 0, 0);
        target.TakeDamage(100);
        Assert.Equal(SpellCastResult.TargetIsDead, service.TryCast(caster, target, spell));
    }

    private static Fighter CreateFighter(string name, int x = 0, int y = 0, int actionPoints = 6,
        int hp = 100, int initiative = 0) =>
        new(Guid.NewGuid(), name, x, y, actionPoints, movementPoints: 3, hp, initiative);

    private static Spell CreateSpell(int cost = 2, int minRange = 0, int maxRange = 3,
        int damage = 10, int cooldown = 0) =>
        new("basic-shot", "Disparo básico", cost, minRange, maxRange, damage, cooldown);
}
