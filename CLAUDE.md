# CLAUDE.md

# Proyecto: videojuego top-down en Unity

## 1. Descripción general

Estoy desarrollando un videojuego en Unity 6 utilizando C#.

El juego tendrá una perspectiva **top-down** (vista cenital) y un estilo visual de pixel art. Es un juego 2D: el mundo vive en el plano XY y se usan las herramientas 2D de Unity.

La intención es construir escenarios mediante tiles 2D organizados en una cuadrícula rectangular. Los personajes, enemigos, NPC y objetos del escenario se representarán principalmente mediante sprites 2D.

El objetivo es desarrollar el videojuego de forma modular, organizada y escalable, evitando implementar todos los sistemas dentro de un único script.

**Alcance (desde el 5 nov 2026):** es el **juego final**, no un prototipo, con entrega el **20 de noviembre de 2026**. Incluye:
- un mapa principal bien elaborado, con al menos 10 minutos de juego, 3 zonas o más y la zona del jefe final;
- **combate por turnos al estilo de Mictlán v5**: el enemigo alcanza al jugador en el mapa y el combate ocurre en el mismo lugar; acciones con estamina; esquivar o interrumpir a tiempo en el turno enemigo;
- progresión de habilidades, incluidas las del juego anterior;
- 3 enemigos con habilidades únicas y un jefe;
- pantalla de inicio, menús y toda la UI, guardado, audio y una build ejecutable.

El diseño detallado se recoge en `TD_Game_DISENO.md`.

## 2. Características principales

- Motor: Unity 6.
- Lenguaje: C#.
- Perspectiva: top-down.
- Estilo visual: pixel art.
- Representación: sprites 2D dentro de un proyecto Unity.
- Escenarios: tiles 2D en cuadrícula rectangular (Tilemap).
- Movimiento del jugador: ocho direcciones.
- Entrada: Unity New Input System.
- Cámara: Orthographic Camera con seguimiento del jugador.
- Animaciones: spritesheets y Animator.
- Física y colisiones: física 2D (Rigidbody2D, Collider2D, Tilemap Collider 2D).
- Organización: componentes independientes, reutilizables y fáciles de mantener.

## 3. Arquitectura visual y del escenario

La primera opción para construir el mundo será el sistema nativo de Unity:

- Grid.
- Tilemap rectangular.
- Tile Palette.
- Tilemap Renderer.
- Tilemap Collider 2D, cuando sea necesario.

Se debe comenzar con un escenario de prueba pequeño para verificar el tamaño de los tiles, la cámara y el orden de renderizado.

No implementar una cuadrícula personalizada ni generar los niveles por código sin justificar previamente por qué el sistema nativo de Unity no es suficiente.

La elevación real del terreno todavía debe definirse. No asumir que el juego tendrá plataformas, escaleras o desniveles físicos. Diseñar inicialmente una solución plana y fácil de ampliar. Si se requieren elevaciones, evaluar varios Tilemaps por capa (suelo, paredes, decoración por encima del jugador).

## 4. Movimiento del jugador

El jugador deberá poder desplazarse en ocho direcciones:

- Arriba.
- Abajo.
- Izquierda.
- Derecha.
- Arriba-derecha.
- Arriba-izquierda.
- Abajo-derecha.
- Abajo-izquierda.

Requisitos:

- Utilizar New Input System.
- Configurar las acciones de movimiento mediante Input Actions.
- Normalizar el vector de movimiento para evitar que el desplazamiento diagonal sea más rápido.
- Separar la lectura de entrada de la lógica de movimiento.
- Mantener la última dirección válida para seleccionar la animación de reposo.
- Evitar dependencias innecesarias entre movimiento y animación.
- Utilizar física 2D (el mundo está en el plano XY con Tilemaps nativos).

No utilizar CharacterController 3D. Las direcciones son relativas a la pantalla: arriba en el teclado = arriba en la pantalla.

## 5. Cámara

Utilizar una cámara ortográfica.

La cámara deberá:

- Seguir al jugador.
- Mantener una vista top-down consistente.
- Evitar movimientos bruscos.
- Permitir ajustar el encuadre y la distancia visual.
- Mantener una representación nítida del pixel art.

La cámara es perpendicular al plano XY y no se rota.

## 6. Pixel art

Configurar los sprites para conservar su apariencia pixelada:

- Filter Mode: Point (no filter).
- Compression: None, cuando corresponda.
- Mip Maps: Off.
- Pixels Per Unit: mantener una escala coherente entre los sprites y los tiles.
- Evaluar el uso de Pixel Perfect Camera.

