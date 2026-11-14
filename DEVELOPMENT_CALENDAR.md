# Calendario de desarrollo — TD_Game (juego final)

**Periodo:** 3 al 20 de noviembre de 2026 · **Actualizado:** 13 de noviembre
**Objetivo:** entregar el **20 de noviembre** el **juego final** (no un prototipo):
- un **mapa principal bien elaborado** con **al menos 10 minutos de juego**, **3 zonas o más** y la **zona del jefe final**;
- **combate por turnos** al estilo de Mictlán v5;
- **progresión de habilidades**, incluidas las de la versión anterior;
- **3 enemigos distintos** con habilidades únicas, más el jefe;
- **pantalla de inicio** y toda la **UI** necesaria;
- arte del equipo en **Aseprite** y una **build** ejecutable.

**Referencias:** `CLAUDE.md` (reglas y decisiones) · `DEVELOPMENT_LOG.md` (registro) · `TD_Game_ARQUITECTURA.md` · `TD_Game_GUIA.md` · `ARQUITECTURA.md` (sistemas del juego anterior que se adaptarán)

## Cómo usar este documento

- **Estados:** `Pendiente` · `En progreso` · `Completado` · `Bloqueado`
- Una casilla `[x]` significa **implementado y validado en Unity** (compila, consola sin errores y probado en Play Mode). El arte cuenta cuando está **importado y funcionando en Unity**.
- Líneas de trabajo:
  - 🛠 **Unity:** sistemas, escenas y configuración. Lo hace Claude por MCP, revisado por el equipo.
  - 🎨 **Arte (Aseprite):** a cargo del equipo.
  - 🗺 **Mapa:** diseño y construcción del nivel.
  - 📐 **Diseño:** reglas del juego, enemigos, habilidades y balance.
- Días colchón: **8 y 15 nov**. Si algo no se termina, se mueve ahí y se anota en *Cambios al calendario*.

---

## Metas del juego final

| # | Meta | Criterio de "hecho" |
|---|---|---|
| M1 | **Jugador completo** | 8 direcciones, colisiones, animaciones (Aseprite), prefab reutilizable |
| M2 | **Combate por turnos** | Inicio de combate al contacto, turnos con estamina, ataque, habilidades y poción, turno enemigo con esquivar o interrumpir a tiempo, victoria, derrota y recompensas |
| M3 | **Progresión** | Habilidades del juego anterior (Mictlan Fury, Seismic Jump, carga del Macuahuitl, pociones) más las nuevas que se definan, desbloqueadas al jugar |
| M4 | **Enemigos** | 3 tipos con IA de exploración (patrulla, detección, persecución) y **habilidades únicas** en combate, más el **jefe final** con fases |
| M5 | **Mapa principal** | Al menos 3 zonas y la del jefe, recorrido de **10 minutos o más**, arte de Aseprite, colisiones, profundidad y progresión que guía al jugador |
| M6 | **Pantallas y UI** | Inicio (splash y menú principal), opciones, HUD de exploración y de combate, pausa, Game Over, pantalla final, tutoriales y transiciones |
| M7 | **Guardado y audio** | Guardar y continuar; música y efectos |
| M8 | **Entrega** | Build de Windows probada de principio a fin y documentación al día |

## Hitos

| Hito | Fecha | Qué debe estar listo |
|---|---|---|
| **H1** | Dom 8 nov | **Diseño del juego** cerrado. Jugador en 8 direcciones con colisiones. **Un enemigo** que patrulla y persigue en `TestLevel` |
| **H2** | Jue 12 nov | **Combate completo de principio a fin** en `TestLevel` (turnos, habilidades, victoria, derrota, recompensas) con UI de combate |
| **H3** | Dom 15 nov | **Juego recorrible de principio a fin** en `MainMap`: 3 zonas, enemigos colocados, progresión y jefe (aunque haya arte placeholder) |
| **H4** | Vie 20 nov | **Juego final**: arte, UI, menús, audio, guardado y build probada |

## Prioridades (por si falta tiempo)

| Imprescindible | Importante | Si da tiempo |
|---|---|---|
| Jugador, combate, 3 enemigos y jefe, mapa con 3 zonas, menú de inicio, HUD, Game Over, final, build | Guardado y continuar, pausa, opciones, tutoriales, audio, animaciones de enemigos | Minimapa, efectos visuales (gore, partículas), arte v2, dificultad |

