# Working on slugger

## Build and test

```bash
dotnet build
dotnet test
dotnet run --project src/Slugger.Cli -- --theme docker --count 3
dotnet run --project src/Slugger.Cli -- --list-themes
dotnet run --project src/Slugger.Cli -- --register ./porno.json
```

The warning ratchet is scoped to CI, following the chapter's convention, so a local build stays
friendly to a half-finished refactoring. `ci.yml` builds, tests and packs every push and pull
request against `main`, on Linux and on Windows, and a runner sets `GITHUB_ACTIONS` — so a
warning that would merge cannot. `GITHUB_ACTIONS=true dotnet build` is that same answer without
waiting for a runner: run it before pushing.

**Run the tests under that variable too**, not only the build. Spectre enriches a console profile
from the environment, and its GitHub Actions enricher turns ANSI back on whatever the settings
asked for - so a test asserting on a sentence met the sentence wrapped in escape codes, on the
runner and nowhere else (measured, and it went red on `main`). `GITHUB_ACTIONS=true dotnet test
--solution slugger.slnx -c Release` is the whole pre-push check; note that the variable also
changes the reporter's output, so read the exit code rather than grepping for a summary.

## Documentation

`docs/idr/` holds the Important Decision Records - nineteen of them, each naming what it rules
out. Read the index before adding an option, a validation rule or an error: most questions
about "why is it like this" are answered there. They follow the chapter's
`important-decision-record-guideline.md`, so an accepted one is never rewritten: a decision
that changes gets a new DEC, and the old one's status line says it was superseded.

`docs/writing-a-theme.md` is for whoever writes a `.json` theme and never opens the C#.

There is no specification any more. `docs/slugger-spec.md` built the tool and was then deleted:
63% of it paraphrased code that the tests already pin, so it could only follow. Git keeps it -
`git show 96a83e7:docs/slugger-spec.md`, its last version.

## Code style

Two tools split the house style, because neither covers all of it on its own.

`.editorconfig` carries what Roslyn's C# formatter understands: a brace on the same line as its
declaration, braces required even around a one-line `if`, and no expression-bodied method,
constructor, operator or local function. `EnforceCodeStyleInBuild` already turns these into build
warnings - promoted to errors in CI by the same ratchet as everything else - so `dotnet format
slugger.slnx` is the fix, and a violation left in place fails the same way a stray warning does.