Evitar escalados fraccionarios y configuraciones que produzcan imágenes borrosas.

## 7. Organización del proyecto

Mantener una estructura clara dentro de Assets. La siguiente es una propuesta inicial; adaptar las carpetas a lo que ya exista y evitar duplicados.

Assets/
- Art/
  - Characters/
  - Tilesets/
  - Environment/
  - Objects/
  - UI/
- Animations/
  - Player/
  - NPC/
  - Enemies/
- Audio/
  - Music/
  - SFX/
- Materials/
- Prefabs/
  - Characters/
  - Environment/
  - Interactables/
- Scenes/
  - MainMenu/
  - Levels/
  - Testing/
- Scripts/
  - Camera/
  - Player/
  - World/
  - Tilemap/
  - Enemies/
  - Interactions/
  - Managers/
  - UI/
- Settings/
  - Input/
  - Rendering/
  - PixelArt/
- ScriptableObjects/
  - Characters/
  - Items/
  - Tiles/

No crear todas las carpetas vacías sin necesidad. Mantener la estructura existente si ya está organizada correctamente.

## 8. Organización de scripts

Separar las responsabilidades. Para el jugador, se propone inicialmente:

- PlayerInput.cs: lectura de las acciones del New Input System.
- PlayerMovement.cs: desplazamiento y velocidad.
- PlayerAnimation.cs: selección y reproducción de animaciones.
- PlayerDirection.cs: dirección actual y última dirección válida.
- PlayerController.cs: coordinación de los componentes del jugador, si resulta necesaria.

No crear scripts innecesarios. Si una responsabilidad es demasiado pequeña, se puede integrar en otro componente, siempre que el resultado sea claro y mantenible.

## 9. Buenas prácticas de desarrollo

- Utilizar nombres descriptivos en inglés para clases, métodos, variables y archivos.
- Mantener el código legible y adecuado para un proyecto en crecimiento.
- Evitar scripts excesivamente largos.
- Evitar valores importantes dispersos por el código; utilizar variables serializadas o ScriptableObjects cuando corresponda.
- No duplicar funcionalidades que Unity ya ofrece.
- Comentar el código cuando ayude a comprender decisiones o comportamientos no evidentes.
- No introducir paquetes externos sin justificar su necesidad.
- Revisar errores y advertencias de la consola después de cada implementación.
- Probar cada sistema antes de continuar con el siguiente.
- Mantener compatibilidad con Unity 6 y el New Input System.

## 10. Reglas para Claude

Antes de realizar cambios:

1. Inspeccionar la estructura actual del proyecto.
2. Revisar las escenas, scripts, paquetes y configuraciones existentes.
3. Identificar qué elementos ya están implementados.
4. Explicar brevemente qué se va a modificar y por qué.
5. Evitar reemplazar sistemas que ya funcionan sin una razón técnica.

Durante el desarrollo:

- Trabajar por etapas pequeñas y verificables.
- No modificar o eliminar archivos sin necesidad.
- No sobrescribir escenas existentes.
- Crear escenas de prueba cuando sea conveniente.
- Preferir cambios incrementales.
- No generar múltiples versiones de un mismo script.
- Mantener referencias y dependencias correctamente configuradas.
- Si una operación puede eliminar trabajo existente, detenerse y solicitar autorización.

Después de cada etapa:

1. Revisar los errores de compilación.
2. Revisar la consola de Unity.
3. Validar los componentes y referencias.
4. Probar el comportamiento en Play Mode cuando sea posible.
5. Documentar qué se implementó, qué archivos se modificaron y qué queda pendiente.

## 11. Prioridad del desarrollo

El orden general de trabajo será:

1. Inspección y organización del proyecto.
2. Configuración visual y pixel art.
3. Configuración de la cuadrícula rectangular y Tilemaps.
4. Cámara ortográfica.
5. Movimiento del jugador en ocho direcciones.
6. Animaciones del jugador.
7. Colisiones y obstáculos.
8. Orden de renderizado y profundidad visual.
9. Interacciones con objetos y entorno.
10. NPC y enemigos.
11. Interfaz de usuario.
12. Escenas, transiciones y guardado.

No avanzar a sistemas complejos mientras los fundamentos no estén funcionando correctamente.

Con el alcance de juego final, a este orden se añaden el **combate por turnos y la progresión** (después de las colisiones) y la **build** al final. El calendario detallado está en `DEVELOPMENT_CALENDAR.md`.

## 12. Documentación

Mantener actualizados los siguientes documentos en la raíz del proyecto:

