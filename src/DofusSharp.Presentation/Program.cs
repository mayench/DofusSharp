using System.Numerics;
using DofusSharp.Combat.AI;
using DofusSharp.Combat.LineOfSight;
using DofusSharp.Combat.Pathfinding;
using DofusSharp.Combat.Progression;
using DofusSharp.Combat.Spells;
using DofusSharp.Combat.State;
using DofusSharp.Domain;
using DofusSharp.GameState.Maps;
using DofusSharp.GameState.Turns;
using DofusSharp.Presentation;
using Raylib_cs;

const int screenWidth = 1280;
const int screenHeight = 800;
const float tileWidth = 48f;
const float tileHeight = 24f;
const string playerTeamId = "blue";

var mapPath = Path.Combine(AppContext.BaseDirectory, "Maps", "arena.json");
var loadedMap = new JsonMapLoader().LoadFromFile(mapPath);
var grid = loadedMap.Grid;
var fighters = loadedMap.Fighters.ToList();
var turnManager = new TurnManager(fighters);
var pathfinder = new AStarPathfinder();
var battleState = new BattleStateService(grid);
var spellCasting = new SpellCastingService(grid, new BresenhamLineOfSightService(), battleState);
turnManager.TurnStarted += spellCasting.NotifyTurnStarted;
var enemyAi = new EnemyAiService(grid, pathfinder, spellCasting, playerTeamId);
IReadOnlyList<Spell> spells = ClassSpellKitProvider.GetKit(turnManager.CurrentFighter.Class);
var experienceService = new BattleExperienceService();
var renderer = new IsometricGrid(screenWidth / 2f, 150f, tileWidth, tileHeight);
FighterMovementAnimation? movement = null;
Spell? selectedSpell = null;
var feedback = "";
var hoveredX = -1;
var hoveredY = -1;
var battleResult = battleState.CheckBattleState(fighters);
EnemyTurnPlan? pendingEnemyTurn = null;
var enemyTurnDelay = 0f;
var enemyTurnPlanned = false;

Raylib.InitWindow(screenWidth, screenHeight, "DofusSharp - Movimiento táctico");
Raylib.SetTargetFPS(60);

