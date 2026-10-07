# Dead Sector animated mannequins

Use Unity menu:

**Dead Sector -> Character -> 01 - Build Animated Mannequins**

This creates two playable prototype prefabs:

- Player_Mannequin_Broad.prefab
- Player_Mannequin_Slim.prefab

Both include:

- full visible body for first person
- arms and hands
- legs and feet
- CharacterController
- gameplay Camera
- PlayerMovement
- Animator
- Idle animation
- Walk animation
- Run animation
- Jump animation
- left/right hand sockets for weapons

Controls:

- WASD: move
- Left Shift: run
- Space: jump
- Mouse: look

The generated mannequins are intentionally simple segmented prototype characters.
They can later be replaced with a Mixamo humanoid character while keeping the
same gameplay/controller architecture.

Optional Mixamo files can be kept in:

Assets/DeadSector/Characters/Player/Mixamo/
