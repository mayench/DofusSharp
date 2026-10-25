using DofusSharp.GameState.Maps;

namespace DofusSharp.Tests;

public sealed class JsonMapLoaderTests
{
    private const string ExampleMapJson = """
        {
          "width": 4,
          "height": 3,
          "cells": [
            { "x": 1, "y": 1, "isWalkable": false, "lineOfSight": false },
            { "x": 2, "y": 1, "isWalkable": true, "lineOfSight": false }
          ],
          "spawns": [
            { "id": "10000000-0000-0000-0000-000000000001", "name": "Cra", "teamId": "blue", "x": 0, "y": 0, "actionPoints": 6, "movementPoints": 5, "hp": 100, "initiative": 20 },
            { "id": "10000000-0000-0000-0000-000000000002", "name": "Iop", "teamId": "red", "x": 3, "y": 2, "actionPoints": 6, "movementPoints": 4, "hp": 120, "initiative": 10 }
          ]
        }
        """;

    [Fact]
    public void LoadFromJson_DeserializesDimensionsAndCreatesSpawnedFighters()
    {
        var map = new JsonMapLoader().LoadFromJson(ExampleMapJson);

        Assert.Equal(4, map.Grid.Width);
        Assert.Equal(3, map.Grid.Height);
        Assert.Equal(2, map.Fighters.Count);
        Assert.Equal("Cra", map.Fighters[0].Name);
        Assert.Equal("blue", map.Fighters[0].TeamId);
        Assert.Equal((0, 0), (map.Fighters[0].X, map.Fighters[0].Y));
        Assert.Equal(5, map.Fighters[0].MovementPoints);
        Assert.Equal(20, map.Fighters[0].Initiative);
        Assert.Equal("Iop", map.Fighters[1].Name);
    }

    [Fact]
    public void LoadFromJson_AppliesWalkabilityAndLineOfSightOverrides()
    {
        var map = new JsonMapLoader().LoadFromJson(ExampleMapJson);

        var movementAndSightBlocker = map.Grid.GetCell(1, 1);
        Assert.False(movementAndSightBlocker.IsWalkable);
        Assert.False(movementAndSightBlocker.LineOfSight);

        var sightOnlyBlocker = map.Grid.GetCell(2, 1);
        Assert.True(sightOnlyBlocker.IsWalkable);
        Assert.False(sightOnlyBlocker.LineOfSight);
        Assert.True(map.Grid.GetCell(0, 1).IsWalkable);
        Assert.True(map.Grid.GetCell(0, 1).LineOfSight);
    }

    [Fact]
    public void LoadFromJson_RejectsSpawnOnNonWalkableCell()
    {
        const string json = """
            { "width": 2, "height": 2,
              "cells": [{ "x": 1, "y": 1, "isWalkable": false, "lineOfSight": false }],
              "spawns": [{ "name": "Cra", "teamId": "blue", "x": 1, "y": 1 }] }
            """;

        Assert.Throws<InvalidDataException>(() => new JsonMapLoader().LoadFromJson(json));
    }
}
