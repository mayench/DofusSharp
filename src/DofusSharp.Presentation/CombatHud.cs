using System.Numerics;
using DofusSharp.Combat.Spells;
using DofusSharp.Domain;
using DofusSharp.GameState.Turns;
using Raylib_cs;

namespace DofusSharp.Presentation;

/// <summary>HUD de combate inspirado en la jerarquía visual clásica de un RPG táctico.</summary>
internal sealed class CombatHud
{
    private const int SidebarX = 850;
    private const int SidebarY = 98;
    private const int SidebarWidth = 406;
    private const int SidebarHeight = 520;
    private const int SpellCardX = SidebarX + 14;
    private const int SpellCardWidth = SidebarWidth - 28;
    private const int SpellCardHeight = 132;
    private const int FirstSpellCardY = SidebarY + 158;
    private const int SpellCardGap = 12;
    private const int BottomDockY = 630;

    private static readonly Color Panel = new(225, 211, 171, 255);
    private static readonly Color Ink = new(83, 53, 34, 255);
    private static readonly Color Muted = new(129, 102, 69, 255);
    private static readonly Color Gold = new(177, 113, 35, 255);

    public void Draw(Fighter fighter, int roundNumber, IReadOnlyList<Spell> spells, Spell? selectedSpell,
        SpellCastingService spellCasting, IReadOnlyList<Fighter> fighters, Fighter initiativeFighter,
        float remainingTurnSeconds, string feedback, string modeText)
    {
        DrawTopBar(roundNumber, initiativeFighter);
        DrawSpellSidebar(fighter, spells, selectedSpell, spellCasting);
        DrawBottomDock(fighter, fighters, initiativeFighter, remainingTurnSeconds, feedback, modeText);
    }

    public static int GetClickedSpellIndex(Vector2 pointer, bool mousePressed, int spellCount)
    {
        if (!mousePressed) return -1;
        for (var index = 0; index < spellCount; index++)
        {
            var cardY = FirstSpellCardY + index * (SpellCardHeight + SpellCardGap);
            if (pointer.X >= SpellCardX && pointer.X <= SpellCardX + SpellCardWidth &&
                pointer.Y >= cardY && pointer.Y <= cardY + SpellCardHeight)
                return index;
        }

        return -1;
    }

    public static bool IsSpellReady(Fighter fighter, Spell spell, SpellCastingService spellCasting) =>
        fighter.ActionPoints >= spell.ActionPointCost && spellCasting.GetRemainingCooldownTurns(fighter, spell) == 0;

    private static void DrawTopBar(int roundNumber, Fighter activeFighter)
    {
        DrawPanel(24, 18, 1232, 62, new Color(113, 78, 47, 255), new Color(183, 139, 68, 255));
        Raylib.DrawRectangle(24, 18, 5, 62, Gold);
        Raylib.DrawText("DOFUSSHARP", 46, 29, 24, new Color(248, 231, 185, 255));
        Raylib.DrawText("MUNDO DE LOS DOCE · ARENA", 48, 55, 11, new Color(223, 195, 137, 255));
        Raylib.DrawText($"RONDA {roundNumber}", 1000, 31, 15, Gold);
        Raylib.DrawText($"TURNO DE {activeFighter.Name.ToUpperInvariant()}", 1090, 31, 15, Ink);
    }

