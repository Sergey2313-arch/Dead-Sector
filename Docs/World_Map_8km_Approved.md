# DEAD SECTOR — OFFICIAL 8 × 8 KM WORLD MAP, V1

**Approved:** 8 October 2026  
**Visual reference:** the generated top-down atlas approved in the project chat (DEAD SECTOR, grid A–H / 1–8, dense forests, lakes, industrial zones, village, abandoned city, airfield and quarry).  
**Source of truth for editable layout:** `Assets/DeadSector/Runtime/SectorMapPlan.cs`.

## Status and fidelity

The image is **approved concept art**, NOT a screenshot of a finished Unity world. A generated image has illustrative roads, coastlines, and non-geodetic text. Exact topology cannot be extracted perfectly, so the approximate coordinates below are deliberate design decisions. The gameplay map should converge on these decisions progressively.

The existing prototype has a 8×8 km terrain generator, streaming tiles, central primitive village/industry/clinic/garages/fuel/checkpoint, a working player prototype, zombie scripts and map/compass code. Its positions, textures, building models and roads do **not** yet match the approved illustration. Nothing in the map plan should be presented as a finished in-game location until built and tested.

## Coordinate system and grid

- Unity +X is **east**, +Z is **north**, +Y is up.
- Map bounds: X and Z from **−4000 m to +4000 m**.
- Grid columns A–H go west → east; rows 1–8 go north → south. Each cell is 1000×1000 m.
- Sample coordinate: (X=−2700, Z=1450) is **B3**.
- The current starter test area remains around **(0, 0)** until navigation, spawning, and buildings are migrated.
- Players travel the **real 8×8 km Unity world**; the concept is the final intended geography.

## Key locations

| Grid | Landmark (reference) | Designed world coordinate X,Z | Stage |
|---|---|---|---|
| B2 | Mountain lake | −2650, +2850 | Terrain/water planned |
| D1 | Forest camp | −750, +3100 | Planned |
| E1 | Radio tower | +720, +3050 | Prototype tower exists near spawn; final site planned |
| H1 | Quarry | +3050, +3150 | Planned |
| H2 | Abandoned city | +3500, +2100 | Planned |
| B3 | Village | −2700, +1450 | Primitive houses in test zone; migration planned |
| D3 | Town | −700, +1500 | Planned |
| E3 | Clinic | +550, +1500 | Primitive clinic in test zone |
| F4 | Gas station | +1900, +850 | Primitive fuel stop in test zone |
| H4 | Military checkpoint | +3700, +750 | Primitive checkpoint in test zone |
| B4 | Garages | −2100, +300 | Primitive garages in test zone |
| D4 | Factory | −350, +100 | Primitive factory in test zone |
| G5 | Warehouse | +2350, −250 | Primitive warehouse in test zone |
| D6 | Construction site | −250, −1350 | Planned |
| B7 | Dam | −2250, −2450 | Planned |
| D7 | Big lake | −250, −2900 | Water basin planned |
| H7 | Airfield | +3050, −2850 | Planned |

Grid positions in the table are approximate, mapped to nearest 1 km zone. Runtime coordinates in `SectorMapPlan.cs` are authoritative.

## Roads and hydrology

The concept includes these major connective corridors:
1. **West–east road:** village → town → clinic → fuel station → military checkpoint.
2. **Industrial corridor:** garages → factory → warehouse.
3. **North–south road:** forest camp → town → industrial zone → construction site.
4. **East coast / airfield link:** warehouse → southern airfield.
5. **Western access:** village → garages → dam.

Additional planned geography:
- Large southern lake/river drainage and a dam.
- Small northwest mountain lake.
- A meandering north-to-south river channel and two to three crossings.
- Uplands/cliffs along the western and northern borders.
- Dense forests and clearings, with the most heavily developed play space in the central 3×3 km.

## UI navigation rules

- The **minimap** represents the player's immediate, actually-loaded surroundings.
- **M** opens the full 8×8 km planning/tactical map. It must visibly distinguish **planned sites** from **built sites**.
- The **azimuth compass** uses degrees from +Z (north), +X (east) and should only promise an in-world POI when that location exists at the shown coordinate.
- POI labels and markers must not imply a planned building is already physically present.

## Roadmap ordered by impact

1. **Canonical blueprint and status tracking** — approved, versioned code + documentation.
2. **Full-world tactical atlas** showing the blueprint, correct player coordinates and build states — first version added in `SectorWorldAtlas.cs`; Unity Editor/URP verification pending.
3. **Terrain and watersheds:** carve southern basin, mountain lake, river and dam corridor; stream water with terrain.
4. **Roads and crossings** with terrain-conforming geometry, navigation and settlement links.
5. **Place real structures at final blueprint POI coordinates**; retire the temporary spawn-zone equivalents selectively.
6. **Detail one playable district at a time**, beginning with the central village/industrial corridor; add foliage, props, loot and zombie density.
7. Replace primitive house geometry with modular art and add interiors.
8. Establish world-state persistence / streaming-safe static world objects.
9. Validate navigation, performance, render order, and UI in Unity Editor/URP.
10. Multiplayer/co-op after the solo world/gameplay loop is dependable.

**Working branch:** `prototype/world-8km` — no release until Unity playtesting passes.