---

## Decisiones

### Tomadas

| Decisión | Resultado |
|---|---|
| Alcance | ✅ **Juego final** el 20 nov (sustituye al "prototipo") |
| Perspectiva y mundo | ✅ Top-down; **Tilemap rectangular** pintado en el editor; tiles de **64 px / PPU 64** |
| Combate | ✅ **Por turnos, como Mictlán v5**: el enemigo alcanza al jugador en el mapa y el combate ocurre en el mismo lugar; acciones con estamina; esquivar e interrumpir a tiempo en el turno enemigo |
| Reutilización | ✅ Se **mantienen los sistemas actuales de TD_Game** (Tilemaps, `PlayerInputReader`, `PlayerMovement`, `CameraFollow`). Los sistemas del juego anterior se **copian y adaptan** dentro de `Assets/TD_Game/` (namespace `TD_Game`) **solo si cumplen `CLAUDE.md`**, o tras adaptarlos para que cumplan. **El juego anterior no se modifica** |
| Contenido | ✅ Mapa principal (≥10 min, ≥3 zonas + jefe), 3 enemigos con habilidades únicas, progresión con las habilidades anteriores |
| Arte | ✅ Equipo en **Aseprite**; primitivas mientras tanto |
| Herramientas | ✅ El juego se construye **dentro de Unity**, sin Python (se permite JSON) |
| Render pipeline | ✅ Built-in |
| Commits | ✅ Los hace el usuario |

### Pendientes

| # | Decisión / información | Recomendación | Antes de |
|---|---|---|---|
| **P0** | **Diseño detallado del juego** (tu explicación): historia, las 3 zonas y la del jefe, los 3 enemigos y sus habilidades únicas, el jefe, la progresión y las habilidades nuevas | Lo recojo en un documento de diseño (`TD_Game_DISENO.md`) que sirve de contrato para el contenido | **6 nov** (bloquea enemigos, mapa y progresión) |
| P1 | Cómo entra el arte de Aseprite en Unity | Paquete oficial **Aseprite Importer** (`com.unity.2d.aseprite`) | 9 nov |
| P2 | Direcciones del arte (personaje y enemigos) | 4 direcciones (abajo, arriba, lado + volteo) | 6 nov |
| P3 | Pixel Perfect Camera (`com.unity.2d.pixel-perfect`) | Instalar | 8 nov |
| ~~P4~~ ✅ | Cómo se construye la UI (resuelta el 13 nov: Canvas generado con una herramienta de editor y editable en la escena) | **Canvas de uGUI** en escenas y prefabs, editable en el editor, reutilizando la **lógica** del juego anterior pero no su UI generada por código | 9 nov |

---

## Plan de adaptación de los sistemas del juego anterior

| Sistema anterior | Qué se hace en TD_Game | Cambios para cumplir `CLAUDE.md` |
|---|---|---|
| `GameState` (modos) | Copiar y adaptar | Namespace `TD_Game` |
| `Health` | Copiar | Namespace |
| `EnemyData` (ScriptableObject) | Copiar y ampliar | Habilidades únicas como datos; referencias serializadas en lugar de `Resources.Load` |
| `EnemyController` (IA) | Adaptar | Separar detección, movimiento y estado; usar las colisiones de los Tilemaps |
| `BattleManager` (679 líneas) | **Reescribir a partir de su lógica** | Dividir en componentes: flujo de turnos, acciones del jugador, ataques enemigos y recompensas; scripts cortos |
| `BattleInput` (acciones en código) | Sustituir | Mapa `Combat` en `PlayerControls.inputactions` |
| `PlayerCombatStats` | Adaptar | Valores en ScriptableObject (`PlayerStats`) |
| `BattleCameraController`, `ActorFeedback`, `Shockwave` | Adaptar | Feedback mediante Animator con el arte de Aseprite |
| `BattleHUD`, `MainMenu`, `PauseMenu`, `GameOverScreen`, `EndingScreen`, `BootSplash`, `TutorialTips` | Reutilizar la lógica y rehacer la UI | Canvas y prefabs (P4) |
| `SceneLoader`, `SaveSystem`, `AudioManager`, `Difficulty` | Copiar y adaptar | Namespace; los datos de guardado según el diseño |
| `Player2D`, `CameraFollow2D`, `YSort`, `LevelBuilder`, `CharacterVisuals`, `SpriteLib` | **No se usan** | Sustituidos por los sistemas actuales de TD_Game y por Tilemaps y Animator nativos |

