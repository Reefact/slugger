# Releasing

This page is for maintainers who publish the packages to nuget.org. To contribute a change, see
[`CONTRIBUTING.md`](../CONTRIBUTING.md); nothing here is needed for a pull request.

## Two packages, two tags

The two packages are released separately, each by a tag of its own:

```bash
git tag lib-v1.2.3 && git push origin lib-v1.2.3   # Slugger, the library
git tag cli-v1.2.3 && git push origin cli-v1.2.3   # Slugger.Cli, the slugger command
```

The version comes from the tag, or from the version you type in a manual run. The `<Version>` in
`Directory.Build.props` only names local builds: the release workflow passes its own version to the
build and to the pack, so no published package ever carries that one.

## What `release.yml` does

1. It refuses a tag whose commit is not on `main`. A tag push bypasses branch protection, and a
   version on nuget.org can never be replaced.
2. It checks that the version is SemVer without `+build` metadata.
3. It builds, runs the whole suite again and packs that package alone.
4. It attests the provenance of the packages it built.
5. It logs in to nuget.org through OIDC trusted publishing — no API key is stored anywhere — and
   pushes.
6. It creates the GitHub release with the packages attached, marked as a prerelease when the
   version has a `-` label. The notes are generated from the titles of the pull requests merged
   since the previous release, of either package, which is why pull request titles follow
   Conventional Commits.

## Rehearse first

Run the workflow by hand (*Actions → release → Run workflow*), choosing the package and a version —
`0.0.0-dry.1` will do. *Dry run* is ticked by default: the run does everything up to and including
the nuget.org login, and stops before the push and the GitHub release.

## Why `Slugger` stays a prerelease

`Slugger` depends on a prerelease version of `FirstClassErrors`, and NuGet refuses to pack a stable
package with a prerelease dependency (warning NU5104, an error under the CI ratchet): `lib-v1.0.0`
fails at the pack step, `lib-v1.0.0-preview.1` does not. `Slugger.Cli` bundles its dependencies and
can be stable. The constraint comes from the dependency, not from a setting in this repository; it
lifts when `FirstClassErrors` ships a stable version.

## Settings outside the repository

Two settings live outside the repository, and every release run, dry run included, fails at the
login step without them:

- a trusted-publishing policy on nuget.org for repository owner `Reefact`, repository `slugger` and
  workflow file `release.yml`, with no environment;
- a repository **variable** (not a secret) `NUGET_USER`, holding the nuget.org username of whoever
  created that policy — which is not necessarily the package owner.

[`CLAUDE.md`](../CLAUDE.md) records how each of these was established, failures included.
