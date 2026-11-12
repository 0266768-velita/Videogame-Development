# Registro de desarrollo

Registro breve de cambios, problemas y decisiones. La entrada más reciente va primero.
Planificación: `DEVELOPMENT_CALENDAR.md` · Reglas y decisiones técnicas: `CLAUDE.md`

---

## 2026-11-12 — Sesión 7: jugador en 8 direcciones y combate por turnos (tareas de Toño)

**Tipo:** implementación en Unity, validada en Play Mode por MCP

### Qué se implementó (calendario 2.5–2.7, 3.1–3.3, 3.6, 3.7, 3.10–3.12)
- **8 direcciones:** `allowDiagonals` activado en `Player`; `PlayerDirection` guarda la dirección actual, la última válida y la dirección `Direction8`.
- **Núcleo:** `GameState` (modos Exploration, Combat, Paused y Cutscene) y `Health`, adaptados del juego anterior. `PlayerMovement` solo mueve en Exploration.
- **Datos:**
  - `PlayerStats` (ScriptableObject) con los valores del juego anterior.
  - `EnemyData` y `EnemyAttack`: ataques con contraataque (Dodge, Interrupt o Unavoidable) y **efecto único** (DrainStamina, HealSelf o Weaken).
- **Input:** mapa `Combat` en `PlayerControls.inputactions`:
  - Attack 1/A, Skill1 2/X, Skill2 3/Y y Potion 4/D-pad arriba;
  - EndTurn Q/RB y TargetPrev/Next con flechas o D-pad;
  - Dodge con Espacio, D o B, e Interrupt con 1/A.
- **Combate** (`Scripts/Combat/`), dividido en scripts cortos en lugar del `BattleManager` de 679 líneas:
  - `CombatManager`: flujo, unión de enemigos cercanos y eventos `CombatStarted`, `CombatWon` y `CombatLost`.
  - `CombatArena`: colocación sobre el mismo mapa y zoom de cámara de 4.21875 a 2.8125 (escala entera ×3).
  - `PlayerTurn`, `EnemyTurn` (telegrafía en rojo, ventana de contraataque, PERFECT +estamina y bloqueo anti-spam) y `CombatRewards` (victoria, derrota y reintento desde el checkpoint).
  - `CombatContext`: daño, objetivo y Last Stand.
  - `CombatFeed`: eventos hacia la UI.
  - `CombatMotion`: embestidas, saltos y destellos provisionales hasta tener animaciones.
- **Habilidades como ScriptableObjects** (`CombatAbility` y sus subclases): Attack, Mictlan Fury, Seismic Jump y Potion. Una habilidad nueva = una subclase y su asset, sin tocar el resto del código.
- **Progresión:** `PlayerProgression` guarda desbloqueos, vida y estamina extra, victorias y checkpoint. La carga del Macuahuitl aumenta el daño. El Last Stand desbloquea Fury.
- `CombatEnemy`: vida, datos e inicio del combate al contacto. La IA de exploración puede llamar `CombatManager.Instance.TryStartCombat(this)`.
- `CameraFollow.Follow(Transform)`: cambio de objetivo con suavizado.
- `CombatDebugHUD` **provisional** (OnGUI) para probar el combate hasta que exista el HUD de Canvas.

### Assets y escena
- `Assets/TD_Game/ScriptableObjects/`:
  - `Characters/PlayerStats_Tlahuelli`;
  - `Abilities/Ability_{Attack, MictlanFury, SeismicJump, Potion}`;
  - `Enemies/EnemyData_Test{Hound, Brute, Witch}`.
- `TestLevel`:
  - `Player` con `PlayerDirection`, `Health`, `PlayerProgression` y `PlayerCombatant`;
  - objeto `Combat` con los componentes del combate;
  - 3 enemigos de prueba. La Hound está sola en la sala izquierda; la Brute y la Witch están juntas en la sala derecha.

### Validación (Play Mode por MCP)
- Contacto → combate, zoom y la Hound ataca primero → ataques → victoria con +1 poción, +8 almas, +3 de estamina máxima y regreso a exploración.
- Brute y Witch se unen en un mismo combate. **Last Stand:** el jugador sobrevive con 1 HP y se desbloquea y activa Fury.
- **Seismic Jump** golpea a los dos enemigos.
- **Derrota:** el jugador vuelve al checkpoint con vida completa y los enemigos regresan a su sitio con vida completa.
- **Victoria con recompensas:** vida máxima 110, estamina máxima 50, 35 almas y nuevo checkpoint.
- Consola de TD_Game sin errores ni advertencias.
- **Sin probar con teclado real:** el *timing* de esquivar e interrumpir (pulsar Espacio o 1 cuando aparece DODGE! o INTERRUPT!).

