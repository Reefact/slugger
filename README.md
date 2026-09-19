# slugger

A .NET CLI that generates `adjective-noun` slugs from JSON themes, with two things existing
generators do not offer: adjectives can be restricted to the nouns they actually fit, through
categories, and several named themes can be selected on the command line.

See [`docs/slugger-spec.md`](docs/slugger-spec.md) for the full specification.

> **Status: scaffolding.** The solution, the layering and the built-in themes are in place and
> the build is green. The generation algorithm itself is not implemented yet — the types that
> will carry it are declared and documented, and throw `NotImplementedException`.

## Layout

```
src/
  Slugger.Domain           the theme model, resolution, generation, formatting, validation
  Slugger.Application      use cases and the ports they need
  Slugger.Infrastructure   JSON, embedded themes, theme directory, XDG config
  Slugger.Core             the public facade — the single reference a library consumer needs
  Slugger.Cli              flag parsing, REPL/oneshot, composition root, clipboard
tests/                     one project per layer
```

Dependencies point inwards only:

```
Cli ──► Core ──► Infrastructure ──► Application ──► Domain
```

`Slugger.Domain` has no `ProjectReference` and no `PackageReference` at all, and
`Slugger.Application` references nothing but the domain. That is not a convention to remember —
it is what the `.csproj` files say, and `DependencyRuleTests` in `Slugger.Domain.Tests`,
`Slugger.Application.Tests` and `Slugger.Cli.Tests` fail if someone loosens it.

`TextCopy` is the project's only external dependency and is scoped to `Slugger.Cli`, so the
engine stays dependency-free for anyone consuming it as a library. A test pins that too.

## Two places the code departs from the spec

**`Theme.LoadFromFile` lives on the facade, not on the entity.** The spec sketches
`Theme.LoadEmbedded("docker")`, which would have the domain entity reach for JSON and the file
system. The loading entry points are on `Slugger.Themes` instead, in `Slugger.Core`. Consumers
still take a single reference and get the promised ergonomics; the domain stays pure.

**Normalization step 4 happens at format time, not at load time.** The spec applies all four
steps when a value is read from the JSON, but step 4 replaces spaces with the separator — and
the separator is only known at generation time, and varies from one draw to the next in
multi-theme `--mimic-style`, since each drawn theme applies its own. `WordNormalizer` does
steps 1 to 3 at load; `SlugFormatter` does step 4. Same result for a single theme, correct
result for several.

## Building

Requires the .NET 10 SDK.

```bash
dotnet build
dotnet test
dotnet run --project src/Slugger.Cli
```

`global.json` pins the SDK band and opts `dotnet test` into Microsoft.Testing.Platform, which
xUnit v3 requires on .NET 10 — VSTest is no longer supported there. The solution is in the
`.slnx` format, which needs Visual Studio 17.13+ or Rider 2024.3+.

## Themes

`slugger`, `heroku` and `docker` are embedded in `Slugger.Core` and work with no setup. Any
other theme is a `.json` file in `~/.slugger/themes/` (or `--theme-dir`), same schema, added
without recompiling. A custom file shadows a built-in theme of the same name.

The `docker` theme keeps the 236 official scientist surnames from
[`moby/moby`](https://github.com/moby/moby) (Apache 2.0); the `heroku` theme takes its nature
nouns from [Haikunator](https://github.com/usmanbashir/haikunator) (MIT). Adjectives and
participles in both are original.

## License

Apache 2.0 — see [LICENSE](LICENSE).
