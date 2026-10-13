# Blender-only script. Called by Convert-DeadSectorBlend.ps1.
# Does not save or modify the source .blend file.
import bpy
import os
import sys

def main():
    argv = sys.argv
    if "--" not in argv:
        raise RuntimeError("Usage: blender -b source.blend --python Export-DeadSectorBlends.py -- OUTPUT.fbx MAX_TRIS")
    args = argv[argv.index("--") + 1:]
    if len(args) != 2:
        raise RuntimeError("Expected OUTPUT.fbx and MAX_TRIS")
    destination = os.path.abspath(args[0])
    max_triangles = int(args[1])
    if max_triangles < 1:
        raise RuntimeError("MAX_TRIS must be positive")
    if os.path.exists(destination):
        raise RuntimeError("Output exists. Refusing to overwrite: " + destination)
    objects = [o for o in bpy.context.scene.objects if o.type == "MESH" and not o.hide_render]
    if not objects:
        raise RuntimeError("No render-visible mesh objects found in the opened .blend")

    triangle_count = 0
    for obj in objects:
        mesh = obj.data
        triangle_count += sum(max(0, len(poly.vertices) - 2) for poly in mesh.polygons)
    print("DEAD_SECTOR_MESHES:", len(objects))
    print("DEAD_SECTOR_TRIANGLES:", triangle_count)
    if triangle_count > max_triangles:
        raise RuntimeError(
            "Too many triangles for the pilot export: %d > %d. "
            "Use Blender to select a lower LOD or optimize first."
            % (triangle_count, max_triangles)
        )
    for obj in bpy.context.view_layer.objects:
        obj.select_set(False)
    for obj in objects:
        obj.hide_set(False)
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    os.makedirs(os.path.dirname(destination), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=destination,
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        add_leaf_bones=False,
        path_mode="AUTO",
    )
    if not os.path.isfile(destination) or os.path.getsize(destination) == 0:
        raise RuntimeError("FBX exporter did not create a nonempty file")
    print("DEAD_SECTOR_EXPORTED:", destination)

if __name__ == "__main__":
    main()