### Problemas
- Unity se abrió mientras se creaban los scripts y dejó 13 sin registrar en la compilación. Se resolvió borrando sus `.meta` (nada los referenciaba) y reiniciando Unity.
- Con Unity en segundo plano, el tiempo de juego avanza a saltos. Las pruebas por MCP esperan por estado y no por tiempo.

### Pendiente
- **3.11:** las habilidades nuevas dependen del diseño (P0, `TD_Game_DISENO.md` no existe). El sistema ya las admite.
- Los 3 enemigos de prueba son provisionales: los reales y su IA dependen del diseño y de la tarea de IA enemiga.
- HUD de combate en Canvas (12 nov). Al hacerlo, quitar `CombatDebugHUD`.
- Animaciones (`PlayerAnimation` y Animator) cuando llegue el arte.

---

## 2026-11-05 — Sesión 6: cambio de alcance a juego final

**Tipo:** planificación (sin cambios en Unity)

### Decisiones del equipo
- El 20 nov se entrega el **juego final**, no un prototipo.
- **Combate por turnos como Mictlán v5.**
- **Reutilización:** se mantienen los sistemas actuales de TD_Game. Los sistemas del juego anterior se copian y adaptan dentro de `Assets/TD_Game/` si cumplen `CLAUDE.md`; los originales no se tocan.
- **Contenido:**
  - mapa principal con al menos 10 minutos de juego, 3 zonas o más y la zona del jefe;
  - progresión de habilidades que incluye las anteriores;
  - 3 enemigos distintos con habilidades únicas.
  - **El detalle lo explicará el usuario** (P0).

### Cambios
- **`DEVELOPMENT_CALENDAR.md` reescrito:**
  - metas M1–M8 e hitos H1 (8 nov), H2 (12 nov, combate completo), H3 (15 nov, juego recorrible) y H4 (20 nov, juego final);
  - prioridades por si falta tiempo y plan de adaptación de cada sistema anterior;
  - especificación de Aseprite ampliada (enemigos, jefe, UI);
  - fases: diseño y enemigos, combate, mapa, jefe y pantallas, pulido y build;
  - tabla de riesgos.
- **`CLAUDE.md`:** §1 con el alcance de juego final, §11 con el combate y la build, y §14 con las decisiones de alcance y de reutilización.

### Pendiente
- **P0:** explicación detallada del juego (zonas, enemigos y habilidades, jefe, progresión), necesaria antes del 6 nov. Se recogerá en `TD_Game_DISENO.md`.
- P1–P4: flujo de Aseprite, direcciones del arte, Pixel Perfect y forma de construir la UI.

---

## 2026-11-05 — Sesión 5: reorganización del calendario (Aseprite y mapa principal)

**Tipo:** planificación (sin cambios en Unity)

### Cambios
- **`DEVELOPMENT_CALENDAR.md` reorganizado a partir del 5 nov:**
  - **Metas** al 20 nov:
    - M1: jugador completo.
    - M2: arte de Aseprite integrado.
    - M3: mapa principal jugable.
    - M4: entorno e interacción.
    - M5: documentación.
  - **Hitos:** H1 el 8 nov, H2 el 15 nov y H3 el 20 nov.
  - **Tres líneas de trabajo** por día: Unity, Arte (Aseprite) y Mapa principal.
  - Nueva sección *Especificación de arte para Aseprite*: tamaños, tags de animación, nombres de archivo y destino en Unity.
  - Se adelantan las tareas del jugador: 8 direcciones el 6 nov, colisiones el 7, animaciones el 11 y prefab el 12.
  - Se añade la construcción de `MainMap`: concepto el 5–6, blockout el 10, arte el 13, playtest el 15 y pulido el 18.
  - La Fase 1 (3–10 nov, completada) queda resumida; el detalle sigue en este registro.
- **`CLAUDE.md` §14:** decisiones sobre el arte en Aseprite y el mapa principal.

### Decisiones pendientes
- **P1:** importar el arte con el paquete Aseprite Importer o exportar PNG. Recomendación: Importer.
- **P2:** 4 u 8 direcciones de arte. Recomendación: 4.
- **P3:** Pixel Perfect.
- **P4:** concepto del mapa principal (tema, tamaño, zonas).

