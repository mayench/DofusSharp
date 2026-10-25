using DofusSharp.Domain;

namespace DofusSharp.Combat.State;

public enum BattleStatus
{
    InProgress,
    Victory,
    Draw
}

public sealed record BattleResult(BattleStatus Status, string? WinningTeamId)
{
    public bool IsFinished => Status != BattleStatus.InProgress;
}

/// <summary>Centraliza daño con liberación de celdas y determina victoria, empate o combate en curso.</summary>
public sealed class BattleStateService
{
    private readonly GameGrid _grid;
    internal GameGrid Grid => _grid;

    public BattleStateService(GameGrid grid) =>
        _grid = grid ?? throw new ArgumentNullException(nameof(grid));

    /// <summary>Aplica daño y, al derrotar al objetivo, limpia el bloqueo que ocupaba en el tablero.</summary>
    public void ApplyDamage(Fighter target, int amount)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "El daño no puede ser negativo.");

        if (target.IsAlive) target.TakeDamage(amount);
        if (target.IsAlive) return;
        if (!_grid.Contains(target.X, target.Y))
            throw new InvalidOperationException($"La posición de {target.Name} queda fuera del tablero.");

        var freedCell = _grid.GetCell(target.X, target.Y);
        freedCell.IsWalkable = true;
        freedCell.LineOfSight = true;
    }

    /// <summary>El equipo del último combatiente vivo gana; si no queda ninguno, el resultado es empate.</summary>
    public BattleResult CheckBattleState(IEnumerable<Fighter> fighters)
    {
        ArgumentNullException.ThrowIfNull(fighters);
        var livingTeams = fighters.Where(fighter => fighter.IsAlive)
            .Select(fighter => fighter.TeamId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return livingTeams.Length switch
        {
            0 => new BattleResult(BattleStatus.Draw, null),
            1 => new BattleResult(BattleStatus.Victory, livingTeams[0]),
            _ => new BattleResult(BattleStatus.InProgress, null)
        };
    }
}
