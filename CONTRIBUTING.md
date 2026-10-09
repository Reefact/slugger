# Contributing to slugger

This page is for anyone who changes the code, the tests or the themes shipped with `slugger`. It
covers how to build and test, what CI checks before a pull request can merge and how to make the
changes people make most often. To write a theme without touching C#,
[`docs/writing-a-theme.md`](docs/writing-a-theme.md) and
[`docs/reviewing-a-theme.md`](docs/reviewing-a-theme.md) are all you need.

The complete house rules are in [`CLAUDE.md`](CLAUDE.md). That file is written for the AI agents
that do much of the work in this repository, which is why it reads as a list of instructions; the
rules apply to everyone, and this page summarises them. For how the code is organised, read
[`docs/architecture.md`](docs/architecture.md); for why it is organised that way, the decision
records in [`docs/idr/`](docs/idr/README.md).

- [Before you start](#before-you-start)
- [Build, test and run](#build-test-and-run)
- [The pre-push check](#the-pre-push-check)
- [What CI enforces and what review judges](#what-ci-enforces-and-what-review-judges)
- [Pull requests](#pull-requests)
- [The golden master](#the-golden-master)
- [Recipes](#recipes)
- [Conventions](#conventions)
- [Mutation testing](#mutation-testing)
- [Releasing](#releasing)

## Before you start

- **The .NET 10 SDK.** `global.json` pins the 10.0.100 feature band and rolls forward to any later
  10.0 SDK.
- **An editor that opens `.slnx` solutions** if you want one: Visual Studio 17.13 or later, or
  Rider 2024.3 or later. Everything on this page also works from a terminal.
- **A POSIX shell and `jq`**, for the coding-rules sweep in the pre-push check.
- **Optionally, `xsel` on Linux**, to try `--clipboard` by hand. The tests use a fake clipboard and
  do not need it.

## Build, test and run

```bash
git clone https://github.com/Reefact/slugger.git
cd slugger
dotnet build slugger.slnx
dotnet test --solution slugger.slnx
dotnet run --project src/Slugger.Cli -- --theme docker --count 3 --oneshot
```

The test projects run on Microsoft.Testing.Platform, which xUnit v3 requires on .NET 10 and which
`global.json` opts into. Under it, `dotnet test` takes the solution through `--solution`; a
positional `dotnet test slugger.slnx` is refused. To run part of the suite:

```bash
dotnet test --project tests/Slugger.Cli.UnitTests --filter-class '*ThemeGoldenMasterTests'
dotnet test --project tests/Slugger.Cli.UnitTests --filter-method '*Lists_the_themes_in_scope'
```

There are three test projects: `tests/Slugger.UnitTests` for the engine,
`tests/Slugger.Cli.UnitTests` for the command line and the golden master, and
`tests/Slugger.ArchitectureTests` for the rules about the shape of the code.

`--oneshot` matters when you run the CLI by hand: without it, and with a terminal on standard
input, `slugger` waits and draws another round each time you press Enter. Ctrl+D quits.

**If you have registered themes of your own**, in `~/.slugger/themes`, one test fails:
`SluggerRunnerTests.Lists_the_themes_in_scope` lists the real theme directory and expects only the
built-in themes. Run the suite with a home directory of its own:

```bash
DOTNET_CLI_HOME="$HOME" NUGET_PACKAGES="$HOME/.nuget/packages" HOME=$(mktemp -d) \
  dotnet test --solution slugger.slnx
```

The two variables before `HOME=` keep the .NET tools and the package cache in your real home, so
nothing is downloaded again.

## The pre-push check

These are the commands CI runs, plus two checks of the house style. Run them before you push:

```bash
GITHUB_ACTIONS=true dotnet build slugger.slnx -c Release
GITHUB_ACTIONS=true dotnet test --solution slugger.slnx -c Release
sh .claude/hooks/coding-rules.sh --all
dotnet format style slugger.slnx --verify-no-changes
```

- **`GITHUB_ACTIONS=true` turns warnings into errors.** Every GitHub runner sets that variable, and
  `Directory.Build.props` reads it to switch on the warning ratchet: `TreatWarningsAsErrors` for the
  compiler and the analyzers, `MSBuildTreatWarningsAsErrors` for MSBuild and the SDK. Only NuGet's
  security advisories stay warnings, so that a vulnerability published overnight does not turn
  every pull request red. Without the variable, a local build reports warnings and still succeeds,
  so that a half-finished refactoring stays buildable; the same warning fails CI.
- **The tests need the variable too.** Spectre.Console detects GitHub Actions and turns ANSI colour
  codes back on, so a test that asserts on a sentence can pass on your machine and fail on the
  runner, where the sentence arrives wrapped in escape codes. The variable also colours the test
  runner's own output, so read the exit code (`echo $?` after the command) rather than searching
  the output for a summary.
- **`coding-rules.sh --all`** sweeps every `.cs` file for the two rules a script can check: a guard
  clause written on three lines, and a blank line between two consecutive guards (see
  [Conventions](#conventions)). It prints nothing and exits 0 when the tree is clean. It needs
  `jq`: without it, the sweep skips every file and stays silent.
- **`dotnet format style`** checks the code-style rules that `.editorconfig` declares — the ones the
  build reports as `IDE` warnings. Leave out `--verify-no-changes` and it fixes them. Use `style`,
  not plain `dotnet format`: the whitespace pass that plain `dotnet format` adds removes the
  vertical alignment the house style asks for, and rewrites hundreds of lines nobody touched.

In PowerShell, set the variable with `$env:GITHUB_ACTIONS = 'true'` before the first two commands,
and remove it afterwards with `Remove-Item Env:GITHUB_ACTIONS`.

### Optional: the JetBrains clean-up

Part of the house style has no Roslyn equivalent: `#region` blocks, vertical alignment of
parameters and fields, the layout of XML doc comments. It lives in `slugger.slnx.DotSettings`, a
copy of the maintainers' shared ReSharper settings, and the JetBrains command-line tool applies it:

```bash
dotnet tool restore   # once per clone
DOTNET_CLI_HOME="$HOME" NUGET_PACKAGES="$HOME/.nuget/packages" HOME=$(mktemp -d) \
  dotnet jb cleanupcode slugger.slnx --settings=slugger.slnx.DotSettings \
  --profile="Really Full Cleanup" --include="src/Slugger/Domain/Token.cs;src/Slugger/Domain/Category.cs"
```

- **The isolated `HOME` is not optional.** The tool writes caches under `~/.local/share/JetBrains`.
  `HOME=` comes last on purpose: the shell applies the assignments from left to right, so the two
  before it still point at your real home, where `dotnet tool restore` put the tool.
- **List your own files in `--include`.** Run over the whole solution, the clean-up currently
  rewrites dozens of files you did not touch, and that noise does not belong in your pull request.
  It takes a minute or two either way.

## What CI enforces and what review judges

| Rule | Checked by |
| --- | --- |
| No warning from the compiler, the SDK, Sonar or the `.editorconfig` code style (`IDE*`: explicit types, braces on every `if`, no expression-bodied method...) | `ci.yml`, building with warnings as errors |
| The whole suite passes, on Linux and on Windows | `ci.yml`, one job per platform |
| Layering, the engine's dependency whitelist, the value-object and entity rules | `tests/Slugger.ArchitectureTests`, part of the suite |
| Every shipped theme loads without `allowSmall` and draws what its golden master pins | `RepositoryThemeTests`, `EmbeddedThemeCatalogTests`, `ThemeGoldenMasterTests` |
| Both packages still pack | `ci.yml`, on the Linux job |
| No analyzer leaks into a package | the `AnalyzersStayPrivate` build target, `PackageWeightTests` |
| No line your diff changes can be broken without a test noticing | `killmutants.yml` on the pull request |
| A guard clause on one line, no blank line between consecutive guards | `coding-rules.sh`, on your machine only |

Review judges the rest, because no tool can:

- One type per file, named after it — test fakes included.
- A comment that explains what an `if` tests becomes a named predicate.
- A name in place of a nested call where the name says something; never on a fluent chain.
- An early return rather than nesting, and early returns rather than a compound condition.
- The value-object rules beyond what the tests measure, and errors owned by their concept.
- A unit test's shape, its name and its arbitrary values.
- Whether a change needs a decision record.

[Conventions](#conventions) summarises each of these; `CLAUDE.md` has the full text and the
reasoning.

## Pull requests

### Branch

Name your branch after the change, short and descriptive: `fix/token-chance-range`,
`docs/contributing`. `CLAUDE.md` asks AI agents for a branch called `claude/<slug>`, with the slug
drawn by `slugger` itself, so that the tool meets a real system on every branch; that convention is
for agent sessions. The maintainers decide whether it applies to you, and may ask for another name.

### Commits

Write [Conventional Commits](https://www.conventionalcommits.org), one intent per commit:
`feat(themes): add a rivers theme`, `fix(validation): ...`, `test: ...`, `docs: ...`,
`refactor: ...`, `chore: ...`.

`main` accepts pull requests only, merged by rebase into a linear history, so each of your commits
lands on `main` as you wrote it and its message stays in the log. The branch must be green when it
becomes mergeable; every commit along the way need not be.

### Title

Give the pull request a Conventional Commit title as well. `release.yml` creates each GitHub
release with `--generate-notes`, and GitHub builds those notes from the titles of the pull
requests merged since the previous release, of either package. The title is what a user reads in
the release notes.

### Checks

The two required checks are the `ci.yml` jobs **Build & test (ubuntu-latest)** and **Build & test
(windows-latest)**. Each restores, builds in Release with warnings as errors and runs the whole
suite; the Linux job also packs both packages, so a packaging mistake fails it too.

A third workflow, `killmutants.yml`, runs **Mutation test (KillMutants)** on every pull request. It
is not among the required checks, but a red run is a finding to fix before merging.

### The KillMutants gate

On a pull request, [KillMutants](https://github.com/Reefact/kill-mutants) mutates only the code
your diff changed — it runs with `--since` set to the base commit — and runs the tests against each
mutant. It fails when a mutant survives or is reached by no test at all, that is, when a line you
changed can be broken without any test noticing. You can run the same check before you push: see
[`docs/mutation-testing.md`](docs/mutation-testing.md#killmutants).

The consequence most people meet first: **a new error message needs its prose asserted**, not
only its code. A test that checks the error code alone passes whatever the message says, so a
mutant that empties the message survives and the check goes red. Assert on the part of the
message that matters, the way `ThemeLoadReportTests` does:

```csharp
Error refusal = Assert.Single(
    Reasons(outcome),
    reason => reason.Code == ThemeErrors.Codes.ParticiplePoolTooSmall && reason.DiagnosticMessage.Contains("noun0", StringComparison.Ordinal));
Assert.Contains("3 participles", refusal.DiagnosticMessage, StringComparison.Ordinal);
```

## The golden master

`tests/Slugger.Cli.UnitTests/ThemeGoldenMasterTests.cs` pins what every theme produces: the three
built-in themes and every file in `themes/`. For each theme it runs the real command line, in
process, under eight seeded variants — the defaults, `--sep _`, a token, a token half of the time,
`--max-segment-words 1`, `--casing camel`, `--max-length 30`, a glued hexadecimal token — twenty
slugs each. It writes down the arguments, the exit code and every line printed, in one file per
theme: `tests/Slugger.Cli.UnitTests/GoldenMaster/<theme>.verified.txt`. The files were written
before the vocabulary refactoring, to prove it changes no slug.

**Why a change can turn many files red at once.** Every variant is seeded, and a batch draws all
its slugs from one random sequence. A change to how a slug is drawn — one draw more or less, two
draws in another order — shifts every slug after it, in every theme. A change to rendering moves
every variant it touches. Editing the words of one theme moves that theme's file alone, and a new
validation rule moves the files where a variant is now refused, since the exit code is recorded.

**Approving a change you meant.** On a mismatch, the test writes what it drew beside the file it
failed against, as `<theme>.received.txt` (`.gitignore` keeps these out of commits). Read the
difference, then rename the received file over the verified one:

```bash
cd tests/Slugger.Cli.UnitTests/GoldenMaster
git diff --no-index docker.verified.txt docker.received.txt
mv docker.received.txt docker.verified.txt
```

Commit the new `.verified.txt` in the same commit as the change that caused it, and say in the
message why the slugs moved. A reviewer reads that diff: it is the visible effect of your change.

**A brand-new theme.** The test lists the themes it finds, so a new file in `themes/` is measured
at once and fails with *Theme "my-theme" has no golden master* — before writing any received file.
Create an empty verified file, run the test again so that it writes the received one, read it and
rename it:

```bash
touch tests/Slugger.Cli.UnitTests/GoldenMaster/my-theme.verified.txt
dotnet test --project tests/Slugger.Cli.UnitTests --filter-class '*ThemeGoldenMasterTests'
mv tests/Slugger.Cli.UnitTests/GoldenMaster/my-theme.received.txt \
   tests/Slugger.Cli.UnitTests/GoldenMaster/my-theme.verified.txt
```

## Recipes

Each recipe lists the files a change of that kind touches, in the order the value travels. Copy the
existing option or rule named in the recipe: the code around it explains the details.

### Add a session option, like `--count`

A session option shapes the run as a whole — how many slugs, the seed, the interactive loop — and
a theme has no say in it.

1. `src/Slugger.Cli/CommandLine/SluggerSettings.cs` — declare it with `[CommandOption]` and a
   `[Description]`, which is its `--help` text. An option that takes a value is bound as text, a
   `string?`, and a switch as a `bool`: given a number type, Spectre would convert as it binds and
   stop at the first failure.
2. `src/Slugger.Cli/CommandLine/CommandLineReader.cs` — convert it in the `Reading` constructor,
   with the helpers already there (`Number`, `Choice`, `SingleCharacter`, `True`). They add a
   complaint and carry on rather than throw, so a command line with three mistakes reports three
   ([DEC0006](docs/idr/DEC0006-rapport-groupe-des-refus.md)). A new kind of complaint gets a
   factory in `CliErrors.cs` and a code in `CliErrorCodes.cs`.
3. `src/Slugger/Application/Options/SluggerOptions.cs` — add a nullable property. Null means "this
   layer says nothing", which is what lets the defaults saved by `--init` speak
   ([DEC0004](docs/idr/DEC0004-chaine-de-priorite-unique.md)). A switch that was not passed is null,
   never `false`.
4. `src/Slugger/Application/Options/OptionResolver.cs` — add it to `Merge`. **Nothing checks that
   `Merge` lists every property.** Forget it there and, as soon as a config file exists, the merged
   options lose it: the value typed on the command line is ignored, and `--init` can no longer save
   it — while every test that runs without a config stays green.
5. Use it from the merged options: `GenerateSlugsUseCase.Execute` reads `session.Count`;
   `SluggerRunner.Generate` reads `session.Oneshot`, because the loop is a decision about the
   terminal.
6. `--init` saves it with no further work: `SaveDefaultsUseCase` merges the line over the saved
   config, and `XdgConfigStore` writes `SluggerOptions` as JSON.
7. Tests: `CommandLineReaderTests` (read, and refused), `OptionResolverTests` (merged),
   `OptionPrecedenceTests` (saved, then overridden on the command line). `GeneratedHelpTests`
   already checks that `--help` lists every declared option.
8. Documentation: the option reference in [`docs/cli.md`](docs/cli.md).

The command line is declared once, on `SluggerSettings`, and `--help` is drawn from it
([DEC0019](docs/idr/DEC0019-ligne-de-commande-declaree-et-rendue-par-spectre.md)): do not write a
second list of options anywhere in the code.

### Add an option a theme's `defaults` can also set, like `--token-glued`

These options decide how a slug looks, and a theme can state its own style for them. They go
through the whole precedence chain: command line, then the drawn theme's `defaults` (one theme
drawn, or `--mimic-style`), then the saved defaults, then the program's default
([DEC0004](docs/idr/DEC0004-chaine-de-priorite-unique.md)).

1. `SluggerSettings.cs`, `CommandLineReader.cs`, `SluggerOptions.cs` and `OptionResolver.Merge` —
   as for a session option.
2. `OptionResolver.cs`, `LayOver` — the per-theme chain. Both `Merge` and `LayOver` list every
   option by hand, and nothing checks either.
3. `src/Slugger/Domain/GenerationOptions.cs` — the property the engine reads, with the program's
   default as its initial value.
4. `src/Slugger/Domain/ThemeDefaults.cs` — the nullable property a theme may set.
5. `GenerationOptions.WithDefaultsOf` — lay the theme's value over the options.
6. `src/Slugger/Infrastructure/Serialization/JsonThemeSerializer.cs`, `ReadDefaults` — read the key
   (`"tokenGlued"`) with the existing readers, which report a malformed value instead of throwing.
7. The engine code that uses it — `SlugFormatter.Format` for `TokenGlued`. Apply it when the slug
   is drawn or rendered, never when the theme is loaded
   ([DEC0005](docs/idr/DEC0005-format-resolu-au-tirage.md)). `SlugBudget` measures with the
   formatter, so `--max-length` takes the new option into account by itself.
8. Tests: the reader, `OptionResolverTests`, `JsonThemeSerializerTests`, `OptionPrecedenceTests`
   (which of the theme's style, the saved defaults and the command line wins) and the tests of the code
   that uses it, such as `SlugFormatterTests`.
9. Documentation: the option reference in [`docs/cli.md`](docs/cli.md) and the `defaults` section of
   [`docs/writing-a-theme.md`](docs/writing-a-theme.md).

If the program's default stays the old behaviour, no golden master moves. **A switch can only be
turned on from the command line.** Absent means "say nothing", so once a theme's `defaults` or the
saved config turn it on, nothing on the command line turns that one option off; `--mimic-style
false` drops the theme's whole style, and only the theme's. If users need to say "explicitly off",
give the option a value for it, as `--mimic-style [true|false]` and `--max-segment-words none` do
([DEC0024](docs/idr/DEC0024-aucun-plafond-explicite-qui-outrepasse-le-theme.md)).

### Add an option that is a command, like `--theme-info`

A command replaces drawing slugs: it does one thing and exits.

1. `SluggerSettings.cs` — declare it; it takes a value when the command needs a name or a path.
2. `src/Slugger.Cli/CommandLine/CliCommand.cs` — add a member.
3. `CommandLineReader.cs`, `ReadCommand` — add an `Asked(...)` line. Two commands on one line are
   refused with `CliErrors.OnlyOneCommand`, never silently resolved.
4. `src/Slugger/Application/UseCases/` — a new `internal sealed` use case, taking the ports it needs
   in its primary constructor and returning a result — an `Outcome` when it can be refused — rather
   than printing anything. If it needs
   something new from a port, add it to the interface in `src/Slugger/Application/Abstractions/` and
   to every implementation, test fakes included: `--theme-info` added `IThemeCatalog.Parse` to the
   embedded, file-system and chained catalogues and to `FakeThemeCatalog`.
5. `src/Slugger.Cli/Program.cs` — build the use case in the composition root and pass it to
   `SluggerRunner`.
6. `src/Slugger.Cli/SluggerRunner.cs` — a constructor parameter, an arm in the `switch` of `Run`, and
   a private method that writes the result, or hands a refusal to `Report`. Decide which options
   the use case receives: the command line alone (`request.Options`), or the line merged over the
   saved defaults (`session`).
7. Every test that builds a `SluggerRunner` by hand gains the new argument: `SluggerRunnerTests`,
   `OptionPrecedenceTests` and `ThemeGoldenMasterTests`.
8. Tests: `CommandLineReaderTests` (recognised, and refused beside another command), a test of the
   use case in `tests/Slugger.UnitTests` and `SluggerRunnerTests` for what it prints.
9. Documentation: the commands table in [`docs/cli.md`](docs/cli.md), and an `AddExample` in
   `SluggerApp.Build` if `--help` should show it.

### Add a theme validation rule

Every rule lives in `src/Slugger/Domain/Validation/ThemeValidator.cs`. `Validate(ThemeResolver,
bool)` runs them all, over every noun and every category, and returns every failure — it never
stops at the first ([DEC0006](docs/idr/DEC0006-rapport-groupe-des-refus.md)). First decide which
kind of rule you are adding:

- **A consistency rule** says the file contradicts itself: a category nobody declares, an
  exclusion that matches no word. Write it as a private method returning
  `IEnumerable<DomainError>` and add it to the list at the top of `Validate`, before the
  `allowSmall` test. It is never waived.
- **A size rule** sets a floor the theme must reach. Add it to `SizeFailures`, which `allowSmall` —
  the theme's own key or `--allow-small-theme` — waives. Measure on the pools the noun actually
  reaches, `resolver.Pool(noun)` and `resolver.ParticiplePool(noun)`, never on the raw lists
  ([DEC0003](docs/idr/DEC0003-validation-sur-le-pool-resolu.md)), and follow the segment mode the
  theme draws, `DrawnMode(resolver)`
  ([DEC0016](docs/idr/DEC0016-planchers-alignes-sur-le-mode-de-segment.md)). Its threshold is a
  public constant beside `MinimumNouns`. A floor is a ratchet: raise it once the themes have grown,
  never lower it to make a red load green.
- **Something legal that probably was not meant** is not a refusal. Add it to `Remarks`, which
  `--register` prints as a warning and `--analyze` lists in its report
  ([DEC0013](docs/idr/DEC0013-mot-declare-dans-les-deux-sections.md)).

A key of the wrong shape — a string where a number belongs — is not a rule either:
`JsonThemeSerializer` reports it as it reads the file.

Then:

1. `src/Slugger/Domain/Validation/ThemeErrors.cs` — a factory that writes the whole sentence, naming
   the noun or the word at fault, and its code in `ThemeErrors.Codes`. `ReportRenderer` prints the
   sentence as written and groups the reasons by code. (See
   [Errors](#value-objects-and-errors) for why this goes in `ThemeErrors` and not in a new
   `ThemeError` type.)
2. Nothing else to wire. A load, `--register` and `--analyze` all call `ThemeValidator`, and
   `GenerateSlugsUseCase` calls it again on a theme that a run narrows.
3. A size rule needs its margin in the `--analyze` report: a measurement in
   `src/Slugger/Domain/Analysis/ThemeMeasurements.cs` that `ThemeAnalyzer.Measure` computes, and a
   row in the margins table that `src/Slugger.Cli/Rendering/ThemeAnalysisRenderer.cs` writes
   ([DEC0014](docs/idr/DEC0014-rapport-d-analyse-d-un-theme.md)). A consistency rule shows up in the
   report without any change, as a refusal.
4. Tests: `ThemeLoadReportTests` for the refusal and its message — assert on the prose, see
   [the KillMutants gate](#the-killmutants-gate) — waived or not under `allowSmall` as the rule
   requires, and `ThemeAnalyzerTests` for a new measurement.
5. Run the whole suite. `RepositoryThemeTests` and `EmbeddedThemeCatalogTests` load every shipped
   theme without `allowSmall`: a rule that refuses one turns them red, and the theme has to grow, or
   the rule has to change. If a theme changes, or a golden-master variant is now refused, the
   [golden master](#the-golden-master) moves too.
6. Documentation: the size rules and the guide to reading a refusal in
   [`docs/writing-a-theme.md`](docs/writing-a-theme.md).

A new rule usually constrains every theme ever written, which makes it a decision: read
[DEC0003](docs/idr/DEC0003-validation-sur-le-pool-resolu.md) and
[DEC0016](docs/idr/DEC0016-planchers-alignes-sur-le-mode-de-segment.md) first, and expect to write
a new record.

### Add a built-in theme

A built-in theme is compiled into the `Slugger` package, so every consumer of the library and every
user of the command gets it. Most themes belong in `themes/` instead, which ships nothing and needs
no code change: add the file and its [golden master](#the-golden-master), run the suite —
`RepositoryThemeTests` and `DrawnSlugTests` read every file there — and follow
[`docs/reviewing-a-theme.md`](docs/reviewing-a-theme.md) for what no test can check.

To build a theme in:

1. `src/Slugger/Infrastructure/Resources/<name>.json` — add the file. `Slugger.csproj` embeds
   every `.json` in that folder, minified, under the resource name `Slugger.Themes.<name>.json`;
   there is nothing to register. When you promote a theme from `themes/`, move the file rather than
   copy it: a copy would shadow the built-in theme and appear twice in the golden master.
2. The tests that list the built-in themes by name:
   - `tests/Slugger.UnitTests/EmbeddedThemeCatalogTests.cs` — the list in
     `Serves_the_three_built_in_themes`, and the `[InlineData]` rows of the four theories that
     follow it, which check that each built-in theme is well formed, loads without `allowSmall`,
     raises no remark and is embedded minified;
   - `tests/Slugger.UnitTests/ThemeDirectoryTests.cs` and `ChainedThemeCatalogTests.cs`;
   - `tests/Slugger.Cli.UnitTests/FacadeTests.cs` and `SluggerRunnerTests.cs`
     (`Lists_the_themes_in_scope`);
   - `tests/Slugger.Cli.UnitTests/ThemeGoldenMasterTests.cs` — the `Embedded` array;
   - `tests/Slugger.UnitTests/Dummies.cs` — `AnyThemeNameOtherThanTheBuiltInOnes` excludes each
     built-in name.
3. `tests/Slugger.UnitTests/BuiltInThemeStyleTests.cs` — if the theme declares a style in its
   `defaults`, pin it there as the other built-in themes are pinned.
4. Its golden master: a theme moved from `themes/` keeps its file; a new one needs one written, as
   [described above](#the-golden-master).
5. Documentation that names or counts the built-in themes: [`README.md`](README.md),
   [`docs/cli.md`](docs/cli.md) and `src/Slugger.Cli/PACKAGE.md`. If the words come from somewhere
   else, credit the source and its licence in the README's licence section, as `docker` and
   `heroku` do.

## Conventions

A summary. [`CLAUDE.md`](CLAUDE.md) has each rule in full, with the reasoning and the measurements
behind it.

### Code style

The build enforces what `.editorconfig` can express, and CI turns it into errors: a brace on the
same line as its declaration, braces even around a one-line `if`, a block body rather than an
expression body for methods, constructors, operators and local functions, explicit types rather
than `var`, collection expressions.

The rest is for you and the reviewer:

- **One type per file**, named after the type — a test fake too.
- **A short guard clause sits on one line**: `if (x is null) { return null; }`, whenever the body
  is a single `return`, `throw`, `break` or `continue`. Consecutive guards form one block, with no
  blank line between them. `coding-rules.sh` checks both.
- **A comment that explains what an `if` tests becomes a method**: a predicate named for the
  intent, with the explanation as its `/// <summary>` —
  `if (TheRollFallsShort(random, chance)) { return null; }`.
- **A name in place of a nested call, where the name says something**: draw a position, take the
  digit at it, append the digit, as three named lines rather than one nested call. Never on a
  fluent chain such as `builder.ToString().Trim()`, and never a name that says nothing.
- **An early return rather than nesting**, and early returns rather than a compound condition. The
  code has no `else` of its own.
- **A rule lives in `CLAUDE.md`, never in a code comment.** A comment explains the code in front of
  it; it does not remind the next person of a convention.

### Tests

- Each test has three commented blocks, `// Setup`, `// Exercise` and `// Verify`, in that order. A
  block with nothing in it is left out.
- The name states the behaviour, not the method: `Glues_a_token_straight_onto_the_last_segment`,
  never `Format_Returns_Correct_Value`.
- xUnit v3 and its `Assert`; no fluent assertion library. Explicit types and block bodies, as in the
  code.
- A value the test does not care about is drawn with [JustDummies](https://github.com/Reefact/just-dummies)
  — `Any.Int32().Between(1, 100_000).Generate()`, or a shape from `Dummies` such as
  `Dummies.AnyWord()` — stating the constraint the domain imposes, never what the test asserts.
  `[assembly: Reproducible]` prints the seed when a test fails, so a failure can be replayed. Keep a
  literal when the literal is the case, and say so in the test's `/// <summary>`.
- For an exact draw, script it with `ScriptedRandomSource`, which fails when the generator asks for
  a draw the script did not expect. For a distribution, use `DefaultRandomSource(seed)` and assert a
  band, not an exact count.
- Prefer the real theme files when the point is the shipped data, and name the DEC in the summary of
  a test that pins a decision.

**Testing internal types.** `Application`, `Infrastructure` and the CLI are internal, and the test
projects reach them through `InternalsVisibleTo`. Test an internal type directly rather than only
through the facade. Two rules of C# and xUnit get in the way:

- xUnit v3 runs only **public** test classes and methods. An internal test class runs zero tests,
  with no error to say so.
- A public method cannot name an internal type in its signature, so a `[Theory]` cannot take one as
  a parameter. Split it into named `[Fact]`s, or declare the data as `TheoryData<object, ...>` and
  cast the value back inside the test.

Never make a type public only to test it.

### Value objects and errors

A type marked `[ValueObject]` — `Word`, `Term`, `Chance` and the others in `src/Slugger/Domain/` —
derives from `ValueType<T>` (from the `Value` package), holds only readonly fields and no settable
property and declares its own `ToString`, pointed at by `[DebuggerDisplay("{ToString()}")]`.
`ValueObjectRulesTests` measures all of that. Beyond what the tests measure:

- It is built through `From`, which returns an `Outcome`, or `FromOrThrow`.
- `ToString` renders it for a human, as a debugging aid; it is never how the value leaves the type.
- The value comes out through an `explicit operator`, or through the single method marked
  `[DehydrationMethod]`, `Dehydrate()`. Inside the domain, only another dehydration and
  `SlugFormatter.Format` may call it, and `DehydrationTests` reads the compiled assembly to check.
- `Adjective`, `Participle` and `Noun` are `[SemanticObject]`s: each wraps a `Term` and hands it over
  through a property called `Value`, so that the compiler refuses one where the other is expected.

**Each concept owns its errors.** The rule, for a new concept: `<Concept>Error` derives from
FirstClassErrors' `Error` and is marked `[ProvidesErrorsFor("<Concept>")]`. It has one factory per
situation with its `[DocumentedBy]` documentation, its codes in a nested `Codes` class and a
`ToException` that raises `<Concept>Exception`. It sits beside the type it speaks for.
`ChanceError` is a short example, and `ValueObjectRulesTests` checks that each such catalogue is
named after a type that exists.

Two older catalogues predate that rule and are still the ones in use:

- `ThemeErrors`, in `src/Slugger/Domain/Validation/`, a static class whose factories return
  `DomainError`. `ThemeValidator` returns `DomainError`s and `ThemeErrors.Rejected` wraps them, so
  **a new refusal of a theme goes into `ThemeErrors`**. Do not start a `ThemeError` beside it: the
  plan is to migrate the old catalogue into a new one, and two side by side would declare the same
  error codes twice. The migration is item 4 of
  [`docs/refactoring-in-progress.md`](docs/refactoring-in-progress.md).
- `CliErrors` and `CliErrorCodes`, in the CLI, where **a new refusal of the command line goes**.

### Analyzer suppressions

Fix a warning before you suppress it. When the rule really is wrong about the code, suppress it
through the catalogue constants of [DiagnosticCatalog](https://github.com/Reefact/diagnostic-catalog)
— `SonarRule`, `NetAnalyzersRule` and `CodeStyleRule`, plus `XunitRule` in the tests — and a reason from
`src/Slugger/SuppressionJustifications.cs`:

```csharp
[SuppressMessage(
    SonarRule.S3218.Category,
    SonarRule.S3218.Id,
    Justification = SuppressionJustifications.CodesMirrorTheirFactories)]
```

All three arguments are compile-time constants, so a typo is a build error rather than a
suppression that silently matches nothing. Add a new reason to `SuppressionJustifications` rather
than writing it inline. Then remove the suppression once and check that the warning comes back.

### Layering

`Domain` names nothing from `Application` or `Infrastructure`, and `Application` nothing from
`Infrastructure`. A new type in `Application` or `Infrastructure` is `internal`. The engine depends
only on `FirstClassErrors` and `Value`; a new dependency is a decision, and goes in the CLI if only
the command needs it. [`docs/architecture.md`](docs/architecture.md) explains why, and names the
tests that enforce it.

### Decision records

Read the decision records before you add an option, a validation rule or an error: most questions of
the form "why is it like this" are answered there. They are in French, and an accepted record is
never rewritten — a decision that changes gets a new one. [The index](docs/idr/README.md) has an
English summary of each and a table of the decisions that constrain each area of the code.

## Mutation testing

Two mutation engines measure the suite: Stryker.NET every night over the whole solution, and
KillMutants every night and on every pull request. If you run either one locally, **give it a home
directory of its own**: a mutant can write to your real `~/.slugger/themes` and
`~/.config/slugger/config.json`. The commands, that warning and how to read the score — which
varies from run to run — are in [`docs/mutation-testing.md`](docs/mutation-testing.md).

## Releasing

This part is for maintainers. The two packages are released separately, each by its own tag:

```bash
git tag lib-v1.2.3 && git push origin lib-v1.2.3   # Slugger, the library
git tag cli-v1.2.3 && git push origin cli-v1.2.3   # Slugger.Cli, the slugger command
```

`release.yml` then:

1. refuses a tag whose commit is not on `main` — a tag push bypasses branch protection, and a
   version on nuget.org can never be replaced;
2. checks that the version is SemVer without `+build` metadata;
3. builds, runs the whole suite again, and packs that package alone;
4. attests the provenance of the packages it built;
5. logs in to nuget.org through OIDC trusted publishing — no API key is stored anywhere — and
   pushes;
6. creates the GitHub release with the packages attached and notes generated from the pull request
   titles, marked as a prerelease when the version has a `-` label.

**Rehearse first.** Run the workflow by hand (*Actions → release → Run workflow*), choosing the
package and a version — `0.0.0-dry.1` will do. *Dry run* is ticked by default: the run does
everything up to and including the nuget.org login, and stops before the push and the GitHub
release.

**`Slugger` stays a prerelease for now.** It depends on a prerelease version of `FirstClassErrors`,
and NuGet refuses to pack a stable package with a prerelease dependency (warning NU5104, an error
under the CI ratchet): `lib-v1.0.0` fails at the pack step, `lib-v1.0.0-preview.1` does not.
`Slugger.Cli` bundles its dependencies and can be stable.

Two settings live outside the repository, and every release run, dry run included, fails at the
login step without them:

- a trusted-publishing policy on nuget.org for repository owner `Reefact`, repository `slugger` and
  workflow file `release.yml`, with no environment;
- a repository **variable** (not a secret) `NUGET_USER`, holding the nuget.org username of whoever
  created that policy — which is not necessarily the package owner.

`CLAUDE.md` records how each of these was established, failures included.
