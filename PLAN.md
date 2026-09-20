# PhilanthropicPhyllonycteris: plan

A BepInEx 5 plugin for Sunless Sea. Right-click anywhere on the chart to drop a marker; while a marker exists and the ship is out of port, a bat hovering on an invisible ring around the ship shows the way to it.

Environment, build conventions and game facts are taken from `../sunless-sea-project` (CLAUDE.md, `decomp/`) and `../EndlessGazetteer`. Game 3.0.0.31, Unity 6000.3, Mono, native Linux build, Harmony works.

## Behaviour

- **Place:** with the chart open, right-click on the chart. The marker appears under the pointer. There is one marker; right-clicking elsewhere moves it.
- **Remove:** right-click on the marker itself (hit test in screen pixels, so it is as easy at any zoom).
- macOS ctrl+left-click counts as a right-click, as it does everywhere else in the game (`OnClickHandler.MacCtrlPressed`; the game's own drag code already ignores that click).
- Clicks on the chart's uGUI controls (close, zoom) and outside the map rectangle do nothing. Fog of unknown does not matter: marking unexplored zee is the point.
- **Indicator:** the game's own seeking bat (the one that circles the ship while the zee-bat is out) with a pale light spreading from its outline, at a fixed radius from the ship, on the bearing to the marker, heading along that bearing and drifting gently in and out. It began as a drawn arrow, which is what the milestone notes below call it; the arrow remains as the stand-in if the game's bat cannot be had. Shown whenever a marker exists, except in dock (`UIState.InDock`), with the chart open (`InMap`), or once the ship is closer to the marker than a threshold (the arrow would otherwise point back across the ship). It is a world-space object, so the book and menus cover it on their own. The drift carries on while the game is paused.
- The marker persists across sessions, per map (see Persistence).

Out of scope for the first version: multiple markers, labels, a beacon drawn at the marked spot in the zee, distance readout, clear-on-arrival. The storage format leaves room for the first two.

## Game facts this rests on (from `decomp/`)

| Fact | Source |
|---|---|
| Chart coordinates are world coordinates / 10. Zee tiles are 1500 world units, chart tiles 150. Everything on the chart is a child of `_mapRoot` ("Map"), which is translated by (-mapWidth/2, -mapHeight/2), so **`_mapRoot` local position x 10 = world position** | `MapProvider.Generate`, `SetupBoatIcon` (`boat.position / 10`), `MarkZeeBatLocation`, `Tiler.LayoutTiles` |
| Pointer to chart: `_mapCamera.ScreenToWorldPoint(Input.mousePosition)`, as the game's own drag code does; then `_mapRoot.transform.InverseTransformPoint` | `MapProvider.CalculateCameraPosition` |
| Map bounds in chart units: 0.._mapWidth, 0.._mapHeight (`TileConfig.Width/Height * 150`) | `MapProvider.Generate` |
| `MapProvider` is a singleton that outlives scenes, but `Generate()` rebuilds every GameObject each time the zee loads (called from `Tiler.LayoutTiles`) | `Tiler.cs:71` |
| `MapProvider.ListenKeyboardEvents()` runs every frame; its `InMap` branch is the chart's per-frame tick | `DebugInputs.cs:19` |
| Icons in the private `_icons` list are rescaled with zoom every frame by `ResizeMapItems` (`1 + (ortho - 150)/150 + 0.2`). Port icons and the bat mark sit at z = -0.5 | `MapProvider.ResizeMapItems`, `SetupMapPort` |
| The game does nothing with the right mouse button on the chart; left button drags | `MapProvider`, `MouseController` |
| The game's own precedent for "a thing that follows the ship": `BatSignalIndication` instantiates a root world-space prefab and sets `transform.position = Boat.transform.position` each update | `Sunless.Game.UI.HUD/BatSignalIndication.cs` |
| Ship: `NavigationProvider.Instance.Boat` (`MoveBoat`). Sailing camera "Main Camera" is orthographic, size 150 to 200 world units | `MouseController` |
| Dock state: `GameProvider.Instance.CurrentUIState.InDock`; chart state: `InMap` | `GameProvider.UIState` |
| A captain is not one file: `Save()` writes "Autosave" and named saves, and an heir may inherit the chart (`LegacyProvider.Chart ?? GenerateTileConfig()`). `CharacterRepository.GetCharacterNames` lists every `saves/*.json` as a captain, so nothing of ours goes in `saves/` | `CharacterRepository`, `LegacyProvider.cs:427` |
| `CurrentCharacter.TileConfig` = `Width`, `Height`, ordered `Tiles[]` with `Name` (the specific tile version) | `SerializableTileConfig` |

## Layout

Same three-project shape as the sibling mods, `Directory.Build.props` copied from EndlessGazetteer (GameDir / `SUNLESS_SEA_DIR`), build copies the plugin to `<game>/BepInEx/plugins/PhilanthropicPhyllonycteris/`.

```
PhilanthropicPhyllonycteris.sln
Directory.Build.props
.gitignore                      bin/ obj/ *.user .vs/ .idea/
README.md                       player-facing: install, use, config
CLAUDE.md                       environment + verified facts, grown as they are measured
PhilanthropicPhyllonycteris.Core/     netstandard2.0, no Unity or game references
  ZeePoint.cs                   readonly struct (X, Y) in world units
  ChartPoint.cs                 readonly struct (X, Y) in chart units, so the two scales cannot be mixed
  ChartSpace.cs                 chart local <-> world (x10), clamp/contains for map bounds
  MapFingerprint.cs             stable hash of width, height, ordered tile names
  MarkerFile.cs                 text format: parse, serialise, per-fingerprint lookup
  IndicatorMath.cs              bearing, ring position, visibility by distance
  MarkerHitTest.cs              "is this click on the marker" in screen pixels
  GlowField.cs                  a glow that follows a shape's outline: distance transform and falloff
PhilanthropicPhyllonycteris/          net472 plugin
  Plugin.cs                     config, Harmony, LateUpdate tick
  MarkerStore.cs                owns the file and the current marker; raises Changed
  Game/MapAccess.cs             FieldRefs into MapProvider privates, in one place
  Game/SeekingBat.cs            the sprites of the game's Sea/BatSignal prefab
  Game/MapDump.cs               Debug.DumpMapIcons: logs how the chart's own pieces are built
  Patches/MapProviderPatches.cs Generate postfix, ListenKeyboardEvents postfix
  Patches/NewMapPatches.cs      GenerateTileConfig postfix: a fresh zee starts unmarked
  UI/ChartMarker.cs             the icon on the chart
  UI/ChartClicks.cs             right-click handling on the open chart
  UI/ShipIndicator.cs           the bat by the ship
  UI/Sprites.cs                 the art: borrowed from the game, drawn around it, or drawn in code
PhilanthropicPhyllonycteris.Tests/    net8.0 xunit, references Core only
```

GUID `nianeyna.PhilanthropicPhyllonycteris`. Code style as in the siblings: nullable on, `region` comments for sections, game privates touched only through `MapAccess`.

## Design

### Reaching into MapProvider

`MapAccess` holds `AccessTools.FieldRefAccess` for `_mapRoot`, `_mapCamera`, `_icons`, `_mapWidth`, `_mapHeight`, resolved once in `Awake`; a missing field logs one error and disables the mod rather than throwing per frame.

Patches (both postfixes, nothing of the game's is skipped):

- `MapProvider.Generate`: the zee has just been (re)built. Compute the fingerprint of `CurrentCharacter.TileConfig`, ask `MarkerStore` for that map's marker, build the `ChartMarker` under `_mapRoot` if there is one. The early-out branch ("Map could not be found") leaves `_mapRoot` null; check for it.
- `MapProvider.ListenKeyboardEvents`: when `InMap` and a right-click went down this frame, run the click handler below.

### Click handling

1. Ignore if `EventSystem.current.IsPointerOverGameObject()`. **To verify:** the chart's labels are uGUI `Text` on world-space canvases; if they are raycast targets they would swallow clicks on label text. If so, replace the check with `EventSystem.RaycastAll` filtered to `Selectable`s.
2. Pointer to chart local point (see facts table). Reject if outside the map rectangle.
3. If a marker exists and the click is within `MarkerHitTest` radius of it (compare in screen space via `_mapCamera.WorldToScreenPoint`), remove it; otherwise set the marker to `ChartSpace.ToWorld(local)`.
4. `MarkerStore` saves and raises `Changed`; `ChartMarker` and `ShipIndicator` react to that, not to the click.

### ChartMarker

A GameObject under `_mapRoot` at `(world.x/10, world.y/10, -0.5)` with a `SpriteRenderer`. Added to `MapProvider._icons` so the game's own `ResizeMapItems` keeps it a constant on-screen size (removed from the list before it is destroyed). Layer, sorting layer and order are copied from the boat icon's renderer so the map camera draws it and the sailing camera does not. **To verify:** what `Map/Icons/Ico_port` is made of (SpriteRenderer or mesh quad) and which layer it is on; one hierarchy dump behind a `Debug.DumpMapIcons` config entry settles it, and the finding goes in CLAUDE.md.

### ShipIndicator

A root world-space GameObject with a `SpriteRenderer`, driven from `Plugin.LateUpdate` so it reads the ship's position after the ship has moved:

```
visible = marker != null && Boat != null && !InDock && !InMap && distance > HideWithin
position = boat.xy + direction * RingRadius      (z and layer copied from the ship's renderer)
rotation = Euler(0, 0, bearingDegrees)           (sprite drawn pointing +X)
```

It dies with the zee scene; the Unity-null check recreates it lazily. Alpha eases in and out over a fraction of a second rather than popping (`Time.unscaledDeltaTime`, so it still fades if the clock is stopped).

**To verify in game:** that the sailing camera never rotates (the ring is world-aligned, which is only right if north stays up); a ring radius that clears the ship sprite and its light cone at both zoom levels (start at 45 world units against an ortho size of 150 to 200); visibility and sort order while submerged in Zubmariner; that `InDock` is false in every at-zee state where the arrow should show (it is set at `NavigationProvider` 390/966/1088).

### Sprites

No art pipeline exists in the sibling mods. The chart marker is the game's own zee-bat icon (`Map/Icons/Ico_bat`): `Sprites` copies its pixels by way of a `RenderTexture` (the game's textures are not CPU-readable) and bleaches the green disc to white, so the renderer's tint (the yellow of the boat icon's disc, `B78639`) decides the disc and the bat stays black. The indicator by the ship uses the black bat of the game's `Sea/BatSignal` prefab as it is. The light around it is made from the bat: `Sprites.Glow` copies the bat's alpha, and `GlowField` (Core) turns it into a white sprite that is full strength under the bat and eases to nothing a set distance from its outline (an exact distance transform, worked out at no more than 128 px across), which takes the config colour. The prefab's own white disc of light looked pasted on under the bat; it is kept only as the fallback if the bat's pixels cannot be read. The stand-ins for when the game's art cannot be had, a cross for the marker and a chevron for the indicator, are drawn in code (antialiased, white, tinted the same way). If hand-drawn art is wanted later, swap the bodies for embedded PNGs through `ImageConversion.LoadImage` (adds a reference to `UnityEngine.ImageConversionModule`); nothing else changes.

