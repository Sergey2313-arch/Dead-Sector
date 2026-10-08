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

## Pre-validation content expansion (8 October 2026)

- Two instant bare-hand attacks: **LMB quick weak punch (12 base damage, 0.27s repeat cooldown)** and **RMB heavy punch (27 base damage, 0.74s repeat cooldown)**. No charging, hold-to-attack or pre-hit delay. Heavier attacks use more stamina.
- Lightweight visible arm punch overlay (alternating quick jabs; right-arm heavy punch). Needs X Bot retargeting verification in Unity.
- Start bare-handed with the knife available to equip through the inventory. Melee-equipped players keep light/heavy clicks; firearm primary fire remains LMB.
- **C**: crafting panel with bandages, medkits, hatchets, improvised spears and hand torches. Ingredients are deducted transactionally with slot/weight checks.
- **E**: gather nearby timber, scrap metal or cloth from deterministic resource nodes. Harvested nodes are persisted in JSON and do not respawn on tile revisit.
- **J**: field objective journal, including village/clinic/factory/radio exploration, loot pickup, gathering, crafting and zombie eliminations. Saves progress.
- Atmosphere/weather prototype: clear, overcast, rain and mist, based on the saved world clock. Local rain particles and fog settings added.
- Additional road dressing per streamed 1km sector: collision-enabled primitive car wrecks, concrete barricades and roadside signs, deterministically aligned with road corridors.
- New editor tests for light/heavy punch tuning, stamina behavior, crafting, item catalog and journal JSON-compatible progress. They have **not been executed in Unity**.
- Current crafted torch is a placeholder held item, **not yet a working flashlight/light source**. Damage and hand animation parameters await playtesting.

## New primitive-survival and horde milestone (8 October 2026)

- **Stone Age progression:** loose ground stones; dry bushes for sticks/fiber; fiber bushes; cotton plants; scrap and timber caches. Initial player has **no weapon**; light/heavy fist attacks work immediately.
- **Crafting hierarchy:** cord → stone knife/stone axe/wood club/spear/torch → wooden head/chest/leg protection → cotton fabric, hood/shirt/pants/footwraps and cotton backpack. See [Primitive Crafting + Horde + UI](Primitive_Crafting_Horde_UI.md).
- **Functional character inventory:** 5 real equipment slots, selectable item grid, equip/unequip, armor mitigation, backpack slot capacity of 22→30, first-pass worn armor meshes.
- **Always-visible character HUD:** HP, stamina, hunger, thirst, survived day, world time, equipment damage reduction.
- **Night hordes:** warning at 20:30 and attack at 21:00 every default game day, initially 6 zombies and +3 per night up to 30 (subject to 45 nearby NPC cap). Nightly intervals are configurable.
- **Persistence:** survived day count, armor and backpack, horde trigger history, crafting inventory, resource depletion in local JSON save.
- **Developer test tool:** `Dead Sector > Debug > Prepare Night Horde` during Play Mode, plus `Add One Survived Day`.
- **Validation required:** dynamic local NavMesh, armor appearance on imported X Bot, full inventory GUI, item recipes, scripted horde day progression, save/load. New Edit Mode tests committed but not executed.
- **Not complete:** co-op network authority, production 3D clothing, full persistence of living zombie state, ragdolls, advanced crafting stations and polished UI.

## Visual and combat pass 0.3 (source ready, Unity validation pending)

- Three zombie classes: normal shambler (100 HP), fast runner (70 HP), heavy brute (210 HP); varied pursuit speed and attack damage. Mixed hordes grow from day 2 and day 4.
- Imported zombie visual scale preserved, class color/detail blockouts, hit flash, stagger and a simple temporary falling corpse; collider and NavMeshAgent sizes match variant.
- Gunshots now notify nearby infected to investigate, with higher hearing radius for rifles than pistols.
- Weapon model blockouts upgraded: distinctive flint/steel tools, axe, club, spear, rifle, pistol, backpack and a luminous held torch.
- Wooden/cotton armor replaced with segmented Humanoid bone attachments (still placeholders requiring retargeting/scale check).
- Immediate click-to-hit light/heavy melee retained; melee equipment uses a swing rather than punch motion. Crosshair, hit markers, damage labels, recoil and muzzle flash added.
- Compact three-slot quickbar and ammunition count. Prototype diagnostics hidden by default; use **F3** to display them.
- A dedicated Editor test suite checks enemy stats and day-based wave types. Source linkage checks pass; neither Unity Editor compilation nor playtests have run.
- Detailed QA checklist: [Visual_Combat_Zombie_Pass_0_3.md](Visual_Combat_Zombie_Pass_0_3.md).

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
- LMB: immediate light punch/primary fire; RMB: immediate strong bare-hand or heavy melee attack
- C: crafting recipes; J: field objective journal; E: harvest resources, use doors or take loot
- I: inventory; F5/F9: save/load. These bindings are prototype defaults, subject to Unity test.

## Tracked GitHub work items

- [#2 — Unity 6 integration and playtesting](https://github.com/Sergey2313-arch/Dead-Sector/issues/2) — blocker before claiming playable milestone.
- [#3 — Production environment and modular world](https://github.com/Sergey2313-arch/Dead-Sector/issues/3).
- [#4 — Survival gameplay polish and complete persistence](https://github.com/Sergey2313-arch/Dead-Sector/issues/4).
- [#5 — Authoritative co-op multiplayer](https://github.com/Sergey2313-arch/Dead-Sector/issues/5) — planned, not working yet.
