# PhilanthropicPhyllonycteris

A BepInEx 5 plugin for Sunless Sea. Right-click anywhere on the chart to drop a marker; while the marker exists and you are out of port, a bat hovering near your ship shows the way to it.

**Status:** the chart marker works; the bat by the ship is built but still being tuned. See PLAN.md for the milestones.

## Using it

Open the chart and right-click (ctrl+click on a Mac) anywhere on the map, explored or not, to put the marker there. Right-click somewhere else to move it, or right-click the marker itself to remove it. There is one marker per map. While a marker exists, a bat with a pale light around it hovers near your ship at zee, on the side the marker is on and heading toward it. It hides in port, while the chart is open, and once you are nearly on top of the marker.

The marker is remembered between sessions, shared by every save of the same zee, and passed on to an heir who inherits the chart.

## Requirements

- Sunless Sea 3.x (Steam). The Windows build, the native Linux build and the Windows build under Proton all work; only the BepInEx install differs.
- [BepInEx 5.4.23.5](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.5).

## Installing

`PhilanthropicPhyllonycteris.dll` and `PhilanthropicPhyllonycteris.Core.dll` both go in `<game>/BepInEx/plugins/PhilanthropicPhyllonycteris/`.

### Windows

1. Download BepInEx `Unity.Mono-win-x64` and extract it into the game folder (next to `Sunless Sea.exe`).
2. Copy the plugin DLLs into `BepInEx/plugins/PhilanthropicPhyllonycteris/` and launch the game normally.

### Native Linux build (Steam Linux Runtime)

1. Download BepInEx `Unity.Mono-linux-x64` and extract it into the game folder (next to `Sunless Sea.x86_64`).
2. Edit `run_bepinex.sh`: set `executable_name="Sunless Sea.x86_64"`, then `chmod u+x run_bepinex.sh`.
3. In Steam, set the game's launch options to `./run_bepinex.sh %command%`.
4. Copy the plugin DLLs into `BepInEx/plugins/PhilanthropicPhyllonycteris/`.

### Windows build under Proton

1. Download BepInEx `Unity.Mono-win-x64` and extract it into the game folder.
2. In Steam, set the game's launch options to `WINEDLLOVERRIDES="winhttp=n,b" %command%`.
3. Copy the plugin DLLs into `BepInEx/plugins/PhilanthropicPhyllonycteris/`.

After the first launch, `BepInEx/LogOutput.log` in the game folder contains a "PhilanthropicPhyllonycteris ... loaded" line.

## Configuration

Edit `BepInEx/config/nianeyna.PhilanthropicPhyllonycteris.cfg` (created on first launch). Every setting applies live, without restarting the game.

| Setting | Default | Effect |
|---|---|---|
| `[Indicator] Enabled` | true | Show the bat by the ship. Off keeps only the chart marker |
| `[Indicator] RingRadius` | 90 | How far from the ship the bat hovers, in zee units (the view is 300 to 400 tall) |
| `[Indicator] HideWithin` | 100 | The bat hides once the marker is closer than this. Keep it above `RingRadius` |
| `[Indicator] Colour` | `FFFFFFE6` | Colour of the light around the bat; the last pair is how strong it is |
| `[Marker] ClearOnArrival` | true | Remove the marker once the ship comes within `HideWithin` of it. A marker placed closer than that stays until the ship has left and come back |
| `[Debug] DumpShip` | false | Development aid: logs how the ship and the sailing camera are built the first time the ship is found |
| `[Debug] DumpMapIcons` | false | Development aid: logs how the chart's own icons, fog and labels are built the first time the chart opens |

Markers are kept in `BepInEx/config/PhilanthropicPhyllonycteris.markers.txt`.

To turn the plugin off, delete its folder from `BepInEx/plugins` with the game closed.

## Building from source

Needs the .NET 8 SDK and a Sunless Sea install with BepInEx already in it; the plugin references the game's assemblies and BepInEx in place.

```sh
dotnet build PhilanthropicPhyllonycteris.sln -c Release
dotnet test PhilanthropicPhyllonycteris.sln
```

The build copies both DLLs straight into `<game>/BepInEx/plugins/PhilanthropicPhyllonycteris/`. The game folder defaults to `~/.local/share/Steam/steamapps/common/SunlessSea`; override it with the `SUNLESS_SEA_DIR` environment variable or `-p:GameDir=...`.