while (!Raylib.WindowShouldClose())
{
    battleResult = battleState.CheckBattleState(fighters);
    var rewardedFighters = experienceService.AwardVictoryExperience(battleResult, fighters, playerTeamId);
    if (rewardedFighters > 0)
        feedback = $"¡Victoria! {rewardedFighters} combatiente(s) reciben 100 XP.";
    if (battleResult.Status == BattleStatus.InProgress && !turnManager.CurrentFighter.IsAlive)
        turnManager.PassTurn();
    spells = ClassSpellKitProvider.GetKit(turnManager.CurrentFighter.Class);

    // La recompensa se entrega al finalizar el combate; los puntos se invierten desde el overlay.
    var progressionFighter = fighters.FirstOrDefault(fighter => fighter.IsAlive && fighter.TeamId == playerTeamId);
    if (battleResult.IsFinished && progressionFighter is not null && TryGetCharacteristicInput(out var attribute))
    {
        feedback = progressionFighter.TryAllocateCharacteristicPoint(attribute)
            ? $"{attribute} aumentada. Puntos disponibles: {progressionFighter.AvailableCharacteristicPoints}."
            : "No hay puntos de característica disponibles.";
    }

    PathResult? previewPath = null;
    var spellTargetCells = new HashSet<(int X, int Y)>();
    hoveredX = -1;
    hoveredY = -1;

    if (movement is null && !battleResult.IsFinished)
    {
        var activeFighter = turnManager.CurrentFighter;
        if (activeFighter.TeamId != playerTeamId)
        {
            // Se deja una pausa antes de actuar para que el cambio de iniciativa sea perceptible.
            if (!enemyTurnPlanned)
            {
                pendingEnemyTurn = enemyAi.PlanTurn(activeFighter, fighters);
                enemyTurnPlanned = true;
                enemyTurnDelay = 0.45f;
                selectedSpell = null;
                feedback = $"Turno de IA: {activeFighter.Name} está pensando...";
            }
            else
            {
                enemyTurnDelay -= Raylib.GetFrameTime();
                if (enemyTurnDelay <= 0f)
                {
                    if (pendingEnemyTurn?.HasMovement == true)
                    {
                        movement = new FighterMovementAnimation(activeFighter, pendingEnemyTurn.MovementPath!);
                    }
                    else if (pendingEnemyTurn is null)
                    {
                        // No queda ningún objetivo del equipo controlado por el jugador.
                        enemyTurnPlanned = false;
                        turnManager.PassTurn();
                    }
                    else
                    {
                        ResolveEnemyTurn();
                    }
                }
            }
        }
        else
        {
            pendingEnemyTurn = null;
            enemyTurnPlanned = false;
            var pointer = Raylib.GetMousePosition();
            var mousePressed = Raylib.IsMouseButtonPressed(MouseButton.Left);
            var clickHandledByHud = false;

        if (Raylib.IsKeyPressed(KeyboardKey.One)) selectedSpell = spells[0];
        if (Raylib.IsKeyPressed(KeyboardKey.Two)) selectedSpell = spells[1];
        if (Raylib.IsKeyPressed(KeyboardKey.Escape)) selectedSpell = null;

        var clickedSpellIndex = GetClickedSpellCard(pointer, mousePressed);
        if (clickedSpellIndex >= 0)
        {
            selectedSpell = spells[clickedSpellIndex];
            clickHandledByHud = true;
        }

        var hasHoveredCell = renderer.TryGetCellAt(grid, pointer, out var cellX, out var cellY);
        if (selectedSpell is not null)
        {
            for (var x = 0; x < grid.Width; x++)
            for (var y = 0; y < grid.Height; y++)
            {
                var hasLivingTarget = fighters.Any(fighter => fighter != activeFighter && fighter.IsAlive &&
                    fighter.X == x && fighter.Y == y);
                if (hasLivingTarget &&
                    spellCasting.CanCastAt(activeFighter, x, y, selectedSpell) == SpellCastResult.Success)
                    spellTargetCells.Add((x, y));
            }

            if (hasHoveredCell && spellTargetCells.Contains((cellX, cellY)))
            {
                hoveredX = cellX;
                hoveredY = cellY;
                if (mousePressed && !clickHandledByHud)
                {
                    var target = fighters.FirstOrDefault(fighter => fighter != activeFighter && fighter.IsAlive &&
                        fighter.X == cellX && fighter.Y == cellY);
                    if (target is null)
                    {
                        feedback = "Casilla válida, pero no hay un combatiente enemigo como objetivo.";
                    }
                    else
                    {
                        var result = spellCasting.TryCast(activeFighter, target, selectedSpell, fighters);
                        feedback = DescribeCastResult(result, activeFighter, selectedSpell, target);
                    }
                }
            }
        }
        else if (hasHoveredCell)
        {
            var candidate = FindPathFor(activeFighter, cellX, cellY);
            if (candidate is not null && pathfinder.CanAfford(activeFighter, candidate))
            {
                previewPath = candidate;
                hoveredX = cellX;
                hoveredY = cellY;

                if (mousePressed && !clickHandledByHud && candidate.MovementCost > 0)
                    movement = new FighterMovementAnimation(activeFighter, candidate);
            }
        }

            if (Raylib.IsKeyPressed(KeyboardKey.Space))
            {
                turnManager.PassTurn();
                selectedSpell = null;
                feedback = "Turno finalizado. PA y PM del siguiente combatiente restaurados.";
            }
        }
    }

    if (movement is not null)
    {
        movement.Update(Raylib.GetFrameTime());
        if (movement.IsComplete)
        {
            movement = null;
            if (pendingEnemyTurn is not null)
                ResolveEnemyTurn();
        }
    }

    battleResult = battleState.CheckBattleState(fighters);
    var turnFighter = turnManager.CurrentFighter;
    spells = ClassSpellKitProvider.GetKit(turnFighter.Class);
    var currentFighter = turnFighter.IsAlive
        ? turnFighter
        : fighters.FirstOrDefault(fighter => fighter.IsAlive) ?? turnFighter;
    IReadOnlySet<(int X, int Y)> pathCells = movement?.HighlightedCells ??
        (previewPath is null
            ? new HashSet<(int X, int Y)>()
            : previewPath.Path.Skip(1).Select(cell => (cell.X, cell.Y)).ToHashSet());

    Raylib.BeginDrawing();
    Raylib.ClearBackground(new Color(22, 27, 32, 255));
    renderer.Draw(grid, hoveredX, hoveredY, pathCells, spellTargetCells);
    Raylib.DrawText("DOFUSSHARP  /  COMBATE TACTICO", 24, 22, 22, new Color(234, 226, 201, 255));
    Raylib.DrawText("Rojo: obstáculo   Azul: bloquea LoS   Violeta: objetivo válido del hechizo seleccionado", 24, 52, 15,
        new Color(183, 191, 184, 255));

    foreach (var fighter in fighters.OrderBy(entity => entity.X + entity.Y))
    {
        if (!fighter.IsAlive) continue;
        var position = movement is not null && fighter == currentFighter
            ? movement.GetVisualPosition(renderer)
            : renderer.GridToScreen(fighter.X, fighter.Y);
        renderer.DrawFighter(position, fighter, fighter == currentFighter);
    }

    DrawHud(currentFighter, turnManager.RoundNumber, spells, selectedSpell, spellCasting);
    var modeText = selectedSpell is null
        ? hoveredX >= 0
            ? $"Destino: ({hoveredX}, {hoveredY})   Coste: {previewPath!.MovementCost} PM"
            : "Movimiento: pasa el cursor por una casilla alcanzable"
        : $"HECHIZO: {selectedSpell.Name}   Objetivos válidos: morado   Esc: cancelar";
    Raylib.DrawText(modeText, 24, screenHeight - 34, 17, new Color(236, 190, 73, 255));
    Raylib.DrawText(feedback, 24, screenHeight - 62, 16, new Color(205, 215, 211, 255));
    Raylib.DrawText("Clic: mover/lanzar   1/2: hechizo   ESPACIO: finalizar turno", 24, screenHeight - 88, 15,
        new Color(183, 191, 184, 255));
    if (battleResult.IsFinished)
        DrawBattleOverlay(battleResult, playerTeamId);
    Raylib.EndDrawing();

    if (battleResult.IsFinished && Raylib.IsKeyPressed(KeyboardKey.Escape)) break;
}

