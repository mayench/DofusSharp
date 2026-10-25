using DofusSharp.Combat.Pathfinding;
using DofusSharp.Combat.Spells;
using DofusSharp.Domain;

namespace DofusSharp.Combat.AI;

/// <summary>
/// Planifica turnos sencillos para combatientes no controlados por el jugador: busca al rival
/// más cercano, avanza usando A* y después intenta lanzar el hechizo válido más dañino.
/// </summary>
public sealed class EnemyAiService
{
    private readonly GameGrid _grid;
    private readonly AStarPathfinder _pathfinder;
    private readonly SpellCastingService _spellCasting;
    private readonly string _playerTeamId;

    public EnemyAiService(GameGrid grid, AStarPathfinder pathfinder, SpellCastingService spellCasting,
        string playerTeamId = "blue")
    {
        _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        _pathfinder = pathfinder ?? throw new ArgumentNullException(nameof(pathfinder));
        _spellCasting = spellCasting ?? throw new ArgumentNullException(nameof(spellCasting));
        if (string.IsNullOrWhiteSpace(playerTeamId))
            throw new ArgumentException("El equipo del jugador es obligatorio.", nameof(playerTeamId));
        _playerTeamId = playerTeamId;
    }

    /// <summary>Selecciona el rival más cercano y calcula el destino alcanzable más próximo a él.</summary>
    public EnemyTurnPlan? PlanTurn(Fighter actor, IEnumerable<Fighter> fighters)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(fighters);
        if (!actor.IsAlive || actor.TeamId == _playerTeamId) return null;

        var allFighters = fighters.Where(fighter => fighter.IsAlive).ToArray();
        if (!allFighters.Contains(actor))
            throw new ArgumentException("El combatiente activo debe formar parte de la lista.", nameof(fighters));

        var target = allFighters
            .Where(fighter => fighter.TeamId == _playerTeamId)
            .OrderBy(fighter => GameGrid.ManhattanDistance(actor.X, actor.Y, fighter.X, fighter.Y))
            .ThenBy(fighter => fighter.Id)
            .FirstOrDefault();
        if (target is null) return null;

        var occupiedCells = allFighters
            .Where(fighter => fighter != actor)
            .Select(fighter => (fighter.X, fighter.Y))
            .ToHashSet();

        // Entre las celdas alcanzables dentro de los PM actuales, se elige la más cercana al rival.
        // Si varias progresan igual, se conserva el camino más corto para no gastar PM lateralmente.
        PathResult? bestPath = null;
        var bestDistance = GameGrid.ManhattanDistance(actor.X, actor.Y, target.X, target.Y);
        for (var x = 0; x < _grid.Width; x++)
        for (var y = 0; y < _grid.Height; y++)
        {
            if (!_grid.GetCell(x, y).IsWalkable || occupiedCells.Contains((x, y))) continue;
            if (GameGrid.ManhattanDistance(actor.X, actor.Y, x, y) > actor.MovementPoints) continue;
            var distance = GameGrid.ManhattanDistance(x, y, target.X, target.Y);
            if (distance > bestDistance) continue;

            var path = _pathfinder.FindPath(_grid, actor.X, actor.Y, x, y, occupiedCells);
            if (path is null || path.MovementCost > actor.MovementPoints) continue;
            if (distance < bestDistance || bestPath is null || path.MovementCost < bestPath.MovementCost)
            {
                bestDistance = distance;
                bestPath = path;
            }
        }

        return new EnemyTurnPlan(actor, target, bestPath);
    }

    /// <summary>
    /// Tras completar (o animar) el movimiento planificado, prueba los hechizos disponibles
    /// contra el objetivo; el servicio de lanzamiento vuelve a validar alcance, LoS, PA y cooldown.
    /// </summary>
    public EnemyTurnResult ResolveAttack(EnemyTurnPlan plan, IReadOnlyList<Spell> availableSpells)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(availableSpells);

        if (!plan.Actor.IsAlive || !plan.Target.IsAlive)
            return new EnemyTurnResult(plan.Target, null, null);

        foreach (var spell in availableSpells.OrderByDescending(spell => spell.Damage)
                     .ThenBy(spell => spell.ActionPointCost))
        {
            if (_spellCasting.CanCastAt(plan.Actor, plan.Target.X, plan.Target.Y, spell) != SpellCastResult.Success)
                continue;

            var result = _spellCasting.TryCast(plan.Actor, plan.Target, spell);
            return new EnemyTurnResult(plan.Target, spell, result);
        }

        return new EnemyTurnResult(plan.Target, null, null);
    }
}
