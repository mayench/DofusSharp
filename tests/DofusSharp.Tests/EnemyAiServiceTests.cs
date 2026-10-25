using DofusSharp.Combat.AI;
using DofusSharp.Combat.Pathfinding;
using DofusSharp.Combat.Spells;
using DofusSharp.Domain;

namespace DofusSharp.Tests;

public sealed class EnemyAiServiceTests
{
    [Fact]
    public void PlanTurn_SelectsNearestPlayerAndPlansMovementWithinAvailablePoints()
    {
        var grid = new GameGrid(8, 5);
        var enemy = CreateFighter("Goblin", "red", 1, 2, movementPoints: 3);
        var farPlayer = CreateFighter("Cra distante", "blue", 7, 2);
        var nearPlayer = CreateFighter("Cra cercano", "blue", 4, 2);
        var ai = CreateAi(grid);

        var plan = ai.PlanTurn(enemy, [enemy, farPlayer, nearPlayer]);

        Assert.NotNull(plan);
        Assert.Same(nearPlayer, plan.Target);
        Assert.NotNull(plan.MovementPath);
        Assert.InRange(plan.MovementPath.MovementCost, 0, enemy.MovementPoints);
        Assert.True(GameGrid.ManhattanDistance(plan.MovementPath.Path[^1].X, plan.MovementPath.Path[^1].Y,
            nearPlayer.X, nearPlayer.Y) < GameGrid.ManhattanDistance(enemy.X, enemy.Y, nearPlayer.X, nearPlayer.Y));
    }

    [Fact]
    public void PlanTurn_UsesAStarDetourAroundObstacle()
    {
        var grid = new GameGrid(5, 3);
        grid.GetCell(2, 1).IsWalkable = false;
        var enemy = CreateFighter("Goblin", "red", 0, 1, movementPoints: 4);
        var player = CreateFighter("Cra", "blue", 4, 1);

        var plan = CreateAi(grid).PlanTurn(enemy, [enemy, player]);

        Assert.NotNull(plan?.MovementPath);
        Assert.True(plan.MovementPath.MovementCost > 1);
        Assert.DoesNotContain(plan.MovementPath.Path, cell => cell.X == 2 && cell.Y == 1);
        Assert.InRange(plan.MovementPath.MovementCost, 0, enemy.MovementPoints);
    }

    [Fact]
    public void ResolveAttack_AutomaticallyCastsBestValidSpellAfterMovement()
    {
        var grid = new GameGrid(6, 3);
        var enemy = CreateFighter("Goblin", "red", 0, 1, actionPoints: 6, movementPoints: 3);
        var player = CreateFighter("Cra", "blue", 4, 1, hp: 80);
        var spellCasting = new SpellCastingService(grid);
        var ai = new EnemyAiService(grid, new AStarPathfinder(), spellCasting);
        var spells = new[]
        {
            new Spell("weak", "Golpe débil", 2, 1, 3, 10, 0),
            new Spell("strong", "Golpe fuerte", 4, 1, 3, 25, 1)
        };

        var plan = ai.PlanTurn(enemy, [enemy, player]);
        Assert.NotNull(plan?.MovementPath);
        foreach (var cell in plan.MovementPath.Path.Skip(1))
            Assert.True(enemy.TryMoveTo(cell.X, cell.Y, 1));

        var result = ai.ResolveAttack(plan, spells);

        Assert.True(result.CastSpell);
        Assert.Equal("strong", result.Spell?.Id);
        Assert.Equal(55, player.HP);
        Assert.Equal(2, enemy.ActionPoints);
    }

    private static EnemyAiService CreateAi(GameGrid grid) =>
        new(grid, new AStarPathfinder(), new SpellCastingService(grid));

    private static Fighter CreateFighter(string name, string team, int x, int y, int actionPoints = 6,
        int movementPoints = 3, int hp = 100) =>
        new(Guid.NewGuid(), name, x, y, actionPoints, movementPoints, hp, teamId: team);
}
