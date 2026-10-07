# Dead Sector terrain textures

Use the Unity menu:

1. **Dead Sector -> World -> 01 - Create Terrain Texture Folders**
2. Put one PBR texture set into each folder:
   - Textures/Grass
   - Textures/Dirt
   - Textures/Mud
   - Textures/Rock
3. For each set keep at least:
   - diffuse / basecolor / albedo
   - nor_gl / normal
4. Run **Dead Sector -> World -> 02 - Build Terrain Layers**

Recommended prototype sources (Poly Haven, CC0):
- Sparse Grass
- Dirt
- Dry Mud Field 001
- Rock Ground

The editor utility scans filenames automatically, configures normal maps and creates:
- TL_Grass.asset
- TL_Dirt.asset
- TL_Mud.asset
- TL_Rock.asset

For the prototype use 2K textures. Mask maps and macro-variation will be added later.
