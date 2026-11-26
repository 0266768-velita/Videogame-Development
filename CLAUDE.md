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
| 2026-11-13 | **Orbes al estilo de los God of War clásicos:** al morir, los enemigos sueltan orbes de alma (Macuahuitl), jade (vida) y tonalli (concentración), que se recogen pasando encima **después** de la pelea. La victoria **ya no rellena la vida**: sanan los orbes de jade y las pociones. |
| 2026-11-13 | **Tonalli** es el recurso de concentración (barra azul) y alimenta el ataque definitivo Tonalli Burst. **Salto solo en exploración**, visual (sprite y sombra), con emboscada al caer sobre un enemigo. |
| 2026-11-13 | **HUD de combate en Canvas (P4 resuelta):** se genera con una herramienta de editor y queda en la escena para editarlo. Fuente: `LegacyRuntime` (uGUI `Text`) mientras no se importe TextMesh Pro. |
| 2026-11-13 | **Estilo del menú de combate** inspirado en los JRPG modernos (comandos en diagonal a la derecha, placas oscuras y filo dorado, orden de turnos arriba), sin copiar arte de ningún juego. Todo texto del HUD va sobre placa opaca para que se lea sobre cualquier fondo. |
| 2026-11-13 | **Paleta mexica seria** (`Scripts/UI/MexicaPalette.cs`), para un juego serio para adolescentes con temática mexica, como lo griego en God of War. Materiales del mundo azteca en lugar de colores saturados: obsidiana, hueso y ceniza (Mictlán), oro viejo y ocre, cinabrio y tezontle (rojo de los códices), jade (vida) y turquesa/xiuhuitl (tonalli). **Todo color nuevo de UI o combate se toma de `MexicaPalette`.** |
| 2026-11-13 | **Más oscuro y con mitología mexica** (petición del equipo: un juego serio, no para niños). Paleta más oscura; grecas de almenas escalonadas (Templo Mayor) en paneles, avisos y sobre el menú; insignias de tecla en rombo de piedra; viñeta que oscurece la arena al empezar el combate. **Las barras se nombran con las tres fuerzas anímicas mexicas:** TEYOLIA · LIFE, IHIYOTL · BREATH y TONALLI · SPIRIT. Los textos de turno van en mayúsculas. |
| 2026-11-17 | **Animación procedural del combate** (`ActorFx` + `Vfx`) mientras no exista el arte animado: todo efecto vive en `Scripts/Combat/Fx/` y usa `MexicaPalette`. Al llegar el Animator se sustituye `CombatMotion` sin tocar turnos ni habilidades. |
| 2026-11-24 | **Mapa del CSV del equipo** (`Settings/Maps/mapa_juego_150x70.csv`) en la escena nueva **`MainMap_CSV`** (copia de `MainMap`, que no se toca), a **1 celda = 2 × 2 tiles**, pintado tal cual (sin suavizar caminos). **El menú y el prólogo cargan `MainMap_CSV`** (`SceneIds.MainMap`); está en Build Settings y en la build del .exe. |
| 2026-11-24 | **Esquiva de una sola tecla** (estilo Clair Obscur): Espacio / B esquiva **e** interrumpe, justo antes del impacto. Éxito = 0 daño + contraataque automático; pronto o tarde = daño completo; pulsar fuera de tiempo bloquea la tecla (anti-spam). El crítico vuelve a ser 1 / A. Se quitan las teclas al azar W A S D / J K L. |
| 2026-11-24 | **En combate, los luchadores se dibujan en un `SortingGroup` de orden 510** (encima del mapa, los puestos y la oscuridad de 500) y la pelea se mueve a un claro libre; los efectos de delante usan 520. |
| 2026-11-24 | **Iconos de botones según el último dispositivo** (teclado / ratón, Xbox o PlayStation): los avisos piden **acciones** (`{Dodge}`, `{Interact}`, `{Next}`…) a `InputPrompts`; los iconos son `ButtonGlyph` (pixel art generado en `ButtonGlyphs`, con colores de `MexicaPalette`). **Nunca escribir teclas fijas en la UI**: usar `InputPrompts.Label`, `Localize` o `ButtonGlyph`. Opción «Button icons» (Auto por defecto). |

