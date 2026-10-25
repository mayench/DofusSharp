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
const string playerTeamId = "blue";

var mapPath = Path.Combine(AppContext.BaseDirectory, "Maps", "arena.json");
var loadedMap = new JsonMapLoader().LoadFromFile(mapPath);
var grid = loadedMap.Grid;
Raylib.InitWindow(screenWidth, screenHeight, "DofusSharp - Aventuras del Mundo de los Doce");
Raylib.SetTargetFPS(60);
DofusTheme.Initialize();
var chosenClass = ClassSelectionScreen.Choose();
var opposingClass = chosenClass == PlayerClass.Ocra ? PlayerClass.Yopuka : PlayerClass.Ocra;
var fighters = loadedMap.Fighters.Select(fighter => CopyForSelectedClass(fighter,
    fighter.TeamId == playerTeamId ? chosenClass : opposingClass)).ToList();
var turnManager = new TurnManager(fighters);
var pathfinder = new AStarPathfinder();
var battleState = new BattleStateService(grid);
var spellCasting = new SpellCastingService(grid, new BresenhamLineOfSightService(), battleState);
turnManager.TurnStarted += spellCasting.NotifyTurnStarted;
var enemyAi = new EnemyAiService(grid, pathfinder, spellCasting, playerTeamId);
IReadOnlyList<Spell> spells = ClassSpellKitProvider.GetAvailableKit(turnManager.CurrentFighter.Class,
    turnManager.CurrentFighter.Level);
var experienceService = new BattleExperienceService();
var renderer = new IsometricGrid(500f, 112f, 50f, 26f);
var hud = new CombatHud();
FighterMovementAnimation? movement = null;
Spell? selectedSpell = null;
var feedback = "";
var hoveredX = -1;
var hoveredY = -1;
var battleResult = battleState.CheckBattleState(fighters);
EnemyTurnPlan? pendingEnemyTurn = null;
var enemyTurnDelay = 0f;
var enemyTurnPlanned = false;
const float turnDurationSeconds = 30f;
var timedFighterId = turnManager.CurrentFighter.Id;
var remainingTurnSeconds = turnDurationSeconds;

