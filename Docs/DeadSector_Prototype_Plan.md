# Dead Sector — prototype roadmap

## Current target
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
- One-click prototype preparation menu.

## Next work
1. Verify player idle/walk/run/jump retargeting in Unity and remove any remaining bad pose.
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