| 2026-11-25 | **v0.9 sobre `MainMap_CSV`:**<br>• los cambios se aplican con **TD_Game > v0.9 > Upgrade MainMap_CSV** (`Editor/V09/`);<br>• todo lo que crea cuelga de raíces `V09_*`, que se borran y se rehacen al volver a ejecutarlo;<br>• para cambiar algo de v0.9, editar el builder y volver a ejecutarlo; no editar a mano esos objetos. |
| 2026-11-25 | **Agua y hoyos son peligros, no paredes:**<br>• van en los Tilemaps `Water` y `Pits`, en la capa **Hazard** (9);<br>• el jugador está en la capa **Player** (8) y no choca con Hazard: si entra, cae (`PlayerHazards`);<br>• los enemigos sí chocan con Hazard;<br>• lo que se puede saltar va en la capa **LowObstacle** (10). |
| 2026-11-25 | **Grupos de enemigos:**<br>• `CombatEnemy.encounter` une un campamento (pelean juntos, máximo 4);<br>• `CombatEnemy.optional` = enemigo de misión: no cuenta para la llave ni para «Enemies Left». |
| 2026-11-25 | **Interacciones con {Interact}:**<br>• todas usan `InteractPrompt` (un solo aviso compartido);<br>• diálogos: `DialogueBox.Play(lines)`;<br>• NPC: `DialogueNPC`;<br>• objetos: `InteractSpot`. |
| 2026-11-25 | **Canvas de pantallas completas:** el mapa, los diálogos y el rompecabezas crean su Canvas **en un objeto raíz**. Si quedan dentro de otro Canvas, heredan su escala y su orden de dibujo. |
| 2026-11-25 | **Audio original generado en código** (`SfxSynth`). Las voces del comerciante salen del texto a voz de Windows (`Audio/VoiceRaw~`, ignorada por Unity) y se procesan en `Audio/Voice`. |

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

- **Input:** `Assets/TD_Game/Settings/Input/PlayerControls.inputactions`. Esquemas `Keyboard` y `Gamepad`. Mapa `Player`:
  - `Move` (Vector2): WASD, flechas, stick izquierdo y D-pad;
  - `Jump` (Button): Espacio / A;
  - `Sprint` (Button, mantener): Shift izquierdo / RT.
- **Primitivas:** `Assets/TD_Game/Art/Tilesets/Placeholder/Textures/`:
  - `Square_64`: relleno blanco.
  - `SquareOutline_64`: con borde.
  - `TallOutline_64x128`: pivote en (0.5, 0.25), de modo que su base ocupa una celda y sobresale una hacia arriba.
- **Tiles placeholder:** `Assets/TD_Game/Art/Tilesets/Placeholder/Tiles/`. Son primitivas teñidas con el color del Tile: `Ground_Grass`, `Ground_Dirt`, `Water`, `Wall_Stone`, `Deco_Pillar_Tall`.
- **Tile Palette:** `Assets/TD_Game/Art/Tilesets/Palettes/TD_Placeholder.prefab` (Rectangle, celda 1 × 1).
- **Iconos de botones** (`Scripts/UI/`):
  - `InputDevices` detecta el último dispositivo; `InputPrompts` traduce acciones a SPACE / B / CIRCLE (tabla de acciones dentro);
  - `ButtonGlyph.Create(parent, "Dodge", altura)` crea un icono que cambia solo; `GlyphText.Set(text, "{Interact}  Talk")` pone el icono delante de un aviso;
  - en frases, `InputPrompts.Localize("Press {Dodge}")` da «Press [SPACE]».
