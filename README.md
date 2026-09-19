# slugger

A .NET CLI that generates `adjective-noun` slugs from JSON themes, with two things existing
generators do not offer: adjectives can be restricted to the nouns they actually fit, through
categories, and several named themes can be selected on the command line.

See [`docs/slugger-spec.md`](docs/slugger-spec.md) for the full specification.

> **Status: scaffolding.** The solution, the layering and the built-in themes are in place, the
> build is green with zero warnings, and 24 tests pass. The generation algorithm itself is not
> implemented yet — the types that will carry it are declared and documented, and throw
> `NotImplementedException`.

## Layout

```
src/
  Slugger.Core        the engine — one assembly, three namespaces
    Domain/             theme model, resolution, generation, formatting, validation
    Application/        use cases and the ports they need
    Infrastructure/     JSON, embedded themes, theme directory, XDG config
    Themes.cs           the public facade
  Slugger.Cli         flag parsing, REPL/oneshot, composition root, clipboard
tests/
  Slugger.Core.UnitTests
  Slugger.Cli.UnitTests
```

The layering is a namespace convention, not an assembly split: for a project this size, one
assembly per layer buys compiler-enforced isolation at the cost of a package graph nobody
needs. `NamespaceDependencyTests` is what replaces the compiler — it reads type signatures and
fails if `Domain` ever names something from `Application` or `Infrastructure`, or `Application`
something from `Infrastructure`. It does not read method bodies; a cheap check that stays true
is worth more than a thorough one nobody maintains.

The one split that is kept is `Slugger.Core` against `Slugger.Cli`, because it is a real
packaging boundary. `TextCopy` is the project's only external dependency and is scoped to the
CLI, so the engine stays dependency-free for anyone referencing it as a library — and
`ClipboardDependencyTests` fails if it ever leaks inwards.

## Two places the code departs from the spec

**`Theme.LoadFromFile` lives on the facade, not on the entity.** The spec sketches
`Theme.LoadEmbedded("docker")`, which would have the domain entity reach for JSON and the file
system. The loading entry points are on `Slugger.Themes` instead. Consumers still take a single
reference and get the promised ergonomics; the domain stays free of I/O.

**Normalization step 4 happens at format time, not at load time.** The spec applies all four
steps when a value is read from the JSON, but step 4 replaces spaces with the separator — and
the separator is only known at generation time, and varies from one draw to the next in
multi-theme `--mimic-style`, since each drawn theme applies its own. `WordNormalizer` does
steps 1 to 3 at load; `SlugFormatter` does step 4. Same result for a single theme, correct
result for several.

## Quality gate

The build carries **zero warnings**, and nothing is silenced by a blanket `NoWarn`:

* `SonarAnalyzer.CSharp` and the SDK's own analyzers run on every build, with
  `EnforceCodeStyleInBuild` so that `.editorconfig` reports at build time rather than only in
  an IDE.
* Suppressions go through [`DiagnosticCatalog.Sonar`](https://github.com/Reefact/diagnostic-catalog):
  `SonarRule.S2325.Id` rather than `"S2325"`, so a typo stops the build instead of compiling
  into a suppression that silently matches nothing — and a `Justification` is required.
  There are two, both recorded: `S2245` on the random source, and `S2325` on the types whose
  bodies still throw.
* The warning ratchet (`TreatWarningsAsErrors` + `MSBuildTreatWarningsAsErrors`) is scoped to
  CI, following the chapter's convention, so the local inner loop stays friendly. **No CI
  workflow is wired yet** — until one exists, run `GITHUB_ACTIONS=true dotnet build` to get the
  same answer CI will give.

## Tests

xUnit v3, arbitrary values from [`JustDummies`](https://github.com/Reefact/just-dummies), and a
`// Setup // Exercise // Verify` split in each test.

A value a test does not care about is drawn rather than written, so the test states its
assumptions and everything else varies between runs:

```csharp
// Setup
string word = Dummies.AnyWord();

// Exercise
string canonical = WordNormalizer.Canonicalize($"   {word}  ");

// Verify
Assert.Equal(word, canonical);
```

`[assembly: Reproducible]` pins each test case to a seed that is reported **only when the test
goes red**, so a failure can be replayed value for value.

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
