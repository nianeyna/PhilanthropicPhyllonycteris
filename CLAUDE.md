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

## Verified in game

Nothing yet. Record each "to verify" item from PLAN.md here as it is measured.
