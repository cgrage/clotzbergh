"""
Builds Stairs4x7.blend from scratch via bmesh - a low-poly stand-in for LEGO part
30134 (Stairs 7x4x6), not a copy of it: 6 floating 1x4-plate-shaped treads
connected by diagonal webs, a double-thick 1x2 support block at the bottom, and a
single-thick 1x2 landing ("nose") at the top - see the 2026-08 chat history for how
that shape was reverse-engineered from the real part's geometry.

Usage (from anywhere):
    blender --background --factory-startup --python ArtSource/build_stairs.py -- ArtSource/Stairs4x7.blend

Re-run ArtSource/regenerate_all.py afterwards to re-export the FBX.
"""

import bpy
import bmesh
import sys

argv = sys.argv[sys.argv.index("--") + 1:]
OUT_PATH = argv[0]
NAME = "Stairs4x7"

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

# Blender axes -> Unity axes (matches ArtSource/regenerate_all.py's axis_forward='Z', axis_up='Y'):
#   Blender X -> Unity X (width - the side-by-side placement axis)
#   Blender Z -> Unity Y (height)
#   Blender Y -> Unity Z (run/climb axis)
CELL_X = 0.36    # WorldDef.SubKlotzSize.x - width axis
CELL_Z = 0.144   # WorldDef.SubKlotzSize.y - height axis, per sub-unit
CELL_Y = 0.36    # WorldDef.SubKlotzSize.z - run/climb axis

WIDTH_CELLS = 4
STEP_COUNT = 6
RISER_SUBUNITS = 3   # 1 brick rise per step
CAP_SUBUNITS = 1     # each tread is a plate-thick slab

WIDTH = WIDTH_CELLS * CELL_X

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


def add_box(x0, x1, y0, y1, z0, z1, stud_cells=1, hole_cells=1):
    # The shader's stud/hole generators size each one from its own face/triangle, not
    # the klotz grid - so a face spanning multiple cells only gets one (wrongly sized)
    # stud/hole unless it's split into one quad per cell here.
    hole_width = (x1 - x0) / hole_cells
    for i in range(hole_cells):
        hx0 = x0 + i * hole_width
        hx1 = hx0 + hole_width
        holes_faces.append(add_face((hx0, y0, z0), (hx0, y1, z0), (hx1, y1, z0), (hx1, y0, z0)))  # bottom
    cell_width = (x1 - x0) / stud_cells
    for i in range(stud_cells):
        cx0 = x0 + i * cell_width
        cx1 = cx0 + cell_width
        studs_faces.append(add_face((cx0, y0, z1), (cx1, y0, z1), (cx1, y1, z1), (cx0, y1, z1)))  # top
    add_face((x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1))  # front
    add_face((x0, y1, z0), (x0, y1, z1), (x1, y1, z1), (x1, y1, z0))  # back
    add_face((x0, y0, z0), (x0, y0, z1), (x0, y1, z1), (x0, y1, z0))  # left
    add_face((x1, y0, z0), (x1, y1, z0), (x1, y1, z1), (x1, y0, z1))  # right


def add_diagonal_web(x0, x1, y_front, y_back, z_front, z_back, thickness):
    # A thin sloped slab: its top surface runs from (y_front, z_front) to (y_back,
    # z_back), its bottom surface is the same diagonal offset down by `thickness`.
    a0, a1 = (x0, y_front, z_front), (x1, y_front, z_front)
    b0, b1 = (x0, y_back, z_back), (x1, y_back, z_back)
    a0b, a1b = (x0, y_front, z_front - thickness), (x1, y_front, z_front - thickness)
    b0b, b1b = (x0, y_back, z_back - thickness), (x1, y_back, z_back - thickness)

    add_face(a0, a1, b1, b0)      # top (sloped)
    add_face(a0b, b0b, b1b, a1b)  # bottom (sloped)
    add_face(a0b, a1b, a1, a0)    # front cap
    add_face(b0b, b0, b1, b1b)    # back cap
    add_face(a0b, a0, b0, b0b)    # left
    add_face(a1b, b1b, b1, a1)    # right


