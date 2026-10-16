# PhilanthropicPhyllonycteris

The Philanthropic Phyllonycteris (or Friendly Flower-Bat, when she's at home) is a rather more helpful than usual zee-bat who will point your way at zee. Right-click anywhere on the chart to plan a route; while there are any route markers and you are out of port, she'll fly ahead of you and show the way to the next one.

Code written primarily with AI tools for expediency; however, for the record the name "Philanthropic Phyllonycteris" is entirely down to me and my ill-advised thesaurus habit. I hope you enjoy the mod, and don't judge me too harshly for making it unspellable.

## Using it

- **Place a marker:** open the chart and right-click anywhere on the map, explored or not. Each right-click adds another to the end of the route, and a line runs from your ship through them in order.
- **Move one:** hold the right button on a marker and drag it.
- **Remove one:** right-click the marker. The route closes up around it.
- **Clear the route:** double right-click any marker.

While there are markers, a bat with a pale light around it hovers near your ship at zee, on the side the next marker is on and heading toward it. It hides in port, while the chart is open, and once you are nearly on top of the marker. When you arrive, that marker is removed for you and the bat turns toward the next.

The route is remembered between sessions, shared by every save of the same zee, and passed on to an heir who inherits the chart. A newly generated zee always starts unmarked.

## Requirements

- Sunless Sea 3.x (Steam). The Windows build, the native Linux build and the Windows build under Proton all work; only the BepInEx install differs.
- [BepInEx 5.4.23.5](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.5).

## Installing

Get the zip from the [latest release](https://github.com/nianeyna/PhilanthropicPhyllonycteris/releases/latest) (or build it, below). It is laid out as `BepInEx/plugins/PhilanthropicPhyllonycteris/`, so it extracts straight onto the game folder; `PhilanthropicPhyllonycteris.dll` and `PhilanthropicPhyllonycteris.Core.dll` end up in that folder.

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
| `[Indicator] HideWithin` | 100 | The bat hides once the next marker is closer than this, and the marker counts as reached. Keep it above `RingRadius`. A marker placed closer than this stays until the ship has left and come back |
| `[Indicator] Colour` | `FFFFFFE6` | Colour of the light around the bat; the last pair is how strong it is |
| `[Debug] DumpShip` | false | Development aid: logs how the ship and the sailing camera are built the first time the ship is found |
| `[Debug] DumpMapIcons` | false | Development aid: logs how the chart's own icons, fog and labels are built the first time the chart opens |

Markers are kept in `BepInEx/config/PhilanthropicPhyllonycteris.markers.txt`, one line per marker, each map's in route order.

## Uninstalling

Delete the plugin's folder from `BepInEx/plugins` with the game closed. Nothing is written to your saves, so they are unaffected; the markers file can be deleted too.

## Building from source

Needs the .NET 8 SDK and a Sunless Sea install with BepInEx already in it; the plugin references the game's assemblies and BepInEx in place.

```sh
dotnet build PhilanthropicPhyllonycteris.sln -c Release
dotnet test PhilanthropicPhyllonycteris.sln
```

The build copies both DLLs straight into `<game>/BepInEx/plugins/PhilanthropicPhyllonycteris/`. The game folder defaults to `~/.local/share/Steam/steamapps/common/SunlessSea`; override it with the `SUNLESS_SEA_DIR` environment variable or `-p:GameDir=...`.

The version comes from the git tag; an untagged build reports itself as the next patch with a pre-release suffix. Releases are cut as described in [RELEASING.md](RELEASING.md).
