# Dead Sector — prototype plan

## Confirmed scope

- Unity 6000.6.4f1, URP 17.7, Input System installed; existing project is based on the URP sample template.
- Playable world: 8 x 8 kilometres (64 square kilometres).
- Movement, sprint, jump, collisions, visible run/jump animation.
- First and third person cameras, selectable during play.
- Zombie AI prototype: wander, visibility detection, pursuit and contact damage.

## Implemented prototype

`Assets/DeadSector/Scenes/DeadSectorPrototype.unity` starts a deterministic runtime generator.
The 64 potential terrain tiles are 1,000 metres each. Only a 3 x 3 neighbourhood is instantiated.
Tiles use shared world-space sampling and Terrain neighbours; old TerrainData is disposed after leaving a tile.
Terrain generation runs on the main thread, one tile per frame; short streaming hitches are possible.

The first valley contains 12 enterable house shells, factory, warehouse, cargo containers,
checkpoint, streetlight geometry, and a jump obstacle course. Forest objects are primitive placeholders.
This is a sparse blockout of the full map, not a finished 64 km² art environment.

The player has a CharacterController, buffered jump and coyote time, ceiling collision handling,
gravity, a camera obstruction sphere cast, health, and respawn.
An articulated mannequin animates its arms/legs for walking, running and jumping using code.
It does not yet contain a skinned human model, Animator clips or Mixamo assets.

Twelve zombie mannequins use a runtime NavMesh in the 900 x 900 metre starting area.
They check line of sight, remember the last seen position for six seconds, follow NavMesh paths,
deal 10 damage per 1.2 seconds at close range, and suspend behaviour when the player is far away.
Navigation across the entire streamed world, combat weapons, loot and multiplayer are future work.

## How to run

1. Open `Assets/DeadSector/Scenes/DeadSectorPrototype.unity` (or menu **Dead Sector > Open playable prototype**).
2. Press Play. Wait for terrain generation and the separate AI navigation build.
3. WASD: move. Left Shift: run. Space: jump. V: change camera. Escape: unlock cursor.
   Left click: capture cursor. R: restore health and respawn.
4. In third person check moving limbs and the airborne pose. Use obstacles near `(20, 60, 38)`.
5. Approach a zombie within 45 m with no wall in between; check pursuit around a house and contact damage.
6. Move across x=0/z=0 and subsequent kilometre boundaries; terrain neighbourhood updates while world coordinates remain stable.

To create a permanent editable terrain scene, use **Dead Sector > Bake editable 8 x 8 km map**.
This saves TerrainData, textures, materials and a scene to a new unique folder under `Assets/DeadSector/Generated`.
It contains all 64 tiles, so editor memory cost is higher. This editing scene has no player or AI;
use the separate prototype scene to play. Baking does not overwrite existing generated maps.

## Validation and next steps

No Unity editor or C# compiler is available in the authoring environment. Engine compilation,
scene import, visual animation, collision, navigation and frame-rate checks require Unity on the user's machine.
EditMode layout tests are included; run via Window > General > Test Runner > EditMode.
Send any first Console error before changing packages or the old player script.

Next: validate the prototype in Unity, replace mannequins with chosen human/zombie assets and
Animator clips, improve terrain art, extend navigation and implement cooperative networking.
Keep further design decisions and validation results in this file.
