# Registro de desarrollo

Registro breve de cambios, problemas y decisiones. La entrada más reciente va primero.
Planificación: `DEVELOPMENT_CALENDAR.md` · Reglas y decisiones técnicas: `CLAUDE.md`

---

## 2026-11-25 — Sesión 24b: correcciones de v0.9 tras jugarla

**Sonido**
- Golpes nuevos (`SfxSynth.Impact`), hechos por capas: chasquido, golpe grave que cae, crujido, saturación y una sala pequeña. 3 variantes para `enemy_hit` y 3 para `hurt`.
- La voz del comerciante ya **no usa el texto a voz de Windows**, que sonaba robótico:
  - ahora es una voz de personaje sintetizada: grave, áspera y murmurada, sigue las vocales y el ritmo del subtítulo y tiene la risa «heh heh»;
  - si alguien del equipo graba las líneas en `Audio/VoiceRecordings/merchant_*.wav`, se usan esas (más graves y ásperas).

**Salto y orden de dibujo**
- El `Player` tiene un `SortingGroup`:
  - en el aire sube 4 órdenes y se ve por **encima** de troncos y rocas;
  - la antorcha ya no atraviesa los techos.

**Arte del equipo** (`PropArt.V09Team`), usado tal como está dibujado, a 40 PPU:
- roca y guijarro de la compañera, separados del PNG;
- 2 árboles (28 colocados en las orillas de los bosques; el que brilla, más en Tlazoltéotl);
- niebla de 12 cuadros (31 manchas junto al agua y en el valle de las brujas);
- arena de 64 px, en manchas.
- Se siguen usando las rocas y los árboles que ya había.
- Las rocas ya no se reparten al azar: van en familias de 2–4 bajo los árboles, en las orillas y en los barrancos, cada una con su sombra y una base sólida. Los guijarros se saltan.

**Casa de las brujas**
- Pared norte de dos filas:
  - la pintura y las cadenas cuelgan **en** la pared;
  - Painal está encadenado a ella y es sólido;
  - velas, cráneos y estantes con colisión.
- Las brujas de la casa se llaman **Cihuateotl** (`EnemyData_Cihuateotl`).

**Puzzle de los cuatro fuegos**
- Los braseros con joyas y el cofre tienen una base sólida.

**Nombres aztecas**
- Cuetlachtli (lobo), Tlahuelpuchi (bruja) y Quinametzin (gigante).

**Dificultad**
- Fuerza 1,35–2,4 según la distancia al inicio.
- Ya no se regala una poción por victoria.

**Economía**
- Lobo 2, bruja 4 y gigante 8 de oro.
- Cofres de 8 + 4·i (cueva 30).
- Poción 40, artefacto 200, apuestas de 15 / 40 con un 40 % de ganar.
- Skins de 120 a 300.

**Otros**
- Orbes: pequeños desde el primer cuadro y como máximo 4 por tipo.
- El *hit stop* al matar baja de 0,12 a 0,05 s: era el «se traba».
- Letrero de «Enemies Left» más ancho y con ajuste de tamaño.
- Créditos: Claude (asistente de IA de Anthropic) aparece al final y en el menú.

**Pruebas**
- Self-test 49/49, sin excepciones.
- .exe reconstruido.

## 2026-11-25 — Sesión 24: versión 0.9 (mapa, pirámide, saltos, NPC, tiendas, misión de las brujas)

Todo se aplica **sobre** `MainMap_CSV` con **TD_Game > v0.9 > Upgrade MainMap_CSV** (`Editor/V09/V09WorldUpgrade*.cs`). El mapa del equipo se conserva y lo nuevo cuelga de objetos `V09_*`. Se puede volver a ejecutar sin duplicar nada.

**UI**
- Pantalla «Rated M»: cada texto tiene su caja dentro del marco y se encoge si no cabe.
- Versión **v0.9** (menú y `bundleVersion` 0.9.0).
- **Mapa de pantalla completa** (`UI/WorldMapScreen.cs`), con M / Tab / View:
  - jugador, enemigos vivos y jefe;
  - tiendas, NPC, misión secundaria y lugares importantes (`World/MapMarker.cs`);
  - nombres de las regiones, leyenda y enemigos restantes.
- **Enemies Left: X** abajo a la derecha (`UI/EnemiesLeftCounter.cs`). Se oculta en combate, en diálogos y con el mapa abierto.

**Mapa y arte** (todo generado en código, en el estilo de 16 px del juego)
- **Terreno:**
  - pasto en zigzag mezclado en manchas suaves con el pasto de Vely;
  - arena con ondas continuas;
  - cempasúchil en Tláloc.
- **Hoyos con profundidad:** pared de tierra en capas, oscuridad, bordes redondeados y borde iluminado.
- **5 puentes nuevos y 4 cruces de piedras para saltar.** Se colocan donde más camino ahorran.
- Los **bloques de templo sueltos (P)** se cambian por troncos caídos para saltar sobre los caminos, y por rocas y arbustos.
- Los **cuadritos de tierra sueltos** de las líneas punteadas se convierten en un sendero de piedra.
- **La roca de la compañera** se recorta del JPG (fondo quitado, escala real de 48 px) y se **mezcla** con las rocas que ya había: grupos, tamaños y espejos. Hay 135 rocas y 209 detalles por región:
  - hongos, helechos y arbustos;
  - árboles secos, cráneos y cuervos;
  - cactus, agaves y cerámica rota.
- **Puntos de referencia:**
  - la Ceiba sagrada, la Roca Calavera, la Cabeza del Dios Caído y la Estatua de Xólotl;
  - estelas con antorcha en los cruces.
- **Pirámide nueva** (`PropArt.V09Pyramid`, 40 × 26 tiles):
  - 5 cuerpos en talud-tablero, friso de cráneos, cabezas de serpiente, grecas, grietas, musgo y enredaderas;
  - escalinata con alfardas que terminan en serpientes emplumadas;
  - santuario en la cima con puerta en forma de boca de serpiente.
  - **Se puede subir:** glifos que se leen y una ofrenda.
  - Alrededor: braseros, antorchas, columnas, chacmool, tzompantli, estandartes y estelas.

**Salto y caídas**
- `PlayerJump`: en el aire ignora la capa **LowObstacle** (troncos, arbustos, muros bajos) y salta un poco más lejos (0,55 s).
- `PlayerHazards` (nuevo): el agua y los hoyos van en los Tilemaps `Water` y `Pits`, en la capa **Hazard**. Los enemigos chocan con ellos; el jugador **cae**.
  - Animación distinta para cada caída:
    - hoyo: tambaleo, giro y oscuridad;
    - agua: chapoteo, ondas y hundimiento.
  - Sonidos propios; la caída cuesta un 8 % de la vida y nunca mata.
  - Reaparece en el último suelo firme, con parpadeo.
- Capas nuevas: Player (8), Hazard (9), LowObstacle (10).

**Combate**
- **20 encuentros** y 40 enemigos: 15 campamentos de 2–3 enemigos mezclados y 5 solitarios. La fuerza crece con la distancia.
- `CombatEnemy.encounter`: el campamento entero entra a la pelea y se avisa entre sí (`EnemyAI`). `CombatManager.maxEnemies` = 4.
- `CombatEnemy.optional` (enemigos de misión): no cuentan para la llave ni para el contador.

**Tiendas y economía**
- Un comerciante en cada región (4).
- **Voces:** saludo, compra, apuesta ganada y perdida, despedida, con varias líneas para cada momento. Son líneas originales en el estilo del comerciante de RE4 (incluido «What do you want, stranger?»).
  - Se generaron con la voz de texto a voz de Windows (`Audio/VoiceRaw~`) y se procesaron con `SfxSynth` (más grave, con aspereza y una sala pequeña).
