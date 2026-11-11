<img src="assets/icon.png" alt="" width="96" align="right">

# slugger

Readable random names, Docker- and Heroku-style, for containers, git branches, preview
environments and test data.

```console
$ slugger --theme docker
tenacious_euclid
$ slugger --theme heroku
jagged-thunder-2506
```

Not a URL slugifier: to turn `Hello World` into `hello-world`, see
[Slugify.Core](https://www.nuget.org/packages/Slugify.Core).

[![ci](https://github.com/Reefact/slugger/actions/workflows/ci.yml/badge.svg)](https://github.com/Reefact/slugger/actions/workflows/ci.yml)
[![Slugger.Cli on NuGet](https://img.shields.io/nuget/v/Slugger.Cli?label=Slugger.Cli)](https://www.nuget.org/packages/Slugger.Cli)
[![Slugger on NuGet](https://img.shields.io/nuget/vpre/Slugger?label=Slugger)](https://www.nuget.org/packages/Slugger)

## Why slugger

Anything a person has to read, say or remember deserves a better name than `3f9c2a1e`: a container
in `docker ps`, a preview environment in a chat thread, a branch in a pull request. Docker and
Heroku showed the way with `focused_turing` and `misty-meadow-4821`, and plenty of libraries copy
them. slugger goes further on four points.

### The words go together

Most generators glue a random adjective to a random noun, and sooner or later they print
`thundering-moon` or `decaf-otter`. Docker even hard-codes a refusal for `boring_wozniak`.

In a slugger theme, nouns and adjectives share categories, and an adjective is only drawn for a
noun it can describe:

```json
{
  "adjectives": {
    "common": ["quiet", "lucky", "brave"],
    "drink":  ["iced", "decaf"],
    "animal": ["furry", "sleepy"]
  },
  "nouns": [
    { "value": "espresso", "categories": ["drink"] },
    { "value": "otter",    "categories": ["animal"] }
  ]
}
```

A thousand draws give `iced-espresso`, `sleepy-otter`, `brave-otter`… and never `decaf-otter`
(a theme this small needs `--allow-small-theme` to load at all — see below). The
built-in `heroku` theme works the same way: a river can be `singing`, the moon cannot.

### Any vocabulary you like

A theme is a plain JSON file — no code, nothing to recompile. Three ship inside the tool, and
[this repository carries more](themes/):

| Theme | Sounds like |
| --- | --- |
| `docker` | `tenacious_euclid`, `loyal_curie` |
| `heroku` | `pale-spruce-5860`, `jagged-thunder-2506` |
| `slugger`, the default | `savvy-lasting-babe-ruth` |
| `jazz` | `loosely-arranged-whispering-take-five` |
| `code-review` | `melancholy-coordinated-value-object` |
| `cocktails` | `festive-expertly-poured-bellini` |
| `cyberpunk` | `liquid-cooled-pursuing-combat-drone` |
| `iceland` | `weather-beaten-reviving-blue-lagoon` |
| `synthesizers` | `crystalline-aliasing-synclavier` |

Download the ones you like from [themes/](themes/) and register them with
`slugger --register jazz.json`, then mix them with
`--theme docker,jazz`, or draw from all of them with `--theme '*'`. Writing your own
takes no code at all: [docs/writing-a-theme.md](docs/writing-a-theme.md).

### Names that fit where they go

A DNS label stops at 63 characters, a Heroku app at 30 and a script wants the same name every
time it replays. slugger shapes the name for its destination — and it never truncates: under a
length limit, the words that would not fit are left out of the draw.

```console
$ slugger --ascii --max-length 63            # a Kubernetes namespace
atmospheric-welcoming-ebbets
$ slugger --casing camel --token-length 4    # an identifier with a suffix
craftyPersistingSpikes1512
$ git switch -c "feat/$(slugger --oneshot)"  # a branch
Switched to a new branch 'feat/natural-soaring-chief-bender'
```

### Themes that hold up

A small vocabulary repeats itself quickly: Docker's own lists make 25,488 names, and the first
repeat comes after a few hundred. slugger checks a theme's variety before it draws a single name —
at least 100 nouns, and at least 100 adjectives within reach of each — and when it refuses one, it
gives every reason at once rather than just the first:

```console
$ slugger --register ./cafe.json
Theme "cafe" was refused for 5 reasons:

  - 2 nouns, but a theme needs at least 100.
  - "espresso" reaches 5 adjectives, but every noun needs at least 100.
  - "otter" reaches 5 adjectives, but every noun needs at least 100.
  ...
```

While you write a theme, `slugger --analyze` tells you how far you are from each floor.

## Install

The command, as a .NET tool (needs .NET 10):

```bash
dotnet tool install --global Slugger.Cli
```

In a terminal, `slugger` stays open: Enter draws another round, Ctrl+D quits. Piped or captured —
`slugger | head -1`, `name=$(slugger)` — it draws once and exits by itself, and `--oneshot` does the
same in a terminal.

The library, for .NET 10 (still a prerelease):

```bash
dotnet add package Slugger --prerelease
```

```csharp
using Slugger;
using Slugger.Domain;
using Slugger.Domain.Generation;

ThemeDocument docker = Themes.LoadEmbedded("docker");
string name = SlugGenerator.Generate(docker, GenerationOptions.Default.WithDefaultsOf(docker));
Console.WriteLine(name);   // e.g. lively_lehmann
```

## Documentation

| To | Read |
| --- | --- |
| use the command | [docs/cli.md](docs/cli.md) |
| use the library | [docs/library.md](docs/library.md) |
| write a theme | [docs/writing-a-theme.md](docs/writing-a-theme.md) |
| contribute | [CONTRIBUTING.md](CONTRIBUTING.md) |
| look up a word — term, epithet, segment… | [docs/ubiquitous-language.md](docs/ubiquitous-language.md) |

Everything else — architecture, decision records — is linked from those pages.

## Licence

Apache-2.0. The `docker` and `heroku` themes borrow nouns from
[moby/moby](https://github.com/moby/moby) (Apache 2.0) and
[Haikunator](https://github.com/usmanbashir/haikunator) (MIT).
