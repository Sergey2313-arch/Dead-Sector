# Dead Sector — prototype roadmap

## Current target
**Approved world design:** `Docs/World_Map_8km_Approved.md` (illustration approved 8 October 2026). Its 17+ landmark placements, routes and implementation status live in `Assets/DeadSector/Runtime/SectorMapPlan.cs`. The pictured landscape is still a target concept, not existing game geometry.

Playable 8 x 8 km survival prototype with a full-body humanoid player, shoulder TPP/FPP switching, animated zombies, explorable settlement, streamed terrain and a stable base for combat/inventory work.

## Implemented
- 8 x 8 km streamed terrain prototype.
- Full-body X Bot player pipeline with Humanoid Mixamo retargeting.
- Single-player spawn integration (legacy procedural player is skipped when the full-body player exists).
- CharacterController movement, sprint and jump.
- TPP shoulder camera with collision and FPP/TPP toggle.
- Player animation state debugging.
- Safer spawn after terrain generation.
- Zombie navigation with NavMesh.
- Zombie AI states: idle/wander, investigate, chase, attack.
- Zombie senses: sight, hearing and short-term memory.
- X Bot zombie prefab builder so zombies use the same mannequin body type as the player.
- Mixamo zombie animation controller support for run, crawl, scream, bite, punch, kick, stand-up and optional walk/idle.
- Expanded procedural settlement with houses, gabled roofs, doors, windows, porches, yards/fences, factory, warehouse, checkpoint and streetlights.
- Extra central POIs: clinic, fuel station, garages and radio tower.
- Runtime top-down minimap that follows the player and renders the actual streamed world.
- Player direction marker and colored POI markers on the minimap.
- M toggles a larger tactical map view.
- Live azimuth compass: 0-359 degree heading, N/NE/E/SE/S/SW/W/NW marks, 5-degree minor ticks, 15-degree labels, and direction-relative POI labels with distances.
- The compass hides while the enlarged tactical map is open; debug panel moved to the lower left.
- M opens a schematic full 8 x 8 km design atlas with road corridors, planned POIs and the player's real world coordinates; this is explicitly not the finished physical map.
- First physical world-map pass: carved northwest mountain lake, southern reservoir, river, dam outlet, and quarry into streamed terrain height data.
- Tile-owned road ribbons with mesh colliders and water surface meshes on the approved road and water coordinates.
- New streamable procedural blockouts at the approved coordinates for village, town, ruined city, factory, warehouse, clinic, gas station, checkpoint, garages, radio tower, forest camp, quarry, construction site, dam and airfield.
- Added Play Mode editor window to teleport to each canonical location for inspection: Dead Sector > Debug > Visit World Locations.
- Important: blockout props are not final art; global NavMesh/zombies, bridge supports, swimming and terrain/URP profiling still require local Unity verification.
- One-click prototype preparation menu.

## Latest playable-systems source-code milestone (Unity validation pending)

- Item definitions for food, water, bandages, medical kits, materials, knives, hatchets, pistols, rifles and ammunition. There are NO rarity/quality tiers.
- Transactional slot-limited (22 slots), weight-limited (35 kg), stackable inventory; invalid/oversized save entries are rejected.
- Runtime supply crates at starter spawn and POIs, deterministic contents, non-duplicating across tile revisit.
- E pickup and E swing hinged doors on streamed modular buildings.
- I inventory GUI with Use/Equip actions, 1/2/3 equipment slots, prototype backpack and held/stowed gun models on X Bot bones.
- Basic hitscan / melee logic with line-of-sight, ammo consumption and zombie health/death; not yet polished ballistics, animations or ragdolls.
- Hunger, thirst, stamina, sprint/jump costs, exhaustion damage, edible/drinkable/medical effects.
- Dynamic day/night light and fog, 60-minute default in-game day.
- JSON Save V1: F5 write, F9 load, autosave, health/needs/time, position, inventory/equipment and depleted loot. Not yet full dynamic-world persistence.
- Test suite for inventory atomicity, stack/weight limits, save JSON, loot transfer, geography and compass. **Tests have been committed but NOT executed in Unity.**
- [Full production and co-op plan](Production_And_Coop_Roadmap.md) records validation, future real art, networking and release gates.

## Next work
0. Validate current game map, water, roads and world blockouts in Unity 6 / URP; fix compile or rendering issues first.
1. Improve road-to-terrain slope smoothing, bridge transitions, and shore edge meshes; current blockouts are first-pass geometry.
2. Verify player idle/walk/run/jump retargeting in Unity and remove any remaining bad pose.
2. Verify minimap/tactical-map rendering in URP and tune scale/viewport.
3. Finish polished shoulder-camera behaviour and FPP head/body visibility.
4. Add Zombie Idle and Zombie Walk clips when available.
4. Add zombie health, hit reactions, death and corpse states.
5. Add melee hitboxes and player damage/combat feedback.
6. Replace prototype house materials/geometry with production modular assets while keeping current layout.
7. Add loot containers, item pickups and inventory.
8. Add weapon slots and visible equipment on the full-body player.
9. Add save/load and world persistence.
10. Add co-op only after the single-player gameplay loop is stable.

## Unity setup shortcuts
- Player: Dead Sector > Character > Mixamo > 00 - BUILD EVERYTHING
- Zombies: Dead Sector > Character > Zombies > 00 - BUILD ZOMBIES
- Whole prototype: Dead Sector > Setup > 00 - PREPARE PLAYABLE PROTOTYPE
- Inspect full world in Play Mode: Dead Sector > Debug > Visit World Locations; select a POI and teleport
- M: approved 8km planning atlas; V: first-person / third-person toggle

## Tracked GitHub work items

- [#2 — Unity 6 integration and playtesting](https://github.com/Sergey2313-arch/Dead-Sector/issues/2) — blocker before claiming playable milestone.
- [#3 — Production environment and modular world](https://github.com/Sergey2313-arch/Dead-Sector/issues/3).
- [#4 — Survival gameplay polish and complete persistence](https://github.com/Sergey2313-arch/Dead-Sector/issues/4).
- [#5 — Authoritative co-op multiplayer](https://github.com/Sergey2313-arch/Dead-Sector/issues/5) — planned, not working yet.