- **Historieta** (`PrologueComic`): Siguiente = Espacio / Enter / clic / A; Omitir = mantener ESC / B. La ayuda de la esquina aparece con cualquier tecla y se oculta tras 3 s.
- **Mapa del CSV:** `Scenes/Levels/MainMap_CSV.unity`, creado con **TD_Game > Build CSV Map (MainMap_CSV)** (`Editor/CsvMapBuilder.cs`, `CsvMap.cs`, `CsvMapPainter.cs`) desde `Settings/Maps/mapa_juego_150x70.csv`. Es una copia de `MainMap` con todos sus sistemas recolocados: jugador en `S`, enemigos en `E`, cofres `LootChest` en `K`, jefe en la pirámide (`M`/`I`) con una `KeyDoor` y una reja en cada entrada, casa de la espada en `H` con el santuario de Alex al lado. 1 celda = 2 × 2 tiles; arena `Sand_1/2`; el puente está en la fila 22, columnas 16–18. Para reconstruirlo, borrar la escena y volver a usar el menú.
- **Escena de pruebas:** `Assets/TD_Game/Scenes/Testing/TestLevel.unity`.
  - `Grid` con tres Tilemaps:
    - `Ground`: orden -10, modo Chunk.
    - `Walls`: orden 0, modo Individual.
    - `Decoration`: orden 0, modo Individual.
  - `Main Camera` ortográfica: tamaño 4.21875, que da 15 × 8,4 tiles a 16:9 y escala entera ×2 a 1080p.
- **Cámara:** `TD_Game.CameraFollow` (`Scripts/Camera/CameraFollow.cs`). Sigue al objetivo con suavizado (`smoothTime`), se limita a los bordes de un Tilemap (`boundsTilemap`) y alinea la posición a la rejilla de píxeles (PPU 64). Métodos `SnapToTarget()`, `RefreshBounds()`, `Follow(Transform)` (cambia de objetivo con suavizado) y `Shake(amplitud, duración)`.
- **Escenario de `TestLevel`:** 30×20 celdas (x -15..14, y -10..9). Sala izquierda, bloque de roca con un pasillo de tierra (y -1..0) y sala derecha; estanques y 9 pilares.
- **Jugador:** objeto `Player` en `TestLevel` (todavía sin prefab; el prefab es del 14 nov):
  - `Rigidbody2D` Dynamic (gravedad 0, rotación congelada, interpolación, colisión continua, *Never Sleep* para que los orbes se recojan aunque esté quieto).
  - `CapsuleCollider2D` en los pies.
  - `PlayerInputReader`: `InputActionReference` a `Player/Move`, `Player/Jump` y `Player/Sprint`.
  - `PlayerJump`: salto **visual**. El hijo `Visual` sube y el hijo `Shadow` (elipse negra, orden -1) se queda en el suelo.
    - En el aire ignora la colisión con los enemigos (los saltas).
    - Si caes encima de uno, empieza una **emboscada**: STOMP con daño y primer turno para el jugador.
  - `PlayerMovement`: `moveSpeed` 4 y `allowDiagonals` **activado** (8 direcciones). Solo mueve en `GameMode.Exploration`. Sprint: `sprintMultiplier` 1.75 con transición suave; expone `IsSprinting` y `CurrentSpeed`.
  - `PlayerDirection`: dirección actual, última válida (`LastValid`) y `Facing` (`Direction8`).
  - `Health`, `PlayerProgression` (desbloqueos, crecimiento, victorias, checkpoint) y `PlayerCombatant` (estamina, pociones, carga del Macuahuitl, **Tonalli**, combo y buffs; usa `PlayerStats_Tlahuelli`).
  - Hijo `Visual` con `SpriteRenderer`: `Art/Characters/Placeholder/CharacterOutline_40x56.png` con pivote en los pies y *Sort Point = Pivot*.
- **Scripts del jugador** (`Scripts/Player/`):
  - `PlayerInputReader`: solo lee el input y expone `Move`, `JumpPressed` y `SprintHeld`.
  - `PlayerMovement`: mueve el `Rigidbody2D` en `FixedUpdate` y expone `MoveDirection`, `Velocity` y `MoveSpeed`.
