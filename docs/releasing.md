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

## The two ids changed hands

Up to `cli-v1.1.0` and `lib-v1.0.0-preview.1`, the tool was published as `Slugger.Cli` and the
library as `Slugger`. They are now `Slugger` and `Slugger.Core`
([DEC0033](idr/DEC0033-identifiants-de-paquet-inverses.md)). The first release under the new ids
takes four steps, in this order:

1. Tag `lib-v1.0.0-preview.2`: the first `Slugger.Core`.
2. On nuget.org, deprecate `Slugger 1.0.0-preview.1` as *Legacy*, with `Slugger.Core` as the
   alternate package. That is what a project still referencing `Slugger` is shown.
3. Tag `cli-v1.2.0`: the first `Slugger` that is a tool.
4. On nuget.org, deprecate every version of `Slugger.Cli` as *Legacy*, with `Slugger` as the
   alternate package.

**Never publish the library under `Slugger` again**, and never a tool version of `Slugger` at or
below `1.0.0-preview.1`. NuGet clients pick the highest version of an id first and look at its
package type afterwards: a library version above the tool's would turn every
`dotnet tool install Slugger` into an `NU1212`, and the reverse is what a stale `dotnet add package
Slugger` now meets — by design.

Step 3 is the one nobody has measured: no rule of nuget.org, documented or in its source, refuses a
`DotnetTool` version under an id whose earlier version is a library, but no push has proven it
either. If nuget.org refuses it, nothing is published; the command then stays `Slugger.Cli`, and
DEC0033 gains a status line saying so.

## Settings outside the repository

Two settings live outside the repository, and every release run, dry run included, fails at the
login step without them:

- a trusted-publishing policy on nuget.org for repository owner `Reefact`, repository `slugger` and
  workflow file `release.yml`, with no environment;
- a repository **variable** (not a secret) `NUGET_USER`, holding the nuget.org username of whoever
  created that policy — which is not necessarily the package owner.

[`CLAUDE.md`](../CLAUDE.md) records how each of these was established, failures included.
