using DofusSharp.Domain;

namespace DofusSharp.GameState.Maps;

/// <summary>Mapa listo para iniciar un combate, con tablero y combatientes creados.</summary>
public sealed record LoadedMap(GameGrid Grid, IReadOnlyList<Fighter> Fighters);