while (!Raylib.WindowShouldClose())
{
    battleResult = battleState.CheckBattleState(fighters);
    var rewardedFighters = experienceService.AwardVictoryExperience(battleResult, fighters, playerTeamId);
    if (rewardedFighters > 0)
        feedback = $"¡Victoria! {rewardedFighters} combatiente(s) reciben 100 XP.";
    if (battleResult.Status == BattleStatus.InProgress && !turnManager.CurrentFighter.IsAlive)
        turnManager.PassTurn();
    if (turnManager.CurrentFighter.Id != timedFighterId)
    {
        timedFighterId = turnManager.CurrentFighter.Id;
        remainingTurnSeconds = turnDurationSeconds;
    }
    if (!battleResult.IsFinished && turnManager.CurrentFighter.TeamId == playerTeamId)
    {
        remainingTurnSeconds = Math.Max(0, remainingTurnSeconds - Raylib.GetFrameTime());
        if (remainingTurnSeconds <= 0 && movement is null)
        {
            turnManager.PassTurn();
            selectedSpell = null;
            feedback = "Tiempo agotado. El turno se ha pasado automáticamente.";
        }
    }
    spells = ClassSpellKitProvider.GetAvailableKit(turnManager.CurrentFighter.Class,
        turnManager.CurrentFighter.Level);

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
    var spellRangeCells = new HashSet<(int X, int Y)>();
    var blockedSpellRangeCells = new HashSet<(int X, int Y)>();
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

        var requestedSpellIndex = Raylib.IsKeyPressed(KeyboardKey.One) ? 0
            : Raylib.IsKeyPressed(KeyboardKey.Two) ? 1
            : Raylib.IsKeyPressed(KeyboardKey.Three) ? 2
            : Raylib.IsKeyPressed(KeyboardKey.Four) ? 3 : -1;
        if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.M) ||
            Raylib.IsMouseButtonPressed(MouseButton.Right))
        {
            selectedSpell = null;
            feedback = "Modo de movimiento activo.";
        }

        var clickedSpellIndex = CombatHud.GetClickedSpellIndex(pointer, mousePressed, spells.Count);
        if (clickedSpellIndex >= 0)
        {
            requestedSpellIndex = clickedSpellIndex;
            clickHandledByHud = true;
        }
        if (requestedSpellIndex >= 0 && requestedSpellIndex < spells.Count)
        {
            var requestedSpell = spells[requestedSpellIndex];
            if (CombatHud.IsSpellReady(activeFighter, requestedSpell, spellCasting))
            {
                if (selectedSpell?.Id == requestedSpell.Id)
                {
                    selectedSpell = null;
                    feedback = "Hechizo deseleccionado. Modo de movimiento activo.";
                }
                else
                {
                    selectedSpell = requestedSpell;
                    feedback = $"{requestedSpell.Name}: selecciona un objetivo válido.";
                }
            }
            else
            {
                selectedSpell = null;
                feedback = activeFighter.ActionPoints < requestedSpell.ActionPointCost
                    ? $"Necesitas {requestedSpell.ActionPointCost} PA para usar {requestedSpell.Name}."
                    : $"{requestedSpell.Name} todavía está en recarga.";
            }
        }

        var hasHoveredCell = renderer.TryGetCellAt(grid, pointer, out var cellX, out var cellY);
        if (selectedSpell is not null)
        {
            for (var x = 0; x < grid.Width; x++)
            for (var y = 0; y < grid.Height; y++)
            {
                var distance = GameGrid.ManhattanDistance(activeFighter.X, activeFighter.Y, x, y);
                if (distance < selectedSpell.MinimumRange || distance > selectedSpell.MaximumRange)
                    continue;

                // El jugador puede leer el alcance completo, incluidos huecos y casillas tras obstáculos.
                if (spellCasting.HasLineOfSight(activeFighter.X, activeFighter.Y, x, y))
                    spellRangeCells.Add((x, y));
                else
                    blockedSpellRangeCells.Add((x, y));

                var hasLivingTarget = fighters.Any(fighter => fighter != activeFighter && fighter.IsAlive &&
                    fighter.X == x && fighter.Y == y);
                if (hasLivingTarget &&
                    spellCasting.CanCastAt(activeFighter, x, y, selectedSpell) == SpellCastResult.Success)
                    spellTargetCells.Add((x, y));
            }

            if (hasHoveredCell && (spellRangeCells.Contains((cellX, cellY)) ||
                                   blockedSpellRangeCells.Contains((cellX, cellY))))
            {
                hoveredX = cellX;
                hoveredY = cellY;
                if (mousePressed && !clickHandledByHud)
                {
                    var target = fighters.FirstOrDefault(fighter => fighter != activeFighter && fighter.IsAlive &&
                        fighter.X == cellX && fighter.Y == cellY);
                    if (target is null)
                    {
                        feedback = "Alcance del hechizo: selecciona una casilla ocupada por un objetivo válido.";
                    }
                    else if (!spellTargetCells.Contains((cellX, cellY)))
                        feedback = DescribeCastResult(spellCasting.CanCastAt(activeFighter, cellX, cellY, selectedSpell),
                            activeFighter, selectedSpell, target);
                    else
                    {
                        var result = spellCasting.TryCast(activeFighter, target, selectedSpell, fighters);
                        feedback = DescribeCastResult(result, activeFighter, selectedSpell, target);
                        if (result == SpellCastResult.Success) selectedSpell = null;
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
    if (turnFighter.Id != timedFighterId)
    {
        timedFighterId = turnFighter.Id;
        remainingTurnSeconds = turnDurationSeconds;
    }
    spells = ClassSpellKitProvider.GetAvailableKit(turnFighter.Class, turnFighter.Level);
    var currentFighter = turnFighter.IsAlive
        ? turnFighter
        : fighters.FirstOrDefault(fighter => fighter.IsAlive) ?? turnFighter;
    IReadOnlySet<(int X, int Y)> pathCells = movement?.HighlightedCells ??
        (previewPath is null
            ? new HashSet<(int X, int Y)>()
            : previewPath.Path.Skip(1).Select(cell => (cell.X, cell.Y)).ToHashSet());

    Raylib.BeginDrawing();
    Raylib.ClearBackground(new Color(130, 142, 91, 255));
    renderer.Draw(grid, hoveredX, hoveredY, pathCells, spellRangeCells, blockedSpellRangeCells, spellTargetCells);

    foreach (var fighter in fighters.OrderBy(entity => entity.X + entity.Y))
    {
        if (!fighter.IsAlive) continue;
        var position = movement is not null && fighter == currentFighter
            ? movement.GetVisualPosition(renderer)
            : renderer.GridToScreen(fighter.X, fighter.Y);
        renderer.DrawFighter(position, fighter, fighter == currentFighter);
    }

    var modeText = selectedSpell is null
        ? hoveredX >= 0 && previewPath is not null
            ? $"Destino: ({hoveredX}, {hoveredY})   Coste: {previewPath.MovementCost} PM"
            : "Movimiento: pasa el cursor por una casilla alcanzable"
        : "Rango oro · LoS bloqueada ocre · válido verde";
    hud.Draw(currentFighter, turnManager.RoundNumber, spells, selectedSpell, spellCasting, fighters,
        turnManager.CurrentFighter, remainingTurnSeconds, feedback, modeText);
    if (battleResult.IsFinished)
        DrawBattleOverlay(battleResult, playerTeamId);
    Raylib.EndDrawing();

    if (battleResult.IsFinished && Raylib.IsKeyPressed(KeyboardKey.Escape)) break;
}

renderer.Dispose();
DofusTheme.Unload();
hud.Dispose();
Raylib.CloseWindow();

static Fighter CopyForSelectedClass(Fighter source, PlayerClass playerClass)
{
    // El kit inicial del prototipo diferencia ambas clases y mantiene los atributos ligados a su elemento.
    var stats = playerClass switch
    {
        PlayerClass.Ocra => new Stats(vitality: 10, strength: 5, intelligence: 5, agility: 25, luck: 10),
        PlayerClass.Yopuka => new Stats(vitality: 20, strength: 30, intelligence: 0, agility: 5, luck: 5),
        _ => source.Stats
    };
    var baseHealth = Math.Max(1, source.HP - source.Stats.Vitality);
    return new Fighter(source.Id, source.Name, source.X, source.Y, source.MaxActionPoints,
        source.MaxMovementPoints, baseHealth, source.Initiative, source.TeamId, playerClass, stats);
}

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
    DofusTheme.DrawText(message, panelX + 125, panelY + 45, 48, messageColor);
    DofusTheme.DrawText("Pulsa ESC para cerrar el combate", panelX + 90, panelY + 120, 20,
        new Color(234, 226, 201, 255));
}