- **Economía:**
  - lobo 3, bruja 5 y gigante 10 de oro;
  - cofres más pobres;
  - poción 25, artefacto 120;
  - apuestas de 10 / 30 con un 45 % de ganar.
- **Skins de Tlahuelli** (`SkinCatalog`, shader `TD_Game/SpriteSkin`): recolorean solo su ropa azul.
  - Jaguar 60, Quetzal 75, Tezontle 90, Hueso de Águila 110, Obsidiana 150.
  - Vista previa animada antes de comprar; las compradas se equipan gratis.
  - Se guardan en el save (`ownedSkins`, `equippedSkin`).

**Sonido** (`Editor/V09/SfxSynth.cs`, sonidos originales hechos con código)
- Se reemplazan el aviso de ataque enemigo (`telegraph`, antes estridente) y el golpe al jugador (`hurt`). Cada uno tiene 3 variantes.
- `swing` y `enemy_hit` reciben variantes nuevas.
- Sonidos nuevos: caída al hoyo, chapoteo, cadenas y campanas del rompecabezas.
- `GameAudio`:
  - escoge variantes al azar;
  - no repite el mismo efecto en menos de 60 ms;
  - tiene un canal de voz propio.
- **Ambiente** (`AmbientSoundscape`): bosque, río, viento del desierto e interior. Depende de dónde está el jugador.

**NPC** (`DialogueNPC`, `DialogueBox`, `InteractPrompt`, `InteractSpot`)
- 9 NPC con personalidad y un motivo para estar donde están:
  - Xochitl, la barquera: saltos y caídas;
  - Cuauhtli, fantasma de guerrero águila: consejos de combate;
  - Itzel: pista de la casa de las brujas;
  - Tecolotl: la pirámide;
  - Tzontli: gigantes y barrancos;
  - Atl: el río y Xólotl;
  - un xoloitzcuintle: las piedras del río;
  - Yaotl: la cueva y la llave;
  - Citlali: las Cihuateteo.
- Sobre cada uno, «!» dorado si tiene algo nuevo y «…» si ya hablaste con él.

**Misión: El prisionero de las cuatro brujas** (`World/WitchHouseQuest.cs`, `UI/PaintingPuzzle.cs`, `World/HouseDoor.cs`)
- La casa está escondida al oeste del Valle de Tlazoltéotl, con velas moradas, árboles secos y cuervos.
- El interior es un cuarto aparte con su propia cuadrícula: círculo ritual, velas, caldero, estantes, cadenas y la pintura rota.
- Pasos de la misión:
  1. combate contra las 4 brujas juntas;
  2. el prisionero explica que las cadenas están atadas a la pintura;
  3. rompecabezas 3 × 3 en una pantalla propia (intercambiar piezas con ratón o teclado / mando, brillo y campanita en cada pieza correcta);
  4. liberación con efectos;
  5. historia de Painal, el corredor de los dioses;
  6. **Amuleto de Painal:** velocidad × 1,3 y estela de viento al correr.
- Se guarda con los ids `witch_puzzle_solved` y `painal_freed`, así que no se repite.

**Pruebas**
- `Testing/V09SelfTest.cs` + **TD_Game > v0.9 > Run Self Test** juega solo en Play Mode con un teclado virtual. Informe y capturas en `Logs/SelfTest`.
- Resultado: **49/49 comprobaciones bien** y sin excepciones en el log:
  - contador y mapa;
  - salto sobre tronco;
  - caída al agua y al hoyo con recuperación;
  - campamento de 3;
  - tienda con skins y apuesta;
  - diálogo;
  - misión completa con 4 brujas, rompecabezas y amuleto;
  - guardado.
- `V09BootShots`: capturas de «Rated M» y del menú (v0.9).
- Errores encontrados y corregidos:
  - el mapa y los diálogos salían al doble de tamaño (eran hijos del Canvas del minimapa);
  - el cartel de interacción vacío se quedaba visible;
  - la pintura se encimaba con el estante;
  - los hoyos se veían como cuadros negros.

**Pendiente / limitaciones**
- Las voces son de texto a voz (Windows), no de un actor.
- El protagonista sigue llamándose **Tlahuelli** en el juego. «Tonalli» es la barra de espíritu.
- No se jugó todo el mapa a mano de principio a fin; las pruebas cubren cada sistema.

## 2026-11-24 — Sesión 23 (Toño): iconos por dispositivo, historieta moderna y auditoría

Siguiendo `auditor_a_de_juego_historieta_y_detecci_n_de_mando.md`.

**Detección de dispositivo**
- `UI/InputDevices.cs`: detecta el último dispositivo usado (teclado / ratón, mando Xbox o PlayStation) en tiempo real.
  - PlayStation = DualShock o DualSense, o un mando de Sony; cualquier otro mando cuenta como Xbox.
  - Los sticks y gatillos solo cuentan por encima de 0,5 (la deriva no cambia los iconos).
- `UI/InputPrompts.cs` (reescrito):
  - los avisos piden **acciones** (`Dodge`, `Interact`, `Next`, `Skip`, `Attack`…) y no teclas, porque Espacio salta en exploración (A) pero esquiva en combate (B);
  - `Label()` da SPACE / B / CIRCLE, y `Localize()` cambia `{Acción}` por `[BOTÓN]` dentro de una frase.
- **Opción «Button icons»** (menú principal y pausa): Auto / Keyboard / Xbox / PlayStation; por defecto, Auto. Sustituye a «Controller buttons».

**Glifos**
- `UI/ButtonGlyphs.cs`: sprites en pixel art de 20 px generados en código, con escala entera:
  - tecla de obsidiana con filo de oro viejo (se estira para SPACE);
  - placa de piedra para LB, R1, START…;
  - botones redondos de Xbox con colores de `MexicaPalette`;
  - ✕ ○ □ △ dibujados como formas;
  - ratón.
- `UI/ButtonGlyph.cs` (un icono que cambia solo) y `UI/GlyphText.cs` (un aviso que empieza con `{Acción}`).
- Dónde se usan:
  - caja de esquiva / crítico (`QuickTimePrompt`);
  - filas del menú de combate: con teclado se queda el rombo de piedra y con mando sale el botón (`ActionButton`);
  - barra DEFEND, pista del menú y consejos (`ContextTutorial`, con marcadores `{Acción}`, también en las escenas);
  - avisos de la tienda y de los braseros;
  - casa de la espada;
  - pantalla de derrota (antes tenía [ENTER] / [ESC] fijos).

**Historieta (`PrologueComic`)**
- **Siguiente:** Espacio / Enter / clic / A (✕). **Omitir:** *mantener* ESC / B (○) 1 s, con una línea dorada que se llena.
- La ayuda ya no está fija: aparece al empezar y con cualquier tecla, y se desvanece tras 3 s sin tocar nada. Placa de obsidiana con filo dorado y glifos.
- Una pulsación = un avance. Pulsar mientras aparece una viñeta solo la completa; probado con 2 pulsaciones rápidas = 1 viñeta + completar la siguiente.

