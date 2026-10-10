# Dead Sector — vegetation library

This directory is the target for the 13 downloaded 2K Poly Haven Blender ZIP asset sets.

| Category | Original download names |
|---|---|
| Trees | fir_tree_01_2k |
| Saplings | pine_sapling_small_2k, pine_sapling_medium_2k |
| Shrubs | shrub_03_2k, shrub_sorrel_01_2k |
| Grass | grass_bermuda_01_2k, grass_medium_01_2k, grass_medium_02_2k |
| Plants | weed_plant_02_2k, nettle_plant_2k, dandelion_01_2k |
| Rocks | rock_moss_set_02_2k |
| Stumps | tree_stump_01_2k |

## Import workflow

1. Unzip each source asset outside the Unity project.
2. In Blender, check scale, orientation, and textures; export as FBX (or glTF if the relevant Unity importer is installed). Avoid placing full Blender archives directly into Assets.
3. In Unity, run **Dead Sector > World > 03 - Create Vegetation Folders**.
4. Copy each exported model and matching textures into the appropriate category folder.
5. Run **Dead Sector > World > 04 - Configure Vegetation Models**.
6. Create materials using the project's actual render pipeline; set alpha clipping for foliage if needed and assign textures manually.
7. Create prefabs, set reasonable LOD groups and colliders, and test performance on the 500 m x 500 m prototype before populating the 8 km x 8 km world.

**Status:** 13 ZIP downloads are recorded in the user's ChatGPT library; the original binary models/textures have **not** been committed to this repository. This commit only adds import infrastructure. Keep large binaries in local Assets with Git LFS or a dedicated asset storage workflow; check licensing/attribution even for CC0 models.
