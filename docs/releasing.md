# Releasing

This page is for maintainers who publish the packages to nuget.org. To contribute a change, see
[`CONTRIBUTING.md`](../CONTRIBUTING.md); nothing here is needed for a pull request.

## Two packages, two tags

The two packages are released separately, each by a tag of its own:

```bash
git tag lib-v1.2.3 && git push origin lib-v1.2.3   # Slugger.Core, the library
git tag cli-v1.2.3 && git push origin cli-v1.2.3   # Slugger, the slugger command
```

The package ids are not the project names: the tool, from `Slugger.Cli`, is `Slugger`, and the
library, from `Slugger`, is `Slugger.Core`
([DEC0033](idr/DEC0033-identifiants-de-paquet-inverses.md)), so that the short id is what a NuGet
search for the name finds. [Below](#the-two-ids-changed-hands) is why the library must never be
published under `Slugger` again, and what the switch to the new ids established.

The version comes from the tag, or from the version you type in a manual run. The `<Version>` in
`Directory.Build.props` only names local builds: the release workflow passes its own version to the
build and to the pack, so no published package ever carries that one.

## What `release.yml` does

1. It refuses a tag whose commit is not on `main`. A tag push bypasses branch protection, and a
   version on nuget.org can never be replaced.
2. It checks that the version is SemVer without `+build` metadata.
3. It builds, runs the whole suite again and packs that package alone. A `cli` release also
   compiles the command natively for Linux x64, `slugger-linux-x64`, and writes its SHA-256 beside
   it ([DEC0032](idr/DEC0032-ligne-de-commande-et-rendu-sans-spectre.md)). `ci.yml` compiles and
   runs that native command on every pull request, so a change that breaks native compilation fails
   there rather than on the day of a tag.
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
the nuget.org login, and stops before the push and the GitHub release. Both trains have published
since — `cli-v1.2.0` and `lib-v1.0.0`, the first under the ids of DEC0033 — so the push is proven
too.

## `Slugger.Core` is stable

`Slugger.Core` is stable since `lib-v1.0.0`, and every `1.x` keeps the contract of
[DEC0037](idr/DEC0037-contrat-de-compatibilite-de-la-1x.md): a change to a public signature, an
error code or a floor waits for a `2.0.0`. `PublicSurfaceTests` and `CompatibilityContractTests` are
where such a change shows first.

## The two ids changed hands

Up to `cli-v1.1.0` and `lib-v1.0.0-preview.1`, the tool was published as `Slugger.Cli` and the
library as `Slugger`. Since `cli-v1.2.0` and `lib-v1.0.0` they are `Slugger` and `Slugger.Core`
([DEC0033](idr/DEC0033-identifiants-de-paquet-inverses.md)), and `Slugger.Core 1.0.0` is the
library's first stable version, under the 1.x contract of
[DEC0037](idr/DEC0037-contrat-de-compatibilite-de-la-1x.md).

What the switch established, measured on 2026-11-11:

- nuget.org accepts a `DotnetTool` version under an id whose earlier version is a library: the push
  of `Slugger 1.2.0` answered `201 Created`, and `dotnet tool install Slugger` installs it;
- a new version took about fourteen minutes to appear in the package content, and the registration
  index the client reads followed a minute later. A client that asked for the id in between keeps
  its cached list of versions for up to thirty minutes: `dotnet nuget locals http-cache --clear`
  ends the wait.

On nuget.org, `Slugger 1.0.0-preview.1` is deprecated as *Legacy* with `Slugger.Core` as its
alternate package; every version of `Slugger.Cli` is deprecated the same way, with `Slugger`. The
id `Slugger.Cli` is retired: nothing is published under it again. The registration index says
which versions carry a deprecation, without signing in:

```bash
curl -s --compressed https://api.nuget.org/v3/registration5-gz-semver2/slugger.cli/index.json
```

**Never publish the library under `Slugger` again**, and never a tool version of `Slugger` at or
below `1.0.0-preview.1`. NuGet clients pick the highest version of an id first and look at its
package type afterwards: a library version above the tool's would turn every
`dotnet tool install Slugger` into an `NU1212`, and the reverse is what a stale `dotnet add package
Slugger` now meets — by design.

## Settings outside the repository

Two settings live outside the repository, and every release run, dry run included, fails at the
login step without them:

- a trusted-publishing policy on nuget.org (*Account settings → Trusted Publishing*) for
  repository owner `Reefact`, repository `slugger` and workflow file `release.yml`, with no
  environment. Its glob is `*` and its scope *Push new packages and package versions*, so one
  policy covers both trains — and that scope is what lets a first push create an id that does not
  exist yet, which is the one thing a rehearsal cannot establish, since it stops before the push;
- a repository **variable** (not a secret) `NUGET_USER`, holding the nuget.org username of whoever
  **created the policy** — which is not necessarily the package owner. Measured: `Reefact` is what
  the policy page shows as *Package owner*, and it fails the exchange with `HTTP 401 [...] No
  matching trust policy owned by user 'Reefact' was found`. nuget.org names the distinction in the
  error itself, and `NuGet/login` documents the input as the account username. Stored as a secret
  rather than a variable, it reads back empty, and the login fails for that reason instead.

## Icons

`assets/icon.svg` is the drawing — a fielder's glove with the ball in its pocket. What the build
packs is rendered from it and committed, since nothing in a .NET build rasterises an SVG:
`assets/icon.png`, the NuGet icon of both packages, and `assets/slugger.ico`, which
`ApplicationIcon` compiles into `Slugger.Cli.dll` as the command's own. Edit the SVG and regenerate
both, never a bitmap by hand:

```bash
NODE_PATH="$(npm root -g)" node assets/render-icons.cjs   # Playwright's Chromium does the drawing
```

The `.ico`'s 16 to 32 pixels come from `assets/icon-small.svg` instead — the same glove without its
grooves, highlights and stitches, which at those sizes only blur the shape. A change to the glove's
outline is made in both files.