**Auditoría (Play Mode, con mandos virtuales Xbox y Sony y teclas simuladas)**
- Flujo completo: Boot → menú → historieta (Siguiente y mantener ESC) → `MainMap_CSV`.
- Combate con poción, Tonalli Burst, ataque, turno enemigo y victoria: sin congelarse. Al terminar solo queda visible el minimapa.
- Llave → puerta de obsidiana (cinemática) → arena → jefe → derrota → Retry: vuelves a `S` con la vida llena y las rejas abiertas.
- Colisiones: relleno con la cápsula real de Tlahuelli y física 2D en todo el mapa. Se llega a los 25 objetivos (enemigos, cofres, braseros, tienda, casa y puertas); el jefe, solo con las puertas abiertas.
- Consola sin errores ni avisos en todas las pruebas.
- **Bugs arreglados:**
  1. La arena del jefe era más pequeña que su campo de visión: Quetzalcóatl empezaba la pelea antes de que cerraran las rejas. Ahora el disparador cubre el gran salón, 3 celdas antes de las puertas (`CsvMapBuilder.FitBossArena`).
  2. La barra DEFEND quedaba encima de la pantalla de derrota: el menú de combate se oculta cuando Tlahuelli cae.
  3. El glifo «D-PAD ▲» tapaba POTION: ahora es compacto.
  4. Había un hueco grande entre la tecla y «to DODGE!».
  5. El cuadro de consejos rozaba el menú de comandos: se mueve un poco a la izquierda.

## 2026-11-24 — Sesión 22 (Toño): mapa del CSV, Tlahuelli visible en combate y esquiva de una tecla

Siguiendo `instrucciones_de_mapa_y_combate_para_claude.md`.

**1. Versión.** Local en `origin/v0.6` (`82faf32`, Sesión 21 de Alex). El ruido de Unity (EOL / fileIDs) se guardó en un `git stash` antes de bajar.

**2. Mapa desde el CSV** (`Settings/Maps/mapa_juego_150x70.csv`, 150 × 70 celdas).
- Escena nueva **`Scenes/Levels/MainMap_CSV.unity`**: copia de `MainMap` con todos sus sistemas. `MainMap` de Alex **no se toca**. **El menú y el prólogo cargan `MainMap_CSV`** (decidido por Toño): `SceneIds.MainMap = "MainMap_CSV"`, `gameScene` de `TD_MainMenu` y `nextScene` de `TD_Prologue`; también está en Build Settings y en la build del .exe (`OneClickPlay`, `FrontEndBuilder`). Probado: New Game → prólogo → MainMap_CSV.
- **1 celda = 2 × 2 tiles** (mundo de 300 × 140 tiles). Tlahuelli no se reescala.
- Se pinta **tal cual el CSV**: no se suavizan ni enderezan caminos ni bordes.
- Leyenda: `.` pasto, `C` tierra, `D` arena (tile nuevo `Sand_1/2`), `W` agua, `V` barranco, `T` árbol, `P` / `M` muro de la pirámide, `I` pasillo, la `M` de la cueva SE es roca. `S`, `E`, `K` y `H` toman el suelo de sus vecinos.
- El CSV no trae ninguna `B`: el puente va donde el camino de la fila 22 cruza el río (columnas 16–18, sector oeste).
- **Colocación:**
  - jugador en `S`;
  - un enemigo por grupo de `E` (dos en los grupos de 4 celdas): lobos en el bosque NO, brujas en el SO y gigante / bruja / lobo en el desierto; más fuertes cuanto más lejos de `S`;
  - cofres `LootChest` en las `K` (el de la cueva es el mayor);
  - Quetzalcóatl en el punto más abierto del gran salón de la pirámide; puerta de obsidiana (`KeyDoor`) y reja del jefe en **cada entrada** (2: este y sur);
  - casa de la espada en la `H` (sprite generado `Art/Props/house_sword.png`), con el santuario de los cuatro fuegos de Alex al lado;
  - tienda en un claro cerca del inicio;
  - territorios reubicados y muros invisibles en el borde.
- Comprobación del builder: desde `S` se alcanzan los 11 grupos de `E`, los 5 de `K` y la casa. El jefe solo es alcanzable cruzando una puerta.
- Scripts: `Editor/CsvMap.cs` (lee el CSV), `Editor/CsvMapPainter.cs` (tiles), `Editor/CsvMapBuilder.cs` (menú **TD_Game > Build CSV Map (MainMap_CSV)**; no sobrescribe: borrar la escena para reconstruir), `Interactions/LootChest.cs`.

**3. Tlahuelli invisible en combate.**
- **Causa:** la arena coloca a los luchadores sin mirar qué hay debajo (la bruja quedaba sobre el río). Además, árboles, puestos y props se dibujan con el mismo orden que los personajes, así que uno más abajo en pantalla tapaba a Tlahuelli. La oscuridad (orden 500) también lo tapaba si la cámara chocaba con el borde.
- **Arreglo (`CombatArena`):**
  - durante la pelea cada luchador va en un `SortingGroup` con orden **510** (por encima del mapa y de la oscuridad), que se quita al terminar;
  - la pelea se mueve al claro libre más cercano (`clearSearchRadius`);
  - los efectos de delante pasan a **520** (`Vfx.FrontOrder`), y las estelas usan el orden del grupo.
- **UI:** el cuadro de consejos (`ContextTutorial`) baja al centro inferior durante el combate; ya no tapa el orden de turnos ni el nombre del ataque.
- Verificado con Shadow Wolf + Mictlan Witch, también pegados al puesto del mercader: un solo `Player`, se ve en todos los turnos y el `SortingGroup` desaparece al acabar.

**4. Esquiva de una sola tecla** (estilo Clair Obscur: Expedition 33).
- **Espacio / B** para esquivar **y** para interrumpir hechizos. Se quitan las teclas al azar W A S D y la `D` de la acción Dodge.
- El crítico del ataque vuelve a ser **1 / A** (sin J / K / L).
- `ActionCommand.Run`:
  - pulsar dentro de la ventana antes del impacto = éxito (al final = PERFECT);
  - antes de tiempo = **TOO EARLY** y la tecla se bloquea, ×1,5 por cada pulsación extra (**LOCKED**, y la caja de la tecla se oculta);
  - sin pulsar o tarde = daño completo.
- **Éxito = 0 daño + contraataque automático** (`EnemyTurn`): estela, anillo, temblor, sonido y «COUNTER». Con PERFECT pega más fuerte y da estamina y Tonalli; el normal pega `goodCounterFraction` (0,6).
- `QuickTimePrompt` ensancha la caja para «SPACE».
- Textos actualizados: barra DEFEND, consejos y pista del menú. Los textos guardados se cambiaron en `MainMap_CSV`, `MainMap` y `TestLevel` (solo esos textos).
- Probado en Play Mode con pulsaciones simuladas:
  - a tiempo: 3/3 esquivas, 0 daño y 3 contraataques;
  - sin pulsar: daño completo;
  - machacando: TOO EARLY → LOCKED y daño completo.

**Pendiente:** los cofres no se guardan en la partida todavía; el título de zona puede aparecer al empezar una pelea si la arena cruza un territorio.

## 2026-11-19 — Sesión 21 (Alex): QTE, mando, dificultad, templo, cómic y outro