- **Estado global:** `TD_Game.GameState` (`Managers/GameState.cs`) con `GameMode` Exploration, Combat, Paused y Cutscene. Como lleva namespace, no choca con el `GameState` del juego anterior.
- **Combate por turnos** (`Scripts/Combat/`). Todo vive en el objeto `Combat` de `TestLevel`:
  - `CombatManager`:
    - flujo, unión de enemigos en un radio de 4 tiles (máximo 3) y eventos estáticos `CombatStarted`, `CombatWon` y `CombatLost`;
    - `TryStartCombat(enemy, ambush)`.
  - `CombatArena`:
    - el jugador va a la izquierda y los enemigos a la derecha sobre el mismo mapa; la cámara hace zoom a 2.8125 (×3);
    - aplica el temblor de cámara y el *hit stop* (`Time.timeScale` a 0.05 un instante);
    - `cameraOffset` (0.6, -0.3), junto con el menú de comandos compacto (escala 0.82), deja a los enemigos a la izquierda del menú. Si la cámara no puede llegar por los límites del mapa, los luchadores se desplazan hacia dentro (`CameraFollow.ClampPosition`);
    - activa la respiración de todos los luchadores mientras dura la pelea;
    - pone a cada luchador en un `SortingGroup` de orden **510** (`fighterOrder`) mientras dura la pelea, para que ni árboles, ni puestos, ni la oscuridad (500) lo tapen; al terminar lo quita;
    - antes de colocar a nadie busca el claro más cercano (`clearSearchRadius`): nadie pelea encima de un árbol, el río o un prop.
  - `PlayerTurn`: 5 ranuras (attack, skill1, skill2, potion, ultimate) expuestas en `Slots`; `Performing` indica la acción en curso.
  - `EnemyTurn`:
    - telegrafía y **una sola tecla** (Dodge: Espacio / B) justo antes del impacto, tanto para esquivar como para interrumpir (`ActionCommand.Run`);
    - éxito: 0 daño y **contraataque automático** (el normal pega `goodCounterFraction`); PERFECT: contraataque completo, +estamina y +Tonalli;
    - pronto: TOO EARLY y la tecla se bloquea (×1,5 por pulsación extra, LOCKED); sin pulsar o tarde: daño completo.
  - `ActionCommand`: pulsación con *timing*, usada para los críticos y los contraataques del jugador.
  - `CombatRewards`: victoria (crecimiento, desbloqueos, poción y checkpoint; **no** rellena la vida ni da almas: eso lo dan los orbes) y derrota.
  - `CombatInput`: lee el mapa `Combat`.
  - `CombatContext`: input, daño (con temblor, *hit stop* y números críticos), objetivo y Last Stand.
  - `CombatFeed`: eventos estáticos para la UI y la cámara; la lógica de combate **nunca** habla con la UI directamente.
  - `CombatMotion`: movimientos con "game feel": anticipación, estirado, estela, impacto y regreso; esquiva con salto; carga del enemigo; reacción al golpe; muerte con almas. Son provisionales hasta tener el Animator, pero mantienen la misma API y los callbacks `onHit`.
  - **Animación procedural** (`Scripts/Combat/Fx/`):
    - `ActorFx` anima solo el hijo `Visual`; el root y la sombra se quedan en el suelo. Incluye squash & stretch con muelle, altura, inclinación alrededor del centro del cuerpo, retroceso, temblor y respiración en combate. Se añade solo con `ActorFx.Of(actor)`.
    - `Vfx` (con `VfxPiece` y `VfxSprites`, texturas generadas en código): anillos y ondas, polvo, escombros, chispas, cortes en media luna, grietas en el suelo, estelas (afterimages), almas y proyectiles.
    - Orden de dibujo de los efectos: -5 los del suelo y **520** los de delante (encima de los luchadores, que en combate van a 510).
    - **`MacuahuitlRig`** (en el `Player` de `TestLevel`, en el de `MainMap` —que no es instancia del prefab— y en `Prefabs/Characters/Player.prefab`): el macuahuitl de Tlahuelli en combate.
      - Es un sprite aparte, pixel art de 7×17 px a 16 PPU generado por `WeaponSprites`: madera, navajas de obsidiana, mango y borla roja. Se puede sustituir por arte de Aseprite en **Weapon Sprite**.
      - Sigue a la mano (`grip` / `combatGrip`) y al cuerpo (`ActorFx`), sin deformarse.
      - En combate muestra la pose de perfil `tlahuelli_2_0` (**Combat Pose**) con una copia **sin el macuahuitl dibujado** (`WeaponSprites.Unarmed`, que quita los colores del arma y rellena el hueco). Requiere **Read/Write** en `tlahuelli.png`.
      - Fuera de combate se oculta y el sprite original vuelve tal cual.
      - Convive con **`MacuahuitlSwing`** de Alex (`Scripts/Player/`, creado por `MainMapBuilder`: un macuahuitl que solo aparece al atacar): mientras el rig está activo lo apaga y al terminar el combate lo devuelve. También convive con `PlayerSpriteAnimator` (caminata de Alex): el rig convierte su cuadro de cada frame a la versión sin arma.
      - API: `RaiseTo`, `Strike(MacuahuitlTechnique, onImpact)`, `Glow`, `Release` y `MacuahuitlRig.Active(player)`.
    - **`MacuahuitlTechnique`** (`Scripts/Combat/Fx/`; se llamaba `MacuahuitlSwing`, renombrado para no chocar con el componente de Alex del mismo nombre): un golpe definido por ángulos (0 = arriba, positivo = hacia atrás), tiempo, punto de impacto y efectos. Técnicas:
      - **Cleave** (tajo), **Low Sweep** (barrido bajo), **Rising Cut** (ascendente con salto) y **Whirlwind** (remolino con giro del cuerpo): son el combo, en `Ability_Attack` → *Combo Swings*;
      - **Obsidian Strike**: crítico, emboscada; agrieta el suelo;
      - **Backhand**: contraataque PERFECT.
