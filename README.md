# DofusSharp

Base modular para un motor de combate táctico por turnos. El tablero y el pathfinding usan coordenadas lógicas ortogonales; la proyección isométrica se mantiene en la capa de presentación.

## Estructura propuesta

```text
DofusSharp/
├── src/
│   ├── DofusSharp.Domain/             # Entidades y reglas/invariantes del dominio
│   ├── DofusSharp.Combat/             # Cálculos de combate: pathfinding, alcance, línea de visión
│   ├── DofusSharp.Application/        # Casos de uso y coordinación de comandos
│   ├── DofusSharp.GameState/          # Estado de partida, turnos, equipos y ocupación
│   └── DofusSharp.Presentation/       # Ventana interactiva raylib y conversión isométrica
└── tests/
    └── DofusSharp.Tests/              # Pruebas unitarias de dominio y combate
```

`DofusSharp.Combat` y `DofusSharp.GameState` dependen de `DofusSharp.Domain`; `DofusSharp.Application` orquesta ambas capas. `TurnManager` emite el inicio de cada turno y el orquestador conecta ese evento con los servicios que gestionan cooldowns. En producción, la confirmación del movimiento debe hacerse desde un caso de uso: validar origen/destino y ocupación, recalcular el camino sobre el estado vigente, comprobar PM y aplicar posición/coste de forma atómica. `LineOfSight` queda como dato de casilla para que el cálculo de visión se implemente como servicio independiente.

## Roadmap

1. **Base del combate** — ✅ dominio de casillas y combatientes, distancias, A* ortogonal y pruebas de regresión.
2. **Estado y ocupación** — tablero de combate con ocupación por combatiente y validación atómica de movimientos.
3. **Gestión de turnos** — en progreso: cola estable por iniciativa, avance circular, salto de combatientes derrotados y refresco de PA/PM en `DofusSharp.GameState`; `DofusSharp.Combat` determina victoria/derrota y libera celdas al morir.
4. **Línea de visión** — en progreso: Bresenham en coordenadas lógicas, bloqueos por terreno/opacidad e integración obligatoria con hechizos.
5. **Sistema de hechizos** — en progreso: definición de hechizos, coste PA, alcance Manhattan, daño base y cooldown por turno del lanzador; quedan áreas, línea de visión y efectos.
6. **Presentación isométrica** — en progreso: proyección de la cuadrícula, vista previa A*, movimiento animado con coste de PM, HUD, selección de hechizos, alcance/LoS y lanzamiento.
7. **Integración y datos** — serialización de mapas/combates, carga de escenarios y puntos de extensión para cliente de juego.

Cada módulo debe mantener las reglas puras en `Domain`/`Combat`, y dejar la orquestación, persistencia y presentación en capas separadas. La solución debe incorporar los proyectos cuando estos módulos pasen de propuesta a implementación.

## Ejecutar la presentación

```sh
dotnet run --project src/DofusSharp.Presentation
```

La demo abre una ventana de 1100 × 720 con una cuadrícula de 14 × 20 celdas. Al mover el puntero sobre un rombo, la celda se resalta y se muestran sus coordenadas lógicas. Verde representa terreno transitable, rojo bloquea movimiento, azul bloquea línea de visión y morado bloquea ambos.
