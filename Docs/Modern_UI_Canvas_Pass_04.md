# Dead Sector — UI pass 0.4: field journal and backpack stack operations

**Working branch:** `prototype/world-8km`.
**Status:** implementation committed; Unity Editor compilation and Play Mode testing required before accepting the release. Main branch, scene files, imported Mixamo models and user-specific Unity `.meta` remain untouched.

## Functional tactical journal

- Replaces the old rectangular `SectorJournal.OnGUI` panel when modern Canvas is enabled. All journal data continues to come from `SectorJournal` and `SectorJournalSnapshot`; save schema does not change.
- `J` opens a full-size tactical Canvas page with eight actual in-game objectives: visited village, clinic, factory, radio station; supplies gathered, harvested resources, crafted gear and infected kills.
- Counters reflect exact saved journal progression and the completed count. Player coordinates and current grid cell use the same map plan as the full atlas.
- Journal page integrates with Inventory/Crafting tabs and displays progress without adding new gameplay conditions or inventing quests.
- `Esc` closes the journal first, releases cursor/UI blocking and restores movement. A separate `CloseJournal` outcome was added to `SectorMenuRules`; old callers preserve compatibility via optional argument.
- `I/C` from journal transitions to corresponding page, and one-frame shortcut suppression prevents double-toggles caused by Unity script execution order. The main pause screen always has focus over gameplay panels.

## Backpack sort and split

- `SectorInventory.SortStacks()` orders item stacks by existing gameplay category, then item label and ID. It **only reorders**, never discards/merges or modifies item amounts/weight.
- `SectorInventory.SplitStack(index,count)` splits a particular existing stack, not the aggregated count for the item ID; requires free bag slot, legal amount `0 < count < stack.count` and stackable item.
- Canvas Inventory now has **SORT BACKPACK** and **SPLIT STACK IN HALF** buttons. Splitting is disabled for non-stackable items or when the bag has no free slots.
- The item inspector shows both the selected stack count and total count of that item ID.
- `SectorInventory.Import` now preserves the split stacks in saves, while rejecting nonexistent items, exceeding slot capacity, overweight contents and malformed stack counts. No save-file version bump.
- UI selection is tracked by stack index so that selecting one of two stacks with the same item ID remains meaningful.

## Code changes

- `Assets/DeadSector/Runtime/SectorModernUI.cs`: journal page, live mission readouts, tabs and new bag actions.
- `Assets/DeadSector/Runtime/SectorJournal.cs`: modern UI visibility and progress accessors, legacy fallback.
- `Assets/DeadSector/Runtime/SectorInventory.cs`: lossless sort/split and split-safe import.
- `Assets/DeadSector/Runtime/SectorGameplay.cs`: action facade, journal modal/input-state integration and one-frame key guard.
- `Assets/DeadSector/Runtime/SectorMenuRules.cs` and `SectorMenuUI.cs`: Escape precedence.
- `Assets/DeadSector/Tests/Editor/SectorInventoryStackOperationsTests.cs` (+ `.meta`): tests for split, sorting, persistence, import limits and journal Escape.

## QA checklist (user's Unity Editor required)

1. **Console and tests:** no red compile errors; run `SectorInventoryStackOperationsTests`, `SectorMainMenuTests` and previous Canvas tests in Unity Test Runner.
2. **Journal:** after Continue, press `J`. Old IMGUI journal must not overlap. Eight objectives show correct progression. Walk to a POI and collect loot; check updates. `J` closes journal and restores pointer lock.
3. **Esc and tabs:** with journal open press `Esc`, then open it and click `I`, `C` or corresponding tabs. No stranded modal/blocked player, no double-toggle. `Esc` while menu settings is open should return home first.
4. **Sort:** fill bag with different kinds of resources; sort. All quantities and total KG stay unchanged; only ordering changes.
5. **Split:** with a stack of >=2 and a free slot, click item then SPLIT STACK IN HALF; selected and new stack appear separately. Try full bag and non-stackable armor; split button disabled.
6. **F5/F9 persistence:** split at least one stack, save and load. Verify the two separate counts survive; inventory weight, equipped armor and journal data remain correct.
7. **Craft/consume after splitting:** using or crafting from items with multiple stacks should still consume correct totals and maintain no-negative inventory.
8. **Resolution check:** 1920×1080, 1600×900, 1280×720 and 16:10. No clipped journal row, tabs, or sorting controls.
9. **Fallback:** disable `SectorBootstrap.useModernUi` in inspector and confirm old journal/gear displays remain available for comparison.

**Caveat:** GitHub source and structural checks cannot prove Unity compilation or interaction at runtime. No finished Play Mode QA has been claimed. If any Console error appears, capture the exact error line before further merging.

## Next priorities
- Safe save-profile selection (preserve and migrate old single-slot save; no destructive New Game).
- Journal POI map markers and waypoint selection, without fake mission teleports.
- Inventory stack transfer/merge, drag-and-drop between slots, tooltip preview, keyboard/controller navigation.
- Unified Canvas atlas, death screen and UI sounds after QA.