- **Ataque y combos:**
  - el ataque tiene una carga de 0.55 s;
  - si pulsas Attack otra vez cuando sale **NOW!**, es **crítico** (×1.6, o ×1.84 si es perfecto) y da +Tonalli;
  - cada ataque seguido en el mismo turno suma +15 % de daño (combo).
- **Tonalli** (concentración, barra azul): se llena con orbes azules, PERFECT (+15) y críticos (+8). Lleno, permite **Tonalli Burst** (5 / LB): golpe ×2.5 a todos los enemigos que vacía la barra.
- **Input de combate:** mapa `Combat` en `PlayerControls.inputactions`:
  - Attack 1/A, Skill1 2/X, Skill2 3/Y, Potion 4/D-pad arriba y Ultimate 5/LB;
  - EndTurn Q/RB y TargetPrev/Next con flechas o D-pad;
  - **Dodge (Espacio / B) es la única tecla de defensa**: esquiva e interrumpe. `Interrupt` (1/A) sigue en el asset, pero el combate ya no la lee.
- **Habilidades:** ScriptableObjects que heredan de `CombatAbility` (`Scripts/Combat/Abilities/`), con assets en `ScriptableObjects/Abilities/`:
  - Attack, Potion y Tonalli Burst están desbloqueadas desde el inicio;
  - Mictlan Fury (una vez por combate) y Seismic Jump se desbloquean.
  - **Para añadir una habilidad:** crear una subclase con `Execute`, crear su asset y asignarlo a una ranura de `PlayerTurn`.
- **Orbes** (`Scripts/Interactions/`). `OrbSpawner` (en el objeto `Combat`) suelta orbes donde muere un enemigo, según su `EnemyData`:
  - **Soul** (dorado): carga del Macuahuitl, `soulsOnDefeat`;
  - **Jade** (verde): vida, `healthOnDefeat`;
  - **Tonalli** (azul): concentración, `concentrationOnDefeat`.
  - Prefab: `Prefabs/Interactables/Orb.prefab` (`SoulOrb`).
  - Solo se recogen **en exploración**, pasando encima (o saltando); de cerca los atrae un imán.
  - El evento `OrbSpawner.Collected` avisa a la UI.
