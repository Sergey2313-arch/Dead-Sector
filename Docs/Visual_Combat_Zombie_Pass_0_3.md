# Dead Sector — visual gameplay pass 0.3

**Branch:** `prototype/world-8km`  
**Implementation status:** scripts committed; Unity 6000.6.4f1 compile, tests and Play Mode visuals **not yet validated**. No release.

## Zombie variety

Three archetypes share the existing zombie AI, Mixamo FBX or fallback procedural rig; `SectorZombieProfiles.cs` carries editable stat defaults.

| Kind | HP | Chase speed (m/s) | Attack damage | Attack interval |
|---|---:|---:|---:|---:|
| Shambler | 100 | 2.8 | 10 | 1.2s |
| Runner | 70 | 4.7 | 8 | 0.84s |
| Brute | 210 | 2.2 | 22 | 1.75s |

- First night remains ordinary infected only.
- Runners join the horde from **Day 2** (approximately every fourth eligible member).
- Brutes join from **Day 4** (approximately every ninth eligible member).
- Ordinary ambient spawns include occasional runners.
- `SectorZombieAppearance` adds visual class tint, chest/shoulder details, scaled models and red hit flash without replacing Mixamo mesh assets.
- `SectorZombieReaction` adds a small stagger on hit and a temporary death fall when Animator stops. Not a physically simulated ragdoll.
- Capsule / NavMesh agent dimensions scale with the silhouette. Live zombie count is refreshed instead of counting destroyed zombies as alive.
- Gunshots notify nearby infected to investigate (rifle ~145 m, pistol ~95 m); sound/audio playback assets are not yet imported.
- Horde size and cap remain governed by `SectorHordeDirector`. Server-authoritative hordes are **not** implemented.

## More detailed temporary equipment models

`SectorEquipmentVisuals` now assembles separate mesh pieces using primitives:

- Rifles and pistols: receiver, grip, barrel, magazine, front/rear sights, rifle stock and handguard; quick temporary muzzle-flash marker.
- Stone/steel knives: wooden grip, binding, flat cutting edge and angled tip.
- Stone/metal axes: wooden haft, heavy axehead and grip binding.
- Wooden club: solid club handle, thicker knot and wrap.
- Stone spear: long shaft, stone tip and binding.
- Torch: wrapped upper end and **functional point light when held** (no fuel/burn time yet).
- Backpack: canvas body, flap, rear pocket, two straps and buckle; only shown when equipped.
- `SectorArmorVisuals` now creates multi-part wooden armor and cotton garments on the relevant Humanoid bones instead of single primitive boxes.

These are better-shaped **blockouts**, not realistic textured production assets; correct scale/retarget pose should be checked in FPP and TPP before importing final models.

## Responsiveness and HUD

- LMB weak / RMB strong melee remain **instant hit resolution** on press. No held mouse and no damage wind-up.
- Punches use short alternating jabs; equipped melee weapons use a different, wider arm-swing trajectory.
- Gunshots show short muzzle flash, small camera recoil; non-gun attacks have subtler immediate recoil.
- `SectorCombatFeedback` draws a compact center reticle, attack spread and fading damage/hit feedback.
- Weapon hotbar now displays **three separate slots** (primary, sidearm, melee) and ammunition count rather than a clipped single text row.
- The always-visible character HP/stamina/hunger/thirst panel remains separate.
- The large prototype diagnostics panel is **hidden by default**. Toggle with **F3** to avoid cluttering the combat HUD.
- Weather text moved below the survivor panel.

## Focused Unity checks for tonight

1. Pull the branch; resolve Console errors; run `SectorZombieProfilesTests` and existing combat, inventory, horde and world tests.
2. Spawn all three infected types. Confirm Mixamo scale is preserved, colliders align, runner is faster and brute has higher HP.
3. Hit each infected with bare fists, crafted knife, axe, and gun. Damage must resolve immediately and display a hit marker + red flash.
4. Confirm falling dead infected no longer attack and expire as planned.
5. Equip/stow crafted weapons and wear wooden/cotton gear. Verify visuals do not detach in walking, running, jumping or FPP/TPP.
6. Fire a weapon, confirm muzzle flash and temporary aim recoil; check ammo consumption.
7. Equip torch at dusk; confirm point light follows hand and vanishes when torch is stowed.
8. Check 3-slot hotbar; F3 toggles debug panel; weather and survivor stats do not overlap.
9. Advance to Day 4 with the horde debug menu and verify mixed-wave spawning and cap. Verify that AI pathfinding handles loaded local terrain.
10. Profile frame time / draw calls before larger zombie groups or art assets.

### Remaining next milestones

- Real textured skinned character/armor/clothing and weapon models, Mixamo-compatible attack animation clips, ragdoll death, sounds and impact particles.
- Loot visuals, weapon magazines/reloads/projectiles; limb-hit detection and improved line-of-sight filtering.
- Physics/world interactions and extended persistence for damage/doors/zombie remains.
- Player-authoritative single-player stabilization before installing network packages for 2-player cooperative.

No changes to the stable/main branch or releases are included in this pass.
