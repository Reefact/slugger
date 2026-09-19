# slugger

A .NET CLI that generates `adjective-noun` slugs from JSON themes, with two things existing
generators do not offer: adjectives can be restricted to the nouns they actually fit, through
categories, and several named themes can be selected on the command line.

See [`docs/slugger-spec.md`](docs/slugger-spec.md) for the full specification.

> **Status: both halves work.** The engine loads, validates and generates; the CLI parses the
> spec's nineteen flags, runs its REPL and drives the five use cases. The build is green with
> zero warnings and 179 tests pass. Not done: a build workflow — the nightly mutation run is
> the only one — and the `--mimic-style` interaction has only unit coverage rather than an
> end-to-end case.

```console
$ slugger --theme docker --count 3        $ slugger --theme heroku --sep = --casing camel
optimizing_johnson                        throbbingSummit4280
sweet_visvesvaraya                        settlingHorizon8073

$ slugger --casing SHOUT --thme docker
The command line was refused for 3 reasons:

  - "--casing" accepts kebab, snake, camel, and "SHOUT" is none of them.
  - Unknown option "--thme". Did you mean "--theme"?
  - "docker" is not attached to any option. Did you mean "--theme docker"?
```

Run with no `--oneshot` and a terminal on standard input and it stays open, drawing another
round on every Enter. Behind a pipe or in CI it generates once and exits, because a `ReadLine`
nobody will answer is a hang rather than a prompt.

## Layout

```
src/
  Slugger             the engine — one assembly, three namespaces
    Domain/             theme model, resolution, generation, formatting, validation  (public)

    Application/        use cases and the ports they need                           (internal)
    Infrastructure/     JSON, embedded themes, theme directory, XDG config          (internal)
    Themes.cs           the public facade
  Slugger.Cli         flag parsing, REPL/oneshot, composition root, clipboard
tests/
  Slugger.UnitTests
  Slugger.Cli.UnitTests
```

The layering is a namespace convention, not an assembly split: for a project this size, one
assembly per layer buys compiler-enforced isolation at the cost of a package graph nobody
needs. `NamespaceDependencyTests` is what replaces the compiler — it reads type signatures and
fails if `Domain` ever names something from `Application` or `Infrastructure`, or `Application`
something from `Infrastructure`. It does not read method bodies; a cheap check that stays true
is worth more than a thorough one nobody maintains.

The one split that is kept is `Slugger` against `Slugger.Cli`, because it is a real
packaging boundary, and it is what decides where a dependency may sit. One taken by
`Slugger` reaches everybody who references it, as a line in the published nuspec; one taken
by `Slugger.Cli` stops at the executable. So the engine's list is a whitelist kept deliberately
short — `FirstClassErrors`, for `Outcome` and the error model — rather than an empty one, and
`NamespaceDependencyTests` makes adding to it a conscious act. `TextCopy` is the CLI's alone: a
clipboard has no business in a slug engine, and `ClipboardDependencyTests` fails if it ever
leaks inwards.

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

## One place the spec was corrected instead

`docs/slugger-spec.md` used to say that `common` had no special status and that a noun with no
category reached no adjective at all. Running the real rules over the shipped files showed that
cannot be what was meant: all 236 of `docker`'s nouns and 103 of `heroku`'s carry no category,
and neither file puts `common` on a noun — so the literal rule gives every one of them an empty
pool and refuses both themes, while the spec claims in the same breath that they clear all three
rules by themselves at 236 nouns against 187 adjectives.

`slugger` settles it: its six categories hold 45 adjectives each and `common` holds 60, so no
category reaches the floor of 100 on its own. Only the sum with `common` does. The spec now says
what the themes were built for — **every noun reaches `common`, on top of whatever it declares**
— and rule 1 likewise accepts a category declared only in `participles`, which is how `heroku`
classifies its nouns by physical capability while keeping its adjectives in a single `common`.

That reading is what makes the spec's own worked example possible: `moon` is declared
`[lumineux, mobile]` and `waning` lives in `participles.common`, yet `waning-moon` is given as a
draw. `ThemeResolverTests` pins it against the shipped file.

## What the package exposes

Seventeen public types, not thirty-seven. `Slugger.Domain` and the `Themes` facade are what the
spec promises a consumer; `Slugger.Application` and `Slugger.Infrastructure` are how the engine
is built and are `internal`, reachable by the CLI and the tests through `InternalsVisibleTo`.

Collapsing four assemblies into one is what made that possible — across assemblies every layer
had to be `public` for the next one to use it — and `NamespaceDependencyTests` fails if a
supporting layer ever becomes visible again. A port can always be published later; unpublishing
one is a breaking change.

The package is `Slugger`, matching its root namespace, and the CLI ships as `Slugger.Cli` with
`slugger` as its command. The CLI's assembly is deliberately **not** named `slugger`: a tool
package carries the library beside it, and `slugger.dll` next to `Slugger.dll` collides on a
case-insensitive file system.

## Loading reports everything at once

A theme file is never refused one complaint at a time. Parsing collects every malformed section
before giving up, validation runs every rule over every noun and every category, and the two
stages report **together** — so one run tells a theme author everything their file needs:

