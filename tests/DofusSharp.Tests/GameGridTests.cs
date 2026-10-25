using DofusSharp.Domain;

namespace DofusSharp.Tests;

public sealed class GameGridTests
{
    [Fact]
    public void Constructor_CreatesDefault14By20WalkableGrid()
    {
        var grid = new GameGrid();

        Assert.Equal(14, grid.Width);
        Assert.Equal(20, grid.Height);
        Assert.True(grid.GetCell(13, 19).IsWalkable);
        Assert.True(grid.GetCell(0, 0).LineOfSight);
    }

    [Fact]
    public void GetCell_RejectsCoordinatesOutsideGrid()
    {
        var grid = new GameGrid(3, 4);

        Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetCell(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetCell(3, 0));
    }

    [Theory]
    [InlineData(0, 0, 3, 4, 7)]
    [InlineData(5, 2, 2, 6, 7)]
    public void ManhattanDistance_ReturnsOrthogonalStepCount(int x1, int y1, int x2, int y2, int expected) =>
        Assert.Equal(expected, GameGrid.ManhattanDistance(x1, y1, x2, y2));

    [Fact]
    public void StraightLineDistance_ReturnsEuclideanDistance()
    {
        Assert.Equal(5d, GameGrid.StraightLineDistance(0, 0, 3, 4));
    }
}
