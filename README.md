<img src="assets/icon.png" alt="" width="96" align="right">

# slugger

Readable random names, Docker- and Heroku-style, for containers, git branches and test data.

```console
$ slugger --theme docker
humming_dirac
```

Not a URL slugifier: to turn `Hello World` into `hello-world`, see
[Slugify.Core](https://www.nuget.org/packages/Slugify.Core).

[![ci](https://github.com/Reefact/slugger/actions/workflows/ci.yml/badge.svg)](https://github.com/Reefact/slugger/actions/workflows/ci.yml)
[![Slugger.Cli on NuGet](https://img.shields.io/nuget/v/Slugger.Cli?label=Slugger.Cli)](https://www.nuget.org/packages/Slugger.Cli)
[![Slugger on NuGet](https://img.shields.io/nuget/vpre/Slugger?label=Slugger)](https://www.nuget.org/packages/Slugger)

## Install

The command (needs .NET 10):

```bash
dotnet tool install --global Slugger.Cli
```

The library (preview):

```bash
dotnet add package Slugger --prerelease
```

```csharp
using Slugger;
using Slugger.Domain;
using Slugger.Domain.Generation;

ThemeDocument docker = Themes.LoadEmbedded("docker");
string name = SlugGenerator.Generate(docker, GenerationOptions.Default.WithDefaultsOf(docker));
```

## Why slugger

- Adjectives fit their nouns: `singing-river`, never `singing-moon`.
- Themes are plain JSON files: three built in, [more in this repository](themes/).
- A theme too thin to stay varied is refused before the first draw.
- `--max-length` and `--ascii` make names fit a DNS label or a git branch.

## Documentation

| To | Read |
| --- | --- |
| use the command | [docs/cli.md](docs/cli.md) |
| use the library | [docs/library.md](docs/library.md) |
| write a theme | [docs/writing-a-theme.md](docs/writing-a-theme.md) |
| contribute | [CONTRIBUTING.md](CONTRIBUTING.md) |

Everything else — architecture, vocabulary, decision records — is linked from those pages.

## License

Apache 2.0. The `docker` and `heroku` themes borrow nouns from
[moby/moby](https://github.com/moby/moby) (Apache 2.0) and
[Haikunator](https://github.com/usmanbashir/haikunator) (MIT).