    private static void DrawSpellSidebar(Fighter fighter, IReadOnlyList<Spell> spells,
        Spell? selectedSpell, SpellCastingService spellCasting)
    {
        DrawPanel(SidebarX, SidebarY, SidebarWidth, SidebarHeight, Panel, new Color(166, 129, 71, 255));
        Raylib.DrawText("COMBATIENTE ACTIVO", SidebarX + 20, SidebarY + 17, 11, Muted);
        Raylib.DrawText(fighter.Name, SidebarX + 20, SidebarY + 35, 24, Ink);
        DrawPill(SidebarX + SidebarWidth - 114, SidebarY + 30, 94, 28,
            fighter.Class.ToString().ToUpperInvariant(), new Color(75, 70, 50, 255), Gold);

        var healthWidth = SidebarWidth - 40;
        Raylib.DrawText($"HP  {fighter.HP}/{fighter.MaxHP}", SidebarX + 20, SidebarY + 71, 13, Ink);
        Raylib.DrawRectangle(SidebarX + 20, SidebarY + 92, healthWidth, 10, new Color(61, 41, 43, 255));
        var healthRatio = fighter.MaxHP <= 0 ? 0f : (float)fighter.HP / fighter.MaxHP;
        Raylib.DrawRectangle(SidebarX + 20, SidebarY + 92, (int)(healthWidth * healthRatio), 10,
            new Color(190, 75, 69, 255));
        Raylib.DrawText($"VIT {fighter.Stats.Vitality}  FUE {fighter.Stats.Strength}  INT {fighter.Stats.Intelligence}  AGI {fighter.Stats.Agility}  SUE {fighter.Stats.Luck}",
            SidebarX + 20, SidebarY + 112, 12, Muted);
        Raylib.DrawLine(SidebarX + 18, SidebarY + 140, SidebarX + SidebarWidth - 18, SidebarY + 140,
            new Color(68, 80, 81, 255));
        Raylib.DrawText("HECHIZOS", SidebarX + 20, SidebarY + 145, 13, Gold);
        Raylib.DrawText("1 / 2", SidebarX + SidebarWidth - 58, SidebarY + 145, 12, Muted);

        for (var index = 0; index < spells.Count; index++)
            DrawSpellCard(fighter, spells[index], index, selectedSpell?.Id == spells[index].Id, spellCasting);

        Raylib.DrawText("Pulsa Esc para volver a movimiento", SidebarX + 20, SidebarY + 474, 12, Muted);
    }

    private static void DrawSpellCard(Fighter fighter, Spell spell, int index, bool isSelected,
        SpellCastingService spellCasting)
    {
        var cardY = FirstSpellCardY + index * (SpellCardHeight + SpellCardGap);
        var ready = IsSpellReady(fighter, spell, spellCasting);
        var cooldown = spellCasting.GetRemainingCooldownTurns(fighter, spell);
        var cardFill = ready ? new Color(246, 233, 192, 255) : new Color(204, 195, 169, 255);
        var border = isSelected ? Gold : ready ? new Color(140, 151, 85, 255) : new Color(166, 154, 128, 255);
        DrawPanel(SpellCardX, cardY, SpellCardWidth, SpellCardHeight, cardFill, border);
        if (isSelected) Raylib.DrawRectangle(SpellCardX, cardY + 9, 4, SpellCardHeight - 18, Gold);

        var badgeColor = ready ? new Color(211, 186, 116, 255) : new Color(177, 170, 151, 255);
        Raylib.DrawCircle(SpellCardX + 43, cardY + 48, 26, badgeColor);
        Raylib.DrawCircleLines(SpellCardX + 43, cardY + 48, 26,
            ready ? new Color(153, 117, 48, 255) : new Color(147, 137, 116, 255));
        Raylib.DrawText(GetAttributeLabel(spell.ScalingAttribute), SpellCardX + 24, cardY + 43, 11,
            ready ? Ink : Muted);

        var titleColor = ready ? Ink : new Color(133, 123, 103, 255);
        Raylib.DrawText($"{index + 1}. {spell.Name}", SpellCardX + 82, cardY + 18, 17, titleColor);
        var costText = $"{spell.ActionPointCost} PA";
        var costColor = ready ? new Color(114, 127, 70, 255) : new Color(146, 139, 120, 255);
        DrawPill(SpellCardX + SpellCardWidth - 78, cardY + 13, 64, 25, costText, costColor,
            ready ? new Color(199, 220, 145, 255) : new Color(128, 136, 135, 255));

        var damage = spell.CalculateDamage(fighter.Stats);
        Raylib.DrawText($"ALCANCE  {spell.MinimumRange}–{spell.MaximumRange}", SpellCardX + 82, cardY + 50,
            12, ready ? Muted : new Color(133, 124, 104, 255));
        Raylib.DrawText($"DAÑO  {damage}", SpellCardX + 82, cardY + 70, 12,
            ready ? new Color(159, 91, 46, 255) : new Color(133, 124, 104, 255));

        var status = cooldown > 0 ? $"RECARGA · {cooldown} TURNOS"
            : ready ? "● DISPONIBLE" : "○ PA INSUFICIENTES";
        Raylib.DrawText(status, SpellCardX + 18, cardY + 105, 11,
            ready ? new Color(92, 123, 55, 255) : new Color(133, 124, 104, 255));
        Raylib.DrawText(ready ? "LISTO" : "INACTIVO", SpellCardX + SpellCardWidth - 77, cardY + 104, 10,
            ready ? new Color(92, 123, 55, 255) : new Color(133, 124, 104, 255));
    }

