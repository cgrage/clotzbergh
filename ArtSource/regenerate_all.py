"""
Regenerates the FBX asset for every klotz model .blend file in this directory.

Convention: each .blend file contains exactly one top-level mesh object, and
its name matches the .blend file's own base name (e.g. Door1x4.blend contains
an object called "Door1x4"). Each is exported to Assets/Models/<name>.fbx.

Usage (from anywhere):
    blender --background --factory-startup --python ArtSource/regenerate_all.py

No arguments needed - it just finds and re-exports every *.blend file next to
this script.
"""

import bpy
import glob
import os

art_source_dir = os.path.dirname(os.path.abspath(__file__))
repo_root = os.path.dirname(art_source_dir)
models_dir = os.path.join(repo_root, "Assets", "Models")

blend_paths = sorted(glob.glob(os.path.join(art_source_dir, "*.blend")))

if not blend_paths:
    print(f"No .blend files found in {art_source_dir}")

for blend_path in blend_paths:
    name = os.path.splitext(os.path.basename(blend_path))[0]
    fbx_path = os.path.join(models_dir, f"{name}.fbx")

    bpy.ops.wm.open_mainfile(filepath=blend_path)

    if name not in bpy.data.objects:
        print(f"SKIPPED {name}: expected an object named '{name}' in {blend_path}, "
              f"found: {[o.name for o in bpy.data.objects]}")
        continue

    bpy.ops.object.select_all(action='DESELECT')
    bpy.data.objects[name].select_set(True)
    bpy.context.view_layer.objects.active = bpy.data.objects[name]

    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_NONE',
        axis_forward='Z',
        axis_up='Y',
        bake_space_transform=True,
        object_types={'MESH'},
        use_mesh_modifiers=True,
        mesh_smooth_type='FACE',
        add_leaf_bones=False,
    )

    print(f"Exported '{name}' -> {fbx_path}")
