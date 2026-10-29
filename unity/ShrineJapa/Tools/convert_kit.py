# Blender (background): convert every .glb in the given folders to an .fbx of the same name, untouched (pivots and
# material slot names are what the game relies on).
#   blender -b -P convert_kit.py -- <out folder> <in folder> [<in folder> ...]
import bpy, glob, os, sys
args = sys.argv[sys.argv.index("--") + 1:]
out = args[0]; os.makedirs(out, exist_ok=True)
for folder in args[1:]:
    for f in sorted(glob.glob(folder + "/*.glb")):
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.gltf(filepath=f)
        name = os.path.basename(f)[:-4]
        bpy.ops.export_scene.fbx(filepath=os.path.join(out, name + ".fbx"), use_selection=False, object_types={'MESH', 'EMPTY'}, apply_scale_options='FBX_SCALE_UNITS', bake_anim=False, path_mode='STRIP', mesh_smooth_type='FACE')
        print("LRINFO", name)