- **Enemigos:**
  - `EnemyData` y `EnemyAttack` (`Scripts/Combat/Data/`) definen ataques, contraataque, efecto único (DrainStamina, HealSelf, Weaken), orbes, recompensas (vida o estamina máxima, `unlocksOnDefeat`) y `lastStandAbility`.
  - `CombatEnemy` (`Scripts/Enemies/`):
    - inicia el combate al contacto (colisión o trigger) y lanza el evento estático `Defeated`;
    - la IA de exploración debe llamar `CombatManager.Instance.TryStartCombat(enemy)` y respetar `GameState.IsExploring`.
- **Enemigos de prueba en `TestLevel`:** `Enemy_TestHound` (sala izquierda, ataca primero), y `Enemy_TestBrute` + `Enemy_TestWitch` (sala derecha; pelean juntos, la Brute da Fury y la Witch da Seismic). Son provisionales hasta que exista el diseño (P0).
- **Paleta:** `MexicaPalette` (`Scripts/UI/`):
  - Obsidian y ObsidianPlate: placas;
  - Bone (texto) y Ash (inactivo);
  - AgedGold (filos, insignias, almas) y Ochre (estamina);
  - Cinnabar (vida), Tezontle (activo y urgente) y Blood (aviso enemigo);
  - Jade (curación) y Turquoise (Tonalli);
  - Deathly: Mictlan Fury.
- **HUD** (Canvas `CombatHUD` en `TestLevel`; scripts en `Scripts/UI/`):
  - Se crea con el menú **TD_Game > Build Combat HUD** (`Scripts/Editor/CombatHUDBuilder.cs`) y después se edita en el Inspector. Para reconstruirlo, borrar `CombatHUD` y volver a usar el menú.
  - `PlayerStatusPanel`: siempre visible arriba a la izquierda (vida, estamina, Tonalli, pociones, Macuahuitl). Barras: TEYOLIA · LIFE, IHIYOTL · BREATH y TONALLI · SPIRIT.
  - `CombatActionMenu` y `ActionButton`: **lista diagonal de comandos** abajo a la derecha (filas 1–5 y End Turn, también con ratón):
    - cada fila tiene una insignia de tecla en rombo de piedra y el nombre grande en cursiva, sobre placa negra opaca con línea dorada;
    - en el turno enemigo el menú se desliza fuera y aparece la barra **DEFEND** con las teclas de defensa.
  - `TurnOrderBar`: orden de turnos arriba; quien actúa sale en dorado (`CombatManager.CurrentEnemy`).
  - `EnemyHealthBars`: barras que siguen a cada enemigo, con marcador ▼ sobre el objetivo.
  - `CombatMessages`: banner, turno, aviso DODGE!/INTERRUPT!/NOW! (con la tecla), combo y números flotantes. **Todo texto va sobre una placa opaca** (el padre del `Text`), que se oculta cuando está vacía.
  - `CombatHUD`: muestra y oculta la parte de combate.
  - **Ornamentos** generados por el builder en `Art/UI/`:
    - `Greca_Almenas.png`: almenas escalonadas, en mosaico, teñidas con DarkGold;
    - `Vignette.png`: oscurece la arena; es el primer hijo de `CombatGroup`.
    - Si borras uno de estos PNG, el builder lo vuelve a crear al reconstruir el HUD.
- **Limitación conocida:** con Unity en segundo plano, el Play Mode se congela o avanza a saltos. En las pruebas por MCP:
  - ejecutar `Application.runInBackground = true` con `execute_code` al entrar en Play Mode (no se guarda en el proyecto);
  - esperar por estado y no por tiempo;
  - no llamar `InputSystem.Update()` al simular teclas.
- **Limitación conocida:** la captura de pantalla de MCP (`manage_camera screenshot`) saca a Unity de Play Mode. Para ver el HUD en Play Mode, usar `ScreenCapture.CaptureScreenshot` con `execute_code`.