- **Esquivar / interrumpir con QTE:** aparece una tecla al azar (W A S D) en un recuadro de color; tecla equivocada = falla. El **crítico** del ataque ahora es J / K / L (el 1 queda solo para elegir la acción).
- **Botones de Xbox** (`UI/InputPrompts.cs`): opción en el menú principal y en pausa. Solo se muestran si están activados **y** hay un mando conectado; por defecto, teclado. Nunca salen los dos a la vez.
- **Smoking Mirror** con animación (humo, espejo, rayos y almas que regresan) y **Tonalli Burst** con grietas, ondas y estelas. Orbes más chicos (`SoulOrb.size`).
- **Más difícil:** ventanas de esquiva ×0.75 (el jefe ×0.56), PERFECT más corto, menos fallos enemigos, zonas ×1.2 / ×1.6 / ×2. **Quetzalcóatl:** 760 de vida, ataque 22, cambia de forma al 60 % y actúa **2 veces por ronda (3 en la segunda forma)**.
- **Al morir vuelves al inicio del mapa** (`PlayerProgression.StartPoint`); conservas habilidades, oro y crecimiento.
- **Puerta del templo:** cinemática (`KeyDoor`): la llave responde, la cámara va a la puerta, los glifos se encienden, tiembla, la losa sube soltando polvo y una ola de luz entra al templo.
- **Interior del templo:** columnas con serpiente, ídolo de Coatlicue, chacmool, tzompantlis, estandartes y ofrendas (`PropArt.Temple.cs`, `MainMapBuilder.BuildTempleDecor`).
- **Cómic del prólogo** repintado a 144×96 con luz y sombra (`PropArt.Paint.cs`, `PropArt.Comic.cs`); nuevo panel `gift` (la mano de Mictlantecuhtli entrega el macuahuitl). El puño de "I will make them answer" tiene contorno, nudillos y pintura de guerra.
- **Outro** (`CreditsScreen`): fuente Pirata One, epílogo en tarjetas, fondo con paneo, brasas, grecas doradas, créditos con roles y "THANK YOU FOR PLAYING" que late.
- Se regeneraron `MainMap` y `TD_Prologue` con el menú Mictlan, se probó en Play Mode (consola limpia) y se rehízo el `.exe` (`Mictlan (juego).zip` en el escritorio). Sin commits.

## 2026-11-17 — Sesión 20 (Toño): traer las animaciones a la rama v0.6

**Tipo:** integración. La rama del equipo pasó de `integrar-version-v5` a **`v0.6`**, la versión 0.6 de Alex.

- Las sesiones 18 y 19 (abajo) estaban sin commit en `integrar-version-v5`. Se guardaron en un stash ("Tono sesiones 14-15…") y se reaplicaron sobre `v0.6`.
- **Sin conflicto:** habilidades, `CombatMotion`, `CombatContext`, `CombatManager`, `EnemyTurn`, `CombatEnemy`, la carpeta `Scripts/Combat/Fx/` y `tlahuelli.png.meta` (Read/Write).
- **Fusionado a mano:**
  - `CameraFollow`: se queda la versión de Alex; su `ClampForView` sustituye a mi `ClampPosition`.
  - `CombatArena`: versión de Alex (encuadre propio y cámara más abierta para enemigos grandes), más `ActorFx` y `MacuahuitlRig`. Mi `cameraOffset` ya no se usa.
  - `CombatHUDBuilder`: versión de Alex (6 acciones), más el menú compacto 0.82.
  - `DEVELOPMENT_LOG`: el de Alex, más estas entradas.
- **Nombre duplicado:** Alex creó su propio `MacuahuitlSwing` (componente, `Scripts/Player/`). Mi clase de datos se renombró a **`MacuahuitlTechnique`**, conservando el GUID del `.meta`.
- **Dos macuahuitles:** mientras mi `MacuahuitlRig` está activo apaga el `MacuahuitlSwing` de Alex y lo devuelve al acabar el combate. Su `PlayerSpriteAnimator` y su `GoreEffects` funcionan junto con el rig y con `CombatMotion.Death`.
- **Escenas y prefab:** se conservaron las de Alex y se reaplicó encima:
  - `MacuahuitlRig` con la pose `tlahuelli_2_0` en `Player.prefab`, `MainMap` y `TestLevel`;
  - `CommandMenu` a escala 0.82.
  - No se reconstruyó ningún HUD.
- **Validado en MainMap de v0.6** contra el Shadow Wolf: arma del rig visible, la de Alex apagada, golpe con la sangre del gore de Alex y consola sin errores.

---

## 2026-11-17 — Sesión 19 (Toño): Tlahuelli pelea con el macuahuitl

**Tipo:** animación de combate, validada en Play Mode con secuencias de capturas

### Qué se implementó
- **`MacuahuitlRig`** (`Scripts/Combat/Fx/`): el macuahuitl como sprite aparte en la mano, que sigue la altura, el aplastamiento y la inclinación del cuerpo sin deformarse.
  - Tiene guardia con balanceo, golpes con estela de hojas fantasma y "follow-through", y brillo con chispas en la punta.
  - Durante el combate Tlahuelli usa la **pose de perfil** (`tlahuelli_2_0`) mirando al enemigo.
- **`WeaponSprites`:**
  - el arma en pixel art (7×17 px, 16 PPU, mismos colores que el sprite de Tlahuelli): madera, navajas de obsidiana con brillos alternos, mango forrado y borla roja;
  - **`Unarmed`** genera en tiempo de ejecución una copia del cuadro **sin el macuahuitl dibujado**: quita los colores del arma, rellena el hueco si estaba delante del cuerpo y rehace el contorno. Así el arma nunca se ve dos veces.
  - Para eso se activó **Read/Write** en `tlahuelli.png`.
- **Golpes que no se repiten** (`MacuahuitlTechnique`). Cada golpe del combo es una técnica distinta y su nombre aparece en pantalla:
  - **Cleave**: tajo diagonal desde arriba;
  - **Low Sweep**: barrido bajo a las piernas;
  - **Rising Cut**: corte ascendente con salto;
  - **Whirlwind**: remolino, el cuerpo gira con el arma y deja doble corte;
  - **Obsidian Strike**: crítico; mazazo con esquirlas de obsidiana, corte cruzado y grieta.
  - El arma se levanta a la posición de cada técnica durante la carga.
- **El arma en todo el combate:**
  - **Seismic:** abajo y atrás al agacharse, sobre la cabeza al subir, brilla arriba y se clava en el suelo al caer;
  - **Mictlan Fury** y **Tonalli Burst:** alzada al cielo brillando; Tonalli la descarga hacia abajo al estallar;
  - **Poción:** la baja;
  - **Esquiva:** se va hacia atrás;
  - **Contraataque PERFECT:** **Backhand** (revés) con corte turquesa;
  - **Emboscada:** Obsidian Strike desde el aire.
- **`CombatMotion.Lunge`** acepta un golpe de arma opcional (`strike`) que llama a `onHit` en el momento del impacto.
- **Encuadre:**
  - el menú de comandos se escaló a 0.82 y `cameraOffset` quedó en (0.6, -0.3) (también en la escena);
  - los enemigos quedan a la izquierda del menú y Tlahuelli a la vista.

### Archivos
- Nuevos: `Scripts/Combat/Fx/{MacuahuitlRig, MacuahuitlSwing, WeaponSprites}.cs`.
- Modificados:
  - `AttackAbility`, `SeismicAbility`, `FuryAbility`, `PotionAbility` y `TonalliBurstAbility`;
  - `CombatMotion`, `CombatArena`, `EnemyTurn`, `CombatManager` y `CombatHUDBuilder`;
  - `Player.prefab`, `TestLevel` (Tlahuelli con su sprite real) y `tlahuelli.png.meta` (Read/Write).

### Validación
- Secuencias de capturas de los 4 golpes del combo, del Seismic y de una victoria.
- Al ganar, el arma se guarda y vuelve el sprite original (`tlahuelli_0_0`).
- Consola de TD_Game sin errores ni advertencias.

