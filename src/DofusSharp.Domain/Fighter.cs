namespace DofusSharp.Domain;

/// <summary>Entidad que participa en un combate por turnos.</summary>
public sealed class Fighter
{
    public Guid Id { get; }
    public string Name { get; }
    public int Initiative { get; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public int ActionPoints { get; private set; }
    public int MovementPoints { get; private set; }
    public int MaxActionPoints { get; }
    public int MaxMovementPoints { get; }
    public int HP { get; private set; }
    public int MaxHP { get; }
    public bool IsAlive => HP > 0;

    public Fighter(Guid id, string name, int x, int y, int actionPoints, int movementPoints, int hp, int initiative = 0)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El nombre es obligatorio.", nameof(name));
        if (actionPoints < 0 || movementPoints < 0 || hp < 0) throw new ArgumentOutOfRangeException(nameof(hp), "Los recursos no pueden ser negativos.");
        Id = id;
        Name = name;
        Initiative = initiative;
        X = x;
        Y = y;
        ActionPoints = actionPoints;
        MovementPoints = movementPoints;
        MaxActionPoints = actionPoints;
        MaxMovementPoints = movementPoints;
        HP = hp;
        MaxHP = hp;
    }

    /// <summary>Aplica un movimiento ya validado por el motor de cálculo.</summary>
    public bool TryMoveTo(int x, int y, int movementCost)
    {
        if (movementCost < 0 || movementCost > MovementPoints) return false;
        X = x;
        Y = y;
        MovementPoints -= movementCost;
        return true;
    }

    /// <summary>Descuenta PA solo si el combatiente puede cubrir el coste.</summary>
    public bool TrySpendActionPoints(int cost)
    {
        if (cost < 0 || cost > ActionPoints) return false;
        ActionPoints -= cost;
        return true;
    }

    /// <summary>Aplica daño sin permitir que los puntos de vida queden por debajo de cero.</summary>
    public void TakeDamage(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "El daño no puede ser negativo.");
        HP = Math.Max(0, HP - amount);
    }

    /// <summary>Restaura PA y PM a los valores máximos al comenzar el turno.</summary>
    public void RefreshTurnResources()
    {
        ActionPoints = MaxActionPoints;
        MovementPoints = MaxMovementPoints;
    }
}