def add_box_no_top(x0, x1, y0, y1, z0, z1, hole_cells=1):
    # Like add_box, but the top is hidden under a tread above it - skip it.
    hole_width = (x1 - x0) / hole_cells
    for i in range(hole_cells):
        hx0 = x0 + i * hole_width
        hx1 = hx0 + hole_width
        holes_faces.append(add_face((hx0, y0, z0), (hx0, y1, z0), (hx1, y1, z0), (hx1, y0, z0)))
    add_face((x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1))
    add_face((x0, y1, z0), (x0, y1, z1), (x1, y1, z1), (x1, y1, z0))
    add_face((x0, y0, z0), (x0, y0, z1), (x0, y1, z1), (x0, y1, z0))
    add_face((x1, y0, z0), (x1, y1, z0), (x1, y1, z1), (x1, y0, z1))


def tread_top_z(step_index):
    return (step_index + 1) * RISER_SUBUNITS * CELL_Z


def tread_bottom_z(step_index):
    return tread_top_z(step_index) - CAP_SUBUNITS * CELL_Z


# --- The 6 treads: flat, plate-thick, full width, floating one riser apart. ---
for step_index in range(STEP_COUNT):
    y0 = step_index * CELL_Y
    y1 = y0 + CELL_Y
    add_box(0.0, WIDTH, y0, y1, tread_bottom_z(step_index), tread_top_z(step_index), stud_cells=WIDTH_CELLS)

# --- Bottom support block: 1x2 footprint, centered, under the lowest tread, double
# a normal plate's thickness - fills the ground-to-tread-1 gap only in the middle 2 studs. ---
add_box_no_top(1 * CELL_X, 3 * CELL_X, 0.0, CELL_Y, 0.0, tread_bottom_z(0), hole_cells=2)

# --- Top support block: 1x2 footprint, centered, in the 7th (final) cell past the
# last tread - single plate thickness, one sub-unit lower than the last tread's cap
# (the real part's diagonal connectors land it there; ours don't exist yet, see below).
top_y0 = STEP_COUNT * CELL_Y
top_y1 = top_y0 + CELL_Y
top_z1 = tread_bottom_z(STEP_COUNT - 1)
top_z0 = top_z1 - CAP_SUBUNITS * CELL_Z
add_box(1 * CELL_X, 3 * CELL_X, top_y0, top_y1, top_z0, top_z1, stud_cells=2, hole_cells=2)

RUN = top_y1

# Two narrow diagonal webs, one on each side of the middle gap, connecting each pair
# of consecutive treads. Each spans exactly one tread's own depth - flush with that
# tread's front edge (where it meets the lower tread's top) and its back edge (where
# it meets its own underside).
WEB_WIDTH = 0.25 * CELL_X
WEB_THICKNESS = 2 * CAP_SUBUNITS * CELL_Z
web_x_ranges = [(1 * CELL_X - WEB_WIDTH, 1 * CELL_X), (3 * CELL_X, 3 * CELL_X + WEB_WIDTH)]

for step_index in range(STEP_COUNT - 1):
    y_front = (step_index + 1) * CELL_Y
    y_back = (step_index + 2) * CELL_Y
    z_front = tread_top_z(step_index)
    z_back = tread_bottom_z(step_index + 1)
    for x0, x1 in web_x_ranges:
        add_diagonal_web(x0, x1, y_front, y_back, z_front, z_back, WEB_THICKNESS)

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

print(f"{NAME}: {len(mesh.polygons)} faces, {len(mesh.vertices)} vertices")
print(f"Bounds: X 0..{WIDTH:.3f} ({WIDTH_CELLS} cells), Y 0..{RUN:.3f} ({RUN / CELL_Y:.1f} cells)")
print(f"Saved to {OUT_PATH}")