### MainMap (con permiso de Toño; la escena es de Alex)
- **HUD reconstruido** con el builder actual. Era la estructura estándar del builder, sin cambios manuales, y ningún script lo referenciaba. Ahora tiene menú compacto 0.82, rombos, grecas y viñeta.
- El `Player` de MainMap **no es instancia del prefab**: se le añadió `MacuahuitlRig` con la pose de combate `tlahuelli_2_0`.
- Validado con una pelea contra el Shadow Wolf: Tlahuelli de perfil con el arma, el lobo libre del menú y el tajo impactando. Sin errores.

### Notas
- En `TestLevel` sigue apareciendo el panel de tutorial de Alex (`ContextTutorial`) arriba de la pelea.
- El `Player` de MainMap y el del prefab son independientes: un cambio en uno no llega al otro.
- Si cambia el arte de Tlahuelli y el arma dibujada usa otros colores, hay que actualizar `WeaponSprites.DrawnClub`.

---

## 2026-11-17 — Sesión 18 (Toño): animaciones del combate y nuevo Seismic Jump

**Tipo:** animación procedural, validada en Play Mode con secuencias de capturas. Hecha **sobre la sesión 13 de Alex** (pull previo).

### Qué se implementó
- **`ActorFx`** (`Scripts/Combat/Fx/`): anima solo el hijo `Visual`, así que colisiones y sombra no se mueven.
  - Squash & stretch con muelle (rebota), altura, inclinación que gira sobre el centro del cuerpo, retroceso, temblor y respiración.
  - Es relativo a la escala del `Visual`, así que sirve con los sprites de Alex.
- **`Vfx`, `VfxPiece` y `VfxSprites`:** efectos generados en código y teñidos con `MexicaPalette`:
  - anillos y ondas expansivas, polvo, escombros que rebotan en el suelo y chispas;
  - corte en media luna, grietas en el suelo y estelas (afterimages);
  - almas que suben y proyectiles.
- **`CombatMotion` reescrito** (misma API):
  - **Ataque:** se echa hacia atrás, se lanza estirado con estela, impacto y regresa con un saltito.
  - **Esquiva:** salto hacia atrás con estela turquesa.
  - **Golpe recibido:** retroceso y aplastamiento.
  - **Enemigo:** se agacha y tiembla durante la telegrafía (`ChargeUp`).
  - **Muerte:** se aplasta, suben almas doradas, salen escombros y se desvanece.
- **Seismic Jump nuevo:** agacharse → salto estirado con voltereta y estela → instante arriba con destello → caída en picada → impacto. El impacto trae aplastamiento, *hit stop*, temblor fuerte, grieta en el suelo, dos ondas y rocas. La onda alcanza a cada enemigo según su distancia y lo lanza por los aires. Todo el ritmo es editable en `Ability_SeismicJump`.
- **Más efectos:**
  - ataque: corte en media luna con chispas; en el crítico, doble corte cruzado y anillo;
  - Mictlan Fury: anillos que se cierran, cenizas y estallido;
  - poción: luz de jade que sube;
  - Tonalli Burst: se eleva con anillos turquesa y estalla sobre todos;
  - STOMP: grieta y polvo;
  - hechizos enemigos: proyectil;
  - interrumpir: el enemigo se tambalea; PERFECT: anillo turquesa;
  - fase 2 del jefe: ondas rojas;
  - inicio del combate: todos se ponen en guardia y empiezan a respirar.
- **Encuadre del combate:** el menú de comandos tapaba al enemigo principal.
  - `CombatArena.cameraOffset` (0.6, -0.8) sube la pelea en pantalla.
  - Si la cámara choca con los límites del mapa, los luchadores se desplazan hacia dentro (`CameraFollow.ClampPosition`).

### Archivos
- Nuevos: `Scripts/Combat/Fx/{ActorFx, Vfx, VfxPiece, VfxSprites}.cs`.
- Modificados:
  - `CombatMotion`, `CombatArena`, `CombatContext`, `CombatManager` (STOMP), `EnemyTurn` y `CameraFollow`;
  - `CombatEnemy` (solo `FadeOut` usa `CombatMotion.Death` y el reinicio restaura `ActorFx`);
  - `AttackAbility`, `SeismicAbility`, `FuryAbility`, `PotionAbility` y `TonalliBurstAbility`;
  - `Ability_SeismicJump.asset`.

### Validación
- Secuencias de 24 y 18 capturas del Seismic Jump y del ataque.
- Encuadre comprobado en el pasillo (Brute) y junto a la pared (Hound).
- Consola de TD_Game sin errores ni advertencias.

---

## 2026-11-17 — Sesión 17 (Alex): sonidos del combate, volumen, Quetzalcóatl, .exe

- **Sonidos del combate:** al cambiar de escena, el `GameAudio` repetido se destruía con `Destroy(gameObject)` y se llevaba al `GameplayAudio` del mismo objeto: en `MainMap` (entrando desde el menú) no sonaban golpes ni esquives. Ahora usa `Destroy(this)`. Se añadieron los sonidos del combate de Mictlán v5 que faltaban: `swing` al atacar, `seismic`, `jump` / `land`.
- **Volumen por defecto** más bajo: general 60 %, música 45 %, efectos 100 %.
- **Créditos** fijos en código (`const`), en el orden Antonio, Alejandro, Adrián, Jetzuvely; la escena ya no puede guardar un texto viejo.
- **Clasificación:** los textos usan *Best Fit* de Unity UI para caber en cualquier tamaño de pantalla.
- **Minimapa:** flecha que gira hacia donde mira Tlahuelli (`PropArt.MapArrow`, `Minimap` lee `PlayerDirection.LastValid`).
- **Quetzalcóatl** nuevo, parecido al de v5 pero más intimidante (`PropArt.Quetzalcoatl`, 4 cuadros).
- **Build de Windows:** menú **Mictlan > Build Game For Friends (.exe)** → `Builds/Mictlan/Mictlan.exe` (la carpeta `Builds/` está en `.gitignore`). Cambia `PlayerSettings` (compañía Micta Forge, producto Mictlan, pantalla completa).

---

## 2026-11-17 — Sesión 16 (Alex): mapa natural, sonido, tienda tipo RE4, gore, cómic

- **Sonido arreglado:** ningún `GameAudio` tenía asignada la `AudioLibrary` (`library: {fileID: 0}`), por eso el juego estaba mudo. `FrontEndBuilder.ConnectAudio()` la vuelve a cargar del disco y la asigna en cada escena; *Play From The Start* además quita el "Mute Audio" de la vista Game.
- **Mapa natural** (`MainMapBuilder.Plan()` + `Editor/TerrainArt.cs`): adiós a los muros de piedra. Bosque denso de bordes irregulares, ríos con **puentes** entre las zonas, cresta de rocas entre A y D, lagunas redondas con orilla de arena/espuma (16 versiones de agua y de pozo según los lados con tierra), pozos, árboles y rocas sueltos que nunca tapan el camino de tierra. Tiles de 16 px (misma densidad que los personajes) con **contorno oscuro** en árboles y rocas; guardados como `Tile` en `Art/Tilesets/Mictlan` para pintar a mano con la Tile Palette.
- **Templo de Quetzalcóatl:** piso y muros de piedra con greca, reja de obsidiana, cabezas de serpiente emplumada y braseros encendidos (`Editor/PropArt.cs`).
- **Títulos de zona estilo GTA** (`UI/ZoneTitleCard.cs` + `World/ZoneArea.cs`): el nombre del territorio del GDD aparece grande y se desvanece al entrar.
- **Tienda tipo RE4:** carpa con toldo rayado, repisas, linternas, caja, barril y tapete; vendedor encapuchado con ojos brillantes (capas ordenadas con `SortingGroup`).
- **Tlahuelli:** el Macuahuitl ya está dibujado en su sprite; el Macuahuitl extra solo aparece al atacar. La antorcha tiene una posición fija por dirección (de frente en la mano libre, de espaldas y de lado detrás del cuerpo).
- **Gore como el juego viejo:** al morir, el sprite del enemigo se corta en pedazos (`Sprite.Create`) que salen volando con `Rigidbody2D`, más huesos y sangre; los enemigos se **ensangrentan** más mientras menos vida tienen.
- **Clasificación** con estilo de etiqueta de caja física (letra sencilla en negritas, caja de la M y caja de descriptores). **Menú** más vivo: antorchas de la pirámide parpadeando, halo de la luna, estrellas y niebla (además de las chispas, que se quedan).
- **Cómic del prólogo** con ilustraciones nuevas: la madre enferma, el altar bajo la lluvia, los tronos vacíos, la tumba, el puño, Mictlantecuhtli y el camino.
- Se borró el menú **Mictlan (OLD v5 - do not use)** (se quitó su `[MenuItem]` en `Assets/Scripts/Editor/MictlanSetupTool.cs`).
- Probado en Play Mode: clasificación, menú, mapa nuevo, pelea con sangre en el lobo y explosión en pedazos. **No probado:** escuchar el audio (no puedo oír), comprar en la tienda, la llave y la pelea con el jefe.

