# Dead Sector — Single-player gameplay framework / M1

**Development branch:** `prototype/world-8km`
**Purpose:** freeze the critical prototype systems before spending time on final character art, environment assets, Mixamo animations, VFX and audio.
**Status:** source implementation complete for the systems marked **implemented**, but **M1 acceptance remains blocked by Unity Editor QA**. Do not merge into main or publish a release yet.

## Core gameplay modules

| System | Code state | Game-view verification |
|---|---|---|
| 8x8 km terrain, 3x3 streamed local tiles, roads, water, POIs | existing implementation | partial: visually distorted/gray terrain on recent screenshots; F3 collision diagnostic REQUIRED |
| Character, walking, sprinting, stamina, jumping, FPP/TPP | existing + fixes | partial: user saw mannequin and potential terrain penetration |
| Health, hunger, thirst, consumables, bandages | existing | HUD renders; survival mechanics need longer session test |
| Items, stack capacity/weight, inventory, sorting, split stacks | existing | Canvas rendered; actions not fully QA tested |
| Crafting (primitive weapons/armor/resources) | existing | code tests present; interaction QA pending |
| Pistol, rifle, fists, melee weapon attacks; infected reactions | existing | horde and zombies visible; weapon damage QA pending |
| **R reload**, separate 12-round pistol/30-round rifle magazines and saved loaded rounds | newly implemented | untested in Unity |
| Shambler, runner, brute, ragdoll, navigation and hordes | existing | visible zombie/horde event; navigation, LOS and animations need QA |
| **B building**: wood foundation, wall, barricade, storage crate, hinged door | newly implemented | untested in Unity |
| Building preview, collisions, resource costs, validation against slope and obstacles | newly implemented | untested in Unity |
| Base defense: zeds strike player-built pieces blocking sightline; structures destroy at zero HP | newly implemented | untested in Unity |
| Storage: E takes first stored item, SHIFT+E deposits selected backpack item | newly implemented | untested in Unity |
| Save profiles: legacy, three independent playthroughs, F5/F9, automatic backups | existing | selection menu rendered; multi-profile read/write not tested |
| Save V1 extensions: buildings (position, kind, HP, storage, open door), magazines | newly implemented, additive fields | save/load test required; old V1 remains readable |
| **Death screen**: last-live-checkpoint restore or camp respawn | newly implemented | untested in Unity |
| Tactical Canvas: HUD, compass, local map, inventory, crafting, journal, front end, pause/settings | existing | visually rendered; some labels remain small and require UI art pass |
| UI/debug F3 for terrain Y vs player feet Y and gameplay state | existing | player must capture diagnostic screenshot |

### Controls (prototype)

- `WASD`: movement; `Shift`: run; `Space`: jump; `V`: FPP/TPP.
- `LMB`: attack; `RMB`: heavy melee; `R`: reload firearm **only when alive** (cannot heal/respawn a live character).
- `1/2/3`: weapon slots, except in build mode.
- `E`: interact with resource/loot/door and take an item from your storage; `Shift+E`: store the last selected inventory item in a nearby storage crate.
- `B`: toggle build mode. Inside build mode use `1..5` for foundation/wall/barricade/crate/door, `Q/E` rotate, `LMB` build, `RMB` cancel.
- `I` bag; `C` crafting; `J` journal; `M` full atlas; `Esc` pause; `F3` physics debug; `F5` save; `F9` load.
- On death: use Canvas **LOAD CHECKPOINT** or **RESPAWN AT CAMP**; no automatic death save. In legacy mode R respawns only if already dead.

## Save compatibility

All new persisted data is appended as optional fields on the existing `SectorGameSave.version=1`:
- `structures: List<SectorBuildSnapshot>` with stable ID, type, world-space position/rotation, health, storage contents and door open flag.
- `pistolRounds`, `rifleRounds` loaded rounds.

Legacy JSON without these fields loads with an empty base and empty magazines. Reserve ammunition is retained in inventory; **press R to load it into a gun after updating**. No save migration, deletion, renaming or profile merge has been performed.

## Unity acceptance gate (MANDATORY before calling M1 done)

1. In a backed-up, **isolated test worktree**, update only after stopping Unity and checking `git status`. Do not use `reset --hard`, `clean` or overwrite model FBXs / scenes / local .meta files.
2. Unity 6000.6.4f1: wait for import, check **0 compiler errors**; run all EditMode tests including `SectorBuildingFrameworkTests`, `SectorMagazineRulesTests`, `SectorSaveProfilesTests` and movement regressions.
3. **P0 terrain**: press F3 while player appears buried and record `Feet Y`, `Terrain Y`, `Grounded`, position X/Z and world tile count. If mismatch, fix ground alignment before any art pass.
4. Fresh PROFILE 01: exactly 100 HP, one water, two bandages, empty base/magazines. Walk, run to exhaustion, jump, switch FPP/TPP. Confirm no free R healing.
5. Harvest stone/sticks/wood, craft primitive tools. Place each of five build plans on legal ground; rejected collision, water and steep slope placements must consume **zero** resources.
6. Select an item inside `I`, approach constructed crate: **SHIFT+E deposit**, **E withdraw**. Open/close a constructed hinged door with E.
7. Build walls near a horde: infected should attack blocking construction, reduce HP and break it after repeated strikes. Check that players cannot walk through closed walls.
8. Acquire pistol/rifle and reserve ammo: tap fire with empty magazine (blocked), press R, wait 1.35/1.8 seconds, shoot rounds until empty; UI count and reserve amount correct.
9. F5, move, modify storage, open door, damage barricade; F9 must restore base placement, storage, HP, door state and loaded magazines. Repeat with PROFILE 02; no cross-profile contamination.
10. Kill character after a successful living save: death overlay freezes game. **Load checkpoint** restores previous life; **Respawn at camp** revives without overwriting old file.
11. New profile safe test: 01 and 02 independent; original `DeadSector_Save_v1.json` and .bak intact. Stop/reopen Unity to test last selected profile.
12. Test 1920×1080, 1280×720, 16:10; UI readability, performance, minimap, horde notification, editor warnings. Verify no duplicate EventSystem and AudioListener.

### Known blockers / deliberately NOT claimed

- **No Unity test execution was available in GitHub tooling**: static source checks are not compiler or Play Mode proof.
- The gray/reflective terrain and potential player-under-ground behavior seen in the user's previous screenshots remain unconfirmed until F3 physics measurements. This is P0.
- Full-body character/zombie prefabs and Mixamo FBXs exist in the user's separate LOCAL worktree, not in the isolated test checkout. Procedural mannequins are expected until those assets are integrated safely.
- Live zombie state is not serialized; loading and profile switches respawn default ambient infected. Co-op/network authority, drivable vehicles, story campaign, robust audio/animation blending and optimized base persistence/streaming are **not implemented**.
- The B-build placement and storage UI are functional-code placeholders; final UX, mesh materials, animations and sounds belong to the next art pass after M1 passes runtime QA.

## Next phase after M1 acceptance

**Art pipeline**: terrain shaders/textures, sky/lighting, full-body animated player + infected, realistic shelters, weapons and gear meshes, item sprites/icons, weather VFX, sound effects, VFX impacts and polished UI. Gameplay APIs `SectorPlayer`, `SectorBuildPiece`, `SectorMagazineRules`, `SectorZombie`, `SectorGameplay` should be kept stable so animations attach to existing states without rewriting survival logic.

**M1 is a SINGLE-PLAYER technical foundation**, not a claim that the entire commercial game or multiplayer is finished.
