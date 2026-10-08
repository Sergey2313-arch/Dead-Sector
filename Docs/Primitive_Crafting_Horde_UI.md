# DEAD SECTOR — Primitive Survival, Character Inventory and Night Hordes

Implementation branch: `prototype/world-8km`. Status: **source code integrated, not yet compiled or Play Mode tested in Unity 6000.6.4f1**. These are first-pass blockouts and IMGUI; no claim of production UI/animations.

## Primitive crafting hierarchy

The player spawns **without free weapons or armor**. They begin with bare-hand fast (LMB) and heavy (RMB) punches and a small survival supply. The first gear must be made from freely gathered wilderness materials. All items have normal physical weights/stack limits and **no loot-rarity tiers**.

### Raw materials

| Find | Action | Drops |
|---|---|---|
| Loose stones on ground | Approach and press `E` | Stone |
| Dry bush / dead branches | Approach and press `E` | Dry sticks + some plant fiber |
| Live green fiber shrub | `E` | Plant fiber |
| Cotton bush | `E` | Raw cotton |
| Metal scrap pile | `E` | Scrap metal |
| Old timber pile | `E` | Timber |

Nodes are deterministic around the player while exploring and the harvested IDs are stored in `DeadSector_Save_v1.json`. This is a first-pass representation of natural resource objects, not authored foliage artwork.

### Recipes

| Stage | Craft | Required ingredients | Result |
|---|---|---|---|
| Stone age | Twisted cord | 3 fiber | 1 cord |
| Stone age | Stone knife | 2 stone + 1 stick + 2 fiber | 1 stone knife |
| Stone age | Stone axe | 3 stone + 2 stick + 1 cord | 1 stone axe |
| Stone age | Wooden club | 3 sticks | 1 club |
| Stone age | Wooden spear | 3 sticks + 1 stone + 1 cord | 1 spear |
| Stone age | Hand torch | 1 stick + 3 fiber | 1 torch *(light effect pending)* |
| Wooden armor | Head guard | 2 timber + 1 cord | Head slot |
| Wooden armor | Chest guard | 4 timber + 2 cord | Chest slot |
| Wooden armor | Leg guards | 3 timber + 2 cord | Legs slot |
| Cotton | Weave cotton | 4 cotton | 2 cloth |
| Cotton | Hood | 2 cloth + 1 cord | Head slot |
| Cotton | Shirt | 4 cloth + 2 cord | Chest slot |
| Cotton | Trousers | 3 cloth + 2 cord | Legs slot |
| Cotton | Footwraps | 2 cloth + 1 cord | Feet slot |
| Cotton | Backpack | 6 cloth + 3 cord | Backpack: 22 → 30 inventory slots |
| Other | Bandage | 2 cloth | 1 bandage |
| Other | Medical kit | 3 bandages + 2 cloth | 1 medical kit |
| Later | Metal hatchet | 3 scrap + 1 timber | 1 hatchet |

**C opens the crafting tree**, grouped into wilderness/primitive, wooden, and cotton stages. The recipe button is disabled unless every ingredient and the resulting weight/slot capacity fit; removed ingredients and outputs are handled transactionally.

### Armor and inventory UI

**I opens character inventory**: character silhouette, 5 functional gear slots (head, chest, legs, feet, bag), item grid, selected item details, Use/Equip and Remove buttons. The UI displays stack counts, inventory capacity and weight. Wooden equipment reduces damage more than cotton. Cotton bags increase the inventory slot limit to 30. Current armor visual models are primitive attachments to the humanoid rig; fitted clothing models still need Unity review.

The always-visible HUD shows **HP, Stamina, Hunger, Thirst, Day, Clock, and Armor damage reduction**. `SectorHUD.cs` owns it, while `SectorGameplay.cs` owns full inventory and crafting panels.

## Nightly Horde system

First playable rules:
- **Night 1 onward**: horde warning at **20:30**, wave starts at **21:00**.
- Defaults to **one attack every game night** (`nightsBetweenAttacks = 1`), adjustable (for example, set to 7 for a weekly Blood Moon).
- Night 1: **6** infected; Night 2: **9**; Night 3: **12**; then +3 per night, maximum **30** in a newly scheduled wave.
- A separate cap of 45 living nearby NPC zombies prevents unbounded spawn.
- Horde zombies appear approximately 50–105 m from player on valid NavMesh rather than right beside them. Horde AI continuously pursues the player instead of forgetting them after a few seconds.
- If local NavMesh is not ready, spawning is **retried**, not silently recorded as a wave.
- A horde is reported repelled when no live horde enemies remain.
- The persistent record contains last triggered and cleared night. Loading during an attack concludes that interrupted wave rather than respawning duplicates. **Live zombie placement/deaths are not yet serialized.**
- Terrain streamer NavMesh rebuilds around the current player area when they travel far from the previous navigation center.

To test quickly in Unity Play Mode: `Dead Sector > Debug > Prepare Night Horde`. This shifts the clock to 20:48 and the warning should display; a short wait reaches 21:00. `Dead Sector > Debug > Add One Survived Day` advances day count for difficulty testing.

## Controls

| Input | Behaviour |
|---|---|
| `WASD` / `Shift` / `Space` | Walk, run, jump |
| `V` | First- / third-person |
| `M` | Full 8×8 km map |
| `I` | Inventory + character equipment |
| `C` | Crafting hierarchy |
| `J` | Objective journal |
| `E` | Collect ground stones, harvest bushes, loot crates, open doors |
| `LMB` | Immediate weak punch or equipped-weapon primary attack |
| `RMB` | Immediate heavy punch or heavy melee (no charging) |
| `1/2/3` | Weapon slots |
| `F5/F9` | Save / Load |

## Evening Unity verification (blocking release)

1. Console compilation; Editor test suite incl. `SectorEquipmentAndHordeTests.cs` and updated `SectorCraftingTests.cs`.
2. Starter has no free weapon; stone and dry bush are found within walking distance, E gathers.
3. Craft stone knife and axe, club and first wooden item; test failed recipes leave ingredients untouched.
4. Collect cotton, weave fabric, equip clothes; verify character slots and defense reduction.
5. Craft cotton bag; verify slot capacity becomes 30 and safely falls back to 22 when unequipped.
6. Verify basic HUD is readable and doesn't overlap azimuth or weather.
7. Fast-forward to 20:48, see warning, then horde spawns near player after 21:00; test attacks and repelling.
8. F5 save / F9 load retains equipped slots, day count, horde trigger history, resource depletion and inventory; no duplicate loot.
9. Teleport to distant POI, allow terrain/navmesh to rebuild, then test horde there.
10. Measure frame rate and review visual quality. The character armor, trees/bushes and zombies are still **blockouts**, not final production assets.

NO release tag/merge requested. All new work stays in `prototype/world-8km`.