- CLAUDE.md: instrucciones permanentes y decisiones técnicas.
- DEVELOPMENT_CALENDAR.md: calendario y seguimiento del desarrollo.
- DEVELOPMENT_LOG.md: registro breve de cambios, problemas y decisiones relevantes.

Si una decisión técnica cambia, actualizar CLAUDE.md para evitar contradicciones en futuras sesiones.

## 13. Contexto: el juego anterior (Mictlán v5)

El repositorio contiene un juego **top-down 2D anterior** (documentado en `ARQUITECTURA.md`). **El juego nuevo se desarrolla desde cero** en este mismo proyecto; el anterior se conserva **solo como referencia** y no se modifica.

- Escenas `Boot`, `MainMenu` y `Game`. El nivel se construye en tiempo de ejecución desde `Assets/Resources/Level/level.json` (`LevelBuilder`), sin Grid ni Tilemap. El juego nuevo **no** sigue ese enfoque: usa Tilemaps pintados en el editor.
- Scripts en `Assets/Scripts/{Audio, Combat, Core, Editor, Enemy, Player, UI, Visuals, World}`. Varias clases tienen nombres genéricos (`Health`, `GameState`, `Audio`, `YSort`, `SaveSystem`…). Las clases nuevas no deben chocar con ellas.
- **No mover ni renombrar nada dentro de `Assets/Resources/`**: el juego anterior carga esos archivos por ruta.
- **No mover ni borrar `Assets/Scenes/Boot.unity` ni `Assets/Resources/Enemies/Enemy_Quetzalcoatl.prefab`**: si faltan, `MictlanAutoSetup` (Editor) regenera las escenas automáticamente al abrir el proyecto.
- `MenuTextureImporter` fuerza su propia importación en `Resources/Sprites` y `Resources/UI`. El arte nuevo va fuera de esas carpetas.
- La rama `iso25D` es un prototipo 3D con URP descartado.

## 14. Decisiones tomadas

| Fecha | Decisión |
|---|---|
| 2026-11-03 | **Perspectiva top-down** (sustituye a la isométrica descrita inicialmente). Cuadrícula **rectangular** con Grid + Tilemap + Tile Palette nativos. |
| 2026-11-03 | **Juego nuevo desde cero** en este proyecto, en escenas y scripts nuevos. El juego anterior (Mictlán v5) queda como referencia y no se modifica. |
| 2026-11-03 | Este archivo vive en la raíz del repositorio (`Videogame-Development/CLAUDE.md`). |
| 2026-11-03 | Se conservan `com.unity.2d.tilemap` (Tile Palette) y `com.unity.2d.sprite` (Sprite Editor). Pixel Perfect se evalúa con la cámara. |
| 2026-11-03 | **Arte:** el arte final lo hará el equipo. Mientras tanto se usan **primitivas** (cuadrados y rectángulos de color creados dentro de Unity). |
| 2026-11-03 | **Todo se hace dentro de Unity**: editor, MCP, C# y assets de Unity. **No se usa Python** ni generadores externos (la carpeta `Tools/` con Python es del juego anterior). Se permite JSON si hace falta. |
| 2026-11-03 | Las direcciones de movimiento son relativas a la pantalla (W / arriba = arriba en pantalla). |

| 2026-11-03 | **Ubicación y namespace:** todo el juego nuevo vive en `Assets/TD_Game/` y su código usa el namespace `TD_Game`. La estructura de §7 se aplica **dentro** de `Assets/TD_Game/`, y las carpetas se crean cuando se necesitan. |
| 2026-11-03 | **Tiles de 64×64 px con PPU 64**: 1 tile = 1 unidad de Unity; Grid rectangular con Cell Size 1 × 1. |
| 2026-11-03 | **Render pipeline: Built-in** (se mantiene el actual; no se usa URP). |
| 2026-11-03 | Las 23 advertencias `CS0618` del juego anterior **se dejan como están** (el juego anterior no se modifica). Para revisar la consola, filtrar por `TD_Game`. |
| 2026-11-03 | **Orden de dibujo:** *Transparency Sort Mode = Custom Axis (0, 1, 0)* en Graphics Settings. Lo que está más abajo en pantalla se dibuja delante. |
| 2026-11-03 | **Commits:** Claude no hace commits ni push; los hace el usuario cuando lo indique. |
| 2026-11-05 | **Arte en Aseprite:** el equipo diseña tiles, personaje y objetos en Aseprite, siguiendo la *Especificación de arte* de `DEVELOPMENT_CALENDAR.md`. Pendiente (P1): importar los `.aseprite` con el paquete oficial Aseprite Importer o exportar PNG. |
| 2026-11-05 | **Mapa principal:** escena `Assets/TD_Game/Scenes/Levels/MainMap.unity`. `TestLevel` queda como escena de pruebas técnicas. Fechas en `DEVELOPMENT_CALENDAR.md`. |
| 2026-11-05 | **Juego final, no prototipo** (ver §1). Entrega el 20 nov. |
| 2026-11-05 | **Reutilización:** se mantienen los sistemas actuales de TD_Game. Los sistemas del juego anterior (combate, IA, menús, UI, guardado, audio…) se **copian y adaptan** dentro de `Assets/TD_Game/` con namespace `TD_Game` **solo si cumplen este documento**, o tras adaptarlos para que cumplan: responsabilidades separadas, scripts cortos, valores en campos serializados o ScriptableObjects, input mediante el asset de Input Actions y referencias serializadas en lugar de `Resources.Load`. **Los archivos originales del juego anterior no se modifican.** El plan por sistema está en `DEVELOPMENT_CALENDAR.md`. |
| 2026-11-12 | **Combate dividido en componentes** (manager, arena, turnos, recompensas, contexto) en lugar de un `BattleManager` grande. Las **habilidades son ScriptableObjects** (`CombatAbility`) y las habilidades únicas de los enemigos son **datos** (`EnemyAttack.effect`). |
| 2026-11-12 | `GameState`, `GameMode`, `Health` y `EnemyData` reutilizan el nombre del juego anterior dentro del namespace `TD_Game` (así lo indicaba el plan de adaptación). En el código nuevo se usan siempre desde `namespace TD_Game`. |

