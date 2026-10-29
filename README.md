# Lagos Run

An endless 3D runner set on the streets of Lagos, in the style of Subway Surfers. Open `index.html` in a browser to play. No build step.

- Run from Ikorodu through Mile 12 Market, Oshodi, Ikeja and Yaba, across Third Mainland Bridge, into Lagos Island, Victoria Island, Lekki, Ajah and Epe, then loop. Areas get longer as you go, and each one starts with a Lagos State welcome sign.
- Dodge danfos (or jump onto their roofs), keke napep, road-works barriers, potholes and overhead banners.
- Collect naira coins. Power-ups: Magnet, Spring sneakers (higher jump), Jollof 2× score.
- Bump into the side of something and whoever controls the area starts chasing you: agberos in Ikorodu, Mile 12, Oshodi, Lagos Island and Ajah; olopa (police) in Yaba, on the bridge, V.I., Lekki and Epe; LASTMA in Ikeja. Bump again before they give up and they catch you: the agberos surround you with sticks and machetes, the olopa grab your arms and cuff you. The local crew also chases you for a few seconds at the start of every area.

## Characters

Every coin you collect goes into your purse as ₦10. Spend it on new runners from the **Characters** button:

| Ladies | Gents | Kids |
| --- | --- | --- |
| Amaka, UNILAG student (₦1,500) | Tunde, Super Eagles fan (free) | Tobi, primary school boy (₦1,000) |
| Iya Basirat, Mile 12 market woman (₦4,000) | Sunday the Konda, danfo conductor (₦2,000) | Kemi, primary school girl (₦2,500) |
| Tiwa, Lekki baddie (₦8,000) | Baba Okada, okada rider (₦5,000) | |
| | Chief Odogwu, Lekki big man (₦10,000) | |

The sidewalks are full of Lagos people too: hawkers with trays on their heads, mothers carrying babies, men in kaftans on the phone, office workers and school kids.

## Controls

| Action | Keyboard | Touch |
| --- | --- | --- |
| Change lane | ← → or A D | Swipe left / right |
| Jump | ↑, W or Space | Swipe up |
| Slide / drop fast | ↓ or S | Swipe down |
| Pause | P or Esc | Pause button |
| Mute | M | Sound button |

Built with [three.js](https://threejs.org/) r128 from cdnjs. All textures and sounds are generated in code.

---

# Japa from the Shrine

A second game in this repo: a Temple Run-style escape in `japa.html`. Open it in a browser to play.

You've taken the sacred calabash from the shrine and the Egungun masquerades are after you. Run along an ancient raised causeway with sharp 90° corners, through five areas that loop:

1. **Sacred Grove**: giant trees, carved stone heads, trees wrapped in red and white cloth
2. **Laterite Ruins**: red earth pillars and walls
3. **Osun River Bridge**: plank bridge with rope rails over the river, more gaps to jump
4. **Village Outskirts**: thatched huts and palms
5. **Road to the Airport**: asphalt, Ghana-must-go bag piles, visa check barriers, speed bumps and a plane overhead

- **Turn** at corners with ← / → (or swipe). Miss the turn and you fall into the bush.
- **Jump** roots, fire pits and gaps. **Slide** under stone arches and fallen trees. **Dodge** shrine statues.
- **Collect cowries.** Power-ups: cowrie magnet, spring legs, double cowries.
- **Stumble** and the Egungun close in. Stumble again and they surround you.
- **Runners:** Tunde, Amaka, Iya Basirat or Kemi.

Same controls as Lagos Run, plus turning. No build step; three.js r128 from cdnjs, everything else generated in code.

## Unity versions

Full Unity 6 (6000.6.5f1) ports of both games, with rigged human characters, recorded motion clips, real building/vehicle models and Poly Haven textures:

- `unity/LagosRun` — Lagos Run
- `unity/ShrineJapa` — Japa from the Shrine

Open either folder in Unity, or build from a terminal with `Unity -batchmode -quit -projectPath unity/LagosRun -executeMethod LRBuild.BuildMac` (also `BuildWeb`, `BuildAndroid`, `BuildIOS`). Each project's own `README.md` has details, and `Assets/Credits.md` lists asset sources and licences (CC-BY / CC0).
