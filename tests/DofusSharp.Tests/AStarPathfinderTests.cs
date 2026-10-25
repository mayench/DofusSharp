using DofusSharp.Combat.Pathfinding;
using DofusSharp.Domain;

namespace DofusSharp.Tests;

public sealed class AStarPathfinderTests
{
    private readonly AStarPathfinder _pathfinder = new();

    [Fact]
    public void FindPath_OnClearGrid_ReturnsShortestOrthogonalPath()
    {
        var grid = new GameGrid(5, 5);

        var result = _pathfinder.FindPath(grid, 0, 0, 3, 2);

        Assert.NotNull(result);
        Assert.Equal(5, result.MovementCost);
        Assert.Equal((0, 0), (result.Path[0].X, result.Path[0].Y));
        Assert.Equal((3, 2), (result.Path[^1].X, result.Path[^1].Y));
        Assert.All(result.Path.Zip(result.Path.Skip(1)), pair =>
            Assert.Equal(1, GameGrid.ManhattanDistance(pair.First.X, pair.First.Y, pair.Second.X, pair.Second.Y)));
    }

    [Fact]
    public void FindPath_AvoidsObstacleAndReturnsDetour()
    {
        var grid = new GameGrid(5, 3);
        grid.GetCell(1, 0).IsWalkable = false;

        var result = _pathfinder.FindPath(grid, 0, 0, 2, 0);

        Assert.NotNull(result);
        Assert.Equal(4, result.MovementCost);
        Assert.DoesNotContain(result.Path, cell => cell.X == 1 && cell.Y == 0);
    }

    [Fact]
    public void FindPath_AvoidsDynamicallyOccupiedCell()
    {
        var grid = new GameGrid(5, 3);
        var occupiedCells = new HashSet<(int X, int Y)> { (1, 0) };

        var result = _pathfinder.FindPath(grid, 0, 0, 2, 0, occupiedCells);

        Assert.NotNull(result);
        Assert.Equal(4, result.MovementCost);
        Assert.DoesNotContain(result.Path, cell => cell.X == 1 && cell.Y == 0);
    }

    [Fact]
    public void FindPath_ReturnsNullWhenDestinationIsUnreachable()
    {
        var grid = new GameGrid(3, 3);
        grid.GetCell(1, 0).IsWalkable = false;
        grid.GetCell(0, 1).IsWalkable = false;

        Assert.Null(_pathfinder.FindPath(grid, 0, 0, 2, 2));
    }

    [Fact]
    public void CanAfford_UsesPathLengthAsMovementPointCost()
    {
        var grid = new GameGrid(5, 5);
        var path = _pathfinder.FindPath(grid, 0, 0, 3, 2)!;
        var fighter = new Fighter(Guid.NewGuid(), "Cra", 0, 0, 6, 4, 100);

        Assert.False(_pathfinder.CanAfford(fighter, path));

        var fighterWithEnoughMovement = new Fighter(Guid.NewGuid(), "Iop", 0, 0, 6, 5, 100);
        Assert.True(_pathfinder.CanAfford(fighterWithEnoughMovement, path));
        Assert.Equal(5, path.MovementCost);
        Assert.Equal(4, fighter.MovementPoints); // La consulta no descuenta PM.
    }

    [Fact]
    public void FighterMove_ConsumesMovementPointsOnlyWhenAffordable()
    {
        var fighter = new Fighter(Guid.NewGuid(), "Eniripsa", 0, 0, 6, 3, 100);

        Assert.False(fighter.TryMoveTo(2, 0, 4));
        Assert.Equal((0, 0, 3), (fighter.X, fighter.Y, fighter.MovementPoints));
        Assert.True(fighter.TryMoveTo(2, 0, 2));
        Assert.Equal((2, 0, 1), (fighter.X, fighter.Y, fighter.MovementPoints));
    }
}
