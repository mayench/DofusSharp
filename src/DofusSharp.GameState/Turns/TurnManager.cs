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
        var ordered = input.OrderByDescending(fighter => fighter.Initiative).ToArray();
        _currentIndex = Array.FindIndex(ordered, fighter => fighter.IsAlive);
        if (_currentIndex < 0) throw new ArgumentException("Debe haber al menos un combatiente vivo.", nameof(fighters));
        _fighters = ordered;
    }

    /// <summary>Pasa al siguiente combatiente vivo, saltando los derrotados y refrescando sus PA/PM.</summary>
    public Fighter PassTurn()
    {
        for (var attempts = 0; attempts < _fighters.Count; attempts++)
        {
            _currentIndex++;
            if (_currentIndex == _fighters.Count)
            {
                _currentIndex = 0;
                RoundNumber++;
            }

            var nextFighter = _fighters[_currentIndex];
            if (!nextFighter.IsAlive) continue;

            TurnNumber++;
            nextFighter.RefreshTurnResources();
            TurnStarted?.Invoke(nextFighter);
            return nextFighter;
        }

        throw new InvalidOperationException("No quedan combatientes vivos para iniciar otro turno.");
    }
}