Raylib.CloseWindow();

PathResult? FindPathFor(Fighter fighter, int targetX, int targetY)
{
    var blockedCells = fighters
        .Where(other => other != fighter && other.IsAlive)
        .Select(other => (other.X, other.Y))
        .ToHashSet();
    return pathfinder.FindPath(grid, fighter.X, fighter.Y, targetX, targetY, blockedCells);
}

void ResolveEnemyTurn()
{
    if (pendingEnemyTurn is null) return;
    var result = enemyAi.ResolveAttack(pendingEnemyTurn, spells, fighters);
    feedback = result.CastSpell
        ? $"IA: {result.Target.Name} recibe {result.Spell!.CalculateDamage(pendingEnemyTurn.Actor.Stats)} de {result.Spell.Name}."
        : $"{pendingEnemyTurn.Actor.Name} finaliza su turno sin hechizo válido.";
    pendingEnemyTurn = null;
    enemyTurnPlanned = false;
    turnManager.PassTurn();
}

static bool TryGetCharacteristicInput(out CharacteristicAttribute attribute)
{
    if (Raylib.IsKeyPressed(KeyboardKey.Three)) attribute = CharacteristicAttribute.Vitality;
    else if (Raylib.IsKeyPressed(KeyboardKey.Four)) attribute = CharacteristicAttribute.Strength;
    else if (Raylib.IsKeyPressed(KeyboardKey.Five)) attribute = CharacteristicAttribute.Intelligence;
    else if (Raylib.IsKeyPressed(KeyboardKey.Six)) attribute = CharacteristicAttribute.Agility;
    else if (Raylib.IsKeyPressed(KeyboardKey.Seven)) attribute = CharacteristicAttribute.Luck;
    else
    {
        attribute = default;
        return false;
    }

    return true;
}

