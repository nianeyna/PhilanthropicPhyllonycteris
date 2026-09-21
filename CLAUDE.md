# PhilanthropicPhyllonycteris

A BepInEx 5 plugin for Sunless Sea: right-click the chart to place a marker, and a bat hovering on a ring around the ship shows the way to it while out of port. The planned features are all built and checked in game. User-facing behaviour, settings, setup and build steps are in README.md.

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
- Every patch is a postfix; nothing of the game's is skipped. Settings are read each frame, so they apply live with no `SettingChanged` handling.
- Clicks on the chart are filtered by `EventSystem.RaycastAll` for `Selectable`s, not `IsPointerOverGameObject()`, which is also true over label text.

## Persistence

- Keying by layout means a captain's autosave and named saves share a marker, and an heir who inherits the chart inherits the mark, while a reshuffled zee starts clean. Accepted cost: loading an older save of the same captain shows the newest marker.
- `FreshZeePatch` (postfix on `CharacterRepository.GenerateTileConfig`, which runs for a new game and for an heir without the chart; an inherited chart bypasses it via `LegacyProvider.Chart ?? GenerateTileConfig()`) drops any stored line for the new layout, so an heir who rolls a past captain's layout (about 1 in 6 billion under the default rules) does not find that captain's marker. It is the one thing never exercised in game: it cannot be staged by play. To try it, copy the new map's fingerprint onto a line of the markers file before creating the captain and look for "its old marker was dropped" in the log.
- Lines for other maps are kept untouched and never pruned (about 60 bytes each). Unparseable lines are dropped, with the original copied to `.damaged` first. Every change is written at once, to a sibling file that is swapped in.
- There is one marker per map. The file format (one line per fingerprint, versioned header) leaves room for several markers or labels if they are ever wanted.

## Game facts (from `decomp/`)

| Fact | Source |
|---|---|
| Chart coordinates are world coordinates / 10. Zee tiles are 1500 world units, chart tiles 150. Everything on the chart is a child of `_mapRoot` ("Map"), translated by (-mapWidth/2, -mapHeight/2), so `_mapRoot` local position x 10 = world position | `MapProvider.Generate`, `SetupBoatIcon`, `MarkZeeBatLocation`, `Tiler.LayoutTiles` |
| Pointer to chart: `_mapCamera.ScreenToWorldPoint(Input.mousePosition)`, as the game's own drag code does, then `_mapRoot.transform.InverseTransformPoint`. Map bounds in chart units are 0.._mapWidth, 0.._mapHeight | `MapProvider.CalculateCameraPosition`, `Generate` |
| `MapProvider` is a singleton that outlives scenes, but `Generate()` rebuilds every GameObject each time the zee loads. Its early-out ("Map could not be found") leaves `_mapRoot` null | `Tiler.cs:71` |
| `MapProvider.ListenKeyboardEvents()` runs every frame; its `InMap` branch is the chart's per-frame tick | `DebugInputs.cs:19` |
| Icons in `_icons` are rescaled with zoom every frame (`1 + (ortho - 150)/150 + 0.2`) | `MapProvider.ResizeMapItems` |
| The game does nothing with the right mouse button on the chart; the left button drags. Ctrl+left-click is a right-click on a Mac everywhere in the game, and the drag code ignores it | `MapProvider`, `MouseController`, `OnClickHandler.MacCtrlPressed` |
| Dock state is `GameProvider.Instance.CurrentUIState.InDock`, chart state `InMap` | `GameProvider.UIState` |
| A captain is not one file: `Save()` writes "Autosave" and named saves, and `CharacterRepository.GetCharacterNames` lists every `saves/*.json` as a captain | `CharacterRepository`, `LegacyProvider.cs:427` |
| `CurrentCharacter.TileConfig` is `Width`, `Height` and ordered `Tiles[]`, each with a `Name` (the specific tile version) | `SerializableTileConfig` |

## Verified in game

From a `Debug.DumpMapIcons` capture, 2026-09-19.

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

Read from `resources.assets` with UnityPy, 2026-09-19:

| Thing | Value |
|---|---|
| Chart icon prefabs | `Ico_boat`, `Ico_port`, `Ico_Bat`: one `SpriteRenderer` each, white tint, 26 x 26 px cells of one `Icons` atlas at 4.5 px per unit (5.78 across; the boat's 8.17 above is its bounds while turned 45 degrees). The colour is in the texture, not the renderer |
| Boat icon disc | RGB 183, 134, 57; the tint of the marker's disc (`ChartMarker.DiscColour`) |
| Seeking bat | Prefab `Sea/BatSignal`, used by `BatSignalIndication` while the zee-bat is out (5 s, circling the ship at radius 300 by `Animator`). Root > `BatLooper` > `Bat` (`SpriteRenderer` `BatSignal_0`, black, head up, 9 x 3.2 world units, sorting layer "Particles", plus a `TrailRenderer`) > `Light` (`SpriteRenderer` `RadialBasic`, a white disc with a soft edge, 94.8 units at scale 0.15, alpha 0.26, on layer 8). The indicator shows the bat at 10 across, with a light of its own making that reaches 2 beyond the bat's outline and is tinted by `Indicator.Colour`; the prefab's disc (at 12) is only the fallback |
| Bat icon | Disc RGB 117, 146, 120 with a black bat; "AboveLights" order 1. The game does not add it to `_icons`, so its own bat mark does not hold its size as the chart zooms; ours does |

From a `Debug.DumpShip` capture, 2026-09-19:

| Thing | Value |
|---|---|
| Sorting layers, bottom to top | BelowWater (-3), SeaBed (-2), Decals (-1), Default (0), Particles (1), LightingOverlay (2), AboveLights (3), Weather (4). The darkness is "LightingOverlay", so only "AboveLights" and "Weather" escape it |
| Player boat | Root "PlayerBoat" on layer 0 at z -0.1, holding `MoveBoat`; hull sprites (`BoatLayer01`, ...) are `Sprites/Default` on sorting layer "Default", about 12 x 41 world units for the starting ship |
| Indicator | Layer 0, the boat's z, sorting layer "AboveLights", on a ring of radius 90, hidden within 100. Being a world-space object it is right on screen however the camera is turned, and the book and menus cover it on their own. The hover does not stop while the game is paused |
