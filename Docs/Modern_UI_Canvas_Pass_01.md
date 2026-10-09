# Dead Sector / Tactical Canvas UI — pass 0.1

**Status:** implemented in `prototype/world-8km`; **not yet compiled or play-tested in the user's Unity Editor**. Do not merge or release until testing. Uses the installed `com.unity.ugui` package, explicitly referenced by `DeadSector.Runtime.asmdef`.

## Visual direction

Original tactical survival design inspired by the legibility of STALKER/DayZ and the equipment density of Tarkov, without copying their assets. Dark charcoal translucent panels, muted olive accents, white text, restrained status colors. Runtime-built screen-space Canvas with a 1600×900 reference resolution and CanvasScaler to adapt to windows.

Implemented:
- Compact health/stamina/hunger/thirst readout, current day/time and F5/F9 reminder.
- Three visible weapon slots with active-slot indicator and real `SectorGameplay` equipment names.
- Contextual `E` prompt prioritizing the same loot/resource/door interactions as gameplay, plus toast messages.
- Centered inventory with 30 pooled visual cells for backpack stacks, weight and slot capacity.
- Equipped head/chest/legs/feet/backpack slots, clickable remove, and a minimal vector silhouette.
- Item inspection, working **USE** and **EQUIP** actions through shared `SectorGameplay` methods.
- **Drag armor from an inventory cell to the matching equipment slot**; mismatched item/slot combinations are ignored.
- Crafting tab with vertically scrollable recipes, live requirement counts and buttons enabled only when the exact gameplay crafting rules allow crafting.
- Dedicated input EventSystem if none exists, compatible with Unity Input System and legacy input.
- Legacy fallback: `SectorBootstrap.useModernUi = false` in Inspector restores the old IMGUI HUD and inventory/crafting windows.
- Existing compass, minimap, journal, crosshair, world atlas and horde banners remain on their existing systems for this pass. No FBX, prefab, scene, or save format migrations.

Source locations:
- `Assets/DeadSector/Runtime/SectorModernUI.cs` (+ `.meta`)
- `Assets/DeadSector/Runtime/SectorBagDragSource.cs` (+ `.meta`)
- `Assets/DeadSector/Runtime/SectorGearDropTarget.cs` (+ `.meta`)
- `Assets/DeadSector/Runtime/SectorGameplay.cs` (UI-facing nonduplicative APIs)
- `Assets/DeadSector/Runtime/SectorBootstrap.cs` (bootstrap and fallback)
- `Assets/DeadSector/Runtime/DeadSector.Runtime.asmdef` (Unity.ugui reference)
- `Assets/DeadSector/Tests/Editor/SectorModernUiFacadeTests.cs`

## Unity verification required before closing this task

1. Open `Assets/DeadSector/Scenes/DeadSectorPrototype.unity`, wait for compilation and ensure zero red errors.
2. Press Play: no old top-left grey debug HUD, new four compact meters and three hotbar boxes. Minimap and compass remain visible.
3. Press I: full inventory appears and mouse cursor unlocks. Select water, click USE, verify one unit consumed and thirst restores. Armor EQUIP/REMOVE must update both slot labels and appearance.
4. Craft inventory resources using C: requirements update after each successful craft, no materials are lost on a disabled/failed recipe. Scroll wheel moves recipe list.
5. Drag suitable armor item from the grid to its matching head/chest/leg/feet/backpack slot. Wrong slot must reject; valid slot must equip.
6. Press Close / I / C; movement and mouse look resume. Open inventory while stamina is depleted; verify player does not punch when clicking UI.
7. Resize Game view through 16:9, 16:10 and smaller windows. Check UI text clipping, overlay with compass/minimap/journal.
8. Save F5, load F9, re-open inventory: equipment, stack quantities, panel buttons remain valid.
9. If the Canvas fails, disable `useModernUi` on the `SectorBootstrap` component and report Unity Console stack trace.

## Next passes (not implemented yet)

- Art-directed icons and item thumbnails, better vector humanoid with actual equipment renders.
- Split / move / sort stacks; drag equipment between weapon slots as well as armor; quick-transfer from loot crates.
- Modernize the compass, minimap, world atlas, journal and horde alert into one cohesive UGUI/UITK experience.
- UI sound, focus/hover states, animations, resolution/ultrawide support, accessible font scaling.
- Play Mode tests for click/drag/scroll and prefab/scene integration, GPU/UI performance measurement.

**Do not treat static syntax/source checks as proof of gameplay.**