---

## Especificación de arte para Aseprite

| Elemento | Tamaño | Notas |
|---|---|---|
| Tiles (suelo, camino, agua, muro, por zona) | 64×64 px | Cuadrícula en Aseprite: *View > Grid > Grid Settings* = 64×64; bordes y transiciones |
| Objetos altos (árbol, columna, ruinas) | ancho múltiplo de 64 | La **base** toca el borde inferior del lienzo |
| Jugador y enemigos | frames de 64×64 (enemigos grandes: 128×128) | **Pies en la fila inferior**, centrados |
| Jefe | 128×128 o 192×192 | Igual: pies abajo y centrado |
| UI | botones, paneles, barras, iconos de habilidades, logo | Según el estilo del equipo; escala ×1 a 640×360 |
| Paleta | limitada y compartida | *Palette > Save Palette* |

**Tags de animación** (una etiqueta por animación; el lado mira a la derecha):

| Personaje | Tags |
|---|---|
| Jugador | `idle_*`, `walk_*` (en las direcciones de P2) · `attack` · `hit` · `dodge` · `skill_fury` · `skill_seismic` · `death` |
| Enemigos | `idle`, `walk`, `telegraph` (preparar el ataque), `attack`, `hit`, `death` y una animación por habilidad única |
| Jefe | lo anterior más las animaciones de cada fase |

**Archivos:**
- Nombres: `chr_player.aseprite`, `enm_<nombre>.aseprite`, `boss_<nombre>.aseprite`, `tiles_<zona>.aseprite`, `obj_<nombre>.aseprite`, `ui_<nombre>.aseprite`.
- Destino: `Assets/TD_Game/Art/{Characters, Enemies, Tilesets, Objects, UI}/`. **Nunca** en `Assets/Resources/`.

---

## Fase 1 — Base técnica · ✅ Completada (3 nov)

Todo validado en Unity; el detalle está en `DEVELOPMENT_LOG.md`, sesiones 1 a 4.

- [x] Estructura `Assets/TD_Game/`, namespace `TD_Game`, `PlayerControls.inputactions` y *Transparency Sort* Custom Axis.
- [x] Grid rectangular (`Ground`, `Walls`, `Decoration`), primitivas de 64 px y paleta `TD_Placeholder`.
- [x] Cámara ortográfica con `CameraFollow` (suavizado, límites, alineación a píxeles).
- [x] `TestLevel` de 30×20 y orden de dibujo delante/detrás validado.
- [x] Jugador: `PlayerInputReader` y `PlayerMovement` (4 direcciones, 4 tiles/s), validado en Play Mode.

---

## Fase 2 — Diseño, jugador y enemigos (5–8 nov)

### Jueves 5 de noviembre — Replanificación y diseño
**Estado:** En progreso
- 📐 **Diseño**
  - [ ] Recibir tu **explicación detallada** del juego (P0) y escribir `TD_Game_DISENO.md`: zonas, enemigos, habilidades, jefe, progresión y duración estimada por zona.
- 🗺 **Mapa**
  - [ ] Concepto del mapa principal: orden de las zonas, tamaño (propuesta: unos 100×70 tiles para 10 minutos o más) y boceto en Aseprite a 1 px = 1 tile.
- 🎨 **Arte**
  - [ ] Aprobar la especificación de arte; definir el estilo y la paleta; decidir P2.
- 🛠 **Unity**
  - [x] Replanificar: calendario, `CLAUDE.md` y registro con el alcance de juego final.

### Viernes 6 de noviembre — Jugador en 8 direcciones · núcleo de juego
**Estado:** Pendiente · **Depende de:** P0 para los datos de juego
- 🛠 **Unity**
  - [x] Diagonales y `PlayerDirection` (dirección actual y última dirección válida).
  - [x] Adaptar `GameState` (modos Exploration, Combat, Paused, Cutscene) y `Health`.
  - [x] Crear el ScriptableObject `PlayerStats` (vida, estamina, daño, costes): son los valores del juego anterior como punto de partida.
