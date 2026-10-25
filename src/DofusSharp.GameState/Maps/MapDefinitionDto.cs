namespace DofusSharp.GameState.Maps;

/// <summary>Representación serializable de un mapa, independiente de las entidades de dominio.</summary>
public sealed class MapDefinitionDto
{
    public int Width { get; init; }
    public int Height { get; init; }
    public List<MapCellDto>? Cells { get; init; } = [];
    public List<FighterSpawnDto>? Spawns { get; init; } = [];
}

/// <summary>Overrides de transitabilidad y visibilidad para una celda del mapa.</summary>
public sealed class MapCellDto
{
    public int X { get; init; }
    public int Y { get; init; }
    public bool IsWalkable { get; init; } = true;
    public bool LineOfSight { get; init; } = true;
}

/// <summary>Datos mínimos para crear un combatiente en el punto de aparición.</summary>
public sealed class FighterSpawnDto
{
    public string? Id { get; init; }
    public string Name { get; init; } = "";
    public string TeamId { get; init; } = "";
    public int X { get; init; }
    public int Y { get; init; }
    public int ActionPoints { get; init; } = 6;
    public int MovementPoints { get; init; } = 3;
    public int HP { get; init; } = 100;
    public int Initiative { get; init; }
}