---

## 2026-11-03 — Sesión 4: escenario, jugador y movimiento (tareas del 7 al 10 nov)

**Tipo:** implementación

### Cambios
- **`TestLevel` ampliado a 30×20:**
  - sala izquierda con un estanque de 3×2 y 5 pilares;
  - bloque de roca central con un pasillo de tierra de 2 tiles de alto;
  - sala derecha con un estanque de 2×2 y 4 pilares;
  - borde de muros.
  - Sustituye a la zona de prueba de 12×8.
- **Nueva primitiva** `TD_Game/Art/Characters/Placeholder/CharacterOutline_40x56.png`: Sprite, PPU 64, pivote en los pies, Point, sin compresión.
- **Scripts nuevos** (namespace `TD_Game`):
  - `Scripts/Player/PlayerInputReader.cs`: lee la acción `Move` mediante un `InputActionReference` y expone `Move`.
  - `Scripts/Player/PlayerMovement.cs`: mueve el `Rigidbody2D` en `FixedUpdate`. Tiene `moveSpeed`, `allowDiagonals` (desactivado = 4 direcciones) y normalización solo si la longitud supera 1.
- **Objeto `Player`** en `TestLevel`:
  - `Rigidbody2D` Dynamic, `CapsuleCollider2D` en los pies, `PlayerInputReader` y `PlayerMovement`;
  - hijo `Visual` con `SpriteRenderer` (*Sort Point = Pivot*).
- `CameraFollow` de la `Main Camera` sigue ahora a `Player`, con un desplazamiento de (0, 0,5) para centrar el cuerpo y no los pies.
- Se actualizaron `CLAUDE.md` (§14), `DEVELOPMENT_CALENDAR.md` y este registro.
- Nuevos documentos: `TD_Game_ARQUITECTURA.md` (arquitectura actual) y `TD_Game_GUIA.md` (explicación y cómo modificar cada cosa).

### Validación
- Compilación sin errores; ninguna advertencia de `TD_Game`.
- **Play Mode con input real del usuario:**
  - `PlayerInputReader.Move` = (0, 1), velocidad del `Rigidbody2D` = (0, 4): 4 tiles/s.
  - El jugador se detiene al soltar.
  - La cámara siguió al jugador hasta la sala derecha y se detuvo en el borde (x = 7,5), en posiciones de píxel exacto.
- **Lógica de dirección**, comprobada llamando al método con valores de prueba:
  - En 4 direcciones: (1, 1) da (1, 0) y (0,3, -0,9) da (0, -0,9).
  - En 8 direcciones: (1, 1) da (0,71, 0,71), con longitud 1.
- **Sorting** (capturas en modo edición): el jugador queda detrás de un pilar con los pies al norte de su base y delante con los pies al sur.
- **Juego anterior:** la escena `Game` arranca en Play Mode y construye su nivel (526 objetos) sin errores.

### Problemas
- **Unity estaba en Play Mode** al retomar la sesión. La escena aparecía con cambios sin guardar y con índice de build 3. Se detuvo Play Mode y se comprobó que el contenido guardado estaba intacto antes de modificarlo.
- **La captura de MCP saca a Unity de Play Mode.** Las validaciones en Play Mode se hacen leyendo valores con `execute_code`.
- **La conexión MCP se cortó** durante la sesión. Se recuperó reiniciando el servidor (*Window > MCP for Unity > Start Server*) y reconectando con `/mcp`.
- **Todavía no hay colisiones** (12 nov): el jugador atraviesa muros, agua y pilares.

### Pendiente
- Decidir **Pixel Perfect Camera**.
- 11 nov: 8 direcciones y `PlayerDirection`.

---

## 2026-11-03 — Sesión 3: Tilemap y cámara (tareas del 5 y 6 nov)

**Tipo:** implementación

### Decisiones
- El arte final lo hará el equipo; mientras tanto se usan **primitivas**.
- **Todo se hace dentro de Unity**, sin Python (se permite JSON).
- Se eliminan las carpetas vacías `Assets/Data` y `Assets/Art/Placeholders`, junto con sus `.meta` (que sí existían y estaban en git: aparecen como borrados en `git status`).

