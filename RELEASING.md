# Releasing

Releases are built on a machine with the game installed, because the plugin compiles against the game's own assemblies. The [ModRelease](https://www.nuget.org/packages/ModRelease) tool does the whole job and refuses to ship anything it cannot vouch for; `modrelease.json` tells it what ships and where the game is. CI (`.github/workflows/ci.yml`) only runs the tests.

The version comes from the git tag. There is nothing to edit in the code: MinVer reads the tag, and the build writes it into the generated `PluginInfo` class behind the `BepInPlugin` attribute. An untagged commit builds as the next patch with a pre-release suffix, e.g. `1.0.1-alpha.0.3`, which is what the game's log shows.

## Cutting a release

1. Add a `## [X.Y.Z] - YYYY-MM-DD` section with the notes to `CHANGELOG.md` and commit it. The tool takes the GitHub release notes and the Nexus changelog from that section, and refuses to run without it.

2. Tag and push the commit, with the tag:

   ```sh
   git tag vX.Y.Z && git push origin main vX.Y.Z
   ```

3. With `GITHUB_TOKEN` set to a token that can write this repository's releases, run:

   ```sh
   dotnet tool restore
   dotnet modrelease release
   ```

   `--dry-run` does everything except the GitHub calls; `--draft` creates the release as a draft to look over.

4. Post to Nexus from `artifacts/nexus-kit-X.Y.Z/`, following its `POSTING.md`. The archive there is byte for byte the one on the GitHub release.

`dotnet modrelease package` runs the same build, checks and kit on any commit, with warnings in place of refusals, so the pipeline can be tried without tagging. The tool's README describes every check it makes.

## After a game patch

The tool compares `Sunless Sea_Data/Managed/Sunless.Game.dll` against the hash in `modrelease.json` and refuses to release against a game it has not been tested with. After re-testing on the new build, record the new value:

```sh
dotnet modrelease fingerprint
```

## Nexus

`nexus/description.bbcode` is the mod page, kept separately from the README because the two read differently. Screenshots go under `nexus/images/`. Once the Nexus page exists, put its mod id into `modrelease.json` so the kit's checklist links straight to it.