### Persistence

One file, `BepInEx/config/PhilanthropicPhyllonycteris.markers.txt`, one line per map:

```
PhilanthropicPhyllonycteris markers v1
<fingerprint>\t<x>\t<y>\t<utc set>
```

- Keyed by **map fingerprint**, not captain or save name: FNV-1a over width, height and the ordered tile names (not `string.GetHashCode`, which is not stable across runtimes). A captain's autosave and named saves then share a marker, and an heir who inherits the chart inherits the mark, while a reshuffled zee starts clean. Known cost: loading an older save of the same captain shows the newest marker. Accepted.
- A freshly generated zee never has a marker: a postfix on `CharacterRepository.GenerateTileConfig()` (called for a new game and for an heir without the chart; an inherited chart bypasses it via `Chart ?? GenerateTileConfig()`) removes any stored line for the new config's fingerprint. Without it, an heir who rolled the same layout as a past captain (about 1 in 6 billion for the default rules: 24 cells drawn without repeats from five sets, before tile versions) would see that captain's marker.
- Invariant-culture floats. Unparseable lines are dropped and the original copied to `.damaged` first, as SunlessSee's seen record does. Written on every change (changes are rare, so no debounce), to a sibling file that is swapped in.
- Lines for other maps are preserved untouched; nothing ever prunes them (a line is ~60 bytes).