The rest - `#region` blocks around statics, fields, constructors and usings; vertical alignment of
multi-line parameters and field declarations; the 4-space indent and the space before `/>` in XML
doc comments - has no Roslyn equivalent. It lives in
[`Reefact/resharper-settings`](https://github.com/Reefact/resharper-settings)' shared
`document.DotSettings`, copied here as `slugger.slnx.DotSettings`, and applied with the JetBrains
CLI (`dotnet-tools.json` pins `jetbrains.resharper.globaltools`, restored the same way as
Stryker's tool):

```bash
dotnet tool restore   # once per clone
DOTNET_CLI_HOME="$HOME" NUGET_PACKAGES="$HOME/.nuget/packages" HOME=$(mktemp -d) \
  dotnet jb cleanupcode slugger.slnx --settings=slugger.slnx.DotSettings --profile="Really Full Cleanup"
```

**The isolated `HOME` is not optional**, for the same reason it is not optional for Stryker below:
`jb cleanupcode` writes its own caches under `~/.local/share/JetBrains`, and an isolated `HOME`
keeps that out of whoever's machine is running it.

Nothing splits a multi-type file into one type per file automatically. Neither tool moves
`SegmentModes` out of `SegmentMode.cs` and into a file of its own - that stays a decision a
reviewer asks for, not something either applies.

**One type per file, always** - a class, an interface, an enum, a record, each in a file named
after it. A fake used by one test project (`FakeThemeCatalog`, `FakeClipboard`, ...) is a type
like any other and gets its own file, even where several of them used to sit together in a
`Fakes.cs`.

**A short guard clause collapses onto one line**: `if (x is null) { return null; }` rather than
three lines, wherever the body is a single `return`, `throw`, `break` or `continue` with nothing
else going on - not for a body that does real work, which stays on its own lines. Neither
`dotnet format` nor `jb cleanupcode` performs this collapse (`KEEP_EXISTING_EMBEDDED_BLOCK_ARRANGEMENT`
does not undo a block someone already wrote as multi-line), so it is applied by hand.
`.claude/hooks/coding-rules.sh` checks this on every edit to a `.cs` file and reports a
three-line violation back to the agent that wrote it, rather than leaving it to a reviewer.

**Consecutive guards are one block, so no blank line separates them.** Three of them in a row
read as one thing - the conditions a method refuses before it starts working - and a blank line
between two of them says they are two thoughts when they are one:

```csharp
if (length < 1) { throw TokenError.LengthBelowOne(length).ToException(); }
if (chance is < 0 or > 100) { throw TokenError.ChanceOutsideAPercentage(chance).ToException(); }
if (chance == 0) { return null; }
```

A blank line before the first or after the last is what separates the block from the work around
it, and stays. The same hook reports this one too.

**A comment explaining what an `if` tests becomes a method.** Where the condition needs a
paragraph to be understood, the paragraph belongs on a named predicate as a `/// <summary>`, not
above the `if` as a `//`:

```csharp
if (TheRollFallsShort(random, chance)) { return null; }
```

The name carries the intent at the call site and the summary carries the reasoning where a reader
of the predicate will look for it - and the explanation stops being invisible to the documentation
the rest of the codebase generates.

This one is **not** in the hook, and deliberately: it is a judgement call, which the hook's own
criterion keeps out of it. Measured across the repository, nine comments sat above an `if` and only
four explained the condition - the other five said why the branch exists, which no predicate name
can hold. A check flagging all nine would be wrong more often than right.

**A name in place of a nested call, where the name says something.** Object Calisthenics calls it
one dot per line, and it is **not to be applied brutally** - `builder.ToString().Trim()` reads
perfectly well and gains nothing from being cut in two. It earns its place when the intermediate
value has a name worth writing:

```csharp
int  position = random.Next(alphabet.Length);
char digit    = alphabet.GetDigit(position);
drawn.Append(digit);
```

rather than `drawn.Append(alphabet.GetDigit(random.Next(alphabet.Length)))`. The three lines say
what the one line did: draw a position, take the digit there, write it down. Explicit types and
real names, never `var` and never `truc` - a name that says nothing is worse than the nested call
it replaced.

It buys two things. A name where a call was, which explains; and a value that can be looked at
before it is used, which is where a null or an out-of-range result stops being invisible.

**Not on a fluent chain.** `DescribeError.WithTitle(...).WithDescription(...).WithRule(...)` and
`builder.ToString().Trim()` are one expression written as several calls, not several steps. Cutting
them up names intermediate states that have no meaning of their own.

**Prefer an early return over nesting**, where it does not complicate the code: a guard clause at
the top of a method reads better than the same check wrapping the rest of the body in an `if`.
This codebase has no `else` in its own code for exactly that reason - grep it and see.

**A compound condition becomes early returns** - *Decompose Conditional*, roughly. `return
!chance.IsAlways && !chance.Covers(random.Next(100))` asks its reader to hold two negations and a
nested call at once, and the draw inside it has no name:

```csharp
if (chance.IsAlways) { return false; }

int roll = random.Next(100);

return !chance.Covers(roll);
```

Each condition that settles the answer returns it where it is decided, and what was buried in the
expression comes out with a name. This is the same instinct as preferring an early return over
nesting, applied inside an expression rather than around a block.

**A rule lives in this file, never in the code.** Where a comment exists so that whoever writes the
next one remembers a convention - take this door and not that one, derive from this base, put the
errors there - it belongs here, found once and applying everywhere. Written in the code it is
recopied into every file that obeys it, drifts from its copies, and says nothing about the lines
below it. A comment earns its place by explaining what is in front of it, not by reminding someone
of what we agreed.

## Value objects

A type carrying `[ValueObject]` keeps five rules, and `ValueObjectRulesTests` measures them: it
derives from `Value`'s `ValueType<T>` so equality is a contract rather than a reference, declares
only readonly fields and no settable property, declares its own `ToString`, and carries
`[DebuggerDisplay("{ToString()}")]` pointing at it.

**`ToString` renders it for a human** - `56 °C` for a temperature, the spelling for a word. It is a
debugging aid, never how the value leaves the type: what a slug is rendered into is the formatter's
business.

**Each concept owns its errors.** `<Concept>Error` derives from FirstClassErrors' `Error`, carries a
factory per situation with its `[DocumentedBy]` documentation, and overrides `ToException` to raise
`<Concept>Exception`. It sits beside the type it speaks for, never in a `Validation` folder: errors
that are first class are not filed away. `DomainError` cannot be the base - every one of its
constructors is internal. A concept with no refusal has no error type, and an architecture rule
holds the naming either way.

No hook and no sweep: the rule is about whether a name has something to say, which only a reader
can judge.

## Mutation testing

```bash
dotnet tool restore     # once per clone: Stryker's version is pinned in dotnet-tools.json
dotnet dotnet-stryker   # doubled on purpose - the manifest's command is `dotnet-stryker`
```

Roughly two minutes on four cores for the whole solution.
`.github/workflows/nightly-mutation.yml` runs the same tool against the same config every night,
with one addition a local run should copy — a home of its own, below.

Everything that is not a Stryker default sits in `stryker-config.json`:

- `"test-runner": "mtp"` — the test projects run on Microsoft.Testing.Platform, which xUnit v3
  requires on .NET 10. Stryker still defaults to VSTest, which cannot run them at all.
- `"solution": "slugger.slnx"` — one run mutates `Slugger` and `Slugger.Cli` together; without
  it Stryker asks for a project at a time.
**`Slugger.ArchitectureTests` cannot be kept out of a Stryker run, and trying costs nothing to
know.** `"test-projects"` is a real option and it is ignored here: measured, adding it beside
`"solution"` left the log without a single mention of it and Stryker still captured "448 tests
across 3 assemblies". Naming the solution is what decides the test set. Do not add the key back
believing it does something.

It matters less than it looks. Stryker runs in `CoverageBasedTest` mode, so a test that covers no
mutant is not run against one: the architecture rules cost the two coverage-capture passes
(measured at about forty seconds each for the whole suite) and close to nothing after that.
KillMutants discovers test projects rather than reading a solution, so there the exclusion does
work — `--exclude "tests/Slugger.ArchitectureTests/*"`, which its README defines as leaving a
project out of the run entirely.
- `"thresholds"` — `break` is the one with teeth: below it the nightly goes red. Treat it as a
  ratchet, like the warning one. Raise it as the score climbs; never lower it to make a red run
  green.

**Do not read a few points as a change.** Seven runs of one commit gave 51.19% four times, mutant
for mutant, and 56.62%, 57.04% and 57.18% the other three — a block of some forty mutants in the
CLI parser and the error literals flips between runs. Stryker's log warns that its MTP runner is
in preview, which is one suspect; the other is this suite, and the second engine below says so:
11 of its 398 mutants also change verdict between two runs, in `CommandLineParser`'s flag
literals and in the `random.Next(2)` branch of `SlugGenerator`. Part of the wobble is ours.
`break` is set at 45, under the lower mode, so the nightly reports a regression rather than it.

The figure on the commit that pinned the option chain is **54.17%**, and `OptionResolver` went
from 20 mutants killed to 39 along the way. Compare a number to that one only if you can rule
the wobble out — two runs of the same commit are the cheapest way.

**Every figure above predates DEC0019**, which replaced the hand-written `CommandLineParser` with
a declaration Spectre binds — so the mutant counts, the score and the `CommandLineParser` the
paragraph above names all describe `Slugger.Cli` as it no longer is. The engine's half of them
still holds. Re-run both tools before comparing anything about the CLI.

A survivor is a mutation no test noticed, which is a missing assertion far more often than it is
a pointless mutant. Read the HTML report — the nightly keeps it as a build artifact — rather
than the score alone.

### It writes to your home directory

A mutant that drops the `directoryPath ?? DefaultDirectoryPath` seam writes where the default
says: `~/.slugger/themes` and `~/.config/slugger/config.json`, the real ones, whatever temporary
directory the test passed in. `dotnet test` leaves both alone — measured, both ways — so this is
mutation's own hazard, and it can overwrite a theme or the saved defaults of whoever is logged
in. Give the run a home of its own:

```bash
DOTNET_CLI_HOME="$HOME" NUGET_PACKAGES="$HOME/.nuget/packages" HOME=$(mktemp -d) dotnet dotnet-stryker
```

`HOME=` goes **last**, and the order is the whole trick: a shell applies a prefix left to right,
each assignment visible to the next, so the two in front still read the real home while the third
moves the one the mutants see. Put `HOME=` first and they follow it — the tool is then looked for
in an empty home and the run dies with `Run "dotnet tool restore"` (measured, once). The
nightly does the same thing with `$RUNNER_TEMP`.

Isolating the home does not change the score — measured, both ways. It protects your files, not
the number.

### A second engine, beside it

`.github/workflows/killmutants.yml` runs [KillMutants](https://github.com/Reefact/kill-mutants)
on the same suite. It compiles one assembly per mutant and launches the xUnit 4 executable
itself, where Stryker injects every mutant into one compilation and speaks to the platform - a
different catalogue, a different selection, a different test host. Measured here: 398 mutants,
58.29% and 57.79% on two runs, 6 to 7 minutes on four cores, against Stryker's 657 tested and
54.17%. The two numbers are not comparable and are not meant to be. What is comparable is where
each says the gap is, and both say the same place: 121 of its 179 `StringLiteral` mutants are
undetected, which is the error prose nothing asserts on.

It is not on NuGet yet, so the workflow builds it from a pinned commit and a local run packs it:

```bash
git clone https://github.com/Reefact/kill-mutants /tmp/kill-mutants
dotnet pack /tmp/kill-mutants/src/KillMutants.Cli -c Release -o /tmp/km-pkg
dotnet tool install KillMutants --tool-path /tmp/km-tool --add-source /tmp/km-pkg --prerelease

DOTNET_CLI_HOME="$HOME" NUGET_PACKAGES="$HOME/.nuget/packages" HOME=$(mktemp -d) \
  /tmp/km-tool/killmutants . --exclude "tests/Slugger.ArchitectureTests/*"
```

The home of its own is not optional here either: its mutants escape into `~/.slugger` exactly
like Stryker's.

On a pull request the workflow runs `--since <base>` instead, which judges the change rather
than the repository - seconds rather than minutes - and **fails when the diff carries a mutant
nothing detects**. That gate is the reason to have it on a pull request at all; if an alpha tool
blocking a merge turns out to be the wrong trade, drop the `pull_request` trigger rather than
the tool.

## Releasing

Two packages, two trains, versioned apart:

```bash
git tag lib-v1.2.3 && git push origin lib-v1.2.3   # Slugger, the engine a consumer references
git tag cli-v1.2.3 && git push origin cli-v1.2.3   # Slugger.Cli, the `slugger` command as a tool
```

`release.yml` refuses a tag that is not an ancestor of `main` — a tag push goes round branch
protection, and a nuget.org version is immutable — then rebuilds, re-runs the suite, packs that
train alone, attests the bytes it produced, and publishes through OIDC trusted publishing. No API
key is stored anywhere. Rehearse with the workflow's manual dispatch: it defaults to a dry run
that does everything up to and including the OIDC exchange, and stops before the push. Rehearsed
green once, on `main`; the push itself is the one step no rehearsal can cover.

**A `lib` version stays prerelease for now.** `Slugger` depends on a prerelease `FirstClassErrors`,
and NuGet refuses a stable package with a prerelease dependency (NU5104, measured): `lib-v1.0.0`
fails at pack, `lib-v1.0.0-preview.1` does not. `Slugger.Cli` bundles its dependencies and is free
of it. This is a fact about the dependency, not a setting in this repository.

**Two things live outside the repository**, and every release run — dry run included — fails at
the login step until they exist:

- a trusted-publishing policy on nuget.org (*Account settings → Trusted Publishing*), with
  repository owner `Reefact`, repository `slugger`, workflow file `release.yml`, no environment.
  Its glob is `*` and its scope *Push new packages and package versions*, so one policy covers
  both trains - and that scope is what lets a first push create an id that does not exist yet,
  which is the one thing a rehearsal cannot establish, since it stops before the push;
- a repository **variable** (not a secret) `NUGET_USER`, holding the username of whoever
  **created the policy** - which is not the package owner. Measured: `Reefact` is what the policy
  page shows as *Package owner*, and it fails the exchange with `HTTP 401 [...] No matching trust
  policy owned by user 'Reefact' was found`. nuget.org names the distinction in the error itself,
  and `NuGet/login` documents the input as the account username. As a secret rather than a
  variable it reads back empty, and the login fails for that reason instead.

## Writing a unit test

Every test is split into three commented blocks, in this order. A block with nothing to say is
left out rather than written empty — most tests have no setup worth naming.

```csharp
[Fact]
public void Glues_a_token_straight_onto_the_last_segment()
{
    // Setup
    GenerationOptions options = new() { Separator = '_', TokenGlued = true };

    // Exercise
    string slug = SlugFormatter.Format(["focused", "turing"], "3", options);

    // Verify
    Assert.Equal("focused_turing3", slug);
}
```

- **Name the behaviour, not the method.** `Glues_a_token_straight_onto_the_last_segment`, not
  `Format_Returns_Correct_Value`. The name is read in a failure report by someone who did not
  write it.
- **xUnit v3 and `Assert`.** No fluent assertion library: it is what the rest of the chapter's
  repositories use, and it is one dependency the test projects do not carry.
- **Explicit types, never `var`** — `IDE0008` is a warning here, as everywhere else in the repo.
- **A value the test does not care about is drawn, not written.** See below.
- **A comment earns its place by saying why, never what.** `// Setup` blocks explain the shape
  of the fixture when it is not obvious; a comment restating the next line is noise.

### Testing internals

`Application` and `Infrastructure` are `internal`, and the test projects reach them through
`InternalsVisibleTo` — declared in `Slugger.csproj` and `Slugger.Cli.csproj`. **Test an internal
type directly rather than only through the facade**: twenty-five of the twenty-seven internal types
are covered that way today - the two left are the CLI entry point and a class of constants.

One C# rule bites, and it is worth knowing before you hit it: **a public method may not name an
internal type in its signature**, and xUnit v3 discovers only public test classes and public test
methods — an `internal` class or method silently runs zero tests, with no error to explain it
(measured, twice).

So a `[Theory]` over an internal type cannot declare it as a parameter. Two ways out, in order of
preference:

1. **Split into named `[Fact]`s.** Usually better anyway: `Force_applies_the_drawn_themes_style_even_among_several()`
   reads in a failure report where a table row does not.
2. **Widen the signature, keep the data typed.** `TheoryData<object, ...>` holds real values and
   the method casts them back:

```csharp
public static TheoryData<object, int, bool> Cases => new()
{
    { MimicStyle.Force, 3, true },   // written typed, where a mistake is caught
};

[Theory]
[MemberData(nameof(Cases))]
public void The_flag_decides(object flag, int themesInScope, bool applies) =>
    Assert.Equal(applies, OptionResolver.AppliesTheStyleOf((MimicStyle)flag, themesInScope));
```

Never make a type public just to test it. That publishes it forever to get a table today.

### Arbitrary values

A literal reads as load-bearing whether or not it is, so nobody dares change one and the test
only ever covers that case. Where the value genuinely does not matter, draw it with
[JustDummies](https://github.com/Reefact/just-dummies), stating the invariant it must satisfy:

```csharp
int seed = Any.Int32().Between(1, 100_000).Generate();
string word = Dummies.AnyWord();   // lowercase letters, 3 to 10 of them
```

`Dummies` holds the shapes this codebase treats as arbitrary — add to it rather than repeating a
constraint. **A constraint states a domain invariant, never what the test asserts.**

`[assembly: Reproducible]` pins every case to a seed that is reported **only when the test goes
red**, so values vary between runs — which is what surfaces a test secretly leaning on one — and
a failure is still replayable value for value.

Keep a literal when the literal *is* the case, and say so:

```csharp
/// <summary>
/// Literal on purpose: the accent is the whole point of the case, so an arbitrary value would
/// say nothing.
/// </summary>
[Fact]
public void Preserves_accents_instead_of_transliterating_them() =>
    Assert.Equal("rené dupont", WordNormalizer.Canonicalize(" René     Dupont "));
```

### Controlling randomness

Generation draws through `IRandomSource`. When a test needs an exact draw, use
`ScriptedRandomSource`, which hands back a written-down sequence and **fails on a mismatch**
rather than quietly asserting something else:

```csharp
ScriptedRandomSource random = new(0, 1);   // first noun, second adjective
```

Use `DefaultRandomSource(seed)` instead when the test is about a distribution rather than a
single draw, and assert a band rather than an exact count.

### What to assert on

- Prefer the real theme files over a fixture when the point is the shipped data: running the
  actual rules over `docker.json` is what caught that the written rule could not be what was
  meant, and became DEC0002.
- Pin a decision from `docs/idr/` to the file that has to honour it, and name the DEC in the
  summary. Those tests are what make a decision reviewable instead of merely written down - and
  what turns reopening one into a red build rather than a discovery six months later.

## Suppressing an analyzer warning

Fix it first. A warning that is right about the code gets a fix, not an attribute — `S2325` and
`CA1859` were both real findings.

When the rule is genuinely wrong about this code, suppress it through the catalogue, never with
literal strings:

```csharp
[SuppressMessage(
    SonarRule.S2325.Category,
    SonarRule.S2325.Id,
    Justification = SuppressionJustifications.ScaffoldedStub)]
```

All three arguments are compile-checked constants, so a typo is a build error rather than a
suppression that silently matches nothing. Add the reason to `SuppressionJustifications` rather
than writing it inline; if an existing one fits, reuse it.

Check the suppression carries its weight: remove it, confirm the warning comes back.

## Layering

`Slugger` is one assembly with three namespaces. Dependencies point inwards only, and
`NamespaceDependencyTests` fails if `Domain` ever names something from `Application` or
`Infrastructure`.

`Application` and `Infrastructure` are `internal` — they are how the engine is built, not what it
offers. The CLI and the tests reach them through `InternalsVisibleTo`. Publishing a type later is
easy; unpublishing one is a breaking change.

## Commits

[Conventional Commits](https://www.conventionalcommits.org), one intent per commit. The branch
must be green when it becomes mergeable, not every commit along the way.

## Branch names

A branch here is named by the tool this repository builds, drawn from every theme at once:

```bash
git switch -c "claude/$(dotnet run --project src/Slugger.Cli -- \
  --theme-dir themes --theme '*' --oneshot)"
```

`--theme-dir themes` is not optional: without it only the three embedded themes are in scope,
and the eleven in `themes/` are never drawn.

**It is there to be exercised, not to be pretty.** A branch name is the one place a slug meets a
real system outside the test suite: it becomes a git ref, survives a push, comes back through a
fetch. Measured over 3000 draws of `--theme '*'`, every one is a valid ref - `git
check-ref-format` refuses none, because DEC0008 turns everything that is neither a letter nor a
digit into a word boundary at load, so the characters git forbids cannot survive. About 5% carry
a non-ASCII letter, which is the half that nothing else tests; one was pushed and fetched back
byte for byte, `é` and all. Drawing from every theme also exercises what a single-theme run never
does: a theme's own defaults switching off, and `WeightedThemePicker` weighing each by its size.

**What it costs**, and it is real: the name says nothing about the work. That is accepted here
because these branches are short-lived and the pull request title carries the meaning - it would
be the wrong trade on a long-lived branch someone has to find again.
