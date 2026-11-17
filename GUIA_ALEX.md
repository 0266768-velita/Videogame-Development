# Guía de Alex: enemigos, menú y música

Esta guía explica **qué hace cada archivo de mi parte** y **qué decir si me preguntan**. Todo el código está en `Assets/TD_Game/Scripts/` y usa el namespace `TD_Game`.

---

## 0. Cómo correr el juego

1. En tu **Escritorio** está el acceso directo **Mictlan (Unity project)** (abre la carpeta `Documentos\Mictlan-v5`).
2. Abre **Unity Hub**: el proyecto **Mictlan-v5** ya está en la lista. Ábrelo (Unity 6000.5.9f1).
   - `Videogame-Development` es tu copia vieja (otra rama): ahí sale el Mictlán viejo. No la uses.
3. Arriba, en el menú **Mictlan**:
   - **Play From The Start**: empieza el juego desde el logo (como el botón del juego anterior).
   - **Rebuild Everything And Play**: prepara todo (arte, enemigos, menús, escenas) y empieza el juego.
   - **Rebuild The Main Map (deletes it)**: borra y vuelve a crear `MainMap`.
   - **Build Game For Friends (.exe)**: crea el juego para Windows en `Mictlan-v5\Builds\Mictlan`. Para compartirlo, comprime **toda** esa carpeta en un .zip (el .exe solo no funciona).

Orden del juego: logo → clasificación → descripción → menú → **Start Game** → **prólogo (cómic)** → mapa.

Controles: **WASD** moverse · **Shift** correr · **Espacio** saltar · **E** hablar con el vendedor · **Esc** pausa.
En combate: **1** atacar · **2/3** habilidades · **4** poción · **5** Tonalli Burst · **6** artefacto · **Q** terminar turno · **Espacio** esquivar · **1** interrumpir hechizos. También puedes hacer clic en los botones.

---

## 1. Enemigos

### Idea general (para explicarlo en 20 segundos)
> "Cada enemigo es un **prefab** con 3 piezas: `CombatEnemy` (su vida y empezar la pelea al tocarte), `EnemyAI` (patrulla, te ve, te persigue y regresa) y un **EnemyData**, que es un **ScriptableObject**: un archivo de datos con su vida, daño, velocidad, ataques y recompensas. Para hacer un enemigo nuevo no se programa: se crea otro EnemyData y se cambian números."

### Archivos

| Archivo | Qué hace | Funciones clave |
|---|---|---|
| `Combat/Data/EnemyData.cs` | **Datos** de un tipo de enemigo (vida, daño, velocidades, radios, ataques, oro, orbes). Es un `ScriptableObject`. | `PickAttack()` elige un ataque al azar según su `weight` (peso). |
| `Combat/Data/EnemyAttack.cs` | Un ataque: daño, cuántos golpes, cuánto avisa, si se **esquiva** o se **interrumpe**, y su **efecto único**. | `enum AttackEffect`: DrainStamina, HealSelf, Weaken, EmpowerSelf, LifeSteal, Stun, Guard. |
| `Enemies/CombatEnemy.cs` | La pieza del enemigo en la escena: su `Health`, empezar la pelea **al chocar** con el jugador, morir. | `OnCollisionEnter2D` → `TryStartCombat`. `OnDefeated()` avisa con el evento `Defeated`. |
| `Enemies/EnemyAI.cs` | El **cerebro** en el mapa: 3 estados. | `Patrol()`, `Chase()`, `Return()`, `CanSeePlayer()` (distancia + `Physics2D.LinecastAll` para que las paredes tapen la vista). |
| `Combat/EnemyTurn.cs` | El **turno del enemigo** en combate: aviso rojo, ventana para esquivar, golpe y efecto. | `ApplyEffect()` con un `switch` por cada efecto. |
| `World/BossArena.cs` | Entrada del jefe: al entrar se cierra la reja y suena la música del jefe. | `OnTriggerEnter2D`. |

### Los 3 enemigos y el jefe (datos en `ScriptableObjects/Enemies/`)
- **Shadow Wolf** (zona 1): rápido y débil, ataca primero. Habilidad única: **Blood Howl** (se hace más fuerte, `EmpowerSelf`). Al verte, **aúlla** y los lobos cercanos también te persiguen (`alertAlliesRadius`).
- **Mictlan Witch** (zona 2): hechizos que **se interrumpen** con 1 en vez de esquivarse. Roba vida (`LifeSteal`), te quita estamina y te debilita.
- **Quinametzin Giant** (zona 3): lento y con **piel gruesa** (recibe 20 % menos daño, `damageTakenMultiplier`). **Ground Quake** no se puede esquivar y te **aturde** (pierdes un turno, `Stun`). **Stone Skin** lo protege (`Guard`).
- **Quetzalcóatl** (jefe): **2 fases**. Al bajar a 50 % de vida cambia a *Feathered Serpent*, pega más fuerte y cambia de ataques (`phaseTwoAttacks`).