---

## 2026-11-16 — Sesión 15 (Alex): animación, llave, vendedor, prólogo, menú Mictlan

Decisiones del equipo (Alex): historia del **GDD** (prólogo en cómic); se quedan **lobos, brujas y gigantes**; la vida **no** se rellena al ganar; el proyecto se queda en `Documentos\Mictlan-v5` con acceso directo en el Escritorio (OneDrive).
- **Tlahuelli animado** en 4 direcciones con el sprite de Mictlán v5 (`PlayerSpriteAnimator`), antorcha en la mano izquierda y **Macuahuitl** en la derecha que se levanta y se estira al atacar (`MacuahuitlSwing`, sprite generado por `ArtGenerator`).
- **Llave de obsidiana** (como en el juego viejo): aparece al vencer a todos los enemigos normales (`ObsidianKey`) y abre la puerta de Quetzalcóatl (`KeyDoor`). Se guarda (`SaveData.hasKey`).
- **Vendedor** (`MerchantShop`) pegado a la pared norte de la sala B, con puesto y antorchas: pociones, apostar 10/50 y el artefacto **Espejo Humeante** (`MirrorAbility`, 6ª ranura de combate, tecla 6 / RT; acción `Combat/Artifact` en `PlayerControls.inputactions`; el HUD tiene 7 filas).
- **Prólogo en cómic** (`TD_Prologue`, `PrologueComic`) con el texto del GDD; **Start Game** lo muestra antes del mapa.
- **Menú:** marcos aztecas del juego viejo (`AztecFrames`, copia de `AztecUI`), fondo que se mueve y chispas; créditos: Antonio, Alejandro, Adrián, Jetzuvely (nombres completos).
- **Cámara** menos cercana (6.75 en exploración, 4.21875 en combate), la pelea se encuadra a la izquierda del menú de comandos; **minimapa y oro arriba a la derecha**; tips de exploración cada 25 s y nombres de territorios del GDD al entrar a cada zona.
- **Menú `Mictlan`** arriba en Unity: *Rebuild Everything And Play*, *Play From The Start*, *Rebuild The Main Map*.
- Probado en Play Mode: menú, créditos, prólogo completo, mapa, caminar, pelea con el ataque del Macuahuitl y la 6ª fila. **No probado:** comprar/apostar en la tienda, la llave y la puerta, la pelea con Quetzalcóatl.

---

## 2026-11-16 — Sesión 14 (Alex): oro y tienda, minimapa, gore, antorcha, mapa grande y oscuro

- **Oro:** `EnemyData.goldOnDefeat` (lobo 8, bruja 15, gigante 30); se suma al ganar (`CombatRewards`), vive en `PlayerProgression.Gold` y se guarda. **Tienda** de pociones (`Interactions/PotionShop.cs`, tecla E, 20 de oro) en la sala B de `MainMap`. Contador `UI/GoldCounter.cs`.
- **Minimapa reutilizable** (`UI/Minimap.cs`): segunda cámara + RenderTexture sobre los límites del Tilemap `Ground` de cualquier escena; punto del jugador; se oculta en combate.
- **Transiciones entre mapas** (petición de Adrián): `World/SceneDoor.cs` + `World/SpawnPoint.cs`.
- **Gore simple** (`World/GoreEffects.cs`): manchas al golpear, charco y pedazos con `Rigidbody2D` al morir; opción en Options. Sprites copiados a `Art/Effects/`.
- **Antorcha** en la mano de Tlahuelli (prefab y `MainMap`) y **oscuridad** pegada a la cámara (`Art/Effects/Darkness.png`, capa TransparentFX para que el minimapa no la dibuje).
- **Menú:** fuente **Pirata One** (Google Fonts, licencia OFL en `Art/Fonts/PirataOne-OFL.txt`) y **marco azteca** 9-slice (`Art/UI/AztecFrame.png`), ambos generados/asignados por `Editor/ArtGenerator.cs` y `FrontEndBuilder`.
- **Combate:** `CombatArena` mueve toda la pelea dentro de lo que la cámara puede mostrar (`CameraFollow.ClampForView`) y abre más la cámara con enemigos grandes: Tlahuelli ya no queda en la orilla ni sale al esquivar.
- **`MainMap` más grande y oscuro:** 88×40 tiles, 5 salas al estilo del mapa de Adrián (mismos tiles), tintes oscuros por Tilemap, 12 enemigos por zonas + jefe. Provisional hasta que Adrián entregue el mapa principal.
- Guía de estudio: `GUIA_ALEX.md`.
- Probado en Play Mode: menú con la nueva fuente y marcos, pelea junto a la esquina del mapa (bien encuadrada), victoria con gore, minimapa y contador de oro. **No probado:** comprar en la tienda, la pelea con el jefe y los créditos.

---

## 2026-11-15/16 — Sesión 13 (Alex): enemigos, flujo del juego y front end de Mictlán v5

**Tipo:** tareas de Alex (2.11, 2.12, 3.8, 3.13, 3.17, 3.18, 4.3, 4.6, 4.7, 4.18, 4.19) + menú, audio y mapa del juego anterior. Validado en Play Mode. **Sin commits.**

### Enemigos y combate
- **IA de exploración** (`Enemies/EnemyAI.cs`): patrulla, detección con línea de visión, persecución, regreso a casa y aullido que alerta aliados. Valores en `EnemyData` (sección *Exploration*). Los enemigos de prueba de `TestLevel` ya persiguen al jugador (2.12).
- **Habilidades únicas como datos** (`AttackEffect` nuevos al final del enum): `EmpowerSelf`, `LifeSteal`, `Stun` (el jugador pierde su turno), `Guard`. Pasiva `damageTakenMultiplier` (piel gruesa).
- **Enemigos finales** (assets en `ScriptableObjects/Enemies/`, prefabs en `Prefabs/Characters/`): Shadow Wolf (zona 1), Mictlan Witch (zona 2), Quinametzin Giant (zona 3) y **Quetzalcóatl** con **segunda fase** al 50 % (`phaseTwoAttacks`). Se crean con **TD_Game > Build Enemy Data And Prefabs** (nunca sobrescribe). También `Player.prefab`.
- **Game Over completo** (`UI/DefeatScreen.cs`), **tutoriales en contexto** (`UI/ContextTutorial.cs`, `Interactions/TutorialZone.cs`), **guardado** (`Managers/GameSave.cs` + `SaveController.cs`, autoguardado tras cada victoria) y **créditos al vencer al jefe** (`UI/CreditsScreen.cs`, `World/BossArena.cs`). Canvas con **TD_Game > Build Game Flow UI**.