static int GetClickedSpellCard(Vector2 pointer, bool mousePressed)
{
    if (!mousePressed) return -1;
    const int cardX = 984;
    const int cardWidth = 254;
    const int cardHeight = 54;
    const int firstCardY = 510;
    const int cardGap = 8;
    for (var index = 0; index < 2; index++)
    {
        var cardY = firstCardY + index * (cardHeight + cardGap);
        if (pointer.X >= cardX && pointer.X <= cardX + cardWidth &&
            pointer.Y >= cardY && pointer.Y <= cardY + cardHeight)
            return index;
    }
    return -1;
}

static string DescribeCastResult(SpellCastResult result, Fighter caster, Spell spell, Fighter target) => result switch
{
    SpellCastResult.Success => $"{spell.Name}: {target.Name} recibe {spell.CalculateDamage(caster.Stats)} de daño.",
    SpellCastResult.InsufficientActionPoints => "PA insuficientes para lanzar el hechizo.",
    SpellCastResult.TargetOutOfRange => "El objetivo está fuera del alcance del hechizo.",
    SpellCastResult.LineOfSightBlocked => "La línea de visión está bloqueada.",
    SpellCastResult.SpellOnCooldown => "El hechizo todavía está en cooldown.",
    SpellCastResult.CasterIsDead => "Un combatiente derrotado no puede lanzar hechizos.",
    SpellCastResult.TargetIsDead => "El objetivo ya ha sido derrotado.",
    SpellCastResult.InvalidTarget => "Este hechizo solo puede usarse sobre un aliado.",
    _ => "No se pudo lanzar el hechizo."
};

