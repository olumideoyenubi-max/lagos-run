# Japa from the Shrine (Unity)

A Temple Run-style escape, ported from the web version to Unity 6000.6.5f1. You have taken the sacred calabash
and the Egungun have woken: run the raised causeway from the Sacred Grove, through the Laterite Ruins, over the
Osun River Bridge and the Village Outskirts, to the Road to the Airport. The path turns at right angles; miss a
corner or a gap and you fall. Stumble twice and the masquerades catch you.

It shares its engine code with Lagos Run (`~/LagosRun`): the `LR*` scripts, shaders and Blender tools are copies.

## Opening it

Open this folder in Unity Hub with editor 6000.6.5f1 (on the NHBuild volume, so LumiTB must be plugged in and
mounted). Open `Assets/Scenes/Main.unity` and press Play.

## Controls

| Action | Keyboard | Touch / mouse |
|---|---|---|
| Turn at a corner, or change lane | Left / Right or A / D | Swipe left / right |
| Jump roots, fire and gaps | Up, W or Space | Swipe up |
| Slide under arches and fallen trees | Down or S | Swipe down |
| Pause | P or Esc | Pause button |
| Mute | M | Speaker button |

## Files

| File | What it holds |
|---|---|
| `Assets/Scripts/SJGame.cs` | Areas, runners, the turning path, spawning, collisions, the Egungun, camera |
| `Assets/Scripts/SJUI.cs` | Menus and HUD |
| `Assets/Scripts/SJArt.cs` | Materials: photo surfaces per area, painted signs and cloth |
| `Assets/Scripts/SJPrefabs.cs` | Scenery and obstacles (models merged with textured shapes) |
| `Assets/Scripts/LR*.cs` | Shared engine: people and how they move (`Runner`, described in the Lagos Run README), mesh merging, texture painter, audio, post-processing |
| `Assets/Resources/Props` | Tree, hut, mask, pot, pillar, banana plant, palm, airliner |
| `Assets/Credits.md` | Attribution required by the CC-BY models, music and animations |

## Building

The **Japa** menu in the editor has the four builds. From a terminal:

```
UNITY="/Volumes/NHBuild/Unity/Hub/Editor/6000.6.5f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -quit -projectPath ~/ShrineJapa -executeMethod LRBuild.BuildMac
```

Swap `BuildMac` for `BuildWeb`, `BuildAndroid` or `BuildIOS`. Output goes to `Builds/`. The Web build must be
served over HTTP.

## Screenshot tour

`"Builds/Mac/Japa from the Shrine.app/Contents/MacOS/Japa from the Shrine" -lrshots <folder>` plays every area on
autopilot, saves fourteen screenshots and quits.
