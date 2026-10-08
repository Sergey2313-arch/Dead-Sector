# Dead Sector — Zombie ragdoll integration

**Branch**: `prototype/world-8km`  
**State**: source code committed; compile, Unity Test Runner and physics behavior **not verified in Unity Editor**. This is not a release.

## Implementation

- `SectorZombieRagdoll.cs` adds a physics-driven corpse, created **on the fatal hit**, not on all living zombies.
- For a valid X Bot Humanoid, uses 11 body segments (hips, chest, head, upper/lower arms and upper/lower legs) with Rigidbody, capsule/sphere colliders and CharacterJoint limits.
- Each collider scales with its local bone segment; model's imported FBX scale remains preserved.
- For an unloaded/missing Mixamo rig, builds a 5–6 body ragdoll from `SectorMannequin`: torso, two arms, two legs, and head if present.
- Disables the active NavMeshAgent, the original root CapsuleCollider and the Animator/procedural rig on death before the bones take control.
- Imparts pre-death velocity and a push along the incoming attack direction at the chest.
- Bone-to-bone self-collisions are suppressed to reduce physics explosions; collisions with the world remain.
- Pauses dynamic rigidbody simulation after 8 seconds to reduce horde cost; corpse is destroyed after 25 seconds, like the previous temporary death implementation.
- Repeated fatal damage does not spawn additional rigidbodies or joints.
- If a required Humanoid bone is missing, the previous simple procedural death-fall remains as a safe fallback.
- Existing zombies retain their previous health/stats, 3 archetypes and nightly horde spawn rules.

## Unity Play Mode QA

1. Pull `prototype/world-8km`, open `DeadSectorPrototype.unity` in Unity 6000.6.4f1, and resolve Console errors first.
2. Run EditMode tests, especially `SectorZombieRagdollTests`. One test verifies an invalid rig cannot create bodies; another checks the procedural mannequin's 6 bodies and duplicate-death guard.
3. Press Play, wait for AI/navigation readiness.
4. Choose `Dead Sector > Debug > Test Nearest Zombie Ragdoll` from Unity's top menu to kill a living zombie without crafting weapons.
5. Verify that the fallen zombie has no active NavMeshAgent / AI attack and limb colliders collide with the terrain.
6. Open Scene view and inspect for stretched joints, mis-scaled capsules or bodies shooting away unexpectedly.
7. Repeat after `Dead Sector > Debug > Prepare Night Horde`, including Runners/Brutes. Check frame rate with several corpses.
8. Test with imported X Bot zombie prefab and without it (mannequin fallback).
9. Check bullet-vs-melee impact direction and save/respawn behavior.
10. Tune impulse (default 16), inherited momentum (.65), simulated settle time (8 seconds) from the `SectorZombieRagdoll` component if desired.

**Limitations:** ragdolls are generated in code, not pre-baked ragdoll-prefabs. Corpse locations are not persisted to disk; the corpse cleanup timer remains 25 seconds. Player death/respawn still uses the existing player mechanics, **not** an additional player ragdoll. Network replication is not implemented.
