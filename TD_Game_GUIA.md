# TD_Game — Guía: qué hay y cómo modificarlo

> Estado al **3 de noviembre de 2026**. Cómo está organizado por dentro: [TD_Game_ARQUITECTURA.md](TD_Game_ARQUITECTURA.md).
> Todo se modifica **dentro de Unity** (Inspector, Tile Palette, Input Actions Editor). No hace falta ninguna herramienta externa.

## Índice
1. [Abrir y probar el juego](#1-abrir-y-probar-el-juego)
2. [El jugador](#2-el-jugador)
3. [Los controles (Input Actions)](#3-los-controles-input-actions)
4. [El escenario (Tilemaps y Tile Palette)](#4-el-escenario-tilemaps-y-tile-palette)
5. [Los tiles y el arte placeholder](#5-los-tiles-y-el-arte-placeholder)
6. [Sustituir las primitivas por el arte del equipo](#6-sustituir-las-primitivas-por-el-arte-del-equipo)
7. [La cámara](#7-la-cámara)
8. [El orden de dibujo (quién tapa a quién)](#8-el-orden-de-dibujo-quién-tapa-a-quién)
9. [Crear cosas nuevas sin romper nada](#9-crear-cosas-nuevas-sin-romper-nada)
10. [Problemas frecuentes](#10-problemas-frecuentes)

---

## 1. Abrir y probar el juego

1. En la ventana **Project**, abre `Assets/TD_Game/Scenes/Testing/TestLevel.unity` con doble clic.
2. Pulsa **Play**.
3. **Haz clic dentro de la ventana Game**: si no tiene el foco, Unity no recibe el teclado.
4. Muévete con **WASD**, las **flechas**, el **stick izquierdo** o el **D-pad**.

Qué deberías ver:
- El rectángulo naranja (el jugador) se mueve a 4 tiles por segundo, **solo en 4 direcciones**: las diagonales se activan el 11 nov.
- La cámara lo sigue con suavidad y **no muestra nada fuera del mapa**.
- Al pasar junto a un pilar, el jugador queda delante o detrás según su posición.
- **Todavía atraviesa muros, agua y pilares**: las colisiones son del 12 nov.

> Los cambios que hagas en el Inspector **durante Play Mode se pierden** al salir. Para probar valores, cámbialos en Play Mode y, cuando te gusten, apúntalos y ponlos fuera de Play Mode.

---

## 2. El jugador

Selecciona **`Player`** en la jerarquía de `TestLevel`.

### Componentes y para qué sirve cada uno

| Componente | Función | Qué puedes cambiar |
|---|---|---|
| **Rigidbody2D** | Cuerpo físico que se mueve | No hace falta tocarlo. Los ajustes clave (gravedad 0, rotación congelada) los fuerza `PlayerMovement` al iniciar |
| **CapsuleCollider2D** | "Huella" del jugador en el suelo; con ella chocará contra muros desde el 12 nov | **Size** y **Offset**: hazla más ancha o estrecha para pasar por huecos. Está en los pies a propósito, para que la cabeza pueda solaparse con objetos que quedan "detrás" |
| **PlayerInputReader** | Lee los controles | **Move Action**: qué acción de input usa (`Player/Move`) |
| **PlayerMovement** | Mueve al jugador | **Move Speed** (tiles por segundo; 1 tile = 64 px) y **Allow Diagonals** (desactivado = 4 direcciones; activado = 8) |
| Hijo **Visual** → **SpriteRenderer** | Lo que se ve | **Sprite** (la imagen), **Color** (tinte), **Order in Layer** |

### Recetas

- **Más rápido o más lento:** `Player` → `PlayerMovement` → **Move Speed**. Por ejemplo, 6 para correr y 2,5 para caminar despacio.
- **Activar las 8 direcciones ya mismo:** marca **Allow Diagonals**. La diagonal no será más rápida, porque el código normaliza el vector.
- **Cambiar de sitio dónde empieza el jugador:** mueve el objeto `Player` en la escena. Pon los pies en el centro de una celda para que quede alineado; por ejemplo, x = -11,5 e y = -0,5.
- **Cambiar el color del placeholder:** `Player/Visual` → `SpriteRenderer` → **Color**.
- **Ver el script:** doble clic en el nombre del componente. Están en `Assets/TD_Game/Scripts/Player/`.

### Cómo está escrito el código (si quieres editarlo)
- **`PlayerInputReader.cs`** solo **lee** el input y lo deja en la propiedad `Move`. Si un día el input viene de otro sitio (un mando especial, una IA), solo se cambia este archivo.
- **`PlayerMovement.cs`** solo **mueve**. Lee `input.Move`, decide la dirección (método `ToMoveDirection`) y fija `body.linearVelocity` en `FixedUpdate`.
- Todo el código del juego nuevo va dentro de `namespace TD_Game { ... }`, para no chocar con las clases del juego anterior.

---

## 3. Los controles (Input Actions)

Archivo: `Assets/TD_Game/Settings/Input/PlayerControls.inputactions`. Ábrelo con **doble clic** y se abre el *Input Actions Editor*.

| Mapa | Acción | Tipo | Teclas / botones |
|---|---|---|---|
| `Player` | `Move` | Value · Vector2 | WASD (compuesto), flechas (compuesto), stick izquierdo, D-pad |

Esquemas de control: `Keyboard` y `Gamepad`.

### Recetas

- **Añadir otra tecla al movimiento** (por ejemplo IJKL):
  1. Selecciona la acción `Move`, pulsa **+** y elige **Add Up/Down/Left/Right Composite**.
  2. Asigna cada dirección con **Listen**.
  3. Marca el esquema `Keyboard`.
  4. Pulsa **Save Asset**, o activa **Auto-Save**.
- **Quitar un control:** selecciona el binding y pulsa **Delete**. Luego **Save Asset**.
- **Zona muerta del stick:** selecciona el binding `<Gamepad>/leftStick` → **Processors** → **+** → **Stick Deadzone**.
- **Acciones nuevas** (por ejemplo `Interact`, prevista para el 18 nov): **+** en *Actions*. Para usarla en código hace falta un campo `InputActionReference` en un componente, igual que `moveAction` en `PlayerInputReader`.

> Si renombras el mapa `Player` o la acción `Move`, revisa que `Player` → `PlayerInputReader` → **Move Action** siga asignado.

---

## 4. El escenario (Tilemaps y Tile Palette)

El mapa está en el objeto **`Grid`** y tiene tres capas, una por cada Tilemap hijo:

| Capa | Para qué | Notas |
|---|---|---|
| `Ground` | suelo: hierba, tierra, agua | siempre detrás de todo. La cámara usa sus bordes como límite del mapa |
| `Walls` | muros y roca | aquí irán las colisiones (12 nov) |
| `Decoration` | objetos altos: pilares, árboles… | se ordenan con el jugador según su altura en pantalla |

### Pintar o borrar

1. Abre **Window > 2D > Tile Palette**.
2. En el desplegable de paletas elige **`TD_Placeholder`**.
3. En **Active Tilemap** (arriba de la paleta) elige la capa: `Ground`, `Walls` o `Decoration`.
4. Elige un tile y una herramienta:
   - **B**: pincel.
   - **U**: rectángulo relleno.
   - **G**: relleno (cubo).
   - **D**: borrar.
   - **I**: cuentagotas.
5. Pinta en la vista **Scene**. Guarda la escena con **Ctrl+S**.

### Recetas

- **Agrandar el mapa:** pinta más suelo en `Ground`. La cámara recalcula los límites al iniciar la escena (`CameraFollow.RefreshBounds()`), así que no hay que configurar nada más.
- **Crear una sala nueva:** suelo en `Ground`, borde en `Walls`, y deja huecos en el muro para las puertas.
- **Objetos altos:** el pilar (`Deco_Pillar_Tall`) va **siempre** en `Decoration`. Ocupa una celda de base y sobresale una hacia arriba.
- **Agua:** por ahora es suelo (`Ground`) y se puede pisar. El 12 nov se decidirá si bloquea el paso.

---

## 5. Los tiles y el arte placeholder

```
Art/Tilesets/Placeholder/Textures/   ← imágenes (primitivas blancas)
Art/Tilesets/Placeholder/Tiles/      ← tiles = imagen + color
Art/Tilesets/Palettes/               ← la paleta
Art/Characters/Placeholder/          ← primitiva del jugador
```

| Tile | Imagen | Color |
|---|---|---|
| `Ground_Grass` | `Square_64` | verde |
| `Ground_Dirt` | `Square_64` | marrón |
| `Water` | `Square_64` | azul |
| `Wall_Stone` | `SquareOutline_64` (con borde) | gris |
| `Deco_Pillar_Tall` | `TallOutline_64x128` | beige |

### Recetas

- **Cambiar el color de un tipo de tile en todo el mapa:** selecciona el `.asset` del tile en *Project* → **Color**. Todas las celdas pintadas con ese tile cambian a la vez.
- **Crear un tile nuevo** (en Unity 6 se crean desde la Tile Palette; no hay opción en el menú *Create*):
  1. Abre **Window > 2D > Tile Palette** y elige `TD_Placeholder`.
  2. Arrastra un sprite desde *Project* (por ejemplo `Square_64`) a una celda libre de la paleta.
  3. Unity pregunta dónde guardar el tile. Guárdalo en `Art/Tilesets/Placeholder/Tiles/` con un nombre descriptivo (por ejemplo `Ground_Sand`).
  4. Selecciona el `.asset` creado y cambia su **Color**.
- **Duplicar un tile existente** (otra forma rápida): selecciona un `.asset` de `Tiles/`, pulsa **Ctrl+D**, renómbralo, cámbiale el color y arrástralo a la paleta.

---

## 6. Sustituir las primitivas por el arte del equipo

### Configuración de importación obligatoria (*Inspector* del PNG → **Apply**)

| Ajuste | Tiles | Personajes y objetos |
|---|---|---|
| Texture Type | Sprite (2D and UI) | Sprite (2D and UI) |
| Sprite Mode | Single (o Multiple si es un atlas) | Multiple (spritesheet) |
| **Pixels Per Unit** | **64** | **64** |
| Pivot | Center | **Bottom** (pies) |
| Filter Mode | **Point (no filter)** | **Point (no filter)** |
| Compression | **None** | **None** |
| Generate Mip Maps | Off | Off |
| Wrap Mode | Clamp | Clamp |

Un **tile** mide 64×64 px. Los objetos más altos (64×128, 64×192…) llevan el pivote en el centro de su celda base. En un sprite de 64×128 eso es (0.5, 0.25).

### Opción A: reemplazar el PNG (más rápido)
Sobrescribe el archivo, por ejemplo `Square_64.png`, con el arte nuevo **con el mismo nombre**, y revisa la importación. Todos los tiles y el jugador que lo usan se actualizan solos.
**Ojo:** `Square_64` lo usan hierba, tierra y agua a la vez, porque todos se tiñen. Con arte real, cada tile necesitará su propia imagen (opción B).

### Opción B: reasignar en cada tile (recomendado con arte real)
1. Importa el arte en una carpeta nueva, por ejemplo `Art/Tilesets/Forest/`, con la configuración de la tabla.
2. Selecciona cada tile (`Ground_Grass.asset`…) → **Sprite** = el nuevo → **Color** = blanco, para no teñirlo.
3. El mapa ya pintado cambia de aspecto sin repintar nada.

### El jugador
`Player/Visual` → `SpriteRenderer` → **Sprite**. Con spritesheet y animaciones, esto lo controlará el Animator a partir del 13 nov.

> **Nunca pongas el arte nuevo en `Assets/Resources/`**: esa carpeta es del juego anterior y un script suyo cambia la importación de lo que entra ahí.

---

## 7. La cámara

Selecciona **`Main Camera`**.

| Componente / campo | Función | Valor actual |
|---|---|---|
| Camera → **Size** | Mitad de la altura visible en tiles. Más grande = más zoom out | 4.21875 (15 × 8,4 tiles a 16:9) |
| CameraFollow → **Target** | A quién sigue | `Player` |
| CameraFollow → **Offset** | Desplazamiento respecto al objetivo | (0, 0,5): centra el cuerpo, no los pies |
| CameraFollow → **Smooth Time** | Segundos en alcanzar al objetivo; 0 = sin suavizado | 0,15 |
| CameraFollow → **Bounds Tilemap** | La cámara nunca enseña nada fuera de este Tilemap | `Ground` |
| CameraFollow → **Snap To Pixel Grid** | Redondea la posición a píxeles para que el arte no tiemble | activado |
| CameraFollow → **Pixels Per Unit** | Debe coincidir con el PPU del arte | 64 |

### Recetas

- **Ver más o menos mapa:** cambia **Size**. Para que el pixel art se vea nítido a 1080p usa valores que den una escala entera:

  | Size | Tiles visibles a 16:9 | Escala a 1080p |
  |---|---|---|
  | 8.4375 | 30 × 16,9 | ×1 |
  | **4.21875** | **15 × 8,4** | **×2** (actual) |
  | 2.8125 | 10 × 5,6 | ×3 |

  La fórmula es *Size = altura de pantalla ÷ (2 × 64 × escala)*.
- **Cámara más rígida o más suelta:** **Smooth Time**. Un valor de 0,05 la hace casi pegada; 0,3 la hace más lenta.
- **Ver los límites:** selecciona la cámara y aparece un **rectángulo cian** en la vista Scene.
- **Cambiar de objetivo desde código:** `GetComponent<CameraFollow>().Target = otroTransform;`. La cámara salta al nuevo objetivo sin suavizado.

> El encuadre actual solo es perfecto en 540p, 1080p y 4K a 16:9. Para el resto está pendiente decidir el paquete oficial **2D Pixel Perfect**.

---

## 8. El orden de dibujo (quién tapa a quién)

**Regla:** entre objetos con el mismo *Order in Layer*, **el que tiene los pies más abajo en pantalla se dibuja delante**.

Lo hacen posible tres ajustes:
1. **Edit > Project Settings > Graphics > Transparency Sort Mode** = *Custom Axis*, con eje (0, 1, 0).
2. Las capas `Walls` y `Decoration` tienen el *Tilemap Renderer* en **Mode = Individual**.
3. El `SpriteRenderer` del jugador tiene **Sprite Sort Point = Pivot**, y el pivote está en los pies.

### Recetas

- **Un objeto nuevo que se ordene con el jugador:** ponle *Order in Layer* = 0, *Sprite Sort Point* = Pivot y el pivote en su base.
- **Algo que siempre quede detrás** (alfombras, charcos): ponlo en `Ground` o dale un orden negativo.
- **Algo que siempre quede delante** (copas de árbol, techos): un orden positivo. Las *Sorting Layers* (`Overhead`…) llegan el 16 nov.

---

## 9. Crear cosas nuevas sin romper nada

| Regla | Por qué |
|---|---|
| Todo lo nuevo va dentro de `Assets/TD_Game/` | separa el juego nuevo del anterior |
| Los scripts nuevos van dentro de `namespace TD_Game` | el juego anterior tiene clases con nombres genéricos (`Health`, `GameState`, `Audio`…) |
| No mover ni borrar nada de `Assets/Resources/`, `Assets/Scenes/Boot.unity` ni `Assets/Resources/Enemies/Enemy_Quetzalcoatl.prefab` | el juego anterior se rompe, o regenera sus escenas automáticamente |
| Nombres en inglés para clases, archivos y variables | convención de `CLAUDE.md` |
| Valores ajustables como campos serializados (`[SerializeField]`) | se tunean desde el Inspector sin tocar código |
| Después de cada cambio: revisar la **Console** (filtra por `TD_Game`) y probar en Play Mode | la consola muestra 23 advertencias antiguas del juego anterior que se ignoran a propósito |

**Escenas de prueba nuevas:** duplica `TestLevel` (Ctrl+D en *Project*) y trabaja sobre la copia, para no estropear la de referencia.

---

## 10. Problemas frecuentes

| Problema | Causa | Solución |
|---|---|---|
| El jugador no se mueve | La ventana Game no tiene el foco | Haz clic dentro de la ventana Game |
| Error *"Move action is not assigned"* | `PlayerInputReader` no tiene la acción | `Player` → **Move Action** → elige `Player/Move` |
| El jugador atraviesa muros | Aún no hay colisiones | Llegan el 12 nov |
| El pixel art se ve borroso | Importación incorrecta | Filter **Point**, Compression **None**, PPU **64** |
| Hay líneas entre tiles | Sprites con filtro o tamaño distinto de 64 px | Revisa la importación; los tiles deben medir exactamente 64×64 px |
| La cámara enseña fuera del mapa | **Bounds Tilemap** vacío | Asigna `Grid/Ground` |
| Cambié valores y se perdieron | Estaban hechos en Play Mode | Haz los cambios fuera de Play Mode |
| Claude no puede conectar con Unity | El servidor MCP se detuvo | En Unity, *Window > MCP for Unity > **Start Server***; después, en Claude, `/mcp` → **UnityMCP** → **Reconnect** |
