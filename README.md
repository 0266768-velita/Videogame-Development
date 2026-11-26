# Mictlán

> **Fecha:** 26 de noviembre de 2026 · **Rama:** `v0.6` (último commit `0d35fc4`) · **Versión del juego:** 0.9 · **Motor:** Unity 6000.5.9f1

**Mictlán** es un juego 2D top-down de pixel art, con estética mexica oscura, de **Micta Forge**.
- Tlahuelli recorre el inframundo mexica: bosques, valles, un desierto y una pirámide.
- Pelea por turnos **en el mismo mapa** contra lobos, brujas y gigantes, consigue la llave de obsidiana y se enfrenta a **Quetzalcóatl**.
- El combate usa estamina y recursos con nombres nahuas (Teyolia, Ihiyotl, Tonalli), y se defiende con **un solo botón**, justo antes del golpe, al estilo de *Clair Obscur: Expedition 33*.

**Estado en `v0.6`:** el juego se puede jugar de principio a fin.
- Logos → menú → historieta → mapa → combates → misión secundaria → llave → jefe → créditos.
- Tiene guardado, audio, menús, mapa completo, tiendas, NPC y soporte de teclado, ratón y mando con iconos automáticos.

---

## Requisitos

| Requisito | Detalle |
|---|---|
| Unity | **6000.5.9f1** (Unity Hub) |
| Plataforma | Windows (la build es `StandaloneWindows64`) |
| Paquetes (se instalan solos con el proyecto) | `com.unity.inputsystem` 1.20.0 · `com.unity.2d.tilemap` · `com.unity.2d.sprite` · `com.unity.ugui` |
| Opcional: MCP | `com.coplaydev.unity-mcp` (puente entre Unity y Claude, solo en el editor). Servidor `mcpforunityserver` 10.3.0 por `uvx`; en Unity: *Window > MCP for Unity* |

## Instalación y ejecución rápida

1. Clona el repositorio y cambia a la rama del juego:
   ```
   git clone https://github.com/0266768-velita/Videogame-Development.git
   cd Videogame-Development
   git checkout v0.6
   ```
2. **Unity Hub → Add → Add project from disk** → la carpeta clonada. Ábrela con Unity **6000.5.9f1** y espera la primera importación.
3. Menú **Mictlan > Play From The Start**, o abre `Assets/TD_Game/Scenes/MainMenu/TD_Boot.unity` y pulsa **Play**.
4. Para jugar directamente en el mapa: abre `Assets/TD_Game/Scenes/Levels/MainMap_CSV.unity` y pulsa **Play**.

**Crear el ejecutable:** **Mictlan > Build Game For Friends (.exe)** → `Builds/Mictlan/Mictlan.exe`.

## Controles

Verificados contra `Assets/TD_Game/Settings/Input/PlayerControls.inputactions` y el código. Los iconos de la interfaz cambian solos según el último dispositivo usado (**Options → Button icons**: Auto, Keyboard, Xbox o PlayStation).

**Exploración**

| Acción | Teclado / ratón | Xbox | PlayStation |
|---|---|---|---|
| Moverse (8 direcciones) | WASD / flechas | stick izquierdo / D-pad | stick izquierdo / D-pad |
| Correr (mantener) | Shift izquierdo | RT | R2 |
| Saltar (troncos, agua estrecha, emboscadas) | Espacio | A | ✕ |
| Interactuar (NPC, tiendas, braseros, puertas) | E | Y | △ |
| Mapa completo | M o Tab | View | Share/Create |
| Pausa | ESC | Start | Options |

**Combate**

| Acción | Teclado / ratón | Xbox | PlayStation |
|---|---|---|---|
| Atacar (pulsa otra vez justo antes del golpe = crítico) | 1 | A | ✕ |
| Habilidad 1 (Mictlan Fury) / Habilidad 2 (Seismic Jump) | 2 / 3 | X / Y | □ / △ |
| Poción | 4 | D-pad ↑ | D-pad ↑ |
| Tonalli Burst (barra azul llena) | 5 | LB | L1 |
| Artefacto (Smoking Mirror, se compra) | 6 | RT | R2 |
| Terminar turno | Q | RB | R1 |
| Cambiar de objetivo | ← / → o clic en el enemigo | D-pad ← / → | D-pad ← / → |
| **Esquivar / interrumpir** (justo antes del golpe) | Espacio | B | ○ |

**Historieta:** Siguiente = Espacio / Enter / clic / A (✕) · Omitir = **mantener** ESC / B (○).

## Qué se puede jugar hoy

