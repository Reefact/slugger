# Contributing to slugger

This page is for anyone who changes the code, the tests or the themes shipped with `slugger`. It
covers how to build and test, what CI checks before a pull request can merge and how to make the
changes people make most often. To write a theme without touching C#,
[`docs/writing-a-theme.md`](docs/writing-a-theme.md) and
[`docs/reviewing-a-theme.md`](docs/reviewing-a-theme.md) are all you need.

The house rules are on this page, in full, with the reasoning behind each. They apply to everyone:
the AI agents that do much of the work in this repository read this same page, which
[`CLAUDE.md`](CLAUDE.md) imports, so each rule is written once. For how the code is organised, read
[`docs/architecture.md`](docs/architecture.md); for why it is organised that way, the decision
records in [`docs/idr/`](docs/idr/README.md).

- [Before you start](#before-you-start)
- [Build, test and run](#build-test-and-run)
- [The pre-push check](#the-pre-push-check)
- [What CI enforces and what review judges](#what-ci-enforces-and-what-review-judges)
- [Pull requests](#pull-requests)
- [The golden master](#the-golden-master)
- [Common changes](#common-changes) — the recipes are in [`docs/recipes.md`](docs/recipes.md)
- [Conventions](#conventions)
  - [Code style](#code-style)
  - [Tests](#tests)
  - [Value objects and errors](#value-objects-and-errors)
  - [Analyzer suppressions](#analyzer-suppressions)
  - [Layering](#layering)
  - [Documentation](#documentation) and [decision records](#decision-records)
- [Mutation testing](#mutation-testing)
- [Releasing](#releasing) — for maintainers, in [`docs/releasing.md`](docs/releasing.md)

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
dotnet run --project src/Slugger.Cli -- --list-themes
dotnet run --project src/Slugger.Cli -- --theme-dir themes --theme jazz --count 3 --oneshot
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
`tests/Slugger.ArchitectureTests` for the rules about the shape of the code. The whole suite takes
one to three minutes, depending on the machine, and almost all of it is `Slugger.Cli.UnitTests`,
where the golden master runs every theme through the command line. The progress line keeps counting
while it works: it has not hung.

`--theme-dir themes` brings in the themes of the repository's `themes/` folder; without it only the
three built-in themes are in scope.

`--oneshot` matters when you run the CLI by hand: without it, and with a terminal on both standard
input and standard output, `slugger` waits and draws another round each time you press Enter.
Ctrl+D quits.

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
  every pull request red. The ratchet is scoped to CI on purpose: without the variable, a local
  build reports warnings and still succeeds, so that a half-finished refactoring stays buildable.
  `ci.yml` builds, tests and packs every push and every pull request against `main`, on Linux and
  on Windows, so a warning that would merge cannot; setting the variable yourself gives the same
  answer without waiting for a runner.
- **Run the tests the way CI does**, with the variable set. Nothing in slugger reads it any more —
  Spectre.Console did, and turned ANSI colour codes back on under GitHub Actions, until
  [DEC0032](docs/idr/DEC0032-ligne-de-commande-et-rendu-sans-spectre.md) removed it — but the test
  runner does: it colours its own output, so read the exit code (`echo $?` after the command)
  rather than searching the output for a summary.
- **`coding-rules.sh --all`** sweeps every `.cs` file for the two rules a script can check: a guard
  clause written on three lines, and a blank line between two consecutive guards (see
  [Code style](#code-style)). It prints nothing and exits 0 when the tree is clean. It needs
  `jq`: without it, the sweep skips every file and stays silent. The same script runs as a hook
  after every edit an AI agent makes to a `.cs` file, but that hook sees only the files the
  editing tools write, never one written by a shell redirection: the sweep is the check that
  counts. Measured, it once found a violation the per-edit hook had never been shown.
- **`dotnet format style`** checks the code-style rules that `.editorconfig` declares — the ones the
  build reports as `IDE` warnings; a violation left in place fails CI like any other warning.
  Leave out `--verify-no-changes` and it fixes them. Use `style`,
  not plain `dotnet format`: the whitespace pass that plain `dotnet format` adds removes the
  vertical alignment the house style asks for, and rewrites hundreds of lines nobody touched.

In PowerShell, set the variable with `$env:GITHUB_ACTIONS = 'true'` before the first two commands,
and remove it afterwards with `Remove-Item Env:GITHUB_ACTIONS`.

### Optional: the JetBrains clean-up

Part of the house style has no Roslyn equivalent: `#region` blocks, vertical alignment of
parameters and fields, the layout of XML doc comments (see [Code style](#code-style)). It lives in
`slugger.slnx.DotSettings`, a copy of the maintainers' shared ReSharper settings, and the JetBrains
command-line tool applies it. `dotnet-tools.json` pins it as `jetbrains.resharper.globaltools`,
restored the same way as Stryker:

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
  It takes a minute or two either way. Leave out `--include` to clean the whole solution.

## What CI enforces and what review judges

| Rule | Checked by |
| --- | --- |
| No warning from the compiler, the SDK, Sonar or the `.editorconfig` code style (`IDE*`: explicit types, braces on every `if`, no expression-bodied method…) | `ci.yml`, building with warnings as errors |
| The whole suite passes, on Linux and on Windows | `ci.yml`, one job per platform |
| Layering, the engine's dependency whitelist, the value-object and entity rules | `tests/Slugger.ArchitectureTests`, part of the suite |
| Every shipped theme loads without `allowSmall` and draws what its golden master pins | `RepositoryThemeTests`, `EmbeddedThemeCatalogTests`, `ThemeGoldenMasterTests` |
| Both packages still pack | `ci.yml`, on the Linux job |
| No analyzer leaks into a package | the `AnalyzersStayPrivate` build target, `PackageWeightTests` |
| A guard clause on one line, no blank line between consecutive guards | `coding-rules.sh`, on your machine only |

Review judges the rest, because no tool can:

- One type per file, named after it — test fakes included.
- A comment that explains what an `if` tests becomes a named predicate.
- A name in place of a nested call where the name says something; never on a fluent chain.
- An early return rather than nesting, and early returns rather than a compound condition.
- The value-object rules beyond what the tests measure, and errors owned by their concept.
- A unit test's shape, its name and its arbitrary values.
- Whether a change needs a decision record.

[Conventions](#conventions) states each of these in full, with the reasoning behind it.

## Pull requests

### Branch

A branch made by an AI agent session is named by the tool this repository builds, drawn from every
theme at once — a session-start hook, `.claude/hooks/session-start-branch-name.sh`, proposes one:

```bash
git switch -c "claude/$(dotnet run --project src/Slugger.Cli -- \
  --theme-dir themes --theme '*' --oneshot)"
```

`--theme-dir themes` is not optional: without it only the built-in themes are in scope, and none of
those in `themes/` is ever drawn.

**It is there to be exercised, not to be pretty.** A branch name is the one place a slug meets a
real system outside the test suite: it becomes a git ref, survives a push, comes back through a
fetch. Measured over 3000 draws of `--theme '*'`, every one is a valid ref — `git check-ref-format`
refuses none, because [DEC0008](docs/idr/DEC0008-reduction-des-caracteres-non-alphanumeriques.md) turns everything that is neither a letter nor a
digit into a word boundary at load, so the characters git forbids cannot survive. About 5% carry a
non-ASCII letter, which is the half that nothing else tests; one was pushed and fetched back byte
for byte, `é` and all. Drawing from every theme also exercises what a single-theme run never does:
a theme's own defaults switching off, and `WeightedThemePicker` weighing each by its size.

**What it costs**, and it is real: the name says nothing about the work. That is accepted because
these branches are short-lived and the pull request title carries the meaning — it would be the
wrong trade on a long-lived branch someone has to find again.

If you name your branch by hand, name it after the change, short and descriptive:
`fix/token-chance-range`, `docs/contributing`. The maintainers decide whether the drawn convention
applies to you, and may ask for another name.

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

No mutation testing runs on a pull request: both engines sweep `main` on a schedule instead. You can
still check your own diff before you push — see [Mutating your diff](#mutating-your-diff).

### Review

The rules on `main` require no approval, so GitHub does not hold the merge waiting for one. A
maintainer reads every pull request and merges it; so far every merge has been the repository
owner's. Expect the review to cover what [the table above](#what-ci-enforces-and-what-review-judges)
leaves to people, to read the golden-master diff when slugs moved and to ask for a decision record
when a change contradicts one. Reply in the pull request itself.

A review is written and answered in [Conventional Comments](https://conventionalcomments.org):
each comment starts with a label — `issue:`, `suggestion:`, `question:`, `nitpick:` and the like —
and the author's reply starts with one too. AI agents review and answer through two skills,
`.claude/skills/review-pull-request` for the reviewer and `.claude/skills/respond-to-review` for
the author, imported from
[`Reefact/conventional-comments-toolkit`](https://github.com/Reefact/conventional-comments-toolkit).
A review session is named by slugger itself, as `review-pull-request/session-id.md` records — which
is why `dotnet-tools.json` lists the published tool.

### Mutating your diff

[KillMutants](https://github.com/Reefact/kill-mutants) run with `--since origin/main` mutates only
the code your branch changed and runs the tests against each mutant. It fails when a mutant survives
or is reached by no test at all, that is, when a line you changed can be broken without any test
noticing. Nothing runs it on your pull request — the weekly sweep finds what slips through — so run
it before you push when your change adds behaviour: see
[`docs/mutation-testing.md`](docs/mutation-testing.md#killmutants).

A diff that touches an existing test file widens the run to every project that test exercises, so
it can take as long as a full sweep.

The consequence most people run into first: **a new error message needs its prose asserted**, not
only its code. A test that checks the error code alone passes whatever the message says, so a
mutant that empties the message survives and the check goes red. Assert on the part of the
message that matters, the way `ThemeLoadReportTests` does:

```csharp
Error refusal = Assert.Single(
    Reasons(outcome),
    reason => reason.Code == SluggerThemeError.Codes.ParticiplePoolTooSmall && reason.DiagnosticMessage.Contains("noun0", StringComparison.Ordinal));
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
at once. It fails with *Theme "my-theme" has no golden master. Read what my-theme.received.txt
pins, then rename it to my-theme.verified.txt.*, having written that received file: read it, and
rename it.

```bash
dotnet test --project tests/Slugger.Cli.UnitTests --filter-class '*ThemeGoldenMasterTests'
mv tests/Slugger.Cli.UnitTests/GoldenMaster/my-theme.received.txt \
   tests/Slugger.Cli.UnitTests/GoldenMaster/my-theme.verified.txt
```

## Common changes

[`docs/recipes.md`](docs/recipes.md) lists, file by file, what each of these changes touches:

- add a session option, like `--count`;
- add an option a theme's `defaults` can also set, like `--token-glued`;
- add an option that is a command, like `--theme-info`;
- add a theme validation rule, or raise a floor;
- add a theme, to `themes/` or built in.

## Conventions

The house rules, each in full with the reasoning and the measurements behind it. They apply to
everyone, people and agents alike: [`CLAUDE.md`](CLAUDE.md) imports this page rather than keeping a
copy of its own.

### Code style

Two tools split the house style, because neither covers all of it on its own.

`.editorconfig` carries what Roslyn's C# formatter understands: a brace on the same line as its
declaration, braces even around a one-line `if`, a block body rather than an expression body for
methods, constructors, operators and local functions, explicit types rather than `var`, collection
expressions. `EnforceCodeStyleInBuild` turns these into build warnings, which CI promotes to errors
with every other warning, so a violation left in place fails the same way a stray warning does.
`dotnet format style slugger.slnx` is the fix — not plain `dotnet format`, whose whitespace pass
undoes the vertical alignment described next.

The rest — `#region` blocks around statics, fields, constructors and usings; vertical alignment of
multi-line parameters and field declarations; the 4-space indent and the space before `/>` in XML
doc comments — has no Roslyn equivalent. It lives in the shared `document.DotSettings` of
[`Reefact/resharper-settings`](https://github.com/Reefact/resharper-settings), copied here as
`slugger.slnx.DotSettings`, and [the JetBrains clean-up](#optional-the-jetbrains-clean-up) applies
it.

Neither tool does what the rules below ask: none of them moves a second type out of a file, and
none collapses a guard clause. Those stay with you and the reviewer, helped by `coding-rules.sh`
for the two a script can check.

**One type per file, always** — a class, an interface, an enum, a record, each in a file named
after it. A fake used by one test project (`FakeThemeCatalog`, `FakeClipboard`, ...) is a type like
any other and gets its own file, even where several of them used to sit together in a `Fakes.cs`.
Nothing splits a multi-type file automatically: it stays a decision a reviewer asks for.

**A short guard clause collapses onto one line**: `if (x is null) { return null; }` rather than
three lines, wherever the body is a single `return`, `throw`, `break` or `continue` with nothing
else going on — not for a body that does real work, which stays on its own lines. Neither
`dotnet format` nor `jb cleanupcode` performs this collapse
(`KEEP_EXISTING_EMBEDDED_BLOCK_ARRANGEMENT` does not undo a block someone already wrote as
multi-line), so it is applied by hand, and `coding-rules.sh` checks it.

**Consecutive guards are one block, so no blank line separates them.** Three of them in a row read
as one thing — the conditions a method refuses before it starts working — and a blank line between
two of them says they are two thoughts when they are one:

```csharp
if (length < 1) { throw TokenError.LengthBelowOne(length).ToException(); }
if (chance is < 0 or > 100) { throw TokenError.ChanceOutsideAPercentage(chance).ToException(); }
if (chance == 0) { return null; }
```

A blank line before the first or after the last is what separates the block from the work around
it, and stays. `coding-rules.sh` checks this one too.

**A method that can fail is named for what it returns.** Its plain name returns an `Outcome<T>` and
reports the failure; the same name with `OrThrow` raises it: `Word.From` and `Word.FromOrThrow`,
`SluggerTheme.LoadEmbedded` and `LoadEmbeddedOrThrow`, `SlugGenerator.For` and `ForOrThrow`,
`ICatalog.Get` and `GetOrThrow`. Never a `*Result` suffix for the reporting form: the shortest call
is the one that cannot surprise its caller with an exception
([DEC0036](docs/idr/DEC0036-api-publique-de-la-bibliotheque.md)).

**A comment explaining what an `if` tests becomes a method.** Where the condition needs a paragraph
to be understood, the paragraph belongs on a named predicate as a `/// <summary>`, not above the
`if` as a `//`:

```csharp
if (TheRollFallsShort(random, chance)) { return null; }
```

The name carries the intent at the call site and the summary carries the reasoning where a reader
of the predicate will look for it — and the explanation stops being invisible to the documentation
the rest of the codebase generates.

This one is deliberately **not** checked by a script: it is a judgement call. Measured across the
repository, nine comments sat above an `if` and only four explained the condition — the other five
said why the branch exists, which no predicate name can hold. A check flagging all nine would be
wrong more often than right.

**A name in place of a nested call, where the name says something.** Object Calisthenics calls it
one dot per line, and it is **not to be applied mechanically** — `builder.ToString().Trim()` reads
perfectly well and gains nothing from being cut in two. It earns its place when the intermediate
value has a name worth writing:

```csharp
int  position = random.Next(alphabet.Length);
char digit    = alphabet.GetDigit(position);
drawn.Append(digit);
```

rather than `drawn.Append(alphabet.GetDigit(random.Next(alphabet.Length)))`. The three lines say
what the one line did: draw a position, take the digit there, write it down. Explicit types and
real names, never `var` and never `thing` — a name that says nothing is worse than the nested call
it replaced.

It buys two things: a name where a call was, which explains; and a value that can be looked at
before it is used, which is where a null or an out-of-range result stops being invisible.

**Not on a fluent chain.** `DomainError.Create(code, diagnostic).WithPublicMessage(...)` and
`builder.ToString().Trim()` are one expression written as several calls, not several steps. Cutting
them up names intermediate states that have no meaning of their own.

**Prefer an early return over nesting**, where it does not complicate the code: a guard clause at
the top of a method reads better than the same check wrapping the rest of the body in an `if`. This
codebase has no `else` in its own code for exactly that reason — grep it and see.

**A compound condition becomes early returns** — *Decompose Conditional*, roughly.
`return !chance.IsAlways && !chance.Covers(random.Next(100))` asks its reader to hold two negations
and a nested call at once, and the draw inside it has no name:

```csharp
if (chance.IsAlways) { return false; }

int roll = random.Next(100);

return !chance.Covers(roll);
```

Each condition that settles the answer returns it where it is decided, and what was buried in the
expression comes out with a name. This is the same instinct as preferring an early return over
nesting, applied inside an expression rather than around a block.

**A rule lives in one place, written once — never in a code comment.** Where a comment exists so
that whoever writes the next one remembers a convention — go through this door and not that one,
derive from this base, put the errors there — it belongs on this page, or in the document the rule
belongs to, written once and applying everywhere. Written in the code, it is copied into every file
that obeys it, drifts from its copies, and says nothing about the lines below it. A comment earns
its place by explaining what is in front of it, not by reminding someone of what we agreed.

### Tests

Every test is split into three commented blocks, in this order. A block with nothing to say is left
out rather than written empty — most tests have no setup worth naming.

```csharp
[Fact]
public void Glues_a_token_straight_onto_the_last_segment() {
    // Setup
    GenerationOptions options = new() { Separator = '_', TokenGlued = true };

    // Exercise
    string slug = SlugFormatter.Format(["focused", "turing"], "3", options);

    // Verify
    Assert.Equal("focused_turing3", slug);
}
```

- **Name the behaviour, not the method.** `Glues_a_token_straight_onto_the_last_segment`, not
  `Format_Returns_Correct_Value`. The name is read in a failure report by someone who did not write
  it.
- **xUnit v3 and `Assert`.** No fluent assertion library: it is what the rest of the maintainers'
  repositories use, and it is one dependency the test projects do not carry.
- **Explicit types, never `var`** — `IDE0008` is a warning here, as everywhere else in the
  repository — and block bodies, as in the code.
- **A value the test does not care about is drawn, not written.** See
  [Arbitrary values](#arbitrary-values).
- **A comment earns its place by saying why, never what.** A `// Setup` block explains the shape of
  the fixture when it is not obvious; a comment restating the next line is noise.

#### Testing internal types

`Application`, `Infrastructure` and the CLI are `internal`, and the test projects reach them through
`InternalsVisibleTo`, declared in `Slugger.csproj` and `Slugger.Cli.csproj`. **Test an internal type
directly rather than only through the facade**: every internal type is covered that way except the
CLI entry point and a class of constants.

Two rules of C# and xUnit get in the way, and are worth knowing before you hit them:

- xUnit v3 discovers only **public** test classes and public test methods. An `internal` class or
  method silently runs zero tests, with no error to explain it (measured, twice).
- **A public method may not name an internal type in its signature**, so a `[Theory]` over an
  internal type cannot declare it as a parameter.

Two ways out of the second, in order of preference:

1. **Split into named `[Fact]`s.** Usually better anyway:
   `Force_applies_the_drawn_themes_style_even_among_several()` reads in a failure report where a
   table row does not.
2. **Widen the signature, keep the data typed.** `TheoryData<object, ...>` holds real values and the
   method casts them back:

```csharp
public static TheoryData<object, int, bool> Cases => new()
{
    { MimicStyle.Force, 3, true },   // written typed, where a mistake is caught
};

[Theory]
[MemberData(nameof(Cases))]
public void The_flag_decides(object flag, int themesInScope, bool applies) {
    Assert.Equal(applies, OptionResolver.AppliesTheStyleOf((MimicStyle)flag, themesInScope));
}
```

Never make a type public just to test it. That publishes it forever to get a table today.

#### Arbitrary values

A literal reads as load-bearing whether or not it is, so nobody dares change one and the test only
ever covers that case. Where the value genuinely does not matter, draw it with
[JustDummies](https://github.com/Reefact/just-dummies), stating the invariant it must satisfy:

```csharp
int seed = Any.Int32().Between(1, 100_000).Generate();
string word = Dummies.AnyWord();   // lowercase letters, 3 to 10 of them
```

`Dummies` holds the shapes this codebase treats as arbitrary — add to it rather than repeating a
constraint. **A constraint states a domain invariant, never what the test asserts.**

`[assembly: Reproducible]` pins every case to a seed that is reported **only when the test goes
red**, so values vary between runs — which is what surfaces a test secretly leaning on one — and a
failure is still replayable value for value.

Keep a literal when the literal *is* the case, and say so:

```csharp
/// <summary>
/// Literal on purpose: the accent is the whole point of the case, so an arbitrary value would
/// say nothing.
/// </summary>
[Fact]
public void Preserves_accents_instead_of_transliterating_them() {
    Assert.Equal("rené dupont", WordNormalizer.Canonicalize(" René     Dupont "));
}
```

#### Controlling randomness

Generation draws through `IRandomSource`. When a test needs an exact draw, use
`ScriptedRandomSource`, which hands back a written-down sequence and **fails on a mismatch** —
a draw the script did not expect — rather than quietly asserting something else:

```csharp
ScriptedRandomSource random = new(0, 1);   // first noun, second adjective
```

Use `DefaultRandomSource(seed)` instead when the test is about a distribution rather than a single
draw, and assert a band rather than an exact count.

#### What to assert on

- Prefer the real theme files over a fixture when the point is the shipped data: running the actual
  rules over `docker.json` showed that the rule as written could not be what was meant — the
  finding that became [DEC0002](docs/idr/DEC0002-common-atteint-par-tout-nom.md).
- Pin a decision from `docs/idr/` to the file that has to honour it, and name the DEC in the
  test's summary. Those tests are what make a decision reviewable instead of merely written down —
  and what turns reopening one into a red build rather than a discovery six months later.
- A new error message needs its prose asserted, not only its code — see
  [Mutating your diff](#mutating-your-diff).

### Value objects and errors

The newer domain types follow four ideas from domain-driven design:

- a **value object** is defined entirely by the values it holds — two with the same values are
  interchangeable — and never changes once built;
- an **entity** is defined by an identity instead: a `Theme` is its name, whatever words it holds;
- a **semantic object** is a thin wrapper that only says what a value means — this term is a noun —
  so that the compiler can tell two meanings apart;
- **dehydration** turns a value object back into the primitive it was built from, a string or a
  number: the inverse of its factory.

The words of the domain itself — slug, term, noun, epithet, token — are defined in
[`docs/ubiquitous-language.md`](docs/ubiquitous-language.md).

A type marked `[ValueObject]` — `Word`, `Term`, `Chance` and the others in `src/Slugger/Domain/` —
keeps five rules, and `ValueObjectRulesTests` measures them: it derives from `ValueType<T>` (from
the `Value` package) so that equality is a contract rather than a reference, declares only readonly
fields and no settable property, declares its own `ToString`, and carries
`[DebuggerDisplay("{ToString()}")]` pointing at it. Beyond what the tests measure:

- **How it is built.** A value object that can refuse its input — `Word`, `Term`, `Category`,
  `Chance`, `TokenLength`, `ThemeName` — is built through `From`, which returns an `Outcome`, or
  `FromOrThrow`, which raises the concept's exception. One that cannot refuse anything is built
  another way — `Token.Draw`, `TokenMould.Of`, the constructors of `Epithet` and `Slug`, the two
  instances `TokenAlphabet` offers — and has no `From`.
- **`ToString` renders it for a human** — `56 °C` for a temperature, the spelling for a word. It is
  a debugging aid, never how the value leaves the type: what a slug is rendered into is the
  formatter's business.
- **The value comes out through an explicit cast, or not at all.** A value object answers
  questions — `chance.Covers(roll)`, `length.Reaches(written)` — so that a caller keeps its meaning
  instead of re-deriving it from a number. Where something genuinely needs the number, such as
  sizing a buffer, an `explicit operator` gives it up and the `(int)` at the call site says so.
  Never an implicit one: that makes the type transparent and every rule it holds optional.
- **`Dehydrate()` is the one door out, and it only opens outwards.** A value object gives up what
  it holds through a single method marked `[DehydrationMethod]` — the inverse of its factory, so
  what `From` hydrates this gives back unchanged. A composite dehydrates to its parts dehydrated,
  which is why `Slug.Dehydrate()` hands over the segments and the token a formatter already takes.
- **Nothing inside the domain calls it**, except another dehydration composing its parts and
  `SlugFormatter.Format`, which is where the boundary is actually crossed: writing a slug out is
  turning it into the string a destination receives. The formatter cannot live outside the domain
  either: `SlugBudget` formats to measure, and the layering would refuse that dependency. The
  exception is named in the rule, so a second one is a visible edit. `DehydrationTests` reads the
  compiled assembly with Cecil and measures the call sites, not the signatures: a domain method
  reaching for a primitive has stopped asking the type and started reading it. Verified by planting
  one — `Slug.ToString` calling `_noun.Dehydrate()` turns it red.
- **A `[SemanticObject]` is the exception, and says so.** Where a type exists only to say what a
  value means — `Adjective`, `Participle`, `Noun`, each wrapping a `Term` — it hands the value over
  through a property called `Value`, always that name and never `Term` or `Spelling`. It is marked
  apart because it breaks the rule above on purpose: the compiler can then refuse
  `ParticiplePool(noun, participle)` where two terms would have passed for one another, which is
  the whole of what it buys.

  Composed rather than derived, and measured: `adjective.Value == participle.Value` is true where
  the term is the same, `adjective.Equals(participle)` is false, and `adjective == participle` does
  not compile at all. Deriving both from `Term` gives all three the same answer, whichever way the
  equality is written — a cast changes nothing, since `Equals` dispatches on the runtime type.
- **`Theme` is the one `[Entity]`.** `EntityRulesTests` checks that it compares by identity rather
  than by contents, and that it has no public constructor.

**Each concept owns its errors.** For a concept that can refuse something, `<Concept>Error`
derives from `Error`, the engine's own (`src/Slugger/Errors/`,
[DEC0029](docs/idr/DEC0029-modele-d-erreurs-propre-au-moteur.md)). It carries a factory per
situation, whose XML documentation says the rule and what usually breaks it, its codes in a nested
`Codes` class, and overrides `ToException` to raise `<Concept>Exception`. `DomainError` cannot be
the base: it is sealed. The error type sits beside the type it speaks for, never in a `Validation`
folder: errors that are first class are not filed away. `ChanceError` is a short example, and
`ValueObjectRulesTests` checks that each such catalogue is named after a type that exists. A
concept that refuses nothing has no error type: `Token`, `TokenMould`, `Epithet` and `Slug` have
none, and `TokenAlphabet` has one only for the digit position it can refuse.

**Errors are built inside the engine only.** The error model's own primitives stay internal —
`DomainError.Create`, `ErrorCode.Create`, `Outcome<T>.Success` — and so does every catalogue's
factory: outside the engine nobody builds an error, and opening one later breaks nobody where
closing one breaks everybody. A concept's catalogue is internal along with the type it speaks for.

**A new refusal of a theme goes into `SluggerThemeError`**, in `src/Slugger/Domain/`, the error of
the theme a consumer holds ([DEC0035](docs/idr/DEC0035-erreur-de-theme-du-theme-public.md)). It is
the one public catalogue, named after `SluggerTheme`, the public type it speaks for, and not after
the internal `Theme`: public as a type to read — the type of every refusal, with its `Codes` and
context keys — never as factories. Do not start a `ThemeError` beside it, for the `Theme` entity:
once wired in, the entity raises `SluggerThemeError` too, so that no code is declared twice.

One older catalogue predates that rule and is still the one in use: `CliErrors` and
`CliErrorCodes`, in the CLI, where **a new refusal of the command line goes**.

No hook and no sweep checks the naming of an error beyond what the architecture tests measure: the
rule is about whether a name has something to say, which only a reader can judge.

### Analyzer suppressions

Fix a warning before you suppress it. A warning that is right about the code gets a fix, not an
attribute — `S2325` and `CA1859` were both real findings here.

When the rule really is wrong about the code, suppress it through the catalogue constants of
[DiagnosticCatalog](https://github.com/Reefact/diagnostic-catalog) — `SonarRule`, `NetAnalyzersRule`
and `CodeStyleRule`, plus `XunitRule` in the tests — and a reason from
`src/Slugger/SuppressionJustifications.cs`, never with literal strings:

```csharp
[SuppressMessage(
    SonarRule.S3218.Category,
    SonarRule.S3218.Id,
    Justification = SuppressionJustifications.CodesMirrorTheirFactories)]
```

All three arguments are compile-time constants, so a typo is a build error rather than a
suppression that silently matches nothing. Reuse an existing reason when one fits; otherwise add
the new one to `SuppressionJustifications` rather than writing it inline. Then check that the
suppression pulls its weight: remove it once and confirm the warning comes back.

### Layering

`Slugger` is one assembly. What a consumer uses sits in the root namespace, `Slugger` —
`SluggerTheme`, `SlugGenerator`, `GenerationOptions` and the types around them — with the error
model in `Slugger.Errors` ([DEC0036](docs/idr/DEC0036-api-publique-de-la-bibliotheque.md)). How the
engine is built sits in three internal namespaces, `Domain`, `Application` and `Infrastructure`.

Dependencies point inwards only. `Domain` names nothing from `Application` or `Infrastructure`, and
`Application` nothing from `Infrastructure`. `NamespaceDependencyTests` fails if `Domain` ever names
something from the other two, or if a public type's signature names a use case or an
infrastructure type: a public type reaches the infrastructure in its body only, as
`SluggerTheme.LoadFromFile` does.

`Domain`, `Application` and `Infrastructure` are `internal` — they are how the engine is built, not
what it offers — and the CLI and the tests reach them through `InternalsVisibleTo`. A new type in
`Application` or `Infrastructure` is `internal`, and so is a new type in `Domain` until the engine's
public API needs it. Publishing a type later is easy; unpublishing one is a breaking change.
`PublicSurfaceTests` lists every public type of the engine, so publishing one is an edit to that
list rather than an accident. The vocabulary of the refactoring described in
[`docs/refactoring-in-progress.md`](docs/refactoring-in-progress.md) stays off that list until
loading and generation use it.

The engine depends only on `Value`; a new dependency is a decision, and goes in the CLI if only the
command needs it. [`docs/architecture.md`](docs/architecture.md) explains why, and names the tests
that enforce it.

### Documentation

Everything is written in English except the decision records, which are in French.

The human-facing documentation starts at [`README.md`](README.md), which maps it by reader:
[`docs/cli.md`](docs/cli.md) for the command, [`docs/library.md`](docs/library.md) for the engine
from C#, [`docs/writing-a-theme.md`](docs/writing-a-theme.md) and
[`docs/reviewing-a-theme.md`](docs/reviewing-a-theme.md) for whoever writes a `.json` theme and
never opens the C#, this page and [`docs/architecture.md`](docs/architecture.md) for contributors.
**A change that alters behaviour updates the page its reader would look in.**

The two NuGet package pages, `src/Slugger/PACKAGE.md` and `src/Slugger.Cli/PACKAGE.md`, sell: they
link to GitHub with absolute URLs, and never repeat the documentation.

There is no specification any more. `docs/slugger-spec.md` built the tool and was then deleted: 63%
of it paraphrased code that the tests already pin, so it could only lag behind. Git keeps it —
`git show 96a83e7:docs/slugger-spec.md` shows its last version.

### Decision records

`docs/idr/` holds the Important Decision Records, each naming what it rules out. Read them before
you add an option, a validation rule or an error: most questions of the form "why is it like this"
are answered there. They are written in French and follow the maintainers'
[IDR guideline](https://github.com/Reefact/guidelines/blob/main/important-decision-record-guideline.md),
so an accepted record is never rewritten: a decision that changes gets a new DEC, and the old
one's status line says it was superseded.
[The index](docs/idr/README.md) has an English summary of each and a table of the decisions that
constrain each area of the code.

## Mutation testing

Two mutation engines measure the suite: Stryker.NET and KillMutants, each over the whole
solution every Monday. If you run either one locally, **give it a home directory of its own**: a
mutant can write to your real `~/.slugger/themes` and `~/.config/slugger/config.json`. The
commands, that warning, why neither engine runs on a pull request and how to read the score —
which varies from run to run — are in [`docs/mutation-testing.md`](docs/mutation-testing.md).

## Releasing

Releasing is for maintainers: the two tags, what `release.yml` does, the dry run, the settings
that live outside the repository, what the `1.x` of `Slugger.Core` promises and how the icons are drawn
are in [`docs/releasing.md`](docs/releasing.md).
