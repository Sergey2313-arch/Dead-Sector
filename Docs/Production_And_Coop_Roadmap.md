# Dead Sector — Remaining production roadmap and verification gates

Main development branch: `prototype/world-8km`. Target game: cooperative post-apocalyptic survival, 8×8 km terrain, FPP/TPP, no rarity/quality-level gimmicks. **Nothing is a release until Unity verification.**

## Added as source-code prototype (Unity test pending)

- 8×8 km streamed terrain, approximate approved-world lakes, river, roads, POIs and simple walk-in landmark blockouts.
- Full-body Mixamo X Bot + movement and shoulder camera.
- Same mannequin model pipeline for zombie animation; local NavMesh detection/attack AI.
- Azimuth compass, local minimap, world-planning atlas.
- Interactive swinging doors in generated houses and gameplay loot crates.
- Item catalog with stack limits, 22 slots, 35 kg load limit, and no item rarity/quality levels.
- Persistent crate contents across temporary streaming and local disk save/load.
- Hunger, thirst, stamina/exhaustion and consumables.
- 3 equipment slots (primary, sidearm, melee), temporary held/stowed mesh blockouts + backpack on humanoid bones.
- Basic point-target melee/shooting/damage with ammo, zombie health and temporary corpse.
- JSON local saves, manual F5/F9 and periodic autosave.

## Phase A — Required stabilization (BLOCKER)

1. Open Unity `6000.6.4f1` with the URP project.
2. Resolve compile errors in Console FIRST. Do not merge more unrelated changes until the new script assembly compiles.
3. Run Edit Mode tests (existing world/grids + `SectorInventoryTests`, `SectorCompassTests`).
4. In `DeadSectorPrototype.unity`, run `Dead Sector > Setup > 00 - PREPARE PLAYABLE PROTOTYPE`, then Play.
5. Confirm one active player (only the scene's X Bot), valid Mixamo rig, stable spawn, WASD/Shift/Space/V and a single AudioListener.
6. Check collisions against slope, doors, water basins and generated bridge roads. Check that no character falls through streaming terrain.
7. Inspect minimap camera render order / layers; confirm M map is north-up and compass matches actual camera heading.
8. Visit every canonical location using `Dead Sector > Debug > Visit World Locations`, inspect geometry and frame rate.
9. Verify I inventory, E crate/door, LMB attack, 1/2/3 equipment, F5/F9 save/load, survival drains and zombie hit/death.
10. Record and fix observed bugs with screenshots and Console stack traces.

## Phase B — Single-player quality

1. Move prototype interactions into formal action interfaces (move, use item, equip, interact, shoot, melee) independent of local input.
2. Use actual weapon, backpack, zombie and building art prefabs. Replace primitive mesh placeholders, author LODs, colliders and occlusion.
3. Rig reload, aim, crouch, prone, damage, hit-react, death, interact and climb animations.
4. Add reliable gun line tracing, ballistics/projectile options, recoil, magazines, reload and ammo UI; melee hitboxes/animation windows.
5. Add equipment durability/condition only if the gameplay design requires it; NO loot rarity tiers.
6. Replace starter loot caches with authored lockers, cupboards, zombie drops and deterministic regional loot tables.
7. Add crafting and item world dropping, dragging between containers, splits, character clothing and backpack size.
8. Implement doors with persistence and network-safe state, ladders, stairs and object-specific interaction markers.
9. Add consumable action duration, bleeding, injury, status effects and audio feedback.
10. Add full-state world persistence and schema migrations. Current save is V1 prototype only: dynamic doors/zombie corpses are not saved.
11. Implement water buoyancy/swimming and shoreline physics. Current surfaces have no swimming.
12. Add proper navigation streaming so NPCs roam all 64 tiles; currently NPC NavMesh is restricted to the central starter area.
13. Profile streaming, foliage, terrain height sampling, GPU draw calls, physics/lighting and memory under gameplay load.

## Phase C — Multiplayer / cooperative play (NOT IMPLEMENTED)

Current repository manifest contains **no multiplayer transport package**. Do NOT claim co-op works or fake it by spawning duplicate local player objects.

Suggested implementation (after Phase A/B are stable):
1. Select networking stack for Unity 6 (Netcode for GameObjects + Unity Transport or a suitable alternative) and pin compatible package versions.
2. Make host/server authoritative for inventories, loot, zombie AI, hits, damage, world state and saves.
3. Add networked player identity, spawn/despawn and separate local/remote camera+input ownership.
4. Replace direct gameplay decisions with validated server commands; reject invalid inventory modifications, range violations and replayed actions.
5. Replicate world cell streaming interests, loot container state, doors and equipment.
6. Replicate root/animator parameters with interpolation, tick budgets and ownership clarity.
7. Test late-join, disconnect/reconnect, host migration policy, 2-player stress, packet loss and save consistency.
8. Configure transport, NAT/relay/dedicated-host choice, server discovery and version handshake.
9. Add cross-session persistence, anti-duplication safeguards and safe world save points.

## Quality gates

- Compile + tests pass in Unity 6; recorded as verified only after actual Editor test output is observed.
- Scene play from a clean checkout with no missing locally imported FBX (or a clearly documented fallback).
- Saving and loading does not duplicate loot, equipment or player objects.
- Zoomed map, minimap and azimuth follow the same world-coordinate convention.
- Performance numbers measured on the actual target PC; not assumed.
- No release tag or stable branch promotion without user approval.

## Input summary (prototype)

`WASD` move, `Shift` sprint, `Space` jump, `V` FPP/TPP, `M` world atlas, `I` inventory, `E` door/loot, `1/2/3` equipment, `LMB` attack, `F5` save, `F9` load, `R` respawn, `Esc` cursor release.

Save file: `Application.persistentDataPath/DeadSector_Save_v1.json`. A prior save is backed up as `.bak` on every save attempt.

Developer note: `SectorGameplay.cs` is temporarily a prototype orchestrator; break it into UI, inventory, loot, combat, persistence and server-command modules before co-op.
