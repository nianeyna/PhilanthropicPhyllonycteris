# Releasing

Releases are built on a machine with the game installed, because the plugin compiles against the game's own assemblies. The [ModRelease](https://www.nuget.org/packages/ModRelease) tool does the whole job and refuses to ship anything it cannot vouch for; `modrelease.json` tells it what ships and where the game is. CI (`.github/workflows/ci.yml`) only runs the tests.

The version comes from the git tag. There is nothing to edit in the code: MinVer reads the tag, and the build writes it into the generated `PluginInfo` class behind the `BepInPlugin` attribute. An untagged commit builds as the next patch with a pre-release suffix, e.g. `1.0.1-alpha.0.3`, which is what the game's log shows.

## Cutting a release

1. Add a `## [X.Y.Z] - YYYY-MM-DD` section with the notes to `CHANGELOG.md` and commit it. The tool takes the GitHub release notes and the Nexus changelog from that section, and refuses to run without it.

2. Tag and push the commit, with the tag:

   ```sh
   git tag vX.Y.Z && git push origin main vX.Y.Z
   ```

3. With `GITHUB_TOKEN` set to a token that can write this repository's releases (see [GitHub token](#github-token)), run:

   ```sh
   dotnet tool restore
   GITHUB_TOKEN=$(cat ~/.config/modrelease/github-token) dotnet modrelease release
   ```

   `--dry-run` does everything except the GitHub calls; `--draft` creates the release as a draft to look over.

4. Post to Nexus from `artifacts/nexus-kit-X.Y.Z/`, following its `POSTING.md`. The archive there is byte for byte the one on the GitHub release.

`dotnet modrelease package` runs the same build, checks and kit on any commit, with warnings in place of refusals, so the pipeline can be tried without tagging. The tool's README describes every check it makes.

## GitHub token

Make a fine-grained personal access token (GitHub > Settings > Developer settings > Fine-grained tokens) limited to this repository, with Contents: read and write, and an expiry.

Keep it in a file outside any repository that only you can read, and paste the token in with an editor so it never goes through a command line:

```sh
mkdir -p ~/.config/modrelease && chmod 700 ~/.config/modrelease
touch ~/.config/modrelease/github-token && chmod 600 ~/.config/modrelease/github-token
```

The release command above reads the file at the point of use, so the token is set for that one process and never appears in the shell history. Do not `export` it from `.bashrc`, which hands it to everything you run. When the token expires, make a new one and overwrite the file.

## After a game patch

The tool compares `Sunless Sea_Data/Managed/Sunless.Game.dll` against the hash in `modrelease.json` and refuses to release against a game it has not been tested with. After re-testing on the new build, record the new value:

```sh
dotnet modrelease fingerprint
```

## Nexus

`nexus/description.bbcode` is the mod page, kept separately from the README because the two read differently. Screenshots go under `nexus/images/`. Once the Nexus page exists, put its mod id into `modrelease.json` so the kit's checklist links straight to it.