    private static void DrawBottomDock(Fighter fighter, IReadOnlyList<Fighter> fighters, Fighter activeFighter,
        float remainingSeconds, string feedback, string modeText)
    {
        DrawPanel(24, BottomDockY, 1232, 150, Panel, new Color(166, 129, 71, 255));

        // Tarjeta del combatiente y salud.
        Raylib.DrawCircle(62, BottomDockY + 48, 22, new Color(142, 112, 60, 255));
        Raylib.DrawCircleLines(62, BottomDockY + 48, 22, Gold);
        Raylib.DrawText(fighter.Name[..Math.Min(1, fighter.Name.Length)], 56, BottomDockY + 37, 24, Ink);
        Raylib.DrawText(fighter.Name, 96, BottomDockY + 27, 18, Ink);
        Raylib.DrawText($"NIVEL {fighter.Level} · {fighter.Class.ToString().ToUpperInvariant()}",
            96, BottomDockY + 51, 11, Muted);
        Raylib.DrawRectangle(96, BottomDockY + 74, 168, 8, new Color(144, 107, 83, 255));
        var hpWidth = fighter.MaxHP == 0 ? 0 : 168 * fighter.HP / fighter.MaxHP;
        Raylib.DrawRectangle(96, BottomDockY + 74, hpWidth, 8, new Color(190, 75, 69, 255));
        Raylib.DrawText($"{fighter.HP}/{fighter.MaxHP} HP   ·   XP {fighter.Experience}/{fighter.ExperienceToNextLevel}",
            96, BottomDockY + 88, 10, Muted);
        Raylib.DrawRectangle(96, BottomDockY + 106, 168, 4, new Color(169, 153, 111, 255));
        var experienceWidth = 168 * fighter.Experience / (float)fighter.ExperienceToNextLevel;
        Raylib.DrawRectangle(96, BottomDockY + 106, (int)experienceWidth, 4,
            new Color(112, 190, 126, 255));

        Raylib.DrawLine(286, BottomDockY + 18, 286, BottomDockY + 130, new Color(68, 80, 81, 255));
        Raylib.DrawText("ORDEN DE INICIATIVA", 306, BottomDockY + 20, 10, Muted);
        var ordered = fighters.OrderByDescending(entry => entry.Initiative).Take(4).ToArray();
        for (var index = 0; index < ordered.Length; index++)
        {
            var entry = ordered[index];
            var cardX = 306 + index * 77;
            var active = entry == activeFighter;
            Raylib.DrawRectangle(cardX, BottomDockY + 42, 68, 62,
                active ? new Color(202, 181, 119, 255) : new Color(212, 201, 168, 255));
            Raylib.DrawRectangleLines(cardX, BottomDockY + 42, 68, 62,
                active ? Gold : new Color(169, 145, 98, 255));
            Raylib.DrawCircle(cardX + 16, BottomDockY + 59, 7,
                entry.TeamId == "blue" ? new Color(102, 174, 219, 255) : new Color(218, 103, 89, 255));
            Raylib.DrawText(entry.Name[..Math.Min(7, entry.Name.Length)], cardX + 8, BottomDockY + 76, 10,
                active ? Ink : Muted);
        }

        DrawTurnClock(690, BottomDockY + 72, remainingSeconds);
        Raylib.DrawLine(756, BottomDockY + 18, 756, BottomDockY + 130, new Color(68, 80, 81, 255));
        DrawResource(782, BottomDockY + 31, "PA", fighter.ActionPoints, fighter.MaxActionPoints,
            new Color(213, 150, 68, 255));
        DrawResource(876, BottomDockY + 31, "PM", fighter.MovementPoints, fighter.MaxMovementPoints,
            new Color(90, 172, 163, 255));

        Raylib.DrawLine(970, BottomDockY + 18, 970, BottomDockY + 130, new Color(68, 80, 81, 255));
        Raylib.DrawText("CONTROLES", 992, BottomDockY + 20, 10, Muted);
        Raylib.DrawText("Izq.: mover / lanzar · Der.: cancelar", 992, BottomDockY + 42, 11, Ink);
        Raylib.DrawText("1–2: hechizo · M/Esc: mover · Espacio: pasar", 992, BottomDockY + 61, 9, Muted);
        Raylib.DrawText(Truncate(feedback, 35), 992, BottomDockY + 87, 11, Gold);
        Raylib.DrawText(Truncate(modeText, 48), 992, BottomDockY + 106, 10, Muted);
    }

