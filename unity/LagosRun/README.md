# Lagos Run (Unity)

A three-lane endless runner from Ikorodu to Epe, ported from the web version to Unity 6000.6.5f1.
Dodge danfos, kekes, road blocks, banners and potholes, grab naira, and don't stumble twice or whoever
controls the area (agberos, olopa or LASTMA) will catch you.

## Opening it

Open this folder in Unity Hub with editor 6000.6.5f1 (installed on the NHBuild volume, so LumiTB must be
plugged in and mounted). Open `Assets/Scenes/Main.unity` and press Play.

## Controls

| Action | Keyboard | Touch / mouse |
|---|---|---|
| Change lane | Left / Right or A / D | Swipe left / right |
| Jump (also onto danfo roofs) | Up, W or Space | Swipe up |
| Slide under banners | Down or S | Swipe down |
| Pause | P or Esc | Pause button |
| Mute | M | Speaker button |

## How it is built

The street, signs, obstacles and sound are generated in code at startup. The people, the danfo and the keke are
real models, and the people move with motion clips prepared in Blender.

| File | What it holds |
|---|---|
| `Assets/Scripts/LRGame.cs` | Areas, cast of characters, spawning, collisions, chase and camera |
| `Assets/Scripts/LRPeople.cs` | People (MakeHuman models), their outfits and props, vehicles, and `Runner`, which animates a person |
| `Assets/Scripts/LRUI.cs` | Menus and HUD |
| `Assets/Scripts/LRArt.cs` | Painted textures and materials |
| `Assets/Scripts/LRPrefabs.cs` | Obstacles and street furniture built from simple shapes |
| `Assets/Scripts/LRShape.cs` | Mesh merging and the web-to-Unity coordinate conversion |
| `Assets/Scripts/LRCanvas.cs` | The 2D painter used for textures |
| `Assets/Scripts/LRAudio.cs` | Synthesised music loop and sound effects |
| `Assets/Resources/LRLit.shader` | The surface shader: per-pixel light, real-time shadows, fog |
| `Assets/Resources/Characters` | Eight MakeHuman people from NAIJA HUSTLE (same skeleton) |
| `Assets/Resources/Vehicles` | Danfo and keke models |
| `Assets/Resources/Motion` | Run, Sprint, Walk, Idle, Knock_Out, Dance, Drink, Eat as rotation data |
| `Assets/Editor/LRCharacterImport.cs` | Import rules for the models (materials, texture sizes) |
| `Assets/Editor/LRBuild.cs` | Project setup and builds |
| `Assets/Credits.md` | Attribution required by the CC-BY models and animations |

Game code keeps the web version's coordinates (x across the road, y up, z toward the camera) and converts
with `V.P` / `V.R` when placing things, so numbers match the original source.

## How people move

`Runner` plays the motion clips and adds what the clips do not have:

- Clips can be laid over one another, on the whole body or part of it: `Play` sets the main clip, `Over` adds one
  on top (the sprint over the run, a landing over the run, a chopping arm over running legs).
- `Animate` is the runner on the road. A jump follows the real arc (arms up, tuck, reach for the ground, knees give
  on landing); a slide drops down, holds and gets back up; `Flinch` is a knock that does not stop the run and
  `Swerve` turns and leans the body into a lane change.
- Hands close: loosely when running, tightly round a weapon or bag (`hold`).
- The head turns toward `look`. The runner watches the road, chasers watch the runner, and bystanders watch the
  runner go by and react (`LRGame.Reaction`).
- Feet are kept out of the ground when two clips are blended.

## Blender tools

Both run with Blender in the background and do not touch an open Blender session.

- `Tools/export_motion.py` writes actions from a .blend as per-bone rotations the game can play on any of the
  people: `blender -b <file.blend> -P Tools/export_motion.py -- <armature> <out.json> <rest | Action@frame> <action:samples:loop> ...`
- `Tools/convert_vehicle.py` joins, lightens and centres a glTF vehicle and writes an FBX:
  `blender -b -P Tools/convert_vehicle.py -- <scene.gltf> <out.fbx> <target triangles>`

## Building

From the editor: the **Lagos Run** menu has Build Mac App, Build Web, Build Android APK and Build iOS
Xcode Project. From a terminal:

```
UNITY="/Volumes/NHBuild/Unity/Hub/Editor/6000.6.5f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -quit -projectPath ~/LagosRun -executeMethod LRBuild.BuildMac
```

Swap `BuildMac` for `BuildWeb`, `BuildAndroid` or `BuildIOS`. Output goes to `Builds/`.

The Web build must be served over HTTP (it will not run from a `file://` path):

```
cd ~/LagosRun/Builds/Web && python3 -m http.server 8080
```

## Motion lab

`"Builds/Mac/Lagos Run.app/Contents/MacOS/Lagos Run" -lrlab <folder> -screen-width 1600 -screen-height 520 -screen-fullscreen 0`
saves a picture per move with eight copies of the runner in a row, each a moment later than the last: every clip in
`LabClips` on its own, then the jump, slide, knock, lane change and drop as the game plays them. Use it to check a
change to the animation frame by frame.

## Screenshot tour

`"Builds/Mac/Lagos Run.app/Contents/MacOS/Lagos Run" -lrshots <folder>` plays a scripted tour, saves ten
screenshots to the folder and quits. Add `-screen-width 420 -screen-height 860 -screen-fullscreen 0` to
check the phone layout.