### Cambios
- **Primitivas** (`TD_Game/Art/Tilesets/Placeholder/Textures/`): `Square_64`, `SquareOutline_64` y `TallOutline_64x128`. Se generaron con C# dentro del editor e importadas como Sprite: PPU 64, Point, sin compresión, sin mip maps, Clamp.
- **Tiles** (`.../Placeholder/Tiles/`): `Ground_Grass`, `Ground_Dirt`, `Water`, `Wall_Stone` y `Deco_Pillar_Tall` (primitiva teñida con el color del Tile).
- **Tile Palette:** `TD_Game/Art/Tilesets/Palettes/TD_Placeholder.prefab`, creada con la API del paquete 2D Tilemap Editor.
- **Escena nueva:** `TD_Game/Scenes/Testing/TestLevel.unity`.
  - `Main Camera`: ortográfica, tamaño 4.21875, con `CameraFollow`.
  - `Grid` con tres Tilemaps: `Ground` (-10, Chunk), `Walls` (0, Individual) y `Decoration` (0, Individual).
  - Zona de prueba de 12×8: hierba, camino de tierra, borde de muros, estanque de 2×2 y dos pilares altos.
- **Script nuevo:** `TD_Game/Scripts/Camera/CameraFollow.cs` (namespace `TD_Game`): seguimiento suavizado, límites del Tilemap y alineación a píxeles.
- Se actualizaron `CLAUDE.md` (§14: decisiones y recursos), `DEVELOPMENT_CALENDAR.md` y este registro.

### Validación
- Compilación sin errores; ninguna advertencia de `TD_Game`.
- **Captura:** los tiles se alinean sin huecos; 1 tile = 64 px a 540p; el pilar de 64×128 apoya su base en su celda.
- **Play Mode** con un mapa temporal de 30×20 y un objetivo creado al ejecutar (descartados al salir):
  - Con el objetivo en (100, 100), la cámara queda en (7,5, 5,78): bordes en x = 15 e y = 10, exactamente el límite del mapa. Funciona igual en la esquina opuesta.
  - Las posiciones quedan en múltiplos de 1/64.
  - La cámara converge al objetivo con suavizado.
- Tras salir de Play Mode, la escena volvió a su estado guardado.

### Problemas
- La captura de cámara tomada en Play Mode mostró la escena guardada: Unity había salido de Play Mode antes de la captura. No afecta a la validación, que se hizo leyendo los valores en Play Mode.

### Pendiente
- **Decidir Pixel Perfect Camera** (paquete oficial `com.unity.2d.pixel-perfect`). El encuadre actual solo da escala entera en 540p, 1080p y 4K a 16:9.
- Validar el seguimiento con el jugador real (10 nov).
- 7 nov: escenario de 30×20.

---

## 2026-11-03 — Sesión 2: organización y configuración (tareas del 4 nov)

**Tipo:** configuración

### Decisiones
- Carpeta `Assets/TD_Game/` y namespace `TD_Game`.
- Tiles de 64×64 px con PPU 64.
- Render pipeline Built-in.
- Las advertencias `CS0618` del juego anterior se dejan como están.
- **Claude no hace commits.**

### Cambios
- **Nuevas carpetas:** `Assets/TD_Game/`, `Assets/TD_Game/Settings/`, `Assets/TD_Game/Settings/Input/`.
- **Nuevo asset:** `Assets/TD_Game/Settings/Input/PlayerControls.inputactions`. Mapa `Player` con la acción `Move` (Vector2, tipo Value): WASD, flechas, stick izquierdo y D-pad; esquemas `Keyboard` y `Gamepad`.
- **`ProjectSettings/GraphicsSettings.asset`:** *Transparency Sort Mode* = Custom Axis (0, 1, 0).
- **`CLAUDE.md` §14:** decisiones nuevas, convención de importación del arte y recursos de TD_Game.
- Se actualizaron `DEVELOPMENT_CALENDAR.md` y este registro.

### Validación
- El asset de input se importa y se carga con el Input System: 1 acción y 12 bindings, como se esperaba.
- La configuración de sorting quedó guardada en disco.
- Consola: 0 errores y ninguna advertencia relacionada con `TD_Game`.

### Problemas
- **GUID de Build Settings:** al reasignar las escenas desde código, Unity conserva los GUID antiguos. No tiene impacto (los GUID antiguos no apuntan a nada y Unity resuelve por ruta). Queda como tarea manual opcional en *File > Build Profiles*.

### Pendiente
- Decidir si se borran las carpetas vacías `Assets/Data` y `Assets/Art/Placeholders`.
- Prueba manual: comprobar que la escena `Game` del juego anterior se ve igual tras el cambio de sorting.
- Siguiente: **5 nov — Configuración del Tilemap**.

