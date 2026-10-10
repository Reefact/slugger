# Recipes for common changes

Each recipe lists the files a change of that kind touches, in the order the value travels through
them. Copy the existing option or rule the recipe names: the code around it explains the details.
For building, testing and opening a pull request, see [`CONTRIBUTING.md`](../CONTRIBUTING.md); for
where each file sits, [`architecture.md`](architecture.md).

- [Add a session option, like `--count`](#add-a-session-option-like---count)
- [Add an option a theme's `defaults` can also set, like `--token-glued`](#add-an-option-a-themes-defaults-can-also-set-like---token-glued)
- [Add an option that is a command, like `--theme-info`](#add-an-option-that-is-a-command-like---theme-info)
- [Add a theme validation rule](#add-a-theme-validation-rule)
- [Add a theme](#add-a-theme)

## Add a session option, like `--count`

A session option shapes the run as a whole — how many slugs, the seed, the interactive loop — and
a theme has no say in it.

1. `src/Slugger.Cli/CommandLine/SluggerSettings.cs` — declare it with `[CommandOption]` and a
   `[Description]`, which is its `--help` text. An option that takes a value is bound as text, a
   `string?`: given a number type, Spectre would convert as it binds and stop at the first failure.
   A switch is declared with `[true|false]` and bound as a `FlagValue<string>?`, like `--oneshot`
   and `--mimic-style`, so that it can be absent, on or explicitly off.
2. `src/Slugger.Cli/CommandLine/CommandLineReader.cs` — convert it in the `Reading` constructor,
   with the helpers already there (`Number`, `Choice`, `SingleCharacter` and `Switch`). `Switch`
   reads a switch as a `bool?`: null when absent, `false` for `--flag false`. The helpers add a
   complaint and carry on rather than throw, so a command line with three mistakes reports three
   ([DEC0006](idr/DEC0006-rapport-groupe-des-refus.md)). A new kind of complaint gets a factory in
   `CliErrors.cs` and a code in `CliErrorCodes.cs`. **Nothing checks that the reader reads every
   declared option.** One declared in `SluggerSettings` and never read here shows in `--help`, is
   accepted on the command line and then does nothing — and no test notices.
3. `src/Slugger/Application/Options/SluggerOptions.cs` — add a nullable property. Null means "this
   layer says nothing", which is what lets the defaults saved by `--init` take effect
   ([DEC0004](idr/DEC0004-chaine-de-priorite-unique.md)). A switch that was not passed is null,
   never `false`.
4. `src/Slugger/Application/Options/OptionResolver.cs` — add it to `Merge`. **Nothing checks that
   `Merge` lists every property either.** Forget it there and, as soon as a config file exists, the
   merged options lose it: the value typed on the command line is ignored, and `--init` can no
   longer save it — while every test that runs without a config stays green.
5. Use it from the merged options: `GenerateSlugsUseCase.Execute` reads `session.Count`;
   `SluggerRunner.Generate` reads `session.Oneshot`, because the loop is a decision about the
   terminal, and `session.Seed`, to make the one random source every round of the session draws
   from.
6. `--init` saves it with no further work: `SaveDefaultsUseCase` merges the line over the saved
   config, and `XdgConfigStore` writes `SluggerOptions` as JSON.
7. Tests: `CommandLineReaderTests` (read, and refused), `OptionResolverTests` (merged),
   `OptionPrecedenceTests` (saved, then overridden on the command line). `GeneratedHelpTests`
   already checks that `--help` lists every declared option.
8. Documentation: the option reference in [`cli.md`](cli.md).

The command line is declared once, on `SluggerSettings`, and `--help` is drawn from it
([DEC0019](idr/DEC0019-ligne-de-commande-declaree-et-rendue-par-spectre.md)): do not write a second
list of options anywhere in the code.

## Add an option a theme's `defaults` can also set, like `--token-glued`

These options decide how a slug looks, and a theme can state its own style for them. They go
through the whole precedence chain: the command line, then the drawn theme's `defaults` (when one
theme is drawn, or with `--mimic-style`), then the saved defaults, then the program's default
([DEC0004](idr/DEC0004-chaine-de-priorite-unique.md)).

1. `SluggerSettings.cs`, `CommandLineReader.cs`, `SluggerOptions.cs` and `OptionResolver.Merge` —
   as for a session option, warnings included.
2. `OptionResolver.cs`, `LayOver` — the per-theme chain. Like `Merge`, it lists every option by
   hand, and nothing checks it.
3. `src/Slugger/Domain/GenerationOptions.cs` — the property the engine reads, with the program's
   default as its initial value.
4. `src/Slugger/Domain/ThemeDefaults.cs` — the nullable property a theme may set.
5. `GenerationOptions.WithDefaultsOf` — lay the theme's value over the options.
6. `src/Slugger/Infrastructure/Serialization/JsonThemeSerializer.cs`, `ReadDefaults` — read the key
   (`"tokenGlued"`) with the existing readers, which report a malformed value instead of throwing.
7. The engine code that uses it — `SlugFormatter.Format` for `TokenGlued`. Apply it when the slug
   is drawn or rendered, never when the theme is loaded
   ([DEC0005](idr/DEC0005-format-resolu-au-tirage.md)). `SlugBudget` measures with the formatter,
   so `--max-length` takes the new option into account by itself.
8. Tests: the reader, `OptionResolverTests`, `JsonThemeSerializerTests`, `OptionPrecedenceTests`
   (which of the theme's style, the saved defaults and the command line wins) and the tests of the
   code that uses it, such as `SlugFormatterTests`.
9. Documentation: the option reference in [`cli.md`](cli.md), the schema in
   [`theme-reference.md`](theme-reference.md) and the `defaults` section of
   [`writing-a-theme.md`](writing-a-theme.md).

If the program's default stays the old behaviour, no golden master moves. An option that is not a
switch needs a value of its own to say "explicitly off" over a theme's `defaults` or the saved
config, as `--max-segment-words none` does
([DEC0024](idr/DEC0024-aucun-plafond-explicite-qui-outrepasse-le-theme.md)); a switch has one
already, `false`.

## Add an option that is a command, like `--theme-info`

A command replaces drawing slugs: it does one thing and exits.

1. `SluggerSettings.cs` — declare it; it takes a value when the command needs a name or a path.
2. `src/Slugger.Cli/CommandLine/CliCommand.cs` — add a member.
3. `CommandLineReader.cs`, `ReadCommand` — add an `Asked(...)` line. Two commands on one line are
   refused with `CliErrors.OnlyOneCommand`, never silently resolved.
4. `src/Slugger/Application/UseCases/` — a new `internal sealed` use case, taking the ports it needs
   in its primary constructor and returning a result — an `Outcome` when it can be refused — rather
   than printing anything. If it needs something new from a port, add it to the interface in
   `src/Slugger/Application/Abstractions/` and to every implementation, test fakes included:
   `--theme-info` added `IThemeCatalog.Parse` to the embedded, file-system and chained catalogues
   and to `FakeThemeCatalog`.
5. `src/Slugger.Cli/Program.cs` — build the use case in the composition root and pass it to
   `SluggerRunner`.
6. `src/Slugger.Cli/SluggerRunner.cs` — a constructor parameter, an arm in the `switch` of `Run`,
   and a private method that writes the result or hands a refusal to `Report`. Decide which options
   the use case receives: the command line alone (`request.Options`), or the line merged over the
   saved defaults (`session`).
7. Every test that builds a `SluggerRunner` by hand gains the new argument: `SluggerRunnerTests`,
   `OptionPrecedenceTests` and `ThemeGoldenMasterTests`.
8. Tests: `CommandLineReaderTests` (recognised, and refused beside another command), a test of the
   use case in `tests/Slugger.UnitTests` and `SluggerRunnerTests` for what it prints.
9. Documentation: the commands table in [`cli.md`](cli.md), and an `AddExample` in
   `SluggerApp.Build` if `--help` should show it.

## Add a theme validation rule

Every rule lives in `src/Slugger/Domain/Validation/ThemeValidator.cs`. `Validate(ThemeResolver,
bool)` runs them all, over every noun and every category, and returns every failure — it never
stops at the first ([DEC0006](idr/DEC0006-rapport-groupe-des-refus.md)). First decide which kind
of rule you are adding:

- **A consistency rule** says the file contradicts itself: a category nobody declares, an
  exclusion that matches no word. Write it as a private method returning
  `IEnumerable<DomainError>` and add it to the list at the top of `Validate`, before the
  `allowSmall` test. It is never waived.
- **A size rule** sets a floor the theme must reach. Add it to `SizeFailures`, which `allowSmall` —
  the theme's own key or `--allow-small-theme` — waives. Measure on the pools the noun actually
  reaches, `resolver.Pool(noun)` and `resolver.ParticiplePool(noun)`, never on the raw lists
  ([DEC0003](idr/DEC0003-validation-sur-le-pool-resolu.md)), and follow the segment mode the theme
  draws, `DrawnMode(resolver)`
  ([DEC0016](idr/DEC0016-planchers-alignes-sur-le-mode-de-segment.md)). The floor itself is a
  public constant beside `MinimumNouns`. A floor is a ratchet: raise it once the themes have grown,
  never lower it to make a red load green.
- **Something legal that probably was not meant** is not a refusal. Add it to `Remarks`, which
  `--register` prints as a warning and `--analyze` lists in its report
  ([DEC0013](idr/DEC0013-mot-declare-dans-les-deux-sections.md)).

A key of the wrong shape — a string where a number belongs — is not a rule either:
`JsonThemeSerializer` reports it as it reads the file.

Then:

1. `src/Slugger/Domain/Validation/ThemeErrors.cs` — a factory that writes the whole sentence, naming
   the noun or the word at fault, and its code in `ThemeErrors.Codes`. `ReportRenderer` prints the
   sentence as written and groups the reasons by code.
   [`CONTRIBUTING.md`](../CONTRIBUTING.md#value-objects-and-errors) explains why this goes in
   `ThemeErrors` and not in a new `ThemeError` type.
2. Nothing else to wire. A load, `--register` and `--analyze` all call `ThemeValidator`, and
   `GenerateSlugsUseCase` calls it again on a theme that a run narrows.
3. A size rule needs its margin in the `--analyze` report: a measurement in
   `src/Slugger/Domain/Analysis/ThemeMeasurements.cs` that `ThemeAnalyzer.Measure` computes, and a
   row in the margins table that `Margins` in `src/Slugger.Cli/Rendering/ThemeAnalysisRenderer.cs`
   writes ([DEC0014](idr/DEC0014-rapport-d-analyse-d-un-theme.md)). A consistency rule shows up in
   the report without any change, as a refusal.
4. Tests: `ThemeLoadReportTests` for the refusal and its message — assert on the prose, see
   [the KillMutants gate](../CONTRIBUTING.md#the-killmutants-gate) — waived or not under
   `allowSmall` as the rule requires, and `ThemeAnalyzerTests` for a new measurement.
5. Run the whole suite. `RepositoryThemeTests` and `EmbeddedThemeCatalogTests` load every shipped
   theme without `allowSmall`: a rule that refuses one turns them red, and the theme has to grow or
   the rule has to change. If a theme changes, or a golden-master variant is now refused, the
   [golden master](../CONTRIBUTING.md#the-golden-master) moves too.
6. Documentation: the new message in the error catalogue of
   [`theme-reference.md`](theme-reference.md), and the size rules and the guide to reading a refusal
   in [`writing-a-theme.md`](writing-a-theme.md).

**Raising an existing floor** is a change of the constant in `ThemeValidator`, with two things
around it. The `--analyze` report reads every floor from `ThemeValidator` and needs no change. The
prose that quotes a floor has to follow:

```bash
grep -rln -E 'at least 100|100 nouns|40,000|40 000|at least 20\b' README.md docs/*.md src/*/PACKAGE.md themes/README.md
```

And every shipped theme has to clear the new floor, which step 5 above checks.

A new rule usually constrains every theme ever written, which makes it a decision: read
[DEC0003](idr/DEC0003-validation-sur-le-pool-resolu.md) and
[DEC0016](idr/DEC0016-planchers-alignes-sur-le-mode-de-segment.md) first, and expect to write a new
record.

## Add a theme

**Most themes belong in `themes/`**, which is not part of any package and needs no code change:
add the file and its [golden master](../CONTRIBUTING.md#the-golden-master), then run the suite —
`RepositoryThemeTests` and `DrawnSlugTests` read every file there. What no test can check, such as
whether an adjective really fits its nouns, is the subject of
[`reviewing-a-theme.md`](reviewing-a-theme.md), and
[`themes/README.md`](../themes/README.md#proposing-a-new-theme) lists what to check before you
open the pull request.

**A built-in theme** is compiled into the `Slugger` package, so every consumer of the library and
every user of the command gets it. To build a theme in:

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
   [`CONTRIBUTING.md`](../CONTRIBUTING.md#the-golden-master) describes.
5. The documentation that names, counts or prints the built-in themes. It is spread over the README,
   the user guides, both package pages and `themes/README.md`; a list written here would be the
   first thing to go stale, so find it instead:

   ```bash
   grep -rln -iE 'docker.*heroku|heroku.*docker|three (built-in|themes|ship)|built-in themes|ListEmbedded' \
     README.md docs/*.md src/*/PACKAGE.md themes/README.md
   ```

   The decision records in `docs/idr/` name the built-in themes too, and stay as they are. If the
   words come from somewhere else, credit the source and its licence in the README's licence
   section, as `docker` and `heroku` do.
