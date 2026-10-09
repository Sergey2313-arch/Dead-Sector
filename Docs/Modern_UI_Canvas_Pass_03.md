# Dead Sector — Tactical UI, pass 0.3: front end and pause/settings

**Implementation branch:** `prototype/world-8km`. **Status:** code committed, not Unity-compiled or visually tested. Never merge to `main` or tag a release without user confirmation.

## Implemented

- Runtime **Screen Space Overlay Canvas** titled `DeadSector_MainPauseCanvas`, separate from the HUD and always higher in the sorting order. Dark graphite/olive style, large DEAD SECTOR title, operational status panel and custom action buttons.
- Intro/title overlay appears **only after world streaming has initialized the player and `SectorGameplay` has loaded existing save data**. This is important: setting `Time.timeScale=0` before terrain coroutine initialization could halt or delay world generation.
- First screen provides **Continue**, **Settings**, **Save Game** and **Quit**. There is **no destructive New Game button** in this pass because the existing prototype uses one automatic save slot and currently autoloads it at startup.
- The same front end opens as **Pause** via `Esc`: suspends time scale, unlocks mouse, blocks player movement, attacks, respawn and weapon hotkeys. Resume restores previous time scale and mouse lock.
- Settings work immediately: master volume (`AudioListener.volume`), look sensitivity (`SectorInput.LookSensitivity`), FOV (`SectorPlayer.firstPersonFov` and `thirdPersonFov`), and graphics quality (`QualitySettings`). Preferences stored in `PlayerPrefs`; no changes to save-file format.
- Escape hierarchy: settings → home; home/pause → resume; inventory/crafting → close; world atlas → close; otherwise open pause.
- Existing Modern UI still controls `I/C`; existing `M` atlas and journal remain. `SectorGameplay.SetExternalUiBlocking` ensures pause and inventory do not fight over cursor state. Legacy fallback unchanged when `SectorBootstrap.useModernUi=false`.
- `SectorPlayer.EscapeHandledByUi` prevents the previous player script from independently releasing the cursor when the new menu is in use. During teardown `OnDisable/OnDestroy` clears its owned pause and lock state.

## Files

- `Assets/DeadSector/Runtime/SectorMenuUI.cs` and `.meta` — runtime Canvas, controls and preferences.
- `Assets/DeadSector/Runtime/SectorMenuRules.cs` and `.meta` — deterministic escape handling.
- `Assets/DeadSector/Runtime/SectorGameplay.cs` — external UI input guard and initial-save-ready status.
- `Assets/DeadSector/Runtime/SectorPlayer.cs` — Escape owner flag and respawn guard.
- `Assets/DeadSector/Runtime/SectorInput.cs` — mouse sensitivity multipliers for old/new input paths.
- `Assets/DeadSector/Runtime/SectorMinimap.cs` — external CloseTacticalMap entry.
- `Assets/DeadSector/Runtime/SectorBootstrap.cs` — front end initialisation after successful modern HUD.
- `Assets/DeadSector/Tests/Editor/SectorMainMenuTests.cs` and `.meta` — Escape precedence and sensitivity bounds.

## Unity QA instructions

1. Back up existing local changes, do not discard user's Unity scene, Mixamo files or .meta GUIDs. Safely update changed scripts; open `Assets/DeadSector/Scenes/DeadSectorPrototype.unity`.
2. Wait for world to initialize. **Expected:** a fully dark title overlay after loading. No indefinite "Preparing map" stall. Press **ВОЙТИ В СЕКТОР / CONTINUE** to enter; world time resumes.
3. Press `Esc`: pause appears, `Time.timeScale == 0`, mouse unlocked; player cannot move, shoot or respawn. Click Continue or `Esc`, mouse relocks and movement resumes.
4. Open `I` or `C`, then `Esc`: respective modal should close first; pause should not stack on top. Open `M`, press `Esc`: atlas closes first.
5. Open settings, adjust volume (audible immediately), mouse sensitivity, camera FOV and quality. Relaunch Play and ensure changes persisted in PlayerPrefs.
6. Save from menu: ensure the ordinary save exists, re-enter game and validate F9 loads correct position and inventory.
7. Resize Game View 1920×1080, 1600×900, 1280×720; ensure buttons and settings sliders stay clickable and not clipped.
8. Enter/exit Play repeatedly: no stuck `Time.timeScale=0` or permanently unlocked mouse. If UI fails, turn off `useModernUi` in SectorBootstrap to recover legacy HUD.
9. Run **EditMode tests** `SectorMainMenuTests` and previous UI pass tests. This still requires the user's Unity Editor; source-level syntax and structure checks are not a substitute.

## Known limitations / next milestones

- This is a title/pause overlay within the **playable prototype scene**, not a standalone load-scene launcher. The existing single save auto-loads before the title appears.
- **New Game** is intentionally excluded until multiple independent save profiles or a recoverable backup/confirmation system is implemented.
- PlayerPrefs are device-local and are not part of in-game save profiles.
- Real UI sound assets, scalable typography for ultrawide and controller navigation need separate QA/passes.
- Existing IMGUI journal, atlas and horde notifications still need migration for a fully unified aesthetic.
- No Unity runtime compilation, performance profile or visual screenshot verification occurred in this environment.

**Next pass:** item splitting, backpack sort/filter, journal redesign and/or safe save-slot selection, depending on Unity feedback.