---

## 2026-11-03 — Sesión 1c: cambio de planes a top-down

**Tipo:** replanificación

### Decisiones
- **Perspectiva top-down** en lugar de isométrica.
- **Juego nuevo desde cero** en este proyecto, en escenas y scripts nuevos. Mictlán v5 se conserva solo como referencia y no se modifica.
- **Escenarios con Tilemap rectangular** (Grid + Tilemap + Tile Palette), pintados en el editor.
- **Se conservan los paquetes** `com.unity.2d.tilemap` y `com.unity.2d.sprite`.
- **Se mantienen las etapas y fechas** del calendario, adaptadas a top-down.

### Qué queda sin efecto de la sesión 1b
- **D1** (prototipo isométrico en paralelo al juego actual): ahora es un juego top-down nuevo.
- **D5** (tiles 32×16 / PPU 32): nueva propuesta **16×16 / PPU 16**, pendiente de confirmar el 4 nov.
- **D6** (mapeo del input en isométrico): ya no aplica; en top-down las direcciones del input coinciden con las de la pantalla.
- Siguen vigentes: CLAUDE.md en la raíz del repositorio, los paquetes 2D y los tiles placeholder generados por script.

### Cambios
- **`CLAUDE.md`:** todas las referencias isométricas pasan a top-down y cuadrícula rectangular (§1–§5, §11). La §13 describe Mictlán v5 como juego anterior de referencia, con las precauciones necesarias (`Resources/`, `MictlanAutoSetup`, nombres de clase). La §14 se rehízo con las decisiones vigentes y las pendientes.
- **`DEVELOPMENT_CALENDAR.md`:** reescrito para top-down. Los cambios están explicados en su tabla *Cambios al calendario*.
- **En Unity no hubo que revertir nada**: no se había creado ninguna escena, script ni asset; solo se habían instalado los paquetes, que se conservan.

### Pendiente (4 nov)
- Confirmar: tamaño de tile y PPU, carpeta y namespace del código nuevo, render pipeline y qué hacer con las advertencias `CS0618`.

---

## 2026-11-03 — Sesión 1b: decisiones y paquetes 2D

**Tipo:** configuración

### Decisiones confirmadas
D1 = A (prototipo en paralelo) · D2 = mover CLAUDE.md · D3 = instalar paquetes · D4 = A (placeholders generados) · D5 = 32×16 / PPU 32 · D6 = A (W = arriba en pantalla).

### Cambios
- `Mictlan/Claude.md` se movió a `Videogame-Development/CLAUDE.md`. Se añadieron dos secciones: §13 (contexto del juego existente) y §14 (decisiones). Las reglas originales no se tocaron.
- Paquetes instalados con MCP: `com.unity.2d.tilemap` 1.0.0 y `com.unity.2d.sprite` 1.0.0 (`Packages/manifest.json` y `packages-lock.json`).
- Se actualizaron `DEVELOPMENT_CALENDAR.md` y este registro.

### Validación
- Unity terminó de compilar después de la instalación: **0 errores**.
- La recompilación mostró **23 advertencias `CS0618`** (API obsoleta en Unity 6.5: `FindFirstObjectByType`, `FindObjectsSortMode`, `ContactFilter2D.NoFilter()`) en 13 scripts del juego actual.
  - Ya existían: en la inspección anterior la consola estaba limpia solo porque no se había recompilado.
  - No las causan los paquetes y hoy no afectan al funcionamiento.
- No se modificó ninguna escena ni ningún script.

### Pendiente
- Confirmar si se mantiene el Built-in Render Pipeline.
- Decidir cuándo corregir las advertencias `CS0618` (afecta a scripts del juego actual).
- Continuar con el resto de tareas del 4 de noviembre.

---

## 2026-11-03 — Sesión 1: diagnóstico y planificación

**Tipo:** inspección (sin cambios en el juego)
**Rama:** `integrar-version-v5` (último commit `fb0abea`; todavía no está fusionada en `main`)

### Qué se hizo
- Lectura de `CLAUDE.md`. Está en `C:\Videogame Development\Mictlan\Claude.md`, **fuera del repositorio**.
- Inspección del proyecto (archivos, git, `ProjectSettings`, `Packages`) y del editor por MCP (escena activa, Build Settings, consola, paquetes instalados).
- Revisión de la rama `iso25D`.
- Creación de `DEVELOPMENT_CALENDAR.md` y de este registro.

