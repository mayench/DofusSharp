namespace DofusSharp.Domain;

/// <summary>Entidad que participa en un combate por turnos.</summary>
public sealed class Fighter
{
    public Guid Id { get; }
    public string Name { get; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public int ActionPoints { get; private set; }
    public int MovementPoints { get; private set; }
    public int HP { get; private set; }

    public Fighter(Guid id, string name, int x, int y, int actionPoints, int movementPoints, int hp)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El nombre es obligatorio.", nameof(name));
        if (actionPoints < 0 || movementPoints < 0 || hp < 0) throw new ArgumentOutOfRangeException(nameof(hp), "Los recursos no pueden ser negativos.");
        Id = id;
        Name = name;
        X = x;
        Y = y;
        ActionPoints = actionPoints;
        MovementPoints = movementPoints;
        HP = hp;
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
}
