using DofusSharp.Domain;
using DofusSharp.Presentation;
using Raylib_cs;

const int screenWidth = 1100;
const int screenHeight = 720;
const float tileWidth = 48f;
const float tileHeight = 24f;

var grid = new GameGrid(14, 20);
// Ejemplos visibles de terreno bloqueado para mostrar ambos estados del tablero.
grid.GetCell(5, 7).IsWalkable = false;
grid.GetCell(5, 7).LineOfSight = false;
grid.GetCell(9, 10).LineOfSight = false;

var renderer = new IsometricGrid(screenWidth / 2f, 145f, tileWidth, tileHeight);
var hoveredX = -1;
var hoveredY = -1;

Raylib.InitWindow(screenWidth, screenHeight, "DofusSharp - Tablero isométrico");
Raylib.SetTargetFPS(60);

while (!Raylib.WindowShouldClose())
{
    var pointer = Raylib.GetMousePosition();
    if (!renderer.TryGetCellAt(grid, pointer, out hoveredX, out hoveredY))
    {
        hoveredX = -1;
        hoveredY = -1;
    }

    Raylib.BeginDrawing();
    Raylib.ClearBackground(new Color(22, 27, 32, 255));
    renderer.Draw(grid, hoveredX, hoveredY);

    Raylib.DrawText("DOFUSSHARP  /  TABLERO TACTICO", 24, 22, 22, new Color(234, 226, 201, 255));
    Raylib.DrawText("Verde: transitable   Rojo: bloquea movimiento   Azul: bloquea LoS   Morado: ambos", 24, 52, 16,
        new Color(183, 191, 184, 255));
    var hoverText = hoveredX >= 0
        ? $"Celda activa: ({hoveredX}, {hoveredY})"
        : "Celda activa: fuera del tablero";
    Raylib.DrawText(hoverText, 24, screenHeight - 38, 18, new Color(236, 190, 73, 255));
    Raylib.EndDrawing();
}

Raylib.CloseWindow();
