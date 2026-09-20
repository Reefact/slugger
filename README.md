# slugger

A .NET CLI that generates `adjective-noun` slugs from JSON themes, with two things existing
generators do not offer: adjectives can be restricted to the nouns they actually fit, through
categories, and several named themes can be selected on the command line.

Writing a theme: [`docs/writing-a-theme.md`](docs/writing-a-theme.md). Why it works this way:
[`docs/adr/`](docs/adr/).

> **Status: both halves work.** The engine loads, validates and generates; the CLI parses the
> twenty flags, runs its REPL and drives the five use cases. The build is green with
> zero warnings and 247 tests pass on Linux and Windows. Not done: neither package has been
> published yet — the release workflow is wired, its nuget.org side is not — and the
> `--mimic-style` interaction has only unit coverage rather than an end-to-end case.

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

## The decisions behind it

`slugger` was built from a written specification. Everything it described is now built, and the
document had become a paraphrase of the code — 63% of its lines restated what the code says and
247 tests already pin. It is deleted; git keeps it. What survives it is in
[`docs/adr/`](docs/adr/), seven decisions that still constrain what can be added, and in
[`docs/writing-a-theme.md`](docs/writing-a-theme.md), the part that had a reader who does not
write C#.

The one worth repeating here, because it was found by measurement rather than decided:
**`common` is a shared floor, not a fallback** — every noun reaches it on top of whatever it
declares. The original reading gave a noun with no category an empty pool, which refuses
`docker` and `heroku` as shipped: all 236 of `docker`'s nouns and 103 of `heroku`'s carry no
category, and neither file puts `common` on a noun. `slugger` settles it from the other side —
its five categories hold 45 adjectives each and `common` holds 60, so only the sum clears the
floor of 100. `ThemeResolverTests` pins it against the shipped file ([ADR
0002](docs/adr/0002-common-est-un-socle-partage.md)).

## What the package exposes

Eighteen public types, not thirty-seven. `Slugger.Domain` and the `Themes` facade are what a
consumer is promised; `Slugger.Application` and `Slugger.Infrastructure` are how the engine
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
$ slugger --register ./demo.json
Theme "demo" was refused for 9 reasons:

  - nouns[2]: no non-empty "value".
  - "defaults.sep" must be a single character.
  - "defaults.casing" must be one of kebab, snake, camel.
  - "riviere" references category "aquatique", which the theme does not declare (it declares common, vegetal).
  - 2 nouns, but a theme needs at least 100.
  - "saule" reaches 3 adjectives, but every noun needs at least 100.
  - "riviere" reaches 2 adjectives, but every noun needs at least 100.
  - Category "aquatique" totals 2 combinations, but every category needs at least 40,000.
  - Category "vegetal" totals 3 combinations, but every category needs at least 40,000.
```

Two things are deliberately *not* reported. Malformed JSON is terminal — nothing can be read
from a document that did not parse. And when a section the rules themselves read is malformed,
the rules are skipped for it: `"nouns" must be an array` already says everything, and
`0 nouns, at least 100 required` on top of it would be noise rather than a second finding.

`Themes.Load*Result` returns the whole report; `Themes.Load*` is the convenience shape, and its
`ThemeRejectedException` carries the same full report rather than only the first complaint. One renderer in the CLI turns facts into prose, which is what makes
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
  CI, following the chapter's convention, so the local inner loop stays friendly. `ci.yml`
  builds, tests and packs every push and pull request against `main`, on Linux and on Windows,
  so a warning that would merge cannot. `GITHUB_ACTIONS=true dotnet build` is that same answer
  without waiting for a runner.

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

### Publishing

Two packages, versioned apart by their own tag:

```bash
git tag lib-v1.2.3 && git push origin lib-v1.2.3   # Slugger,     the engine
git tag cli-v1.2.3 && git push origin cli-v1.2.3   # Slugger.Cli, the `slugger` command
```

`release.yml` refuses a tag that is not on `main`, rebuilds, re-runs the suite, packs that train
alone, signs a provenance attestation over the bytes it just produced, and pushes through OIDC
trusted publishing — no API key is stored. Its manual dispatch defaults to a dry run, which
rehearses everything including the OIDC exchange and stops before publishing.

`Slugger` cannot ship a *stable* version while `FirstClassErrors` is a prerelease: NuGet refuses a
stable package with a prerelease dependency (NU5104). `Slugger.Cli` bundles its dependencies and
is unaffected.

### Mutation testing

```bash
dotnet tool restore     # once per clone; Stryker's version is pinned in dotnet-tools.json
dotnet dotnet-stryker   # doubled on purpose — the manifest's command is `dotnet-stryker`
```

Two engines, not one: [Stryker.NET](https://stryker-mutator.io/) nightly over the whole
solution, and [KillMutants](https://github.com/Reefact/kill-mutants) beside it — a different
catalogue and a different test host, so a survivor both of them report is a survivor twice over,
and one they disagree about is worth reading. KillMutants also judges a pull request's diff
alone, in seconds, and fails it when the change carries a mutant nothing detects.

Stryker edits the source a thousand ways — 1037 of them here —
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

Where the survivors were: 45 of `OptionResolver`'s 48 were the one `??` chain that decides
whether a command-line flag, a saved default or the drawn theme's own value wins, and nothing
pinned it. The CLI tests written since took that file from 20 mutants killed to 39, and reading
the chain that closely is what settled where a saved option stands — under the style of a theme
you explicitly asked for, over the program's own defaults — and what a second `--init` does to
the first. The score moved with it, 51.19% to 54.17%.
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