### La IA en palabras simples (`EnemyAI`)
```
Patrol  → camina a puntos al azar cerca de su casa.
          ¿Ve al jugador (cerca y sin pared en medio)?  → Chase
Chase   → camina hacia el jugador.
          ¿El jugador se alejó mucho o yo me alejé mucho de casa? → Return
Return  → regresa a casa → Patrol
```
Se mueve con `Rigidbody2D.MovePosition` (física de Unity) y, si choca con una pared, prueba solo horizontal o solo vertical.

### Oro, tienda y llave
- `EnemyData.goldOnDefeat`: oro que da cada enemigo (lobo 8, bruja 15, gigante 30).
- `Combat/CombatRewards.cs` → al ganar suma el oro: `progression.AddGold(gold)`.
- `Player/PlayerProgression.cs` → `Gold`, `AddGold()`, `TrySpendGold(precio)` (si alcanza, paga y regresa `true`), `HasKey`.
- `Interactions/MerchantShop.cs` → el **vendedor** (pegado a la pared norte del Valle de Tlazoltéotl). Te paras enfrente, **E** abre su ventana:
  - **Poción** (20 de oro): `TrySpendGold(20)` y `AddPotions(1)`.
  - **Apostar** 10 o 50: `Random.value < 0.5f` es como lanzar una moneda: ganas el doble o pierdes lo apostado.
  - **Artefacto**: el **Espejo Humeante de Tezcatlipoca** (60 de oro, una vez). `Unlock(artifact)` lo desbloquea y se usa en combate con la tecla **6** (`Combat/Abilities/MirrorAbility.cs`: golpea a todos los enemigos y te cura la mitad del daño).
- `Interactions/ObsidianKey.cs` → cuando **todos** los enemigos normales están muertos, aparece la **llave de obsidiana** junto a ti (como en el juego viejo).
- `Interactions/KeyDoor.cs` → la puerta de Quetzalcóatl: un collider sólido que solo se apaga si tocas la puerta con la llave.

### Gore (`World/GoreEffects.cs`)
- Cada golpe → mancha de sangre en el piso **y sobre el enemigo**: mientras menos vida (`Health.Normalized`), más manchas lleva encima.
- Al morir → `Sprite.Create` corta el dibujo del enemigo en una cuadrícula de pedazos; cada pedazo tiene un `Rigidbody2D` con una velocidad al azar (`linearVelocity`) y `linearDamping` lo frena solo. Además huesos y un charco. `Destroy(objeto, 60)` los borra después de 60 s.
- Se apaga en **Options → Gore**.

---

## 2. Menú

| Archivo | Qué hace |
|---|---|
| `UI/BootScreens.cs` | Pantallas al abrir el juego: **logo** → **clasificación M 17+** → **descripción**. Cualquier tecla salta. Usa *coroutines* (`IEnumerator` + `yield return`) para esperar y hacer los fundidos. |
| `UI/TitleMenu.cs` | Menú principal: "PRESS ENTER", después **Start Game, Continue, Options, Credits, Exit**. Options: volumen general, música, efectos, pantalla completa y gore. |
| `UI/PauseScreen.cs` | Pausa con **Esc**: `Time.timeScale = 0` congela el juego; Resume lo regresa a 1. |
| `UI/MenuWidgets.cs` | "Caja de herramientas" para crear textos y botones con la fuente **Pirata One** y el **marco azteca**. |
| `Managers/SceneFader.cs` | Cambia de escena con fundido a negro (`SceneManager.LoadSceneAsync`). |
| `Managers/GameSave.cs` + `SaveController.cs` | Guardado: se guarda solo al ganar cada pelea, en un archivo JSON (`JsonUtility`). **Continue** lo carga. |
| `UI/DefeatScreen.cs` | Game Over: Retry (vuelves al último checkpoint) o Main Menu. |
| `UI/CreditsScreen.cs` | Créditos al vencer a Quetzalcóatl. |
| `UI/PrologueComic.cs` | La historia del GDD en viñetas de cómic al darle **Start Game**. Las viñetas (imagen + texto) se cambian en el Inspector de la escena `TD_Prologue`. |
| `UI/AztecFrames.cs` | Los marcos aztecas del juego viejo (copiados): dibuja la greca pixel por pixel del tamaño exacto de cada botón. |
| `UI/Minimap.cs` | Minimapa arriba a la izquierda. **Sirve para cualquier mapa**: mide el Tilemap `Ground`, pone una **segunda cámara** encima que ve todo el mapa y lo dibuja en una **RenderTexture**; un `RawImage` la muestra. Un punto rojo marca al jugador. |

**Cómo explicarlo:** *"Los menús se arman al iniciar con funciones de Unity UI (Canvas, Button, Text). Cada botón tiene `onClick.AddListener(función)`. El menú solo llama a `SceneFader.Load(escena)` para cambiar de escena y a `GameSave` para guardar/cargar."*

El marco azteca y la oscuridad se crean con **TD_Game > Generate Menu Frame And Darkness** (`Editor/ArtGenerator.cs`): dibuja los pixeles con `Texture2D.SetPixel` y los guarda como PNG.

