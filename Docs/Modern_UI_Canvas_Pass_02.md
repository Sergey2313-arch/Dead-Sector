# Dead Sector — Tactical UI pass 0.2: minimap, compass, item icons

**Status:** committed to `prototype/world-8km`; **pending Unity compilation and Play Mode QA**. No changes to the user's local scene or Mixamo assets, no release.

## What changed

### North-up local minimap
- `SectorTacticalMapRaster.cs` computes an unlit 128×128 schematic from the deterministic ground heights, road routes and water locations; this is **not** a dark camera photograph of the level. It stays readable at night and is refreshed only after player movement with a cooldown.
- `SectorModernUI.cs` displays the raster in a 214×214 tactical HUD panel in the top-right corner. The player remains centered, facing direction rotates the visible arrow, map North stays upward.
- Named POI location pins appear when inside the local window. X/Z coordinates and a `[M] ATLAS` reminder are shown.
- `SectorMinimap.SetCanvasHudActive(true)` disables the legacy minimap camera and IMGUI border while the new Canvas exists; the old `M` world atlas remains active and is visible without Canvas overlays.
- Opening inventory/crafting while the world atlas is visible automatically closes the atlas; prevents an invisible cursor-locked UI.

### Canvas compass
- Direction in degrees, north-up headings and moving cardinal notches every 5°, plus visible nearby POI markers with rough distance.
- Legacy `SectorCompass` OnGUI disabled while the new Canvas is active, restored if the component is destroyed. Existing gameplay code used for `Bearing`, `RelativeAngle` and cardinal naming.

### Inventory thumbnails
- `SectorItemIcons.cs` generates hand-authored vector-like pixel silhouettes for all existing gameplay item classes: water bottle, tin food, bandage, medkit, two kinds of ammo, pistol, rifle, knives, axe, spear, torch, club/fists, different armor pieces, backpack, stones, scrap, wood, sticks, vegetation, cotton, cloth and cord.
- These are item-specific graphical sprites, not colored RPG rarity tiers. The sprites use an internal cache and are disposed of on UI teardown; no external font/icon/asset download is required.
- Thumbnails in backpack cells, equipment slots, weapon hotbar and selected-item details; compact item counts and names.

## Changed files
- `Assets/DeadSector/Runtime/SectorTacticalMapRaster.cs` and `.meta` (new)
- `Assets/DeadSector/Runtime/SectorItemIcons.cs` and `.meta` (new)
- `Assets/DeadSector/Runtime/SectorModernUI.cs`
- `Assets/DeadSector/Runtime/SectorMinimap.cs`
- `Assets/DeadSector/Runtime/SectorBootstrap.cs`
- `Assets/DeadSector/Tests/Editor/SectorTacticalUiPass02Tests.cs` and `.meta` (new)

## Required local Unity QA

1. With Play stopped, preserve local changed Unity scenes, FBX files and `.meta`. Update the code from the branch safely.
2. **Compilation**: Zero red Console errors. Run EditMode test suite `SectorTacticalUiPass02Tests` in Test Runner (actual Test Runner execution is still pending).
3. **Day/night**: Map remains legible with the sun down; roads and water use distinct colors independent of lighting.
4. **Movement**: Face North/East/South/West; map remains North-up and player arrow rotates properly. Walk far enough for map texture to update. Nearby POI markers should move smoothly relative to player.
5. **Compass**: Heading rotates and labels wrap correctly at 359° → 000°. POI names and distances should not crowd the center too much.
6. **World atlas**: `M` opens the full 8×8 km plan; new Canvas hides temporarily. `M` closes atlas and Canvas returns. If `I` opens while Atlas is visible, it should not strand the player in an invisible menu.
7. **Inventory**: `I` opens 30 visual cells. Item icons display for every existing item ID, selecting a stack updates the large preview, using/equipping retains correct inventory quantities.
8. **Crafting**: `C` still shows requirements, crafting buttons and scrolling; no new regression in controls.
9. **Resolutions**: Test 1920×1080, 1600×900, 1280×720 and 16:10. Ensure minimap not clipped, compass not hidden, inventory scroll works and POI text fits.
10. **FPS profiling**: Walk across tile edges, open and close menus, verify 128×128 terrain raster redraw does not cause unacceptable frame spikes.
11. **Fallback**: Disable `SectorBootstrap.useModernUi` in Inspector if any new UI compile/runtime blocker is detected, then report exact Console error.

**Caveat:** static source/brace and presence checks completed; Unity compiler and Play Mode were not available in this session. These visuals are procedural placeholder art and should be reviewed in Game view before final art polish.

## Next design pass

- Real atlas panel and map key in Canvas (the M atlas still uses IMGUI).
- Icon styling polish with selected-state animations, tooltips, quick transfer to containers and stack splitting.
- UI sounds, menu hover/focus animations and adaptive layout for ultrawide.
- Unified modern journal, horde messages and main menu.