```console
$ slugger broken.json
theme "broken" was refused for 14 reasons:

  - nouns[2]: no non-empty "value"
  - "defaults.sep" must be a single character
  - "defaults.casing" must be one of kebab, snake, camel
  - "defaults.tokenLength" must be a whole number
  - "willow" references category "vegetal", which the theme does not declare (it declares common, stadium)
  - "river" references category "aquatique", which the theme does not declare (it declares common, stadium)
  - defaults.segmentMode asks for "either", but the theme declares no participle anywhere
  - 3 nouns, but a theme needs at least 100
  - "willow" reaches 2 adjectives, but every noun needs at least 100
  ...
```

Two things are deliberately *not* reported. Malformed JSON is terminal — nothing can be read
from a document that did not parse. And when a section the rules themselves read is malformed,
the rules are skipped for it: `"nouns" must be an array` already says everything, and
`0 nouns, at least 100 required` on top of it would be noise rather than a second finding.

`Themes.Load*Result` returns the whole report; `Themes.Load*` is the convenience shape the spec
sketches, and its `ThemeRejectedException` carries the same full report rather than only the
first complaint. One renderer in the CLI turns facts into prose, which is what makes
`--register` and a runtime load produce the same wording — there is only one wording.

## Quality gate

The build carries **zero warnings**, and nothing is silenced by a blanket `NoWarn`:

* `SonarAnalyzer.CSharp` and the SDK's own analyzers run on every build, with
  `EnforceCodeStyleInBuild` so that `.editorconfig` reports at build time rather than only in
  an IDE.
* Suppressions go through [DiagnosticCatalog](https://github.com/Reefact/diagnostic-catalog),
  one catalogue per analyzer family that actually runs here — `.Sonar` for `S****`,
  `.NetAnalyzers` for the SDK's `CA****`, `.CodeStyle` for the `IDE****`, and `.Xunit` in the
  test projects. `SonarRule.S2325.Id` rather than `"S2325"`, so a typo stops the build instead
  of compiling into a suppression that silently matches nothing — and a `Justification` is
  required. There are two, both recorded: `S2245` on the random source, and `S2325` on the
  types whose bodies still throw.
* **None of that reaches a consumer, measured rather than assumed.** Adding the three extra
  catalogues left every packed file byte-for-byte the same size and the `<dependencies>` group
  empty. Two separate mechanisms make that true, and each has its own guard: the catalogue
  values are compile-time constants folded before the assembly is written, so nothing survives
  to be referenced (`PackageWeightTests`); and `PrivateAssets="all"` is what keeps them out of
  the nuspec, which no assembly-level assertion can see — drop it and the package grows a
  dependency while every test still passes, so the `AnalyzersStayPrivate` MSBuild target guards
  that one.
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

### Mutation testing

```bash
dotnet tool restore     # once per clone; Stryker's version is pinned in dotnet-tools.json
dotnet dotnet-stryker   # doubled on purpose — the manifest's command is `dotnet-stryker`
```

[Stryker.NET](https://stryker-mutator.io/) edits the source a thousand ways — 1037 of them here —
and reports how many of those edits no test noticed. It takes about two minutes for the whole
solution, which is too long for a push, so `.github/workflows/nightly-mutation.yml` runs it on a
schedule and keeps the HTML report as a build artifact.

**Give a local run a home of its own.** A mutant that drops the `directoryPath ??
DefaultDirectoryPath` seam writes where the default says — `~/.slugger/themes` and
`~/.config/slugger/config.json`, the real ones — so it can overwrite a theme or the saved
defaults of whoever is logged in. `dotnet test` touches neither; this is mutation's own hazard:

```bash
DOTNET_CLI_HOME="$HOME" NUGET_PACKAGES="$HOME/.nuget/packages" HOME=$(mktemp -d) dotnet dotnet-stryker
```

**The score is not yet stable.** Seven runs of this same commit landed on 51.19% four times —
identical mutant for mutant — and on 56.62%, 57.04% and 57.18% the other three, with the same
1037 mutants and the same 179 tests either way. What flips is a block of about forty, all of
them in the CLI parser and in error-message literals, as if a whole test assembly counted
towards them in one run and not the next; Stryker's own log warns that its Microsoft Testing
Platform runner is in preview and that results should be verified. Until that is understood the
break threshold sits at 45, below the lower mode, so a red nightly means a regression rather
than that spread. It is still a ratchet, like the warning one: raise it as the score climbs.

Where the survivors are: 45 of `OptionResolver`'s 48 are the one `??` chain that decides whether
a command-line flag, a saved default or the built-in value wins — no test pins that precedence.
Most of the rest are message literals in `ThemeErrors` and `CliErrors`, which say that the error
*codes* are asserted and the prose is not, plus 37 mutants in `JsonThemeSerializer` that no test
reaches at all.

## Themes

`slugger`, `heroku` and `docker` are embedded in `Slugger` and work with no setup. A noun
that belongs to no category simply omits `categories`, which is why `docker.json` is a list of
`{ "value": "agnesi" }` lines — writing the empty array out cost it 41% of its size. Any
other theme is a `.json` file in `~/.slugger/themes/` (or `--theme-dir`), same schema, added
without recompiling. A custom file shadows a built-in theme of the same name.

The `docker` theme keeps the 236 official scientist surnames from
[`moby/moby`](https://github.com/moby/moby) (Apache 2.0); the `heroku` theme takes its nature
nouns from [Haikunator](https://github.com/usmanbashir/haikunator) (MIT). Adjectives and
participles in both are original.

## License

Apache 2.0 — see [LICENSE](LICENSE).