---

## 2b. Tlahuelli (animación)

| Archivo | Qué hace |
|---|---|
| `Player/PlayerSpriteAnimator.cs` | Escoge la fila del sprite según hacia dónde mira (abajo = de frente, arriba = de espaldas, lado) y avanza los 4 cuadros al caminar (más rápido al correr). Voltea la antorcha y el Macuahuitl de mano. |
| `World/ZoneArea.cs` + `UI/ZoneTitleCard.cs` | Al entrar a una zona aparece su nombre grande y se desvanece (como en GTA). |
| `Player/MacuahuitlSwing.cs` | Mientras atacas en combate, levanta el Macuahuitl y lo baja **estirándolo** (`localScale`) para alcanzar al enemigo. |

## 2c. El mapa (dentro de Unity)

- `Editor/TerrainArt.cs` dibuja los tiles con código (`PixelCanvas`: rectángulos, círculos, ruido y contornos) y los guarda como **Tile** en `Art/Tilesets/Mictlan`. Se pueden pintar a mano con la **Tile Palette**.
- `Editor/MainMapBuilder.cs` → `Plan()` decide qué es cada celda con reglas simples: bosque, claro, camino, río, puente, roca, pozo, templo. El agua y los pozos escogen su dibujo según qué lados tocan tierra (16 combinaciones), por eso las orillas son redondas.
- `Editor/PropArt.cs` dibuja la carpa del vendedor, el vendedor, la entrada del templo y las viñetas del cómic.

## 3. Música y sonidos

| Archivo | Qué hace |
|---|---|
| `Audio/AudioLibrary.cs` | **ScriptableObject** con todas las canciones (menú, exploración, combate, jefe, game over) y los efectos **por nombre** (`"hurt"`, `"victory"`...). Asset: `Assets/TD_Game/Audio/AudioLibrary.asset`. |
| (importante) | Cada escena tiene un `GameAudio` que **necesita** el `AudioLibrary` asignado; si está vacío, el juego queda mudo. *Rebuild Everything* lo asigna solo. |
| `Audio/GameAudio.cs` | El **reproductor**. Usa dos `AudioSource` para pasar de una canción a otra suavemente (*cross-fade*) y varios para los efectos. Sobrevive entre escenas con `DontDestroyOnLoad`. Guarda los volúmenes con `PlayerPrefs`. |
| `Audio/GameplayAudio.cs` | **Escucha eventos** del juego y pone el sonido correcto: empieza la pelea → música de combate (o del jefe), ganas → `victory`, te pegan → `hurt`, PERFECT → `perfect`... |

Usarlo desde cualquier script, una línea:
```csharp
GameAudio.PlaySfx("hurt");                 // un efecto
GameAudio.PlayMusic(MusicTrack.Battle);    // cambiar canción
```

**Cómo explicarlo:** *"La lógica del combate no toca el audio. Lanza eventos (`CombatStarted`, `CombatWon`...) y `GameplayAudio` los escucha (`+=`) y reproduce el sonido. Así cada parte está separada."*

---

## 4. Lo mínimo de C# / Unity que debo saber

| Concepto | En una frase |
|---|---|
| `MonoBehaviour` | Un script que se pega a un objeto. `Start()` corre una vez, `Update()` cada frame, `FixedUpdate()` cada paso de física. |
| `[SerializeField]` | Hace que una variable aparezca en el **Inspector** para cambiarla sin tocar código. |
| `ScriptableObject` | Un archivo de **datos** (como EnemyData o AudioLibrary). |
| `OnTriggerEnter2D` / `OnCollisionEnter2D` | Unity los llama cuando algo entra a una zona o choca. |
| `event` y `+=` | "Avísame cuando pase X". Ej.: `CombatEnemy.Defeated += OnEnemyDied;` |
| Coroutine (`IEnumerator`, `yield return`) | Una función que puede **esperar** (`WaitForSeconds`) sin congelar el juego. |
| `Instantiate` / `new GameObject` / `Destroy` | Crear y borrar objetos en el juego. |
| `PlayerPrefs` | Guardar números pequeños (volumen, gore on/off). |

---

## 5. Herramientas del menú **TD_Game** (todo dentro de Unity)

- **Build Front End (Menus, Audio, Scenes)**: hace todo (arte, enemigos, menús, mapa, Build Settings). No borra nada que ya exista.
- **Build Main Map (big, Adrian's style)**: crea `MainMap` (borra primero el viejo para regenerarlo). Las posiciones de enemigos, tienda y jefe están al inicio de `Editor/MainMapBuilder.cs`.
- **Build Enemy Data And Prefabs**: crea los EnemyData y prefabs de los enemigos.
- **Generate Menu Frame And Darkness**: crea el marco azteca y la oscuridad.

Para un **cambio de mapa** (pedido de Adrián): pon un objeto con `World/SceneDoor.cs` (Collider *Is Trigger*, escena destino y nombre del punto de llegada) y en la otra escena un objeto con `World/SpawnPoint.cs` con ese mismo nombre.
