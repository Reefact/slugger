# zeroslugger: a size experiment in Zero C#

A Docker-style name generator (`humming_dirac`) written in [Zero C#](https://github.com/OnurGumus/zero-csharp):
C# compiled to a small native program, with no .NET runtime and no garbage collector.

**This is an experiment, not a second implementation of slugger.** It exists to answer one question:
how small can the part of slugger that matters to an agent or a CI job — draw a name — get? The
`slugger-linux-x64` command compiled with NativeAOT weighs about 4.8 MB (`cli-v1.2.1`).

## What it does, and what it leaves out

It draws `adjective<sep>noun` from the words of the built-in `docker` theme, baked in by
[`generate-words.sh`](generate-words.sh):

```console
$ zeroslugger --count 3 --seed 42
$ zeroslugger --sep -
```

It leaves out everything that needs what Zero C# does not have:

| slugger | here |
| --- | --- |
| JSON themes, `--register`, `--theme-dir` | the docker words only, generated at build time |
| categories, participles, floors, analysis | none: any adjective goes with any noun |
| `--ascii`, `--casing`, `--max-length`, `--token-*` | `--sep` only |
| reflection-based command line, `TextCopy`, XDG config | a hand-written loop over `args` |
| exceptions turned into `Outcome<T>` | no exception is caught: a refused command line exits with 2 |

A seed gives other names than `slugger --seed` does: Zero C# brings its own generator (xoshiro256**).

## Build

The project uses the `ZeroCSharp.Sdk` NuGet package, so the .NET 10 SDK is all it needs:

```bash
cd experiments/zero-csharp
dotnet build -c Release
```

Its own empty `Directory.Build.props` and `Directory.Packages.props` keep the repository's analyzers,
warning ratchet and central package versions away from it. It is not in `slugger.slnx`, so nothing
in the normal build, test or release reads it.

The `zero-csharp` workflow builds it and writes its size to the run's summary.

## Licence

This directory is Apache-2.0 like the rest of the repository. Zero C# is built on a fork of
[bflat](https://flattened.net/), which is AGPL-3.0, and uses it as a compiler only: the programs link
`zerocore`, which is MIT. Its README says `zerocore` was not written in a clean room and recommends a
review before any commercial product. That review has not been done here.

## Status

Written without a .NET SDK at hand: **it has not been compiled by its author.** The first run of the
workflow is the first time anyone builds it, so expect a fix or two.