- 🎨 **Arte**
  - [ ] Tileset de la zona 1; boceto del personaje.
- 📐 **Diseño**
  - [ ] Cerrar `TD_Game_DISENO.md` (P0).

### Sábado 7 de noviembre — Colisiones · IA enemiga
**Estado:** Pendiente · **Depende de:** 6 nov
- 🛠 **Unity**
  - [ ] Colisiones de los Tilemaps (`TilemapCollider2D` + `CompositeCollider2D`), del agua y de la base de los objetos; movimiento sin atascos.
  - [ ] Adaptar `EnemyData` y `EnemyController`: patrulla, detección (cono de visión, oído y línea de visión) y persecución; regreso a su zona.
  - [ ] Un enemigo de prueba en `TestLevel` que persigue al jugador.
- 🎨 **Arte**
  - [ ] Tileset de la zona 1 (terminar); personaje v1 (idle y walk).

### Domingo 8 de noviembre — Revisión · **H1**
**Estado:** Pendiente · Día colchón
- [ ] Revisión y corrección de errores; decidir P3.
- [ ] 🎨 Entregar el **tileset de la zona 1**.
- [ ] Actualizar el registro y el calendario.

**Entregable H1:** diseño cerrado, jugador en 8 direcciones con colisiones y un enemigo que patrulla y persigue.

---

## Fase 3 — Combate por turnos (9–12 nov)

### Lunes 9 de noviembre — Núcleo del combate
**Estado:** Pendiente · **Depende de:** H1
- 🛠 **Unity**
  - [x] Mapa de acciones `Combat` en `PlayerControls.inputactions`: atacar, habilidad 1, habilidad 2, poción, terminar turno, cambiar objetivo, esquivar e interrumpir.
  - [x] Inicio del combate al contacto: el jugador y los enemigos se colocan en arena sobre el mismo mapa, la cámara de combate hace zoom y se unen los enemigos cercanos.
  - [x] Turno del jugador: estamina, atacar, poción, terminar turno y cambio de objetivo.
  - [ ] Flujo Aseprite → Unity (P1): importar el tileset de la zona 1 y crear su paleta.
- 🎨 **Arte**
  - [ ] Enemigo 1 (exploración y combate).

### Martes 10 de noviembre — Turno enemigo y fin del combate
**Estado:** Pendiente
- 🛠 **Unity**
  - [x] Turno enemigo: telegrafía en rojo, ventana de **esquivar** o **interrumpir**, PERFECT (+estamina) y bloqueo anti-spam.
  - [x] Daño, **victoria** (recompensas y regreso a la exploración) y **derrota** (Game Over básico y reintento desde el checkpoint).
  - [ ] Habilidades únicas del **enemigo 1** según el diseño.
- 🎨 **Arte**
  - [ ] Enemigo 2; personaje v1 (entrega).

### Miércoles 11 de noviembre — Habilidades y progresión
**Estado:** Pendiente
- 🛠 **Unity**
  - [x] Habilidades del juego anterior:
    - **Mictlan Fury**: cura, estamina llena y más daño durante 3 turnos.
    - **Seismic Jump**: golpea a todos los enemigos.
    - Carga del **Macuahuitl** con las almas de los enemigos.
    - **Pociones**.
  - [ ] Habilidades nuevas del diseño (P0). **Bloqueado por P0**; el sistema ya las admite (subclase de `CombatAbility` + asset).
  - [x] Progresión: desbloqueos, crecimiento de vida y estamina por victoria, *Last Stand*.
  - [ ] Habilidades únicas del **enemigo 2**.
  - [ ] Animaciones del jugador con el Animator y `PlayerAnimation` (exploración y combate).
- 🎨 **Arte**
  - [ ] Enemigo 3; tileset de la zona 2.

### Jueves 12 de noviembre — UI de combate · **H2**
**Estado:** Pendiente
- 🛠 **Unity**
  - [x] HUD de combate en Canvas (P4): barras de vida, estamina y carga, menú de acciones, vida de los enemigos, aviso DODGE! / INTERRUPT!, números de daño y banners.
  - [ ] Habilidades únicas del **enemigo 3**.
  - [ ] Prefab `Player` y prefabs de los 3 enemigos.
- 🎨 **Arte**
  - [ ] UI: paneles, botones, barras e iconos de habilidades.

