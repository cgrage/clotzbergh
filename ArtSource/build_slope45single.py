"""
Builds one Slope45SingleNxM.blend from scratch via bmesh - a lean-to/pitched-roof
wedge (part 4445-style: studs on one half of the top, a slope with a small plinth
on the other), parametrized by length so it can produce every Slope45Single2xN size.

Usage (from anywhere), once per length needed:
    blender --background --factory-startup --python ArtSource/build_slope45single.py -- ArtSource/Slope45Single2x8.blend Slope45Single2x8 8

Re-run ArtSource/regenerate_all.py afterwards to re-export the FBX(es).
"""

import bpy
import bmesh
import sys
import math

argv = sys.argv[sys.argv.index("--") + 1:]
OUT_PATH = argv[0]
NAME = argv[1]
LENGTH_CELLS = int(argv[2])

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

# Blender axes -> Unity axes (matches ArtSource/regenerate_all.py's axis_forward='Z', axis_up='Y'):
#   Blender X -> Unity X (length)
#   Blender Z -> Unity Y (height)
#   Blender Y -> Unity Z (depth)
CELL_X = 0.36    # WorldDef.SubKlotzSize.x
CELL_Z = 0.144   # WorldDef.SubKlotzSize.y (height axis, Blender Z)
CELL_Y = 0.36    # WorldDef.SubKlotzSize.z (depth axis, Blender Y)

HEIGHT_CELLS = 3
DEPTH_CELLS = 2

LENGTH = LENGTH_CELLS * CELL_X
HEIGHT = HEIGHT_CELLS * CELL_Z   # 0.432
DEPTH = DEPTH_CELLS * CELL_Y     # 0.72

# Lean-to / pitched-roof profile, constant along the full length (X):
# - y: 0..CELL_Y  -> flat lane at full HEIGHT, studs on top
# - y: CELL_Y..DEPTH -> the slope, from the ridge down to the top of the plinth
# A short vertical plinth (half a stud tall) runs along the outer bottom edge,
# flush with y=DEPTH - just a straight wall, no separate tread.
FLAT_Y = CELL_Y
PLINTH_HEIGHT = 0.0765 / 2  # half of PlasteShader.shader's procedural stud height

bm = bmesh.new()
verts = {}


def v(x, y, z):
    key = (round(x, 6), round(y, 6), round(z, 6))
    if key not in verts:
        verts[key] = bm.verts.new((x, y, z))
    return verts[key]


def add_face(*coords):
    return bm.faces.new([v(*c) for c in coords])


studs_faces = []
holes_faces = []

# Bottom (holes): LENGTH_CELLS x DEPTH_CELLS unit quads at z=0.
for ix in range(LENGTH_CELLS):
    x0, x1 = ix * CELL_X, (ix + 1) * CELL_X
    for iy in range(DEPTH_CELLS):
        y0, y1 = iy * CELL_Y, (iy + 1) * CELL_Y
        f = add_face((x0, y0, 0), (x1, y0, 0), (x1, y1, 0), (x0, y1, 0))
        holes_faces.append(f)

# Top of the flat lane (studs): LENGTH_CELLS x 1 unit quads at z=HEIGHT, y: 0..FLAT_Y.
for ix in range(LENGTH_CELLS):
    x0, x1 = ix * CELL_X, (ix + 1) * CELL_X
    f = add_face((x0, 0, HEIGHT), (x0, FLAT_Y, HEIGHT), (x1, FLAT_Y, HEIGHT), (x1, 0, HEIGHT))
    studs_faces.append(f)

# Slope face: from the ridge straight down to the top of the plinth.
add_face((0, FLAT_Y, HEIGHT), (LENGTH, FLAT_Y, HEIGHT), (LENGTH, DEPTH, PLINTH_HEIGHT), (0, DEPTH, PLINTH_HEIGHT))

# Plinth: a short vertical wall at the outer edge, y=DEPTH.
add_face((0, DEPTH, 0), (LENGTH, DEPTH, 0), (LENGTH, DEPTH, PLINTH_HEIGHT), (0, DEPTH, PLINTH_HEIGHT))

# Back wall of the flat lane, at y=0.
add_face((0, 0, 0), (LENGTH, 0, 0), (LENGTH, 0, HEIGHT), (0, 0, HEIGHT))

# End caps, at x=0 and x=LENGTH (the flat+slope+plinth cross-section silhouette).
add_face((0, 0, 0), (0, 0, HEIGHT), (0, FLAT_Y, HEIGHT), (0, DEPTH, PLINTH_HEIGHT), (0, DEPTH, 0))
add_face((LENGTH, 0, 0), (LENGTH, DEPTH, 0), (LENGTH, DEPTH, PLINTH_HEIGHT), (LENGTH, FLAT_Y, HEIGHT), (LENGTH, 0, HEIGHT))

bmesh.ops.recalc_face_normals(bm, faces=bm.faces)

# recalc_face_normals gets the bottom faces backwards (pointing +Z, into the
# solid) for this stepped cross-section - flip them back to the correct
# outward (-Z) direction explicitly rather than trusting it here.
bottom_faces_wrong_way = [f for f in holes_faces if f.normal.z > 0]
if bottom_faces_wrong_way:
    bmesh.ops.reverse_faces(bm, faces=bottom_faces_wrong_way)

bm.faces.index_update()
studs_face_indices = [f.index for f in studs_faces]
holes_face_indices = [f.index for f in holes_faces]

mesh = bpy.data.meshes.new(NAME)
bm.to_mesh(mesh)
bm.free()

obj = bpy.data.objects.new(NAME, mesh)
bpy.context.collection.objects.link(obj)
bpy.context.view_layer.objects.active = obj
obj.select_set(True)

for poly in mesh.polygons:
    poly.use_smooth = False

mesh.materials.append(bpy.data.materials.new("Default"))
mesh.materials.append(bpy.data.materials.new("HasStuds"))
mesh.materials.append(bpy.data.materials.new("HasHoles"))

studs_indices = set(studs_face_indices)
holes_indices = set(holes_face_indices)
for poly in mesh.polygons:
    if poly.index in studs_indices:
        poly.material_index = 1
    elif poly.index in holes_indices:
        poly.material_index = 2
    else:
        poly.material_index = 0

bpy.context.scene.cursor.location = (0, 0, 0)
bpy.ops.object.origin_set(type='ORIGIN_CURSOR')

mesh.update()
bpy.ops.wm.save_as_mainfile(filepath=OUT_PATH)

slope_angle = math.degrees(math.atan2(HEIGHT - PLINTH_HEIGHT, DEPTH - FLAT_Y))
print(f"{NAME}: {len(mesh.polygons)} faces, {len(mesh.vertices)} vertices "
      f"({len(studs_faces)} studs, {len(holes_faces)} holes)")
print(f"Slope angle: {slope_angle:.1f} deg")
print(f"Saved to {OUT_PATH}")