1. **Inicio:** logo del estudio, clasificación «Rated M», menú (New Game, Continue, Options, Credits, Exit) e historieta del prólogo.
2. **Mapa diseñado por el equipo** (`Settings/Maps/mapa_juego_150x70.csv`; la parte jugable son 125 × 70 celdas = 250 × 140 tiles), con 4 regiones:
   - Territorios de Tláloc;
   - Valle de Tlazoltéotl;
   - Valle de las Serpientes;
   - Templo Celestial.
   Hay ríos y barrancos en los que se puede caer, puentes, piedras para saltar y la pirámide. Setos de árboles y una franja de rocas marcan por dónde se puede ir.
3. **36 enemigos** en 18 campamentos (lobos, brujas y gigantes), más fuertes cuanto más lejos del inicio. Al tocarte, la pelea empieza allí mismo.
4. **Combate por turnos:**
   - varias acciones por turno con estamina;
   - combos y críticos con *timing*;
   - esquiva o interrupción de un botón con contraataque automático y bloqueo anti-spam;
   - Tonalli Burst;
   - habilidades que se desbloquean venciendo enemigos.
5. **Al ganar:** orbes de alma (carga del Macuahuitl), jade (vida) y Tonalli, oro y guardado automático.
6. **Al perder:** pantalla «YOU HAVE FALLEN» y vuelta al inicio del mapa.
7. **Mundo:**
   - 3 comerciantes (pociones, artefacto, skins y apuestas);
   - 9 aldeanos con diálogos;
   - 4 cofres en el mapa y la ofrenda de la cima de la pirámide;
   - el puzle de los cuatro fuegos;
   - la misión **El prisionero de las cuatro brujas** (pelea, rompecabezas de la pintura y recompensa);
   - mapa completo (M) y contador «Enemies Left».
8. **Final:** al derrotar a todos los enemigos aparece la llave de obsidiana. Se entra a la pirámide por la boca de serpiente de la cima; en la antesala, la llave abre la puerta de obsidiana; tras el laberinto, las rejas se cierran y empieza el combate contra **Quetzalcóatl**, que cambia de forma a mitad de la pelea. Al vencerlo se ven los créditos.

## Estructura del proyecto

```
Assets/TD_Game/
├── Art/                 personajes, props, tiles, historieta, efectos, UI y fuente Pirata One
├── Audio/               AudioLibrary, música, efectos y voces
├── Prefabs/             Player, enemigos, jefe y orbe
├── Scenes/              MainMenu/ (TD_Boot, TD_MainMenu, TD_Prologue) · Levels/ (MainMap_CSV, MainMap) · Testing/
├── ScriptableObjects/   estadísticas del jugador, habilidades, enemigos y skins
├── Scripts/             Audio, Camera, Combat, Enemies, Interactions, Managers, Player, UI, World, Editor, Testing
├── Settings/            Input (PlayerControls.inputactions) y Maps (CSV del mapa)
├── Materials/ · Shaders/   recolor de skins (SpriteSkin)
```

Todo el código usa el namespace `TD_Game`.

## Documentación

| Documento | Para qué |
|---|---|
| [TD_Game_ARQUITECTURA.md](TD_Game_ARQUITECTURA.md) | cómo está construido el juego, sistema por sistema, con diagramas |
| [TD_Game_GUIA.md](TD_Game_GUIA.md) | cómo cambiar, personalizar o arreglar cualquier elemento desde Unity |
| [CLAUDE.md](CLAUDE.md) | reglas del proyecto y decisiones técnicas |
| [DEVELOPMENT_CALENDAR.md](DEVELOPMENT_CALENDAR.md) | calendario y tareas |
| [DEVELOPMENT_LOG.md](DEVELOPMENT_LOG.md) | registro de cambios por sesión |
| [AI_USAGE.md](AI_USAGE.md) | uso de IA en el proyecto: herramientas, sesiones, verificación, errores y límites |

## Pendientes

- Probar con **mandos físicos** Xbox y PlayStation: solo se probaron con dispositivos virtuales.
- Guardar los cofres abiertos: hoy se pueden volver a abrir al cargar la partida.
- Voces del comerciante grabadas por una persona: hoy son de texto a voz.
- Animaciones con Animator: hoy son procedurales y por cuadros.
- Quitar de *Build Profiles* las escenas que el juego no carga (`MainMap`, `TestLevel`).

## Créditos

**Micta Forge** (según los créditos del juego, `Scripts/UI/CreditsScreen.cs`):

| Persona | Aporte |
|---|---|
| Antonio Enríquez Velasco | combate, animación y efectos |
| Alejandro Cornejo Pérez | enemigos, menús, audio y guardado |
| Adrián Gallardo Medina | mapa y diseño de niveles |
| Jetzuvely del Carmen González Gutiérrez | arte y sprites |
| Claude (asistente de IA de Anthropic) | programación, arte procedural y sonido |

Hecho con Unity 6. Fuente: Pirata One (SIL Open Font License).
