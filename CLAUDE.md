# PhilanthropicPhyllonycteris

A BepInEx 5 plugin for Sunless Sea: right-click the chart to place a marker, and an arrow on a ring around the ship points to it while out of port. PLAN.md holds the design, the game facts it rests on, the milestones and the list of things still to verify in game. User-facing setup and build steps are in README.md.

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

- **`PhilanthropicPhyllonycteris.Core`** (netstandard2.0, no Unity or game dependencies): coordinates, map fingerprint, marker file format, indicator maths. Everything testable lives here.
- **`PhilanthropicPhyllonycteris`** (net472 plugin): Harmony patches, the marker store, and the GameObjects. Game privates are touched only through `Game/MapAccess`.
- **`PhilanthropicPhyllonycteris.Tests`** (net8.0 xunit): references Core only. Anything holding a GameObject is checked by hand in the game.

## Rules

- Markers are keyed by `MapAccess.Fingerprint` (layout hash), never by captain or save name, and live in `BepInEx/config/PhilanthropicPhyllonycteris.markers.txt`, never in the game's `saves/` folder (the game lists every json there as a captain). `MapFingerprint`'s algorithm is pinned by a test; changing it orphans every stored marker.
- `MarkerStore.Changed` is the only thing the chart icon (and later the arrow) reacts to. Click handling never touches GameObjects.
- `MapProvider.Generate` runs on every zee load and rebuilds the whole chart, `_icons` included, so the marker is rebuilt from its postfix. Being in `_icons` is what keeps the marker a constant size on screen; the game writes `new Vector3(s, s)` to those, so their z scale is 0.
- The arrow (`ShipIndicator`) is a root world-space sprite ticked from `Plugin.LateUpdate`, so it reads the boat after the boat has moved. It dies with the zee scene and is recreated on the next tick that needs it. Never call `NavigationProvider.Instance.Boat` per frame: with no boat in the scene it runs `GameObject.Find` every call.
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
| Marker | Sorts on the boat icon's layer at the boat's order + 1 (above the fog, under the labels); 6 chart units across |

From a `Debug.DumpShip` capture, 2026-09-19:

| Thing | Value |
|---|---|
| Sorting layers, bottom to top | BelowWater (-3), SeaBed (-2), Decals (-1), Default (0), Particles (1), LightingOverlay (2), AboveLights (3), Weather (4). The darkness is "LightingOverlay", so only "AboveLights" and "Weather" escape it |
| Player boat | Root "PlayerBoat" on layer 0 at z -0.1, holding `MoveBoat`; hull sprites (`BoatLayer01`, ...) are `Sprites/Default` on sorting layer "Default", about 12 x 41 world units for the starting ship |
| Arrow | Layer 0, the boat's z, sorting layer "AboveLights"; 12 world units across on a ring of radius 45, hidden within 90 |
