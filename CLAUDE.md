# PhilanthropicPhyllonycteris

A BepInEx 5 plugin for Sunless Sea: right-click the chart to place a marker, and a bat hovering on a ring around the ship shows the way to it while out of port. PLAN.md holds the design, the game facts it rests on, the milestones and the list of things still to verify in game. User-facing setup and build steps are in README.md.

## Environment

| Item | Value |
|---|---|
| Game | 3.0.0.31, Unity 6000.3.2f1, Mono scripting (Harmony works) |
| Build in use | Native Linux via the `steamlinuxruntime` compat tool |
| Game folder | `~/.local/share/Steam/steamapps/common/SunlessSea` |
| Persistent data (content JSON, saves) | `~/.config/unity3d/Failbetter Games/Sunless Sea/` |
| Decompiled game source | `../sunless-sea-project/decomp/` (sibling checkout; regenerate with the command in its README) |
| BepInEx log | `<game>/BepInEx/LogOutput.log`; Unity's own log is `~/.config/unity3d/Failbetter Games/Sunless Sea/Player.log` |

Sibling mods with the same conventions: `../sunless-sea-project` (SunlessSee) and `../EndlessGazetteer`.

## Build and test

```sh
dotnet build PhilanthropicPhyllonycteris.sln -c Release   # also copies the plugin into <game>/BepInEx/plugins/PhilanthropicPhyllonycteris/
dotnet test PhilanthropicPhyllonycteris.sln
```

The game path comes from `Directory.Build.props` (`GameDir`, override with `SUNLESS_SEA_DIR`).

## Architecture

- **`PhilanthropicPhyllonycteris.Core`** (netstandard2.0, no Unity or game dependencies): coordinates, map fingerprint, marker file format, indicator maths, the glow's distance field. Everything testable lives here.
- **`PhilanthropicPhyllonycteris`** (net472 plugin): Harmony patches, the marker store, and the GameObjects. Game privates are touched only through `Game/MapAccess`.
- **`PhilanthropicPhyllonycteris.Tests`** (net8.0 xunit): references Core only. Anything holding a GameObject is checked by hand in the game.

## Rules

- Markers are keyed by `MapAccess.Fingerprint` (layout hash), never by captain or save name, and live in `BepInEx/config/PhilanthropicPhyllonycteris.markers.txt`, never in the game's `saves/` folder (the game lists every json there as a captain). `MapFingerprint`'s algorithm is pinned by a test; changing it orphans every stored marker.
- `MarkerStore.Changed` is the only thing the chart icon (and the indicator by the ship) reacts to. Click handling never touches GameObjects.
- `MapProvider.Generate` runs on every zee load and rebuilds the whole chart, `_icons` included, so the marker is rebuilt from its postfix. Being in `_icons` is what keeps the marker a constant size on screen; the game writes `new Vector3(s, s)` to those, so their z scale is 0.
- The bat by the ship (`ShipIndicator`) is a root world-space sprite, with the light around it as its child (`Sprites.Glow`, drawn from the bat's own outline and to its scale), ticked from `Plugin.LateUpdate`, so it reads the boat after the boat has moved. It dies with the zee scene and is recreated on the next tick that needs it. Never call `NavigationProvider.Instance.Boat` per frame: with no boat in the scene it runs `GameObject.Find` every call.
- If a `MapProvider` field is missing, `MapAccess.Available` is false and no patch is applied at all.

## Verified in game

From a `Debug.DumpMapIcons` capture, 2026-09-19. Record further "to verify" items from PLAN.md here as they are measured.

| Thing | Value |
|---|---|
| Map camera | Looks along +z from z = 0, near -20, far 20, culling mask layer 11 only, ortho size 150 at closest zoom |
| Layer | Everything on the chart is on layer 11 ("Map") |
| Map root | No components, world (-450, -450, 0) on the 6 x 6 map |
| Boat icon | `SpriteRenderer`, `Sprites/Default`, sorting layer "AboveLights" order 3, z 0, 8.17 across |
| Port icon | `SpriteRenderer`, `Sprites/Default`, sorting layer "Default" order 1, z -0.5, 5.78 across |
| Fog of unknown | `MeshRenderer`, `Unlit/Transparent`, z -5, sorting layer **"Particles"** order 0 (set by a `ParticleEffectsLayering` component). It hides everything on "Default", port icons included, whatever their z or order: the sorting layer decides, not depth |
| Labels | World-space canvas `MapLabelCanvas`, "AboveLights" order 10, with a `GraphicRaycaster`, and label `Text` is a raycast target, so `EventSystem.IsPointerOverGameObject()` is true over label text |
| Marker | Sorts on the boat icon's layer at the boat's order + 1 (above the fog, under the labels); the game's bat icon, so 5.78 chart units across |

Read from `resources.assets` with UnityPy, 2026-09-19 (not yet seen in game):

| Thing | Value |
|---|---|
| Chart icon prefabs | `Ico_boat`, `Ico_port`, `Ico_Bat`: one `SpriteRenderer` each, white tint, 26 x 26 px cells of one `Icons` atlas at 4.5 px per unit (5.78 across; the boat's 8.17 above is its bounds while turned 45 degrees). The colour is in the texture, not the renderer |
| Boat icon disc | RGB 183, 134, 57; the default `Marker.Colour` |
| Seeking bat | Prefab `Sea/BatSignal`, used by `BatSignalIndication` while the zee-bat is out (5 s, circling the ship at radius 300 by `Animator`). Root > `BatLooper` > `Bat` (`SpriteRenderer` `BatSignal_0`, black, head up, 9 x 3.2 world units, sorting layer "Particles", plus a `TrailRenderer`) > `Light` (`SpriteRenderer` `RadialBasic`, a white disc with a soft edge, 94.8 units at scale 0.15, alpha 0.26, on layer 8). The indicator shows the bat at 10 across, with a light of its own making that reaches 2 beyond the bat's outline and is tinted by `Indicator.Colour`; the prefab's disc (at 12) is only the fallback |
| Bat icon | Disc RGB 117, 146, 120 with a black bat; "AboveLights" order 1. The game does not add it to `_icons`, so its own bat mark does not hold its size as the chart zooms; ours does |

From a `Debug.DumpShip` capture, 2026-09-19:

| Thing | Value |
|---|---|
| Sorting layers, bottom to top | BelowWater (-3), SeaBed (-2), Decals (-1), Default (0), Particles (1), LightingOverlay (2), AboveLights (3), Weather (4). The darkness is "LightingOverlay", so only "AboveLights" and "Weather" escape it |
| Player boat | Root "PlayerBoat" on layer 0 at z -0.1, holding `MoveBoat`; hull sprites (`BoatLayer01`, ...) are `Sprites/Default` on sorting layer "Default", about 12 x 41 world units for the starting ship |
| Indicator | Layer 0, the boat's z, sorting layer "AboveLights", on a ring of radius 90, hidden within 100 (first measured at 45 and 90 with the 12-unit arrow that the bat replaced). The hover does not stop while the game is paused |