### Config

| Section.Key | Default | Purpose |
|---|---|---|
| `Indicator.Enabled` | true | Turn the bat off and keep the chart marker |
| `Indicator.RingRadius` | 90 | Distance of the bat from the ship, world units |
| `Indicator.HideWithin` | 100 | Hide the bat when the marker is closer than this |
| `Indicator.Colour` | white, `FFFFFFE6` | Colour and strength of the light around the bat |
| `Debug.DumpMapIcons` | false | Log the chart icon prefabs' components, layers and sorting once |

No `SettingChanged` handling: the indicator reads every setting each frame, so they all apply live.

## Tests (Core only)

Worth having, and nothing beyond:

- `ChartSpace`: chart/world round trip; bounds rejection at the edges.
- `MapFingerprint`: same config gives the same value (pinned literal, to catch an accidental algorithm change that would orphan everyone's markers); swapping two tiles changes it.
- `MarkerFile`: round trip; clearing a fingerprint (the fresh-zee rule) removes only that line; other maps' lines survive a set and a remove; a damaged line is reported and the rest kept; decimal comma cultures do not change the output.
- `IndicatorMath`: bearing for the four quadrants; position lies on the ring; hidden inside `HideWithin`.
- `MarkerHitTest`: inside and outside the radius.

Everything holding a GameObject is checked by hand in the game, as in the sibling mods.

## Milestones

1. **Scaffold.** ✅ Done (2026-09-19; load line confirmed in the BepInEx log). Solution, props, three projects, plugin loads and logs its version. README and CLAUDE.md stubs.
2. **Core + tests.** ✅ Done (2026-09-19; 33 tests green). The Core files and their tests, green.
3. **Chart marker.** ✅ Done (2026-09-19). Confirmed in game: shows over fog, place, move, remove, constant size under zoom, survives quit and reload, chart controls unaffected. Both chart verifications are settled by a `DumpMapIcons` capture (recorded in CLAUDE.md): labels are raycast targets, so clicks are filtered by `RaycastAll` for `Selectable`s; the fog is on its own sorting layer, so the marker sorts on the boat icon's layer, one above the boat. The captain and heir checks are in Deferred tests. `MapAccess`, both patches, click handling, `ChartMarker`, `MarkerStore`. Resolve the two chart verifications (icon composition/layer, label raycasts). Check: place, move, remove, zoom scaling, survives quit and reload, separate captains with different maps do not share it, an heir with the chart keeps it and one without starts clean, chart controls still work.
4. **Ship indicator.** ✅ Done (2026-09-19; tracking, radius, size, hiding in port / on the chart / on arrival and reappearing all confirmed in game). As built: the arrow sorts on "AboveLights" (where the game puts its own range indicators, so the darkness overlay does not cover it); the boat is looked up by name once a second rather than through `NavigationProvider.Boat`, which searches the scene on every call while there is no boat; a `Debug.DumpShip` capture covers the zee verifications. The camera-rotation worry below is moot: the arrow is a world-space object, so it is right on screen however the camera is turned. `ShipIndicator` and the `LateUpdate` tick. Resolve the zee verifications (camera rotation, radius, submerged, dock states). Check: arrow tracks while circling the marker, hides in port / on the chart / on arrival, reappears after undocking and after a scene reload.
5. **Polish.** Fades, colours, final sprite shapes, README, record the measured facts in CLAUDE.md.

## Deferred tests

In-game checks that are tedious to set up, so they wait for a convenient moment rather than blocking a milestone. Until then the behaviour rests on the Core tests and on reading the game's code.

- **A captain on a different map does not see the marker.** Mark the chart, start a new captain (fresh zee), open the chart: no marker, and the first captain's line is still in `PhilanthropicPhyllonycteris.markers.txt`.
- **An heir who inherits the chart keeps the marker.** Mark the chart, die, take the chart as a legacy: the marker is where it was.
- **An heir without the chart starts clean.** Same, without the chart legacy: no marker. (The identical-layout case behind `FreshZeePatch` cannot be staged by play; to exercise it, copy the new map's fingerprint onto a line in the markers file before creating the captain and check the log for "its old marker was dropped".)