### Archivos creados o modificados
- **Nuevo:** `DEVELOPMENT_CALENDAR.md`
- **Nuevo:** `DEVELOPMENT_LOG.md`
- No se modificó ninguna escena, script, prefab, asset ni configuración.

### Diagnóstico

**Entorno**
- Unity 6000.5.9f1 y Built-in Render Pipeline (sin URP). Color space Linear.
- Input System 1.20.0, configurado en modo *Input System Package (New)* exclusivo.
- Física 2D sin gravedad (`Physics2DSettings` en 0, 0).
- Consola: **0 errores y 0 advertencias**.
- MCP conectado (una sola instancia: `Videogame-Development`).

**Contenido del proyecto (Mictlán v5)**
- Es un juego **top-down 2D completo**: 44 scripts, 3 escenas (Boot, MainMenu, Game), 4 enemigos y un jefe, combate por turnos, guardado, UI y audio. La arquitectura está documentada en `ARQUITECTURA.md`.
- El nivel se construye **en tiempo de ejecución** desde `Resources/Level/level.json` (`LevelBuilder`), con sprites sueltos y `BoxCollider2D`. **No usa Grid ni Tilemap.**
- Movimiento en 8 direcciones (`Player2D`, vector normalizado, Rigidbody2D). La entrada se lee dentro del mismo script, con Input Actions creadas en código.
- Cámara ortográfica con seguimiento suavizado y límites (`CameraFollow2D`) y bloqueo 16:9 (`AspectRatioLock`).
- Animación por código (`CharacterVisuals`), sin Animator. Profundidad con el script `YSort`.
- Interacciones puntuales por distancia (`KeyPickup`, `PyramidDoor`); no hay un sistema genérico.

**Rama `iso25D`** (commit `9a40aac`, 15 oct)
- Otra línea de trabajo: prototipo **3D con URP** (Terrain, árboles FBX, `CharacterController`, `OrbitCamera`, `CalibrationScene`).
- No se fusionó y no sigue el enfoque de CLAUDE.md (Tilemap 2D nativo).

### Problemas encontrados
1. **El proyecto no coincide con CLAUDE.md:**
   - CLAUDE.md pide un juego isométrico con Grid/Tilemap; el proyecto es top-down con el nivel generado en código.
   - Además existe una rama 3D paralela.
   - → Decisión D1 en el calendario.
2. **Faltan paquetes 2D:** no están `com.unity.2d.tilemap` (sin él no hay Tile Palette) ni `com.unity.2d.sprite` (Sprite Editor). Solo está el módulo `com.unity.modules.tilemap`.
3. **No existe arte isométrico** (ni tiles ni personajes en 8 direcciones).
4. **Sprites importados como Texture** (`spriteMode: 0`, PPU 100). `SpriteLib` los convierte en sprites al ejecutar con PPU 16, así que no sirven directamente para la Tile Palette ni para el Animator. Además, `MenuTextureImporter` fuerza la configuración en `Resources/Sprites` y `Resources/UI`.
5. **No hay ningún asset `.inputactions`**; las acciones se crean en código en 5 sitios.
6. **Build Settings con GUID desactualizados:** las 3 escenas apuntan a GUID que no coinciden con sus `.meta`. Ahora funciona porque se resuelven por ruta, pero es frágil.
7. **`Player2D` mezcla responsabilidades** (input, movimiento, salto, peligros), en contra de CLAUDE.md §4 y §8.
8. **Transparency Sort Mode** en el valor por defecto; el isométrico necesita *Custom Axis* (0, 1, 0).
9. **Carpetas vacías locales** `Assets/Data` y `Assets/Art/Placeholders`: restos de la versión anterior, sin `.meta` y fuera de git.
10. **El paquete MCP apunta a `#main`** (versión flotante): una actualización del repositorio podría romper el proyecto sin aviso.
11. **CLAUDE.md está fuera del repositorio:** no se versiona y no se carga automáticamente desde la raíz del proyecto Unity.

### Decisiones pendientes
D1–D6, detalladas en `DEVELOPMENT_CALENDAR.md`. La principal es **D1**: cómo se relaciona el prototipo isométrico con el juego actual. Recomendación: prototipo en paralelo en una escena de pruebas.

### Próximo paso
Confirmar D1–D5 y empezar el **4 de noviembre — Organización y configuración**.