**Entregable H2:** combate completo de principio a fin en `TestLevel` contra los 3 enemigos, con UI.

---

## Fase 4 — Mapa principal, jefe y pantallas (13–17 nov)

### Viernes 13 de noviembre — Mapa principal: blockout completo
**Estado:** Pendiente · **Depende de:** P0, boceto del mapa
- 🗺 **Mapa**
  - [ ] Escena `Assets/TD_Game/Scenes/Levels/MainMap.unity` con las capas `Ground`, `Walls`, `Water`, `Decoration` y `Overhead`.
  - [ ] **Blockout** de las 3 zonas y la del jefe, con caminos, obstáculos y puntos de interés.
  - [ ] Colocar enemigos por zona según la curva de dificultad del diseño.
  - [ ] **Progresión del mapa** según el diseño (por ejemplo, zonas que se abren al derrotar enemigos o al conseguir una habilidad).
- 🎨 **Arte**
  - [ ] Tileset de la zona 3 y objetos (árboles, rocas, ruinas, carteles).

### Sábado 14 de noviembre — Jefe final
**Estado:** Pendiente
- 🛠 **Unity**
  - [ ] Jefe: datos, ataques, **fases** (cambio de ataques y varios ataques por turno) y habilidades únicas.
  - [ ] Zona del jefe: entrada, combate y **pantalla final** con créditos.
  - [ ] Profundidad: *Sorting Layers* (`Ground`, `World`, `Overhead`, `UI`) y *Sorting Groups*.
- 🎨 **Arte**
  - [ ] Jefe y tileset de su zona.

### Domingo 15 de noviembre — Revisión · **H3**
**Estado:** Pendiente · Día colchón
- [ ] **Playtest completo** de `MainMap` de principio a fin: medir la duración (objetivo de 10 minutos o más), buscar atascos, huecos y picos de dificultad.
- [ ] Corregir errores y actualizar la documentación.

**Entregable H3:** juego recorrible de principio a fin con jefe y final (el arte puede estar incompleto).

### Lunes 16 de noviembre — Pantalla de inicio y menús
**Estado:** Pendiente
- 🛠 **Unity**
  - [ ] Escenas `Boot` (splash: logo y avisos) y `MainMenu` de TD_Game: título, Nueva partida, Continuar, Opciones, Créditos y Salir.
  - [ ] Opciones: volumen general y de música, y dificultad si da tiempo.
  - [ ] Transiciones entre escenas con fundido (`SceneLoader` adaptado).
  - [ ] *Build Settings* con las escenas de TD_Game.
- 🎨 **Arte**
  - [ ] Logo y título, fondo del menú; arte del mapa (aplicar los tilesets en `MainMap`).

### Martes 17 de noviembre — UI de exploración, pausa y guardado
**Estado:** Pendiente
- 🛠 **Unity**
  - [ ] HUD de exploración: vida, estamina y habilidades desbloqueadas.
  - [ ] Menú de pausa: continuar, controles, guardar y salir al menú.
  - [ ] Game Over completo (reintentar o menú) y tutoriales en contexto.
  - [ ] **Guardar y continuar** (`SaveSystem` adaptado): progreso, habilidades, enemigos derrotados y posición.
  - [ ] Interacciones: carteles o puntos de interés del mapa (`IInteractable`).
- 🎨 **Arte**
  - [ ] Arte de los enemigos en el mapa; animaciones restantes.

---

## Fase 5 — Pulido y entrega (18–20 nov)

### Miércoles 18 de noviembre — Audio, arte final y entorno
**Estado:** Pendiente
- [ ] 🛠 Audio: música por zona, música de combate y del jefe, y efectos (UI, golpes, esquiva, habilidades). Se usan placeholders si el equipo no aporta audio.
- [ ] 🗺 Pulido visual de `MainMap`: decoración, variedad y capa `Overhead`.
- [ ] 🎨 Sustituir los últimos placeholders.

### Jueves 19 de noviembre — Balance, pruebas y build
**Estado:** Pendiente
- [ ] Balance: vida, daño, ventanas de esquiva y recompensas. Comprobar que dura 10 minutos o más y que el jefe supone un reto sin ser injusto.
- [ ] Playtests completos (al menos dos personas distintas), corrección de errores y Profiler.
- [ ] **Build de Windows** de prueba y recorrido completo con la build.

