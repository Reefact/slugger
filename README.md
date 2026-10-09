<img src="assets/icon.png" alt="" width="96" align="right">

# slugger

**Readable random names for the things you spin up and throw away** — containers, git branches,
preview environments, test databases. slugger draws Docker- and Heroku-style names from themes in
which every adjective fits its noun: `singing-river`, never `singing-moon`.

It invents names. It does not turn "Hello World" into `hello-world` — for URL slugs, see
[Slugify.Core](https://www.nuget.org/packages/Slugify.Core).

[![ci](https://github.com/Reefact/slugger/actions/workflows/ci.yml/badge.svg)](https://github.com/Reefact/slugger/actions/workflows/ci.yml)
[![Slugger.Cli on NuGet](https://img.shields.io/nuget/v/Slugger.Cli?label=Slugger.Cli)](https://www.nuget.org/packages/Slugger.Cli)
[![Slugger on NuGet](https://img.shields.io/nuget/vpre/Slugger?label=Slugger)](https://www.nuget.org/packages/Slugger)

## Install

The command, as a .NET tool (needs the .NET 10 runtime):

```console
$ dotnet tool install --global Slugger.Cli
$ slugger --theme docker --count 3 --oneshot
humming_dirac
hale_merkle
blithe_bohr
$ slugger --theme heroku --count 2 --oneshot
silvery-mud-7643
waxing-sprig-0239
```

The engine, as a library for .NET 10 — it is in preview, hence `--prerelease`:

```bash
dotnet add package Slugger --prerelease
```

```csharp
using Slugger;
using Slugger.Domain;
using Slugger.Domain.Generation;

ThemeDocument docker = Themes.LoadEmbedded("docker");
GenerationOptions dockerStyle = GenerationOptions.Default.WithDefaultsOf(docker);

Console.WriteLine(SlugGenerator.Generate(docker, dockerStyle));   // e.g. epic_villani
```

## Why slugger

- **Words that belong together.** Nouns and epithets share categories, and an adjective is only
  drawn for a noun it can describe. A theme can also rule out a word for one noun: the built-in
  `docker` theme never calls Wozniak boring, as Docker itself never does.
- **Your vocabulary is a JSON file.** Three themes are built in — `slugger`, `docker`, `heroku` —
  and [more live in this repository](themes/): jazz, mineralogy, cocktails, Icelandic place
  names... Draw from several at once with `--theme docker,jazz`, or from all of them with
  `--theme '*'`.
- **Themes are checked, not trusted.** A theme too thin to stay varied is refused before the first
  draw, with every reason at once, and `slugger --analyze` reports the margins of a theme you are
  writing.
- **Names that fit where they land.** `--max-length 63` keeps a name inside a DNS label by leaving
  out the words that would not fit — it never truncates. `--ascii` guarantees an ASCII name;
  separator, casing and a numeric or hexadecimal token do the rest.

## Documentation

| You want to | Read |
| --- | --- |
| Run the command, in a terminal or a script | [docs/cli.md](docs/cli.md) |
| Generate names from C# | [docs/library.md](docs/library.md) |
| Write your own theme | [docs/writing-a-theme.md](docs/writing-a-theme.md) |
| Check that a theme's word pairs make sense | [docs/reviewing-a-theme.md](docs/reviewing-a-theme.md) |
| Browse the themes this repository ships | [themes/](themes/) |
| Learn the vocabulary — term, epithet, segment... | [docs/ubiquitous-language.md](docs/ubiquitous-language.md) |
| Contribute a change | [CONTRIBUTING.md](CONTRIBUTING.md) |
| Understand how the code is organised | [docs/architecture.md](docs/architecture.md) |
| Know why it works the way it does | [docs/idr/](docs/idr/) — decision records, in French |

## Versions

`Slugger.Cli` is stable. `Slugger`, the library, stays in preview while one of its dependencies,
FirstClassErrors, is itself a preview: NuGet does not let a stable package depend on a prerelease.
The library's API may still change before its stable release. Both packages are released from this
repository, each with its own version — see [CONTRIBUTING.md](CONTRIBUTING.md#releasing).

## Credits

The `docker` theme keeps the scientists' surnames of [moby/moby](https://github.com/moby/moby)
(Apache 2.0); the `heroku` theme takes its nature nouns from
[Haikunator](https://github.com/usmanbashir/haikunator) (MIT). Their adjectives and participles, and
every other theme, are original.

## License

Apache 2.0 — see [LICENSE](LICENSE).
