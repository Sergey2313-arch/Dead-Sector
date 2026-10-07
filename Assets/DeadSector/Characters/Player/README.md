# Dead Sector player animation setup

## Current prototype mannequin

Use:

**Dead Sector -> Character -> 01 - Build Animated Mannequins**

This creates:

- Player_Mannequin_Broad.prefab
- Player_Mannequin_Slim.prefab

Both have a full visible body, hands, legs, CharacterController, gameplay Camera,
PlayerMovement and prototype animation support.

## Mixamo full-body player

Put Mixamo FBX files here:

Assets/DeadSector/Characters/Player/Mixamo/

Current supported names:

- Idle.fbx
- Swagger Walk.fbx
- Running.fbx
- Falling To Roll.fbx
- Jumping.fbx
- Running Jump.fbx
- Jumping Down.fbx
- Falling To Landing.fbx
- Hard Landing.fbx
- Boxing.fbx
- Punching.fbx
- Outward Block.fbx
- Crouched To Standing.fbx

Then use:

**Dead Sector -> Character -> Mixamo -> 00 - BUILD EVERYTHING**

The tool will:

1. configure every FBX as Humanoid
2. bake root movement so CharacterController remains in charge
3. create AC_Player_Mixamo.controller
4. create an upper-body animation mask
5. wire Idle / Walk / Run
6. wire hard-landing roll
7. wire Boxing / Punch / Block on the upper body
8. build Player_Mixamo_FullBody.prefab when a skinned humanoid model is present

Prototype controls:

- WASD = move
- Left Shift = run
- Space = jump
- Mouse = look
- F = toggle boxing/combat stance
- Left Mouse = punch
- Right Mouse = block

## Important: character model

Animation-only FBX files are not enough to display a character.

Download one Mixamo character/model with:

- Format: FBX for Unity
- Skin: With Skin
- FPS: 30

The currently supplied X Bot file is motion-only / Without Skin, so the setup
tool can use the animations but cannot build a visible X Bot player from that file.

## Still wanted for the final locomotion set

- normal Walking
- Sprinting
- Standing To Crouched
- Crouch Idle
- Crouch Walk
- Strafe Left
- Strafe Right

Swagger Walk is currently used as the temporary Walk animation until a neutral
walking clip is added.


## Current jump mapping

- Jumping.fbx -> normal jump
- Running Jump.fbx -> jump while running
- Jumping Down.fbx -> falling state
- Falling To Landing.fbx -> normal landing
- Hard Landing.fbx -> hard landing
- Falling To Roll.fbx -> very hard landing / roll

Landing severity is selected from vertical speed:

- normal landing: above -7.5 m/s
- hard landing: -7.5 m/s or faster downward
- roll landing: -11.5 m/s or faster downward
