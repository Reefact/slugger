# Releasing

This page is for maintainers who publish the packages to nuget.org. To contribute a change, see
[`CONTRIBUTING.md`](../CONTRIBUTING.md); nothing here is needed for a pull request.

## Two packages, two tags

The two packages are released separately, each by a tag of its own:

```bash
git tag lib-v1.2.3 && git push origin lib-v1.2.3   # Slugger.Core, the library
git tag cli-v1.2.3 && git push origin cli-v1.2.3   # Slugger, the slugger command
```

The version comes from the tag, or from the version you type in a manual run. The `<Version>` in
`Directory.Build.props` only names local builds: the release workflow passes its own version to the
build and to the pack, so no published package ever carries that one.

## What `release.yml` does

1. It refuses a tag whose commit is not on `main`. A tag push bypasses branch protection, and a
   version on nuget.org can never be replaced.
2. It checks that the version is SemVer without `+build` metadata.
3. It builds, runs the whole suite again and packs that package alone. A `cli` release also
   compiles the command natively for Linux x64, `slugger-linux-x64`, and writes its SHA-256 beside
   it ([DEC0032](idr/DEC0032-ligne-de-commande-et-rendu-sans-spectre.md)).
4. It attests the provenance of the packages it built, and of the native command.
5. It logs in to nuget.org through OIDC trusted publishing — no API key is stored anywhere — and
   pushes.
6. It creates the GitHub release with the packages attached — and the native command with its
   checksum, for a `cli` release — marked as a prerelease when the
   version has a `-` label. The notes are generated from the titles of the pull requests merged
   since the previous release, of either package, which is why pull request titles follow
   Conventional Commits.

## Rehearse first

Run the workflow by hand (*Actions → release → Run workflow*), choosing the package and a version —
`0.0.0-dry.1` will do. *Dry run* is ticked by default: the run does everything up to and including
the nuget.org login, and stops before the push and the GitHub release.

## When `Slugger.Core` can be stable

Nothing in the packaging holds `Slugger.Core` in preview any more. NuGet refuses to pack a stable
package with a prerelease dependency (warning NU5104, an error under the CI ratchet), and the engine
used to depend on a prerelease `FirstClassErrors`; since
[DEC0029](idr/DEC0029-modele-d-erreurs-propre-au-moteur.md) it carries its own error model and
depends on `Value` alone, and a pack at `1.0.0` succeeds. What keeps it in preview is a decision:
the public surface a `1.0.0` would freeze is not settled yet
([#63](https://github.com/Reefact/slugger/issues/63)).

The first `Slugger.Core` is meant to be that `1.0.0` (below), so settling #63 is what the library's
first release under its new id waits for. The id carries no preview: `Slugger 1.0.0-preview.1` is
the library's only prerelease, published under the id it is leaving.

## The two ids changed hands

Up to `cli-v1.1.0` and `lib-v1.0.0-preview.1`, the tool was published as `Slugger.Cli` and the
library as `Slugger`. The projects now pack them as `Slugger` and `Slugger.Core`
([DEC0033](idr/DEC0033-identifiants-de-paquet-inverses.md)). The first release under the new ids
takes these steps, in this order:

1. **Tag `cli-v1.2.0`: the first `Slugger` that is a tool.** It goes first because it is the one
   step nobody has measured: no rule of nuget.org, documented or in its source, refuses a
   `DotnetTool` version under an id whose earlier version is a library, but no push has proven it
   either. Nothing has been published or deprecated yet when it runs, so a refusal leaves nothing
   half done — see below. From then on an unpinned `dotnet add package Slugger` picks the tool,
   which is why the library's install examples pin `--version 1.0.0-preview.1` until step 5.
2. Tag `lib-v1.0.0`: the first `Slugger.Core`, and the library's first stable version — once
   [#63](https://github.com/Reefact/slugger/issues/63) has settled the public surface it freezes.
   Nothing in steps 1, 3 and 4 depends on it but step 3, which needs `Slugger.Core` to exist.
3. On nuget.org, deprecate the library version `Slugger 1.0.0-preview.1` as *Legacy*, with
   `Slugger.Core` as the alternate package. That is what a project still referencing `Slugger` is
   shown. The id `Slugger` itself lives on, as the tool's.
4. On nuget.org, deprecate every version of `Slugger.Cli` as *Legacy*, with `Slugger` as the
   alternate package. That id is retired: nothing is published under it again.
5. In one pull request, switch what a reader installs from — the badges and install commands of
   `README.md`, `docs/cli.md` and `docs/library.md` — and `dotnet-tools.json`. Not before step 1
   and 2 succeed: until then, those instructions would name packages nuget.org cannot serve. That
   pull request says:
   - for a **tool manifest**, the key and the version move together: `dotnet tool uninstall
     --local Slugger.Cli`, then `dotnet tool install --local Slugger --version 1.2.0`. Renaming
     the key `slugger.cli` alone asks for `Slugger 1.0.0`, which was never published;
   - for the **command**, through version `1.1.0` it was published as `Slugger.Cli`, and starting
     with `1.2.0` it installs as `Slugger`; an install of `Slugger.Cli` stays on `1.1.0`;
   - for the **library**, the move from `Slugger 1.0.0-preview.1` to `Slugger.Core 1.0.0` is more
     than a rename: it crosses the API changes listed under *Stability* in `docs/library.md` —
     `FirstClassErrors` replaced by `Slugger.Domain.Errors`, the vocabulary types made internal —
     and links there. The install command loses its pin and its `--prerelease`:
     `dotnet add package Slugger.Core`. *Stability* stops calling the library a preview.

**Never publish the library under `Slugger` again**, and never a tool version of `Slugger` at or
below `1.0.0-preview.1`. NuGet clients pick the highest version of an id first and look at its
package type afterwards: a library version above the tool's would turn every
`dotnet tool install Slugger` into an `NU1212`, and the reverse is what a stale `dotnet add package
Slugger` now meets — by design.

### If nuget.org refuses step 1

The push fails and nothing is published. `main` still packs the tool as `Slugger`, so the next
`cli` tag would only try the refused id again. Before any other release:

- set `PackageId` back to `Slugger.Cli` in `Slugger.Cli.csproj`, and its package page and the
  release notes back to that id;
- decide what the library becomes. Nothing is lost by keeping `Slugger.Core`, but the reason for
  it is gone: setting it back to `Slugger` restores the ids as they were;
- skip steps 3 and 4: neither old id is replaced;
- add a status line to DEC0033 saying the decision could not be applied, and why.

## Settings outside the repository

Two settings live outside the repository, and every release run, dry run included, fails at the
login step without them:

- a trusted-publishing policy on nuget.org for repository owner `Reefact`, repository `slugger` and
  workflow file `release.yml`, with no environment;
- a repository **variable** (not a secret) `NUGET_USER`, holding the nuget.org username of whoever
  created that policy — which is not necessarily the package owner.

[`CLAUDE.md`](../CLAUDE.md) records how each of these was established, failures included.