### Viernes 20 de noviembre — Entrega final · **H4**
**Estado:** Pendiente
- [ ] Build final probada de principio a fin.
- [ ] Actualizar `CLAUDE.md`, `DEVELOPMENT_LOG.md`, `TD_Game_ARQUITECTURA.md`, `TD_Game_GUIA.md` y `TD_Game_DISENO.md`.
- [ ] Marcar el estado final de este calendario e informe de cierre.

**Entregable H4:** juego final.

---

## Dependencias críticas

```
📐 Diseño (P0, 5–6) ──► enemigos (7, 10–12) · progresión (11) · mapa (13) · jefe (14)
🛠 Jugador (6–7) ──► [H1 · 8] ──► combate (9–10) ──► habilidades (11) ──► UI de combate (12) ──► [H2 · 12]
🗺 Boceto (5–6) ──► blockout (13) ──► jefe (14) ──► [H3 · 15] ──► arte en el mapa (16–18)
🛠 Menús (16) · HUD, pausa y guardado (17) ──► audio (18) ──► balance y build (19) ──► [H4 · 20]
🎨 Zona 1 (6–8) ─► enemigos 1–3 (9–11) ─► zona 2 (11) ─► UI (12) ─► zona 3 y objetos (13) ─► jefe (14) ─► logo y menú (16) ─► final (18)
```

## Riesgos

| Riesgo | Impacto | Mitigación |
|---|---|---|
| **Alcance grande para 15 días** | Alto | Prioridades definidas (imprescindible / importante / si da tiempo); se reutiliza la lógica del juego anterior; los hitos detectan pronto los retrasos |
| **Diseño sin cerrar** (P0) | Bloquea enemigos, mapa y jefe | Cerrar `TD_Game_DISENO.md` el 6 nov; mientras tanto se avanza en sistemas genéricos |
| **Volumen de arte** (3 zonas, 3 enemigos, jefe, jugador y UI) | Alto | Plan B: primitivas que se sustituyen sin cambiar código; priorizar jugador → enemigos → tilesets → UI |
| Conexión MCP inestable | Medio | Reiniciar con *Window > MCP for Unity > Start Server* y `/mcp` → Reconnect |

---

## Cambios al calendario

| Fecha | Cambio | Motivo |
|---|---|---|
| 3 nov | Replanificación a top-down con un juego nuevo y Tilemap rectangular; Fase 1 completada | Decisiones del equipo |
| 5 nov | Se añade el diseño en Aseprite y el mapa principal | Petición del equipo |
| **5 nov** | **El objetivo pasa de prototipo a juego final** (M1–M8, hitos H1–H4) | Actualización general del equipo |
| 5 nov | **Nuevo:** combate por turnos (9–12), progresión (11), 3 enemigos con habilidades únicas (7–12), jefe (14), pantalla de inicio y menús (16), HUD, pausa y guardado (17), audio (18), balance y build (19) | Requisitos del juego final |
| 5 nov | Se compactan tareas del plan anterior: animaciones el 11, prefabs el 12, profundidad el 14 e interacciones el 17 | Hacer sitio a los sistemas nuevos |
| 5 nov | **Nuevo:** plan de adaptación de los sistemas del juego anterior | Decisión: copiar y adaptar lo que cumpla `CLAUDE.md` |
| 5 nov | Ningún objetivo anterior eliminado; todos están integrados en el plan nuevo | — |
| 12 nov | Tareas de Toño (2.5–2.7, 3.1–3.3, 3.6–3.7, 3.10, 3.12) completadas y validadas en `TestLevel` con 3 enemigos de prueba y un HUD de depuración provisional; 3.11 sigue bloqueada por P0 | Sesión 7 del registro |
| 13 nov | **Nuevo:** salto en exploración con emboscada, orbes de alma, jade y tonalli (God of War clásico), Tonalli y Tonalli Burst, críticos con timing, combos, contraataque PERFECT, impacto (temblor y hit stop) y HUD de combate en Canvas | Petición del equipo (Toño); sesión 8 del registro |
| 13 nov | **Nuevo:** sprint en exploración; `TD_Game_ARQUITECTURA.md` y `TD_Game_GUIA.md` actualizados | Petición del equipo (Toño); sesión 9 del registro |
