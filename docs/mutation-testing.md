# Mutation testing

A green suite cannot tell you whether it would notice a mistake. Mutation testing asks: if one line
of the source changes — a `<` becomes `<=`, a condition is negated, a string is emptied — does any
test fail? Each changed copy of the code is a *mutant*. A mutant that no test notices *survives*,
and a survivor is a missing assertion far more often than a pointless mutant.

This page is for contributors who want to run the engines locally or read their reports. For the
check that runs on your pull request, [`CONTRIBUTING.md`](../CONTRIBUTING.md#the-killmutants-gate)
is enough.

## Two engines

| | Stryker.NET | KillMutants |
| --- | --- | --- |
| How it mutates | every mutant in one compilation, switched on at run time | one compilation per mutant, and it runs the xUnit test executable itself |
| When CI runs it | every night, or by hand: `.github/workflows/nightly-mutation.yml` | every night, on every pull request to `main` or by hand: `.github/workflows/killmutants.yml` |
| What fails the run | a score under the `break` threshold in `stryker-config.json` | on a pull request, a mutant in the diff that survives or that no test reaches |
| What it keeps | the HTML report, as a build artefact | a JSON report as a build artefact, and a summary on the run's page |

The two use different catalogues of mutations and a different test host, so their scores are not
comparable and are not meant to be. What is worth comparing is where each one says the gaps are.
When both point at the same code, believe them; when they disagree about a survivor, read it.
KillMutants is an alpha tool that is not on NuGet yet, and the decision record index lists the
two-engine set-up as an experiment that will become a decision or disappear.

## Give the run a home of its own

**A mutant can write to your real home directory.** The theme directory and the saved defaults
default to `~/.slugger/themes` and `~/.config/slugger/config.json`, and the tests always pass a
temporary directory instead. A mutant that drops the `directoryPath ?? DefaultDirectoryPath`
fallback writes to the real ones, whatever the test passed in, and can overwrite a theme or the
saved defaults of whoever is logged in. `dotnet test` touches neither; this hazard belongs to
mutation testing alone.

So every command below starts with the same prefix:

```bash
DOTNET_CLI_HOME="$HOME" NUGET_PACKAGES="$HOME/.nuget/packages" HOME=$(mktemp -d) ...
```

`HOME=` comes last, and the order matters. The shell expands the assignments from left to right,
so the first two still read your real home — where the .NET tools and the package cache are — and
only the third moves the home the mutants see. Put `HOME=` first and the tools are looked for in an
empty directory: the run stops at once and asks you to run `dotnet tool restore`.

The nightly workflows do the same with a directory under the runner's temporary folder. A home of
its own protects your files; it does not change the score.

## Stryker.NET

```bash
dotnet tool restore     # once per clone: Stryker's version is pinned in dotnet-tools.json
DOTNET_CLI_HOME="$HOME" NUGET_PACKAGES="$HOME/.nuget/packages" HOME=$(mktemp -d) dotnet dotnet-stryker
```

`dotnet dotnet-stryker` is doubled on purpose: the tool manifest names the command
`dotnet-stryker`. Everything that is not a Stryker default is in `stryker-config.json`:

- `"solution": "slugger.slnx"` — one run mutates `Slugger` and `Slugger.Cli` together.
- `"test-runner": "mtp"` — the test projects run on Microsoft.Testing.Platform, and Stryker's
  default runner, VSTest, cannot run them. Stryker's log warns that its runner for that platform is
  still in preview.
- `"thresholds"` — `high` and `low` colour the report; `break` is the score under which the run
  fails. Treat `break` as a ratchet: raise it as the score climbs, never lower it to turn a red run
  green.

The report is written to `StrykerOutput/<date>/reports/`. Read the HTML report rather than the score:
it names each surviving mutant and the line it lives on.

`Slugger.ArchitectureTests` cannot be kept out of a Stryker run. The `"test-projects"` option is
ignored when `"solution"` is set — measured: the log never mentions it, and Stryker still runs every
test assembly — so do not add it believing it does something. It costs little: Stryker runs only
the tests that cover a mutant, and the architecture tests cover none, so they add only the coverage
passes, and almost nothing afterwards.

## KillMutants

KillMutants is not on NuGet yet. Build it from source, at the commit that `killmutants.yml` pins
in `KILLMUTANTS_REF` if you want the results CI would give:

```bash
KM=$(mktemp -d)
git clone https://github.com/Reefact/kill-mutants "$KM/src"
git -C "$KM/src" checkout 9fd45e92647639eb910cae41b34631dcdfde4908
dotnet pack "$KM/src/src/KillMutants.Cli" -c Release -o "$KM/pkg"
dotnet tool install KillMutants --tool-path "$KM/tool" --add-source "$KM/pkg" --prerelease
```

Then run it from the repository root:

```bash
DOTNET_CLI_HOME="$HOME" NUGET_PACKAGES="$HOME/.nuget/packages" HOME=$(mktemp -d) \
  "$KM/tool/killmutants" . --exclude "tests/Slugger.ArchitectureTests/*"
```

- `--exclude` leaves the architecture tests out: they read the shape of the assembly rather than
  run its code, so they kill nothing and would cost a run per mutant.
- `--since origin/main` mutates only what your branch changed, the way the pull-request check judges
  your diff: from the merge base with `origin/main` to your working tree, uncommitted changes
  included. It prints no score, only a verdict, and fails if a mutant in that scope survives or is
  reached by no test at all. It takes seconds to minutes instead of the full run.
- The nightly run adds `--verify-kills 10`, which tests ten kills again on their own: a verdict
  that does not survive its own repetition was never a measurement.

## Reading the score

**Do not read a few points as a change.** The score moves between two runs of the same commit.
Before DEC0019, seven Stryker runs of one commit landed in two groups about six points apart, with
the same mutants and the same tests; a block of some forty mutants in the command-line
parser and the error messages changed verdict from one run to the next. KillMutants saw a smaller
drift of the same kind, in the old parser's flag names and in one random branch of `SlugGenerator`. So
part of the wobble comes from the tools and part from the suite. The `break` threshold sits below
the lower of the two scores, so that a red nightly means a regression rather than the wobble.

**The figures recorded in `CLAUDE.md` are out of date.** They were measured before DEC0019 replaced
the hand-written command-line parser with a declaration that Spectre binds, and before the
vocabulary refactoring, so they describe a `Slugger.Cli` that no longer exists. Before you compare a
score with anything, run both engines on the commit you want to compare against — twice, if you
need to rule the wobble out.

**Where the survivors are.** When last measured, both engines pointed at the same gap: string
literals, most of them the prose of error messages, which tests check by error code and not by
text. That is why the pull-request check
asks for a new message's prose to be asserted.
