using DofusSharp.Combat.State;
using DofusSharp.Combat.LineOfSight;
using DofusSharp.Combat.Pathfinding;
using DofusSharp.Domain;

namespace DofusSharp.Tests;

public sealed class BattleStateServiceTests
{
    [Fact]
    public void CheckBattleState_ReturnsWinningTeamWhenOnlyOneTeamHasLivingFighters()
    {
        var blue = CreateFighter("Cra", "blue");
        var red = CreateFighter("Iop", "red");
        red.TakeDamage(100);
        var service = new BattleStateService(new GameGrid());

        var result = service.CheckBattleState([blue, red]);

        Assert.Equal(BattleStatus.Victory, result.Status);
        Assert.Equal("blue", result.WinningTeamId);
        Assert.True(result.IsFinished);
    }

    [Fact]
    public void CheckBattleState_ReturnsInProgressWhileTwoTeamsRemain()
    {
        var service = new BattleStateService(new GameGrid());

        var result = service.CheckBattleState([CreateFighter("Cra", "blue"), CreateFighter("Iop", "red")]);

        Assert.Equal(BattleStatus.InProgress, result.Status);
        Assert.Null(result.WinningTeamId);
        Assert.False(result.IsFinished);
    }

    [Fact]
    public void CheckBattleState_ReturnsDrawWhenNoFightersRemainAlive()
    {
        var blue = CreateFighter("Cra", "blue");
        var red = CreateFighter("Iop", "red");
        blue.TakeDamage(100);
        red.TakeDamage(100);

        var result = new BattleStateService(new GameGrid()).CheckBattleState([blue, red]);

        Assert.Equal(BattleStatus.Draw, result.Status);
        Assert.Null(result.WinningTeamId);
    }

    [Fact]
    public void ApplyDamage_ReleasesDefeatedFightersCellForMovementAndVision()
    {
        var grid = new GameGrid(3, 3);
        grid.GetCell(1, 1).IsWalkable = false;
        grid.GetCell(1, 1).LineOfSight = false;
        var fighter = new Fighter(Guid.NewGuid(), "Sram", 1, 1, 6, 3, hp: 10, teamId: "red");

        new BattleStateService(grid).ApplyDamage(fighter, 10);

        Assert.False(fighter.IsAlive);
        Assert.True(grid.GetCell(1, 1).IsWalkable);
        Assert.True(grid.GetCell(1, 1).LineOfSight);
        Assert.NotNull(new AStarPathfinder().FindPath(grid, 0, 1, 2, 1));
        Assert.True(new BresenhamLineOfSightService().HasLineOfSight(grid, 0, 1, 2, 1));
    }

    private static Fighter CreateFighter(string name, string teamId) =>
        new(Guid.NewGuid(), name, 1, 1, 6, 3, hp: 100, teamId: teamId);
}