### Convención de importación del arte de TD_Game

| Ajuste | Tiles | Personajes / objetos (spritesheets) |
|---|---|---|
| Texture Type | Sprite (2D and UI) | Sprite (2D and UI) |
| Sprite Mode | Single (o Multiple si es un atlas de tiles) | Multiple, cortado con el Sprite Editor |
| Pixels Per Unit | 64 | 64 |
| Pivot | Center | Bottom Center (en los pies) |
| Filter Mode | Point (no filter) | Point (no filter) |
| Compression | None | None |
| Generate Mip Maps | Off | Off |
| Wrap Mode | Clamp | Clamp |

El arte va en `Assets/TD_Game/Art/` y nunca en `Assets/Resources/` (allí `MenuTextureImporter` del juego anterior fuerza otra configuración).

### Recursos de TD_Game

- **Input:** `Assets/TD_Game/Settings/Input/PlayerControls.inputactions`. Mapa `Player` con la acción `Move` (Vector2): WASD, flechas, stick izquierdo y D-pad; esquemas `Keyboard` y `Gamepad`.
- **Primitivas:** `Assets/TD_Game/Art/Tilesets/Placeholder/Textures/`:
  - `Square_64`: relleno blanco.
  - `SquareOutline_64`: con borde.
  - `TallOutline_64x128`: pivote en (0.5, 0.25), de modo que su base ocupa una celda y sobresale una hacia arriba.
- **Tiles placeholder:** `Assets/TD_Game/Art/Tilesets/Placeholder/Tiles/`. Son primitivas teñidas con el color del Tile: `Ground_Grass`, `Ground_Dirt`, `Water`, `Wall_Stone`, `Deco_Pillar_Tall`.
- **Tile Palette:** `Assets/TD_Game/Art/Tilesets/Palettes/TD_Placeholder.prefab` (Rectangle, celda 1 × 1).
- **Escena de pruebas:** `Assets/TD_Game/Scenes/Testing/TestLevel.unity`.
  - `Grid` con tres Tilemaps:
    - `Ground`: orden -10, modo Chunk.
    - `Walls`: orden 0, modo Individual.
    - `Decoration`: orden 0, modo Individual.
  - `Main Camera` ortográfica: tamaño 4.21875, que da 15 × 8,4 tiles a 16:9 y escala entera ×2 a 1080p.