### Front end y mapa de Mictlán v5 (copiados y adaptados, los originales no se tocan)
- Música y efectos copiados a `Assets/TD_Game/Audio/` y reunidos en `AudioLibrary.asset`. `GameAudio` (música con fundido, volúmenes en PlayerPrefs) y `GameplayAudio` (escucha los eventos del combate; la lógica no reproduce sonidos).
- Escenas nuevas: `Scenes/MainMenu/TD_Boot.unity` (logo Micta Forge → clasificación M 17+ → descripción del juego) y `TD_MainMenu.unity` (mismo fondo y música del menú anterior; Start, Continue, Options, Credits, Exit). Pausa con ESC (`UI/PauseScreen.cs`).
- **`Scenes/Levels/MainMap.unity` usa el mapa de Adrián** (los Tilemaps de `TestLevel`), **no** el mapa del juego anterior. Se genera con **TD_Game > Build Main Map (Adrian's map)** (`Editor/MainMapBuilder.cs`; las posiciones están arriba del archivo). Curva de dificultad (4.3): inicio abajo a la izquierda → lobos (zona 1) → brujas (zona 2, arriba) → gigante que bloquea el pasillo (zona 3) → Quetzalcóatl en la sala derecha; al entrar, una reja cierra el pasillo hasta terminar la pelea. Se añadió `TilemapCollider2D` al Tilemap `Walls` de `MainMap` para que la piedra bloquee.
- Sprites de personajes y fondo del menú del juego anterior en `Art/Characters/Legacy` y `Art/UI/Menu` (PPU 16, cortados por **TD_Game > Import Character And Menu Art**).
- El menú del juego viejo se renombró a **Mictlan (OLD v5 - do not use)**: al usarlo se sobrescribían las escenas `Boot`, `MainMenu` y `Game` y Build Settings volvía al juego anterior (cambio de una línea en `Assets/Scripts/Editor/MictlanSetupTool.cs`, autorizado por el equipo).
- **Build Profiles:** `TD_Boot` → `TD_MainMenu` → `MainMap` (+ `TestLevel`).
- Todo junto: **TD_Game > Build Front End (Menus, Audio, Scenes)**.

### Pendiente / a revisar
- Los números de vida, daño y tiempos son propuestos: balancear en el playtest (5.4).
- El mapa de Adrián mide 30×20; cuando lo amplíe (4.1/4.2), mover las posiciones en `MainMapBuilder.cs`, borrar `MainMap` y regenerarlo.
- `TestLevel` cambió (IA en los enemigos de prueba, GameFlowUI, audio y pausa): hacer pull antes de commitear para evitar conflictos.

---

## 2026-11-13 — Sesión 12: más oscuro y con mitología mexica

**Tipo:** estilo visual, validado con capturas en Play Mode

### Petición
Colores más oscuros y un menú con aire de mitología mexica: que se note que es un juego serio y no para niños.

### Qué se cambió
- **`MexicaPalette` más oscura:** obsidiana casi negra, hueso apagado para el texto, oro viejo y ocre más profundos, cinabrio, tezontle, jade y turquesa oscuros.
- **Ornamentos mexicas**, generados como texturas dentro de Unity por `CombatHUDBuilder`, sin herramientas externas:
  - **greca de almenas escalonadas** (`Art/UI/Greca_Almenas.png`, como las almenas del Templo Mayor), en el panel del jugador, sobre el menú de comandos y bajo cada aviso;
  - **viñeta** (`Art/UI/Vignette.png`) que oscurece la arena al empezar el combate: 30 % en el centro y 90 % en las esquinas.
- **Insignias de tecla en rombo de piedra** en lugar de círculos.
- **Nombres nahuas:** las barras usan las tres fuerzas anímicas de la cosmovisión mexica: **TEYOLIA · LIFE** (corazón, vida), **IHIYOTL · BREATH** (aliento, estamina) y **TONALLI · SPIRIT**.
- Turnos en mayúsculas (YOUR TURN, TEST BRUTE ATTACKS) y tintes de los placeholders más oscuros.

### Validación
- Capturas en tu turno, con el aviso DODGE! y con los orbes en el suelo.
- Consola de TD_Game sin errores ni advertencias.

---

## 2026-11-13 — Sesión 11: paleta mexica seria

**Tipo:** estilo visual, validado con capturas en Play Mode

### Petición
Colores más serios para el menú de combate, los orbes y el combate: un juego serio para adolescentes, con temática mexica, como lo griego en God of War.

### Qué se cambió
- **Nueva `MexicaPalette`** (`Scripts/UI/MexicaPalette.cs`). Toma materiales del mundo azteca en lugar de colores saturados:
  - obsidiana, hueso y ceniza (Mictlán);
  - oro viejo y ocre;
  - cinabrio y tezontle (el rojo de los códices);
  - jade (vida) y turquesa/xiuhuitl (tonalli).
- **HUD:**
  - placas de obsidiana con filo de oro viejo e insignias de tecla en oro viejo;
  - barras de vida en cinabrio, estamina en ocre y Tonalli en turquesa;
  - el turno activo y el aviso DODGE! van en hueso sobre tezontle;
  - los rombos ◆ sirven de ornamento en el título y en la barra DEFEND.
- **Orbes:** almas en oro viejo, vida en jade y Tonalli en turquesa, con un brillo lento que "respira" (`SoulOrb.glowPulse`).
- **Combate:**
  - telegrafía enemiga en rojo sangre;
  - impactos en blanco hueso y críticos en oro;
  - daño recibido en rojo apagado;
  - Mictlan Fury en el blanco pálido de Mictlantecuhtli (antes, morado);
  - poción en jade y Tonalli Burst en turquesa.
- **Tintes de los placeholders:** jugador en bronce; Hound en tierra, Brute en tezontle y Witch en ceniza.
- **Corrección:** `CombatEnemy` volvía a blanco tras reiniciarse por una derrota; ahora recupera su tinte original.
- Assets de habilidades, `OrbSpawner` y `EnemyTurn` actualizados en la escena, y HUD reconstruido.

### Pendiente
- Los tiles placeholder del mapa (pasto verde y agua azul) siguen con colores vivos; se sustituyen con el arte del equipo.

---

## 2026-11-13 — Sesión 10: menú de combate legible (estilo JRPG moderno)

**Tipo:** UI, validada en Play Mode con capturas

### Problema
En el turno enemigo el menú se atenuaba (alpha 0.55) sobre fondos café: los textos no se distinguían.

### Qué se cambió
- **Menú de comandos** (`ActionButton` y `CombatActionMenu` reescritos):
  - lista diagonal abajo a la derecha; cada fila tiene una insignia de tecla redonda, el nombre en mayúsculas cursivas, el coste y una línea dorada, sobre placa negra **opaca**;
  - estados legibles: disponible, falta estamina (coste en rojo), LOCKED (gris claro) y en curso (placa dorada);
  - con el ratón, la fila se desliza al pasar por encima;
  - Tonalli Burst muestra `60/100` y late en azul cuando está lleno.
- **Turno enemigo:** el menú se desliza fuera de la pantalla y aparece la barra **DEFEND**: `SPACE / B dodge · 1 / A interrupt spells · late press = PERFECT`.
- **Avisos con la tecla:** `DODGE!  SPACE / B`, `INTERRUPT!  1 / A` y `NOW!  1 / A`.
- **Todo texto sobre placa opaca:** turno, banner, aviso y ayuda. Las placas se ajustan al ancho del texto y se ocultan cuando están vacías.
- **Nuevo `TurnOrderBar`:** orden de turnos arriba, con quien actúa en dorado. `CombatManager.CurrentEnemy` indica qué enemigo está atacando.
- `CombatHUDBuilder` reescrito con el diseño nuevo y el HUD reconstruido en `TestLevel`.

### Problemas
- Las placas salían muy altas porque el `Text` heredado devuelve alturas preferidas poco fiables. Se resolvió con altura fija y ancho ajustado al texto.
- El `Outline` dorado teñía las placas semitransparentes de café. Se resolvió con placas opacas.

---

## 2026-11-13 — Sesión 9: sprint y guías al día

**Tipo:** implementación en Unity (validada en Play Mode por MCP) y documentación

### Qué se implementó
- **Sprint:** acción `Player/Sprint` (mantener Shift izquierdo / RT).
  - `PlayerInputReader.SprintHeld` lee el botón.
  - `PlayerMovement` multiplica la velocidad por `sprintMultiplier` (1.75), con transición suave (`speedChangeRate`), y expone `IsSprinting` y `CurrentSpeed` para las animaciones.
  - Solo en exploración.
- **`TD_Game_ARQUITECTURA.md` y `TD_Game_GUIA.md` al día:** seguían en el estado del 3 nov. Ahora incluyen sprint, salto, combate, orbes, HUD, la tabla de controles completa, dónde se ajusta cada cosa y problemas frecuentes.

### Validación
- Caminando: 4 tiles/s. Corriendo: 7 tiles/s. En diagonal corriendo: 7 (normalizado). Al soltar: 0.
- Consola sin errores.

### Pendiente
- Las colisiones de los muros siguen pendientes: al correr se ve que el jugador atraviesa la roca.

---

## 2026-11-13 — Sesión 8: salto, orbes, combate más divertido y HUD en Canvas

**Tipo:** implementación en Unity, validada en Play Mode por MCP

### Qué se implementó
- **Salto en exploración** (`PlayerJump`, acción `Player/Jump` con Espacio / A):
  - es visual: sube el sprite y la sombra se queda en el suelo;
  - en el aire se pasa por encima de los enemigos;
  - **emboscada:** al caer sobre un enemigo, el jugador hace STOMP (×1.2 de daño) y tiene el primer turno.
- **Orbes** (`SoulOrb` y `OrbSpawner`; prefab `Prefabs/Interactables/Orb.prefab`). Al morir, cada enemigo suelta:
  - orbes de **alma** (dorados): carga del Macuahuitl;
  - orbes de **jade** (verdes): vida;
  - orbes de **tonalli** (azules): concentración.
  - Saltan desde el enemigo, flotan, los atrae un imán de cerca y **solo se recogen al terminar la pelea, pasando encima**.
  - Las cantidades están en `EnemyData` (`soulsOnDefeat`, `healthOnDefeat` y `concentrationOnDefeat`).
- **Tonalli** (concentración): barra que persiste entre combates y se llena con orbes azules, PERFECT y críticos. Llena, activa **Tonalli Burst** (5 / LB, `TonalliBurstAbility`): ×2.5 a todos los enemigos.
- **Combate más divertido:**
  - **Crítico con timing** (`ActionCommand`): el ataque se carga y, si pulsas Attack cuando aparece NOW!, hace ×1.6 (×1.84 si es perfecto) y da +Tonalli.
  - **Combo:** +15 % de daño por cada ataque seguido en el mismo turno.
  - **Contraataque automático** al esquivar o interrumpir con PERFECT.
  - **Impacto:** temblor de cámara (`CameraFollow.Shake`) y *hit stop* en críticos, en muertes y en las habilidades grandes.
- **HUD en Canvas** (sustituye a `CombatDebugHUD`, que se eliminó):
  - panel del jugador siempre visible (vida, estamina, Tonalli, pociones, Macuahuitl y aviso TONALLI READY);
  - menú de acciones con tarjetas 1–5 y End Turn: muestra tecla y coste, está bloqueado o atenuado según el caso y se puede pulsar con el ratón;
  - línea de ayuda;
  - barras de vida que siguen a cada enemigo, con marcador de objetivo;
  - banner animado, aviso DODGE!/INTERRUPT!/NOW! que late, contador de combo y números flotantes, también al recoger orbes.
  - Se genera con **TD_Game > Build Combat HUD** (`Scripts/Editor/CombatHUDBuilder.cs`).
- **Cambio de diseño:** la victoria ya no rellena la vida ni da las almas directamente; ahora se obtienen de los orbes.

### Archivos
- Nuevos:
  - `Scripts/Player/PlayerJump.cs`
  - `Scripts/Interactions/{SoulOrb, OrbSpawner}.cs`
  - `Scripts/Combat/ActionCommand.cs`
  - `Scripts/Combat/Abilities/TonalliBurstAbility.cs`
  - `Scripts/UI/{UIBar, PlayerStatusPanel, ActionButton, CombatActionMenu, EnemyHealthBars, CombatMessages, CombatHUD}.cs`
  - `Scripts/Editor/CombatHUDBuilder.cs`
  - `ScriptableObjects/Abilities/Ability_TonalliBurst.asset`
  - `Prefabs/Interactables/Orb.prefab`
- Modificados:
  - `PlayerInputReader`, `PlayerCombatant`, `PlayerStats`, `EnemyData`
  - `CombatEnemy`, `CombatContext`, `CombatManager`, `CombatArena`, `CombatFeed`, `CombatInput`, `CombatRewards`
  - `PlayerTurn`, `EnemyTurn`, `AttackAbility`, `SeismicAbility`, `CameraFollow`
  - `PlayerControls.inputactions`, los 3 `EnemyData_Test*` y `TestLevel`
- Eliminado: `Scripts/UI/CombatDebugHUD.cs`.

### Validación (Play Mode por MCP)
- El salto sube el sprite hasta 0.43 tiles y aterriza.
- Saltar sobre la Hound inicia una **emboscada**: STOMP de 17 y primer turno del jugador.
- El ataque suma combo y Tonalli Burst vacía la barra y gana el combate.
- La Hound suelta 7 orbes (2 de alma, 2 de jade y 3 de tonalli), que no se recogen durante la pelea.
- Al pasar encima en exploración se recogen todos: +10 de vida, +8 de carga y +15 de Tonalli.
- La vida no se rellena al ganar.
- El HUD se comprobó con capturas en combate y en exploración.
- Consola de TD_Game sin errores ni advertencias.
- **Sin probar con teclado real:** el *timing* del crítico (NOW!) y de esquivar e interrumpir. Por MCP no se puede acertar la ventana por la latencia.

### Problemas
- Con Unity en segundo plano, el Play Mode no avanzaba (`Time.time` = 0). Se resolvió con `Application.runInBackground = true` solo durante las pruebas.
- Al simular teclas, llamar `InputSystem.Update()` a mano hacía que `WasPressedThisFrame` no se detectara.
- Los paneles del HUD se veían marrones porque el `Outline` dorado se transparentaba bajo el fondo semitransparente. Se resolvió con fondos opacos.

### Pendiente
- Sonidos para los orbes, el crítico y el contraataque (18 nov).
- Animaciones reales en lugar de `CombatMotion` y del salto con el sprite.
- Valores de los orbes y del combate: balance del 19 nov.

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
