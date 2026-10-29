# Blender (background): write actions as per-bone world-rotation deltas from a reference pose, converted to Unity axes.
#   blender -b <file.blend> -P export_motion.py -- <armature> <out.json> <reference> <action:samples:loop> ...
# <reference> is "rest" (the armature's rest pose) or "Action@frame" (a standing frame, for rigs whose rest pose is unusable).
import bpy, sys, json, re, math

args = sys.argv[sys.argv.index("--") + 1:]
if args[0].lower().endswith((".glb", ".gltf")):          # an animation file instead of an armature name: import it
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=args[0])
    arm = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE'][0]
else:
    arm = bpy.data.objects[args[0]]
out_path, ref_spec = args[1], args[2]
scene = bpy.context.scene
mw = arm.matrix_world
sides = lambda names: [n + s for s in ("_l", "_r") for n in names]
wanted = set(["pelvis", "spine_01", "spine_02", "spine_03", "neck_01", "head"] + sides(["clavicle", "upperarm", "lowerarm", "hand", "thigh", "calf", "foot", "ball"])
             + sides([f + "_0" + str(i) for f in ("index", "middle", "pinky", "ring", "thumb") for i in (1, 2, 3)]))

def clean(n):
    m = re.match(r"^(.*?)_0?(\d+)$", n)      # the glTF import appended node numbers: spine_01_07 -> spine_01
    return m.group(1) if m and m.group(1) in wanted else n

bones = [pb for pb in arm.pose.bones if clean(pb.name) in wanted]
names = [clean(pb.name) for pb in bones]
by = dict(zip(names, bones))

def use(action_name):
    act = bpy.data.actions[action_name]
    if arm.animation_data is None: arm.animation_data_create()
    for pb in arm.pose.bones:                 # clear what the previous action left on channels this one does not key
        pb.location = (0, 0, 0); pb.rotation_quaternion = (1, 0, 0, 0); pb.rotation_euler = (0, 0, 0); pb.scale = (1, 1, 1)
    arm.animation_data.action = act
    if hasattr(arm.animation_data, "action_slot") and len(getattr(act, "slots", [])) > 0:
        try: arm.animation_data.action_slot = act.slots[0]
        except Exception as e: print("LRINFO slot", e)
    return act
def goto(f):
    scene.frame_set(int(math.floor(f)), subframe=f - math.floor(f))
    bpy.context.view_layer.update()

arm_dirs = ref_spec != "rest"                      # "rest+arms": rest pose for the body, pointing directions for the arms
if ref_spec in ("rest", "rest+arms"):
    ref_q = [(mw @ pb.bone.matrix_local).to_quaternion() for pb in bones]
    ref_p = (mw @ by["pelvis"].bone.matrix_local).translation.copy()
    hl, hr = (mw @ by["foot_l"].bone.matrix_local).translation, (mw @ by["foot_r"].bone.matrix_local).translation
else:
    a, f = ref_spec.split("@"); use(a); goto(float(f))
    ref_q = [(mw @ pb.matrix).to_quaternion() for pb in bones]
    ref_p = (mw @ by["pelvis"].matrix).translation.copy()
    hl, hr = (mw @ by["foot_l"].matrix).translation.copy(), (mw @ by["foot_r"].matrix).translation.copy()
ref_h = ref_p.z - min(hl.z, hr.z)        # hip height above the feet in the reference pose
s = 1.0 if hl.x > hr.x else -1.0          # +1: the character faces -Y in Blender
print("LRINFO facing", "-Y" if s > 0 else "+Y", "pelvis ref", tuple(round(v, 3) for v in ref_p), "bones", len(names))

def uq(q): return [round(s * q.x, 5), round(-q.z, 5), round(s * q.y, 5), round(q.w, 5)]   # x, y, z, w in Unity axes
def uv(v): return [round(-s * v.x, 5), round(v.z, 5), round(-s * v.y, 5)]

clips = []
for spec in args[3:]:
    aname, nsamp, loop = spec.rsplit(":", 2); nsamp = int(nsamp); loop = loop == "1"
    act = use(aname)
    f0, f1 = act.frame_range
    qs, ps, arms = [], [], []
    for i in range(nsamp):
        goto(f0 + (f1 - f0) * i / (nsamp if loop else nsamp - 1))
        for pb, rq in zip(bones, ref_q):
            qs.extend(uq((mw @ pb.matrix).to_quaternion() @ rq.inverted()))
        # where each arm segment points; used instead of rotations when the reference pose has the arms somewhere odd
        for side in ("l", "r"):
            pts = [(mw @ by[n + "_" + side].matrix).translation for n in ("upperarm", "lowerarm", "hand", "middle_01")]
            for k in range(3): arms.extend(uv((pts[k + 1] - pts[k]).normalized()))
        # hip height is measured above the lower foot, and drift from the clip's first frame, so clips that move
        # the whole rig about (or sit at a different height) still come out standing on the ground
        pel = (mw @ by["pelvis"].matrix).translation
        low = min((mw @ by["foot_l"].matrix).translation.z, (mw @ by["foot_r"].matrix).translation.z)
        if i == 0: first = pel.copy()
        d = pel - first; d.z = (pel.z - low) - ref_h
        ps.extend(uv(d / ref_h))
    print("LRINFO clip", aname, "seconds", round((f1 - f0) / scene.render.fps, 3), "pelvis y range", round(min(ps[1::3]), 3), round(max(ps[1::3]), 3))
    clips.append({"name": aname.replace("NH_", "").replace(" ", "_"), "samples": nsamp, "loop": loop, "seconds": (f1 - f0) / scene.render.fps, "standRef": arm_dirs, "q": qs, "pelvis": ps, "arms": arms})

json.dump({"bones": names, "clips": clips}, open(out_path, "w"))
print("LRINFO wrote", out_path, len(names), "bones")