static void DrawHud(Fighter fighter, int roundNumber, IReadOnlyList<Spell> spells,
    Spell? selectedSpell, SpellCastingService spellCasting)
{
    const int panelX = 970;
    const int panelY = 150;
    const int panelWidth = 282;
    const int panelHeight = 540;
    var background = new Color(34, 42, 46, 255);
    var foreground = new Color(234, 226, 201, 255);

    Raylib.DrawRectangle(panelX, panelY, panelWidth, panelHeight, background);
    Raylib.DrawRectangleLines(panelX, panelY, panelWidth, panelHeight, new Color(91, 113, 104, 255));
    Raylib.DrawText("PERSONAJE ACTIVO", panelX + 18, panelY + 18, 16, new Color(236, 190, 73, 255));
    Raylib.DrawText(fighter.Name, panelX + 18, panelY + 48, 28, foreground);
    Raylib.DrawText($"Clase: {fighter.Class}", panelX + 18, panelY + 82, 16, foreground);
    Raylib.DrawText($"Nivel {fighter.Level}", panelX + 18, panelY + 104, 16, foreground);
    Raylib.DrawText($"XP {fighter.Experience}/{fighter.ExperienceToNextLevel}", panelX + 18, panelY + 126, 14, foreground);
    const int experienceBarWidth = panelWidth - 36;
    Raylib.DrawRectangle(panelX + 18, panelY + 148, experienceBarWidth, 8, new Color(45, 55, 58, 255));
    var progressWidth = experienceBarWidth * fighter.Experience / (float)fighter.ExperienceToNextLevel;
    Raylib.DrawRectangle(panelX + 18, panelY + 148, (int)progressWidth, 8, new Color(112, 214, 130, 255));
    Raylib.DrawText($"Vida: {fighter.HP}/{fighter.MaxHP}", panelX + 18, panelY + 168, 18, foreground);
    Raylib.DrawText($"PA: {fighter.ActionPoints}/{fighter.MaxActionPoints}", panelX + 18, panelY + 194, 18, foreground);
    Raylib.DrawText($"PM: {fighter.MovementPoints}/{fighter.MaxMovementPoints}", panelX + 18, panelY + 220, 18, foreground);
    Raylib.DrawText($"VIT {fighter.Stats.Vitality}  FUE {fighter.Stats.Strength}  INT {fighter.Stats.Intelligence}",
        panelX + 18, panelY + 250, 14, foreground);
    Raylib.DrawText($"AGI {fighter.Stats.Agility}  SUE {fighter.Stats.Luck}",
        panelX + 18, panelY + 270, 14, foreground);
    Raylib.DrawText($"Puntos: {fighter.AvailableCharacteristicPoints} | 3 VIT 4 FUE 5 INT 6 AGI 7 SUE",
        panelX + 18, panelY + 292, 12, new Color(236, 190, 73, 255));
    Raylib.DrawText($"Posición: ({fighter.X}, {fighter.Y})   Ronda: {roundNumber}",
        panelX + 18, panelY + 316, 14, foreground);
    Raylib.DrawText("HECHIZOS DE CLASE  (teclas 1 y 2)", panelX + 18, panelY + 338, 15, foreground);
    for (var index = 0; index < spells.Count; index++)
    {
        var spell = spells[index];
        const int cardX = panelX + 14;
        const int cardWidth = panelWidth - 28;
        const int cardHeight = 54;
        var cardY = panelY + 360 + index * (cardHeight + 8);
        var isSelected = selectedSpell?.Id == spell.Id;
        var cardColor = isSelected ? new Color(76, 94, 111, 255) : new Color(45, 55, 58, 255);
        Raylib.DrawRectangle(cardX, cardY, cardWidth, cardHeight, cardColor);
        Raylib.DrawRectangleLines(cardX, cardY, cardWidth, cardHeight,
            isSelected ? new Color(236, 190, 73, 255) : new Color(76, 92, 87, 255));
        Raylib.DrawText($"{index + 1}. {spell.Name}  ·  {spell.ActionPointCost} PA", cardX + 8, cardY + 7, 15, foreground);
        var remainingCooldown = spellCasting.GetRemainingCooldownTurns(fighter, spell);
        var cooldownText = remainingCooldown == 0 ? "Listo" : $"CD {remainingCooldown}";
        var effectiveDamage = spell.CalculateDamage(fighter.Stats);
        Raylib.DrawText($"Alcance {spell.MinimumRange}-{spell.MaximumRange}  ·  Daño {effectiveDamage}  ·  {cooldownText}",
            cardX + 8, cardY + 31, 12, new Color(190, 201, 195, 255));
    }
    var modeLabel = selectedSpell is null ? "Modo: movimiento" : $"Modo: {selectedSpell.Name}";
    Raylib.DrawText(modeLabel, panelX + 18, panelY + 490, 14, new Color(236, 190, 73, 255));
}

static void DrawBattleOverlay(BattleResult result, string playerTeamId)
{
    Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, new Color(8, 11, 14, 210));
    const int panelX = 390;
    const int panelY = 285;
    const int panelWidth = 500;
    const int panelHeight = 190;
    Raylib.DrawRectangle(panelX, panelY, panelWidth, panelHeight, new Color(34, 42, 46, 255));
    Raylib.DrawRectangleLines(panelX, panelY, panelWidth, panelHeight, new Color(236, 190, 73, 255));

    var message = result.Status == BattleStatus.Draw
        ? "¡Empate!"
        : result.WinningTeamId == playerTeamId ? "¡Victoria!" : "¡Derrota!";
    var messageColor = message == "¡Victoria!" ? new Color(112, 214, 130, 255) : new Color(236, 190, 73, 255);
    Raylib.DrawText(message, panelX + 125, panelY + 45, 48, messageColor);
    Raylib.DrawText("Pulsa ESC para cerrar el combate", panelX + 90, panelY + 120, 20,
        new Color(234, 226, 201, 255));
}
