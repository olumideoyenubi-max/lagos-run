# Blender (background): turn a downloaded glTF vehicle into one clean, lighter FBX for Unity.
#   blender -b -P convert_vehicle.py -- <scene.gltf> <out.fbx> <target triangles>
import bpy, sys, math
from mathutils import Vector

src, out, target = sys.argv[sys.argv.index("--") + 1:][:3]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
for o in bpy.context.scene.objects: o.select_set(False)
for o in meshes:
    o.select_set(True)
    world = o.matrix_world.copy(); o.parent = None; o.matrix_world = world   # keep each part where it is
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.join()
car = bpy.context.view_layer.objects.active
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tris = sum(len(p.vertices) - 2 for p in car.data.polygons)
if tris > int(target):
    mod = car.modifiers.new("lighter", 'DECIMATE'); mod.ratio = int(target) / tris
    bpy.ops.object.modifier_apply(modifier=mod.name)
# stand it on the ground, centred
co = [car.matrix_world @ v.co for v in car.data.vertices]
lo = Vector((min(c.x for c in co), min(c.y for c in co), min(c.z for c in co)))
hi = Vector((max(c.x for c in co), max(c.y for c in co), max(c.z for c in co)))
car.location -= Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))
bpy.ops.object.transform_apply(location=True)
car.name = "Vehicle"
for o in list(bpy.context.scene.objects):
    if o != car: bpy.data.objects.remove(o, do_unlink=True)
print("LRINFO size", tuple(round(v, 2) for v in (hi - lo)), "tris", tris, "->", sum(len(p.vertices) - 2 for p in car.data.polygons), "materials", [m.name for m in car.data.materials])
bpy.ops.export_scene.fbx(filepath=out, use_selection=False, object_types={'MESH'}, apply_scale_options='FBX_SCALE_UNITS', bake_anim=False, path_mode='STRIP', mesh_smooth_type='FACE')
print("LRINFO wrote", out)
