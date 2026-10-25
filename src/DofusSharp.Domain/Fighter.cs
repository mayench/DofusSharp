namespace DofusSharp.Domain;

/// <summary>Entidad que participa en un combate por turnos.</summary>
public sealed class Fighter
{
    public Guid Id { get; }
    public string Name { get; }
    public string TeamId { get; }
    public PlayerClass Class { get; }
    public Stats Stats { get; private set; }
    public int Level { get; private set; } = 1;
    /// <summary>Experiencia acumulada dentro del nivel actual.</summary>
    public int Experience { get; private set; }
    public int ExperienceToNextLevel => checked(Level * 100);
    public int AvailableCharacteristicPoints { get; private set; }
    public int Initiative { get; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public int ActionPoints { get; private set; }
    public int MovementPoints { get; private set; }
    public int MaxActionPoints { get; }
    public int MaxMovementPoints { get; }
    public int HP { get; private set; }
    public int MaxHP { get; private set; }
    public bool IsAlive => HP > 0;

    public Fighter(Guid id, string name, int x, int y, int actionPoints, int movementPoints, int hp,
        int initiative = 0, string teamId = "default", PlayerClass playerClass = PlayerClass.None, Stats? stats = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El nombre es obligatorio.", nameof(name));
        if (string.IsNullOrWhiteSpace(teamId)) throw new ArgumentException("El equipo es obligatorio.", nameof(teamId));
        if (actionPoints < 0 || movementPoints < 0 || hp < 0) throw new ArgumentOutOfRangeException(nameof(hp), "Los recursos no pueden ser negativos.");
        Id = id;
        Name = name;
        TeamId = teamId;
        Class = playerClass;
        Stats = stats ?? global::DofusSharp.Domain.Stats.Zero;
        Initiative = initiative;
        X = x;
        Y = y;
        ActionPoints = actionPoints;
        MovementPoints = movementPoints;
        MaxActionPoints = actionPoints;
        MaxMovementPoints = movementPoints;
        MaxHP = checked(hp + Stats.Vitality);
        HP = MaxHP;
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

    /// <summary>Otorga experiencia y procesa todas las subidas de nivel alcanzadas.</summary>
    public int GainExperience(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "La experiencia no puede ser negativa.");
        var pendingExperience = checked(Experience + amount);
        var levelsGained = 0;
        while (pendingExperience >= checked(Level * 100))
        {
            pendingExperience -= checked(Level * 100);
            Level = checked(Level + 1);
            AvailableCharacteristicPoints = checked(AvailableCharacteristicPoints + 5);
            levelsGained++;
        }

        Experience = pendingExperience;
        return levelsGained;
    }

    /// <summary>Invierte un punto disponible en una característica; Vitalidad también aumenta el HP máximo.</summary>
    public bool TryAllocateCharacteristicPoint(CharacteristicAttribute attribute)
    {
        if (AvailableCharacteristicPoints <= 0 || !Enum.IsDefined(attribute)) return false;

        Stats = Stats.AddPoint(attribute);
        AvailableCharacteristicPoints--;
        if (attribute == CharacteristicAttribute.Vitality)
        {
            MaxHP = checked(MaxHP + 1);
            if (IsAlive) HP = Math.Min(MaxHP, checked(HP + 1));
        }

        return true;
    }
}
