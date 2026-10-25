using System.Numerics;
using DofusSharp.Combat.Pathfinding;
using DofusSharp.Domain;
using DofusSharp.GameState.Turns;
using DofusSharp.Presentation;
using Raylib_cs;

const int screenWidth = 1280;
const int screenHeight = 800;
const float tileWidth = 48f;
const float tileHeight = 24f;

var grid = new GameGrid(14, 20);
// Terreno de ejemplo para mostrar bloqueos de movimiento y de visión.
grid.GetCell(5, 7).IsWalkable = false;
grid.GetCell(5, 7).LineOfSight = false;
grid.GetCell(9, 10).LineOfSight = false;

var fighters = new List<Fighter>
{
    new(Guid.NewGuid(), "Cra", 2, 4, actionPoints: 6, movementPoints: 5, hp: 100, initiative: 20),
    new(Guid.NewGuid(), "Iop", 10, 14, actionPoints: 6, movementPoints: 4, hp: 120, initiative: 10)
};
var turnManager = new TurnManager(fighters);
var pathfinder = new AStarPathfinder();
var renderer = new IsometricGrid(screenWidth / 2f, 150f, tileWidth, tileHeight);
FighterMovementAnimation? movement = null;
var hoveredX = -1;
var hoveredY = -1;

Raylib.InitWindow(screenWidth, screenHeight, "DofusSharp - Movimiento táctico");
Raylib.SetTargetFPS(60);

while (!Raylib.WindowShouldClose())
{
    PathResult? previewPath = null;
    hoveredX = -1;
    hoveredY = -1;

    if (movement is null && renderer.TryGetCellAt(grid, Raylib.GetMousePosition(), out var cellX, out var cellY))
    {
        var activeFighter = turnManager.CurrentFighter;
        var candidate = FindPathFor(activeFighter, cellX, cellY);
        if (candidate is not null && pathfinder.CanAfford(activeFighter, candidate))
        {
            previewPath = candidate;
            hoveredX = cellX;
            hoveredY = cellY;

            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && candidate.MovementCost > 0)
                movement = new FighterMovementAnimation(activeFighter, candidate);
        }
    }

    if (movement is null && Raylib.IsKeyPressed(KeyboardKey.Space))
        turnManager.PassTurn();

    if (movement is not null)
    {
        movement.Update(Raylib.GetFrameTime());
        if (movement.IsComplete) movement = null;
    }

    var currentFighter = turnManager.CurrentFighter;
    IReadOnlySet<(int X, int Y)> pathCells = movement?.HighlightedCells ??
        (previewPath is null
            ? new HashSet<(int X, int Y)>()
            : previewPath.Path.Skip(1).Select(cell => (cell.X, cell.Y)).ToHashSet());

    Raylib.BeginDrawing();
    Raylib.ClearBackground(new Color(22, 27, 32, 255));
    renderer.Draw(grid, hoveredX, hoveredY, pathCells);

    foreach (var fighter in fighters.OrderBy(entity => entity.X + entity.Y))
    {
        var position = movement is not null && fighter == currentFighter
            ? movement.GetVisualPosition(renderer)
            : renderer.GridToScreen(fighter.X, fighter.Y);
        renderer.DrawFighter(position, fighter, fighter == currentFighter);
    }

    DrawHud(currentFighter, turnManager.RoundNumber, screenHeight);
    var hoverText = hoveredX >= 0
        ? $"Destino: ({hoveredX}, {hoveredY})   Coste: {previewPath!.MovementCost} PM"
        : "Selecciona una celda alcanzable con el ratón";
    Raylib.DrawText(hoverText, 24, screenHeight - 34, 18, new Color(236, 190, 73, 255));
    Raylib.DrawText("Clic: mover   ESPACIO: finalizar turno", 24, screenHeight - 62, 16,
        new Color(183, 191, 184, 255));
    Raylib.EndDrawing();
}

Raylib.CloseWindow();

PathResult? FindPathFor(Fighter fighter, int targetX, int targetY)
{
    var blockedCells = fighters
        .Where(other => other != fighter)
        .Select(other => (other.X, other.Y))
        .ToHashSet();
    return pathfinder.FindPath(grid, fighter.X, fighter.Y, targetX, targetY, blockedCells);
}

static void DrawHud(Fighter fighter, int roundNumber, int windowHeight)
{
    const int panelX = 970;
    const int panelY = 150;
    const int panelWidth = 282;
    const int panelHeight = 284;
    var background = new Color(34, 42, 46, 255);
    var foreground = new Color(234, 226, 201, 255);

    Raylib.DrawRectangle(panelX, panelY, panelWidth, panelHeight, background);
    Raylib.DrawRectangleLines(panelX, panelY, panelWidth, panelHeight, new Color(91, 113, 104, 255));
    Raylib.DrawText("PERSONAJE ACTIVO", panelX + 18, panelY + 18, 16, new Color(236, 190, 73, 255));
    Raylib.DrawText(fighter.Name, panelX + 18, panelY + 48, 28, foreground);
    Raylib.DrawText($"Vida: {fighter.HP}", panelX + 18, panelY + 96, 20, foreground);
    Raylib.DrawText($"PA: {fighter.ActionPoints}/{fighter.MaxActionPoints}", panelX + 18, panelY + 132, 20, foreground);
    Raylib.DrawText($"PM: {fighter.MovementPoints}/{fighter.MaxMovementPoints}", panelX + 18, panelY + 168, 20, foreground);
    Raylib.DrawText($"Posición: ({fighter.X}, {fighter.Y})", panelX + 18, panelY + 204, 16, foreground);
    Raylib.DrawText($"Ronda: {roundNumber}", panelX + 18, panelY + 232, 16, foreground);
    Raylib.DrawText("ESPACIO  ·  FINALIZAR TURNO", panelX, windowHeight - 40, 14,
        new Color(183, 191, 184, 255));
}
