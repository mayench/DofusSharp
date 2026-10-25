using DofusSharp.Domain;

namespace DofusSharp.Combat.LineOfSight;

/// <summary>Abstracción para comprobar visibilidad entre dos coordenadas lógicas del tablero.</summary>
public interface ILineOfSightService
{
    bool HasLineOfSight(GameGrid grid, int startX, int startY, int endX, int endY);
}