    private static void DrawTurnClock(int centerX, int centerY, float remainingSeconds)
    {
        Raylib.DrawCircle(centerX, centerY, 45, new Color(121, 87, 50, 255));
        Raylib.DrawCircleLines(centerX, centerY, 45, new Color(181, 143, 78, 255));
        Raylib.DrawCircleLines(centerX, centerY, 39, Gold);
        for (var tick = 0; tick < 12; tick++)
        {
            var angle = tick * MathF.Tau / 12f - MathF.PI / 2f;
            var inner = new Vector2(centerX + MathF.Cos(angle) * 33, centerY + MathF.Sin(angle) * 33);
            var outer = new Vector2(centerX + MathF.Cos(angle) * 37, centerY + MathF.Sin(angle) * 37);
            Raylib.DrawLineV(inner, outer, new Color(171, 157, 116, 255));
        }
        var timeColor = remainingSeconds <= 10 ? new Color(231, 112, 91, 255) : Ink;
        Raylib.DrawText(Math.Max(0, (int)MathF.Ceiling(remainingSeconds)).ToString(), centerX - 16, centerY - 17, 28,
            timeColor);
        Raylib.DrawText("SEG", centerX - 10, centerY + 14, 9, Muted);
        Raylib.DrawText("TIEMPO", centerX - 22, centerY + 52, 9, Muted);
    }

    private static void DrawResource(int x, int y, string label, int current, int maximum, Color accent)
    {
        Raylib.DrawRectangle(x, y, 82, 81, new Color(37, 46, 50, 255));
        Raylib.DrawRectangleLines(x, y, 82, 81, new Color(68, 80, 81, 255));
        Raylib.DrawText(label, x + 10, y + 8, 11, Muted);
        Raylib.DrawText(current.ToString(), x + 10, y + 27, 28, accent);
        Raylib.DrawText($"/ {maximum}", x + 42, y + 40, 11, Muted);
        var pips = Math.Min(maximum, 6);
        for (var pip = 0; pip < pips; pip++)
            Raylib.DrawRectangle(x + 10 + pip * 10, y + 64, 7, 4,
                pip < current ? accent : new Color(176, 165, 138, 255));
    }

    private static void DrawPill(int x, int y, int width, int height, string label, Color fill, Color textColor)
    {
        Raylib.DrawRectangle(x, y, width, height, fill);
        Raylib.DrawText(label, x + 7, y + 7, 11, textColor);
    }

    private static void DrawPanel(int x, int y, int width, int height, Color fill, Color border)
    {
        Raylib.DrawRectangle(x + 3, y + 4, width, height, new Color(88, 60, 36, 85));
        Raylib.DrawRectangle(x, y, width, height, fill);
        Raylib.DrawRectangleLines(x, y, width, height, border);
    }

    private static string GetAttributeLabel(SpellScalingAttribute attribute) => attribute switch
    {
        SpellScalingAttribute.Strength => "FUE",
        SpellScalingAttribute.Intelligence => "INT",
        SpellScalingAttribute.Agility => "AGI",
        SpellScalingAttribute.Luck => "SUE",
        _ => "NEU"
    };

    private static string Truncate(string text, int maxLength) => text.Length <= maxLength
        ? text
        : string.Concat(text.AsSpan(0, maxLength - 1), "…");
}
