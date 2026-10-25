using DofusSharp.Domain;

namespace DofusSharp.GameState.Turns;

/// <summary>Gestiona la iniciativa y el avance circular de turnos de una lista fija de combatientes.</summary>
public sealed class TurnManager
{
    private readonly IReadOnlyList<Fighter> _fighters;
    private int _currentIndex;

    public Fighter CurrentFighter => _fighters[_currentIndex];
    public int RoundNumber { get; private set; } = 1;
    public int TurnNumber { get; private set; }

    /// <summary>Se emite al entrar en el turno de un combatiente (después de refrescar sus recursos).</summary>
    public event Action<Fighter>? TurnStarted;

    public TurnManager(IEnumerable<Fighter> fighters)
    {
        ArgumentNullException.ThrowIfNull(fighters);
        var input = fighters.ToArray();
        if (input.Length == 0) throw new ArgumentException("Debe haber al menos un combatiente.", nameof(fighters));
        if (input.Any(fighter => fighter is null)) throw new ArgumentException("La cola no puede contener combatientes nulos.", nameof(fighters));
        if (input.Select(fighter => fighter.Id).Distinct().Count() != input.Length)
            throw new ArgumentException("Cada combatiente debe tener un identificador único.", nameof(fighters));

        // OrderBy es estable: en caso de empate se conserva el orden de entrada.
        _fighters = input.OrderByDescending(fighter => fighter.Initiative).ToArray();
    }

    /// <summary>Pasa el turno al siguiente combatiente, refrescando sus PA y PM.</summary>
    public Fighter PassTurn()
    {
        _currentIndex++;
        if (_currentIndex == _fighters.Count)
        {
            _currentIndex = 0;
            RoundNumber++;
        }

        TurnNumber++;
        var nextFighter = CurrentFighter;
        nextFighter.RefreshTurnResources();
        TurnStarted?.Invoke(nextFighter);
        return nextFighter;
    }
}