- **Cámara:** `TD_Game.CameraFollow` (`Scripts/Camera/CameraFollow.cs`). Sigue al objetivo con suavizado (`smoothTime`), se limita a los bordes de un Tilemap (`boundsTilemap`) y alinea la posición a la rejilla de píxeles (PPU 64). Métodos `SnapToTarget()` y `RefreshBounds()`.
- **Escenario de `TestLevel`:** 30×20 celdas (x -15..14, y -10..9). Sala izquierda, bloque de roca con un pasillo de tierra (y -1..0) y sala derecha; estanques y 9 pilares.
- **Jugador:** objeto `Player` en `TestLevel` (todavía sin prefab; el prefab es del 14 nov):
  - `Rigidbody2D` Dynamic (gravedad 0, rotación congelada, interpolación, colisión continua).
  - `CapsuleCollider2D` en los pies.
  - `PlayerInputReader`: `InputActionReference` a `Player/Move`.
  - `PlayerMovement`: `moveSpeed` 4 y `allowDiagonals` **activado** (8 direcciones). Solo mueve en `GameMode.Exploration`.
  - `PlayerDirection`: dirección actual, última válida (`LastValid`) y `Facing` (`Direction8`).
  - `Health`, `PlayerProgression` (desbloqueos, crecimiento, victorias, checkpoint) y `PlayerCombatant` (estamina, pociones, carga del Macuahuitl y buffs; usa `PlayerStats_Tlahuelli`).
  - Hijo `Visual` con `SpriteRenderer`: `Art/Characters/Placeholder/CharacterOutline_40x56.png` con pivote en los pies y *Sort Point = Pivot*.
- **Scripts del jugador** (`Scripts/Player/`):
  - `PlayerInputReader`: solo lee el input y expone `Move`.
  - `PlayerMovement`: mueve el `Rigidbody2D` en `FixedUpdate` y expone `MoveDirection`, `Velocity` y `MoveSpeed`.
- **Estado global:** `TD_Game.GameState` (`Managers/GameState.cs`) con `GameMode` Exploration, Combat, Paused y Cutscene. Como lleva namespace, no choca con el `GameState` del juego anterior.
- **Combate por turnos** (`Scripts/Combat/`). Todo vive en el objeto `Combat` de `TestLevel`:
  - `CombatManager`: flujo, unión de enemigos en un radio de 4 tiles (máximo 3) y eventos estáticos `CombatStarted`, `CombatWon` y `CombatLost`.
  - `CombatArena`: el jugador va a la izquierda y los enemigos a la derecha sobre el mismo mapa; la cámara hace zoom a 2.8125 (×3).
  - `PlayerTurn`: 4 ranuras de habilidad (attack, skill1, skill2, potion).
  - `EnemyTurn`: telegrafía, ventana de esquivar o interrumpir, PERFECT y anti-spam.
  - `CombatRewards`: victoria y derrota.
  - `CombatInput`: lee el mapa `Combat`.
  - `CombatContext`: daño, objetivo y Last Stand.
  - `CombatFeed`: eventos estáticos para la UI; la lógica de combate **nunca** habla con la UI directamente.
  - `CombatMotion`: movimientos provisionales hasta tener el Animator.
- **Input de combate:** mapa `Combat` en `PlayerControls.inputactions`:
  - Attack 1/A, Skill1 2/X, Skill2 3/Y y Potion 4/D-pad arriba;
  - EndTurn Q/RB y TargetPrev/Next con flechas o D-pad;
  - Dodge con Espacio, D o B, e Interrupt con 1/A.
- **Habilidades:** ScriptableObjects que heredan de `CombatAbility` (`Scripts/Combat/Abilities/`), con assets en `ScriptableObjects/Abilities/`:
  - Attack y Potion están desbloqueadas desde el inicio;
  - Mictlan Fury (una vez por combate) y Seismic Jump se desbloquean.
  - **Para añadir una habilidad:** crear una subclase con `Execute`, crear su asset y asignarlo a una ranura de `PlayerTurn`.
- **Enemigos:**
  - `EnemyData` y `EnemyAttack` (`Scripts/Combat/Data/`) definen ataques, contraataque, efecto único (DrainStamina, HealSelf, Weaken), recompensas (almas, vida o estamina máxima, `unlocksOnDefeat`) y `lastStandAbility`.
  - `CombatEnemy` (`Scripts/Enemies/`) inicia el combate al contacto (colisión o trigger). La IA de exploración debe llamar `CombatManager.Instance.TryStartCombat(enemy)` y respetar `GameState.IsExploring`.
- **Enemigos de prueba en `TestLevel`:** `Enemy_TestHound` (sala izquierda, ataca primero), y `Enemy_TestBrute` + `Enemy_TestWitch` (sala derecha; pelean juntos, la Brute da Fury y la Witch da Seismic). Son provisionales hasta que exista el diseño (P0).
- **`CombatDebugHUD`** (`Scripts/UI/`) es **provisional** (OnGUI). Se elimina cuando exista el HUD de combate en Canvas.
- **Limitación conocida:** con Unity en segundo plano, el tiempo de Play Mode avanza a saltos. En las pruebas por MCP, esperar por estado y no por tiempo.
- **Limitación conocida:** la captura de pantalla de MCP (`manage_camera screenshot`) saca a Unity de Play Mode. Para validar en Play Mode, leer los valores con `execute_code` en lugar de capturar.
