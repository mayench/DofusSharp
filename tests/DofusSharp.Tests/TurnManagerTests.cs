using DofusSharp.Domain;
using DofusSharp.GameState.Turns;

namespace DofusSharp.Tests;

public sealed class TurnManagerTests
{
    [Fact]
    public void Constructor_OrdersFightersByInitiativeAndPreservesTies()
    {
        var slower = CreateFighter("Sram", initiative: 10);
        var firstTie = CreateFighter("Cra", initiative: 20);
        var secondTie = CreateFighter("Iop", initiative: 20);

        var turns = new TurnManager([slower, firstTie, secondTie]);

        Assert.Same(firstTie, turns.CurrentFighter);
        Assert.Same(secondTie, turns.PassTurn());
        Assert.Same(slower, turns.PassTurn());
    }

    [Fact]
    public void PassTurn_RefreshesNextFighterAndAdvancesRoundAfterQueueEnds()
    {
        var first = CreateFighter("Cra", initiative: 20);
        var second = CreateFighter("Iop", initiative: 10);
        var turns = new TurnManager([first, second]);
        Assert.True(first.TrySpendActionPoints(2));
        Assert.True(first.TryMoveTo(1, 0, 2));

        Assert.Same(second, turns.PassTurn());
        Assert.Equal((6, 3), (second.ActionPoints, second.MovementPoints));
        Assert.Same(first, turns.PassTurn());
        Assert.Equal((2, 2), (turns.RoundNumber, turns.TurnNumber));
        Assert.Equal((6, 3), (first.ActionPoints, first.MovementPoints));

        turns.PassTurn();
        Assert.Equal(2, turns.RoundNumber);
    }

    [Fact]
    public void PassTurn_SkipsDeadFightersInInitiativeQueue()
    {
        var active = CreateFighter("Cra", initiative: 30);
        var defeated = CreateFighter("Sram", initiative: 20);
        var nextLiving = CreateFighter("Iop", initiative: 10);
        var turns = new TurnManager([active, defeated, nextLiving]);
        defeated.TakeDamage(100);

        var next = turns.PassTurn();

        Assert.Same(nextLiving, next);
        Assert.True(next.IsAlive);
    }

    [Fact]
    public void Constructor_RejectsEmptyQueue()
    {
        Assert.Throws<ArgumentException>(() => new TurnManager([]));
    }

    private static Fighter CreateFighter(string name, int initiative) =>
        new(Guid.NewGuid(), name, 0, 0, actionPoints: 6, movementPoints: 3, hp: 100, initiative);
}
