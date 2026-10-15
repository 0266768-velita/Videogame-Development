# Mictlán (Micta Forge) – Unity 2D project

Top-down 2D (classic Zelda / Pokémon feel), locked to 16:9, with a turn-based combat system and a full
Boot -> Main Menu -> Game flow. Everything is in English.

## What's new in this version (v4)

**1. Game Over screen.** When Tlahuelli falls, a Game Over screen appears with **Play Again** (retry from the last
checkpoint, fully healed, enemies reset, at least 2 potions) and **Main Menu**. Code: `UI/GameOverScreen.cs`, used by `BattleManager.DefeatRoutine`.

**2. Balance.** Tuned with a Monte Carlo simulation (human reaction-time model), not live playtests, so treat the numbers as a starting point.

| Value | Before | Now |
|---|---|---|
| Giant HP / attack | 90 / 20 | 130 / 32 |
| Witch HP / attack | 60 / 14 | 80 / 22 |
| Spark Rain dodge window | 0.30 s | 0.35 s |
| Stamina regen per turn | 100% | 60% |
| Player base damage | 12 | 14 |
| Potion cost / heal | 10 / 40% | 15 / 35% |
| Potions | none refilled | +1 after each victory (max 5) |
| Dodge spam lockout | 0.45 s | 0.30 s (`earlyPressLockout`) |

Also: a **Last Stand** (the first time a Quinametzin would kill you, you survive at 1 HP and Fury awakens, so the unlock cannot be missed)
and **difficulty** (Options > Difficulty): Easy (enemy damage x0.7, wider dodge window), Normal, Hard (x1.3, narrower window). Code: `Core/Difficulty.cs`.

**3. Minimap** (top-right, `UI/Minimap.cs`). Shows the map, your position and enemies (orange when chasing). **M** / gamepad Select toggles it.

**4. Tutorial tips** (`UI/TutorialTips.cs`). New Game starts a short guide (move, sprint, hop, minimap, enemies, pause), then pop-ups the first time you
fight, get attacked, face an interruptible spell, win, unlock Fury or Seismic. Each shows once; Start Game resets them. Pause > **Controls** lists all keys.

**5. Sound** (`Audio/AudioManager.cs`). UI, movement and combat sound effects plus music for menu, exploration, battle and game over. All files are
**synthesized placeholders** in `Assets/Resources/Audio/`: replace any file with your own using the same name (wav/ogg/mp3) and it is picked up automatically.
Options has Master and Music volume. Regenerate placeholders with `Tools/generate_audio.py`.

**6. Menu background.** `Assets/Resources/UI/MenuBackground.png` (pixel-art night temple, replace it with your own 16:9 image, or assign
the `Background` field on MainMenu). Slow zoom animation included. Generator: `Tools/generate_menu_background.py`.

Not verified in Unity: I had no Unity compiler here, so if the Console shows a compile error after import, send it to me.

## How to open it (3 steps)

1. Unzip the download. You get one folder called **Mictlan** (it contains `Assets`, `Packages`, `ProjectSettings`).
2. Open **Unity Hub** -> **Add** -> **Add project from disk** -> select the **Mictlan** folder.
   Use **Unity 6000.5.9f1** (the version this project was created with). If the Hub offers a different Unity 6 version, that works too.
3. Open the project and wait. The first import takes a few minutes, and when it finishes the project
   **builds its own scenes automatically** (Boot, MainMenu, Game) and opens `Boot`.
   Just press **Play**.

Tips:
- In the **Game** tab set the aspect to **16:9 Aspect**.
- If Unity asks about enabling the new Input System backend, click **Yes** (it is already configured for it).
- To rebuild the scenes any time: menu **Mictlan > Rebuild Scenes (Boot, MainMenu, Game)**. This recreates the 3 scenes
  (back them up first if you edited them). Prefabs and Enemy Data assets are kept, so your tuning is never overwritten.

## What you get

- **Boot:** Micta Forge logo (already assigned) -> ESRB "M" screen -> legal notice -> Main Menu. Any key skips a screen.
- **Main Menu:** title -> "PRESS ENTER / START" -> Start Game / Continue / Options / Credits / Exit Game.
- **Game:** 2D map, Tlahuellí, 2 Quinametzin Giants and 2 Witches, pause menu, autosave.
- **Enemies:** patrol, detect (view cone + hearing + line of sight), chase around obstacles, give up if you escape.
- **Combat:** turn-based, Tlahuellí left / enemies right, camera zooms in. Several actions per turn while stamina lasts;
  enemy attacks flash red and open a dodge window just before impact (late half of the window = PERFECT, +10 stamina).
  Witch spells can be interrupted instead of dodged.
- **Mictlán Fury:** against the Quinametzin, if Tlahuellí drops to 25% health for the first time, Mictlantecuhtli awakens it
  (heals, refills stamina, +50% damage for 3 turns).
- **Macuahuitl:** every defeated enemy charges it (`soulsOnDefeat`), raising damage up to x2 and changing its form
  (Base / Empowered / Ascended, event `OnFormChanged`).
- **Save:** victories autosave; Pause > Save Game; Main Menu > Continue restores stats, position and defeated enemies.

## Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| Move | WASD / Arrows | Left stick / D-pad |
| Sprint | Left Shift | Left stick click |
| Hop (exploration) | Space | A |
| Pause | Esc | Start |
| Menus | Arrows + Enter / mouse | D-pad/stick + A, B = back |
| **Combat** Attack | 1 | A |
| Mictlán Fury | 2 | X |
| Seismic Jump | 3 | Y |
| Potion | 4 | D-pad up |
| End turn | Q | RB |
| Change target | Left / Right arrows | D-pad left / right |
| Dodge (enemy turn) / Interrupt | D / 1 | B / A |

For testing, select the player in the Game scene, open the `PlayerCombatStats` component menu (three dots) and use
**DEBUG: unlock Fury and Seismic Jump** or **DEBUG: Macuahuitl to 100%**.
Unlock Seismic Jump from story code with `FindFirstObjectByType<PlayerCombatStats>().UnlockSeismic()`.

## Where to change things

| I want to... | Do this |
|---|---|
| Change the studio logo | Boot scene > `BootSplash` > **Studio Logo** (any PNG). Logo file: `Assets/Art/Branding/MictaForge_Logo.png` |
| Add the real ESRB image / sound | Same object: **Rating Image**, **Logo Sound** |
| Add the game title art, menu background, music | MainMenu scene > `MainMenu` > **Game Logo**, **Background**, **Music** |
| Zoom the camera in/out | Main Camera > **Aspect Ratio Lock** > **View Height In Tiles** (10 close, **12 default**, 14-16 far) |
| Zoom during combat | `BattleCameraController.battleViewHeight` (default 10) in `Assets/Scripts/Combat` |
| Tune an enemy (health, damage, attacks, AI) | `Assets/Data/Enemies/*.asset` (no code) |
| Tune Tlahuellí (stamina, damage, potions, skills) | `PlayerCombatStats` on the Tlahuelli prefab |
| Make a new enemy | Right click > Create > Mictlan > Enemy Data, then duplicate a prefab in `Assets/Prefabs/Enemies` and assign the data |
| Resize the map | Main Camera > `CameraFollow2D` > **Bounds** |

## Using real sprites and animation

1. Import the PNG: Texture Type **Sprite (2D and UI)**, **Pixels Per Unit = 16**, Filter Mode **Point (no filter)**, Compression **None**.
2. Open a prefab (`Assets/Prefabs/Tlahuelli`, `Assets/Prefabs/Enemies/...`), select the child **Model**, set the Sprite Renderer's sprite and reset its Scale to (1,1,1). Adjust the `Circle Collider 2D` to the feet and `bodyRadius` in the Enemy Data.
3. If your sprites face left by default, untick **Sprites Face Right** on `ActorFeedback`.
4. Animator parameters (all optional, missing ones are ignored without errors):

| Parameter | Type | When |
|---|---|---|
| `Speed` | Float | movement speed (Idle <-> Walk) |
| `MoveX`, `MoveY` | Float | facing direction (4-direction blend tree) |
| `Alert` | Trigger | enemy spots the player |
| `Hit` | Trigger | takes damage |
| `Die` | Trigger | dies |
| per attack | Trigger | the `animatorTrigger` field of each attack in the Enemy Data (e.g. `Attack_Smash`) |

5. For a real map use a **Tilemap** (GameObject > 2D Object > Tilemap > Rectangular) plus a `Tilemap Collider 2D` on layers with obstacles.
   Props the player can walk behind get the `YSort` script. To slice sprite sheets in the Sprite Editor, install the **2D Sprite** package from Package Manager if it is not already present.

## Project structure

```
Assets/
  Art/Branding/MictaForge_Logo.png     studio logo (from the GDD)
  Scripts/
    Core/     GameState, SceneLoader (fade), SaveSystem, AspectRatioLock (16:9), YSort, GameBootstrap
    Combat/   BattleManager, EnemyData, PlayerCombatStats, Health, BattleInput, BattleCameraController, ActorFeedback
    Enemy/    EnemyController (exploration AI)
    Player/   Player2D, CameraFollow2D
    UI/       UIKit, BootSplash, MainMenu, PauseMenu, BattleHUD
    Editor/   MictlanSetupTool (auto-builds scenes/prefabs/data on first open)
```
(Scenes, Prefabs, Data and Placeholders folders are generated on first open.)

## Design decisions to confirm with the team (GDD v0.2)

1. Combat happens on the same map (no separate scene). Movement is free 8-direction (Zelda), not grid-based (Pokémon).
2. **Dodging only works on the enemy's turn** (GDD section 5 says every combat input only acts on Tlahuellí's turn, but section 16 says Giants "must be dodged").
3. **Stamina rule** (open item in the appendix): several actions per turn, refilled at the start of each turn (`staminaRegenFraction`).
4. Seismic Jump hits "multiple enemies", so groups of up to 3 enemies can join one fight.
5. Update GDD sections 6 and 9: the camera is no longer 3D "in the sky".
6. Fury only unlocks against a Quinametzin at critical health; consider also calling `UnlockFury()` at the end of the Tláloc territory so it can never be missed.

## Troubleshooting

| Problem | Fix |
|---|---|
| Red errors in the Console after opening | Copy them and send them over; the code was written without access to the Unity compiler. |
| No Boot/MainMenu/Game scenes | Menu **Mictlan > Rebuild Scenes**. |
| "Cannot load scene" | File > Build Profiles must list Boot, MainMenu, Game (Rebuild Scenes fixes it). |
| Menus ignore arrows / gamepad | Keep a single EventSystem using the Input System UI Input Module (created automatically). |
| Enemy never notices you | It is facing away or a wall is in between; walk closer (hearing radius) or raise `detectionRadius`. |
