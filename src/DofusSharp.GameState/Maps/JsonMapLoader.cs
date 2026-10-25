using System.Text.Json;
using System.Text.Json.Serialization;
using DofusSharp.Domain;

namespace DofusSharp.GameState.Maps;

/// <summary>Deserializa mapas JSON y traduce los DTO a objetos de dominio validados.</summary>
public sealed class JsonMapLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Carga un mapa desde disco. Los errores de lectura mantienen su excepción original.</summary>
    public LoadedMap LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var json = File.ReadAllText(path);
        return LoadFromJson(json);
    }

    /// <summary>Deserializa, valida y crea la cuadrícula y los combatientes del mapa.</summary>
    public LoadedMap LoadFromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        var definition = JsonSerializer.Deserialize<MapDefinitionDto>(json, SerializerOptions)
            ?? throw new InvalidDataException("El archivo JSON no contiene una definición de mapa.");

        if (definition.Width <= 0 || definition.Height <= 0)
            throw new InvalidDataException("El mapa debe tener ancho y alto mayores que cero.");

        var cellDefinitions = definition.Cells
            ?? throw new InvalidDataException("La lista de celdas no puede ser null.");
        var spawnDefinitions = definition.Spawns
            ?? throw new InvalidDataException("La lista de spawns no puede ser null.");

        var grid = new GameGrid(definition.Width, definition.Height);
        var definedCells = new HashSet<(int X, int Y)>();
        foreach (var cell in cellDefinitions)
        {
            if (!grid.Contains(cell.X, cell.Y))
                throw new InvalidDataException($"La celda ({cell.X}, {cell.Y}) está fuera del mapa.");
            if (!definedCells.Add((cell.X, cell.Y)))
                throw new InvalidDataException($"La celda ({cell.X}, {cell.Y}) está definida más de una vez.");

            var gridCell = grid.GetCell(cell.X, cell.Y);
            gridCell.IsWalkable = cell.IsWalkable;
            gridCell.LineOfSight = cell.LineOfSight;
        }

        var fighters = new List<Fighter>(spawnDefinitions.Count);
        var occupiedSpawns = new HashSet<(int X, int Y)>();
        foreach (var spawn in spawnDefinitions)
        {
            if (!grid.Contains(spawn.X, spawn.Y))
                throw new InvalidDataException($"El spawn de {spawn.Name} ({spawn.X}, {spawn.Y}) está fuera del mapa.");
            if (!grid.GetCell(spawn.X, spawn.Y).IsWalkable)
                throw new InvalidDataException($"El spawn de {spawn.Name} está en una celda no transitable.");
            if (!occupiedSpawns.Add((spawn.X, spawn.Y)))
                throw new InvalidDataException($"Hay más de un combatiente en el spawn ({spawn.X}, {spawn.Y}).");

            var id = string.IsNullOrWhiteSpace(spawn.Id) ? Guid.NewGuid() : ParseId(spawn.Id);
            fighters.Add(new Fighter(id, spawn.Name, spawn.X, spawn.Y, spawn.ActionPoints,
                spawn.MovementPoints, spawn.HP, spawn.Initiative, spawn.TeamId, spawn.PlayerClass,
                spawn.Stats?.ToDomain() ?? throw new InvalidDataException($"Los atributos de {spawn.Name} no pueden ser null.")));
        }

        if (fighters.Select(fighter => fighter.Id).Distinct().Count() != fighters.Count)
            throw new InvalidDataException("Los identificadores de combatiente deben ser únicos.");

        return new LoadedMap(grid, fighters);
    }

    private static Guid ParseId(string value) => Guid.TryParse(value, out var id) && id != Guid.Empty
        ? id
        : throw new InvalidDataException($"El identificador de combatiente '{value}' no es un GUID válido.");
}
