# Dead Sector — Save Profiles v0.1 (UI Pass 0.5)

**Branch:** `prototype/world-8km`. **Status:** implemented in GitHub source; awaiting Unity Editor compilation, EditMode tests and actual Play Mode validation. No branch merge or release.

## Four independent playthrough slots

| UI slot | PersistentDataPath filename | Behavior |
|---|---|---|
| LEGACY / OLD SAVE | `DeadSector_Save_v1.json` | Existing V1 save retained at its original location, never moved/deleted by profile creation |
| PROFILE 01 | `DeadSector_Profile_01_v1.json` | Separate save state |
| PROFILE 02 | `DeadSector_Profile_02_v1.json` | Separate save state |
| PROFILE 03 | `DeadSector_Profile_03_v1.json` | Separate save state |

The title/pause menu now lists all four entries and their saved day/time/health, `EMPTY / NEW GAME` or `UNREADABLE / PRESERVED`.

- Clicking an existing readable slot loads it and changes the active profile.
- Clicking an empty numbered slot creates an independent starter game: 100 HP, stamina, hunger and thirst, day 1 at noon, unarmed, 1 water + 2 bandages, no crafted gear, empty journal / containers / resources / horde progress.
- Clicking the already selected active slot leaves it unchanged. Creating a new game **never overwrites an existing slot**.
- A malformed, unreadable or unsupported save file is **never treated as an empty slot** and is not overwritten by F5/autosave.
- Existing `DeadSector_Save_v1.json` stays in place. The former save V1 JSON schema remains unchanged, including separated stacks saved by the previous UI pass.
- F5/F9 and the every-90-seconds autosave operate **only on the selected slot**. Existing-save writes retain the `.bak` backup mechanism.
- Last successfully loaded profile is recorded in `PlayerPrefs`; on startup it is restored **only if its file exists and validates**, otherwise the legacy slot is considered.
- Switching in the **initial title menu** needs no extra save: the player has not yet started playing. Switching after Continue first saves the current profile. If saving fails, the switch is blocked and current progress and selected slot remain in place.
- Profile transitions replace inventory, weapons/armor, health/needs, clock, journal, looted containers and harvested resources using the existing save importer. Living infected from the previous profile are removed, and normal prototype spawn population is re-created where navigation is ready.
- No Delete Profile control is included; any manual cleanup or rename is intentionally outside the game until we can add explicit backups and confirmation.

## Files

- `Assets/DeadSector/Runtime/SectorSaveProfiles.cs` + `.meta`: fixed safe filenames, V1 validation, create-only save writer, starter snapshot, status.
- `Assets/DeadSector/Runtime/SectorGameplay.cs`: active profile selection, load/save routing and state reset, corrupt file protection.
- `Assets/DeadSector/Runtime/SectorMenuUI.cs`: four-slot title/pause selector, labels and safe switching.
- `Assets/DeadSector/Runtime/SectorNavigation.cs`: clear/recreate infected on profile transition.
- `Assets/DeadSector/Tests/Editor/SectorSaveProfilesTests.cs` + `.meta`: isolated-directory regression tests; no real user saves are touched by tests.

## Local Unity verification required

1. **Back up the current save before testing**, independently of Git: copy `DeadSector_Save_v1.json` (and, if present, its `.bak`) from the folder shown in `Application.persistentDataPath` to your own backup folder.
2. Stop Play, safely update branch scripts without overwriting scene/FBX/.meta local changes. Open Unity and verify zero red Console compile errors.
3. Run `SectorSaveProfilesTests` in Unity Test Runner, followed by the earlier `SectorInventoryStackOperationsTests`.
4. Start Play: verify old V1 file appears as **LEGACY** with its game day, and profile slots 01–03 show empty (unless already created). Select LEGACY; character position, inventory and journal should load unchanged.
5. Click empty **PROFILE 01**: new unarmed starter with two bandages and one water. Verify old LEGACY file still exists and has unchanged data. Save with F5, change loot and save again.
6. Create PROFILE 02, gather different loot and save. Switch back to PROFILE 01. Verify all independent inventory, journal, world time, containers and character positions are maintained.
7. Exit Play / reopen scene; previous selected valid profile should remain selected, loading correct file automatically before title.
8. While playing after Continue, hit Esc and switch profiles: current profile must be saved first. Verify failure to save blocks switching; no progress loss.
9. Copy a deliberately corrupt JSON file only into an isolated **test directory** or disposable test project and confirm occupied/corrupt slot is shown as unreadable and never replaced. Never corrupt actual saves for testing.
10. Resize menu in 1920×1080, 1600×900 and 1280×720 to ensure slot texts/buttons do not clip.
11. Check cross-profile infected resets, the streaming terrain's new player location, and absence of carried-over active horde zombies.

**Known limitations:** Save V1 does not serialize live zombie positions, ragdolls or detailed world construction; profile switching clears/re-spawns default infected. Save profiles are local to the machine (no Steam cloud). Standalone Main Menu scene is not implemented; UI is displayed after streamed world initialization in the prototype playable scene. Profile names fixed to 01–03, no delete/rename. Source static review is NOT Unity Play Mode verification.

## Recommended follow-up

After acceptance in Unity: standalone title/loading scene, expandable save slots with explicit clone/delete/rename and confirmation, live zombie/world construction persistence, save corruption recovery wizard from .bak, auto-save status icon.
