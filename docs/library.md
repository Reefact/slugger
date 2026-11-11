# Using the `Slugger` library

`Slugger` is the engine behind the `slugger` command, published as a NuGet package so that you can
generate readable random names — `vigorous_herschel`, `bleak-tooth-0360` — from C#. This page is for
a .NET developer who references the package. To run the command instead, see [cli.md](cli.md); to
write your own vocabulary, see [writing-a-theme.md](writing-a-theme.md).

- [Install](#install)
- [Quick start](#quick-start)
- [The API at a glance](#the-api-at-a-glance)
- [Loading themes](#loading-themes)
- [Reproducible slugs](#reproducible-slugs)
- [Errors](#errors)
- [Shaping slugs](#shaping-slugs)
- [Length and word limits](#length-and-word-limits)
- [Cost and threads](#cost-and-threads)
- [Repeats and uniqueness](#repeats-and-uniqueness)
- [Stability](#stability)
- [See also](#see-also)

## Install

```bash
dotnet add package Slugger --version 1.0.0-preview.1
```

The package is a preview, and the version is pinned: the id `Slugger` is moving to the command
([DEC0033](idr/DEC0033-identifiants-de-paquet-inverses.md)), and once a tool version of it is
published, an unpinned `dotnet add package Slugger --prerelease` picks the tool and fails with
`NU1212`. The pinned version stays installable throughout.

- **Target framework:** `net10.0` only.
- **Dependencies:** [Value](https://www.nuget.org/packages/Value), which provides the `ValueType<T>`
  base class of `ErrorCode` and of the engine's value objects. It comes in with the package. `Outcome<T>` and the error types are the library's own, in
  `Slugger.Errors`.
- **Licence:** Apache-2.0.
- **Trimming and Native AOT:** no warning in a test publish, not promised — see [Stability](#stability).
- **IntelliSense:** the documentation comments this page refers to ship from the preview after
  `1.0.0-preview.1`. The `1.0.0-preview.1` on nuget.org predates them.

## Quick start

```csharp
using Slugger;

// Load once, keep it: every load parses and validates the theme again.
SluggerTheme docker = SluggerTheme.LoadEmbeddedOrThrow("docker");

// A generator draws in the theme's own style, settled and checked once.
SlugGenerator generator = SlugGenerator.ForOrThrow(docker);
Console.WriteLine(generator.Next());

// The library's defaults instead of the theme's style: ignore the style you are handed.
SlugGenerator plain = SlugGenerator.ForOrThrow(docker, _ => GenerationOptions.Default);
Console.WriteLine(plain.Next());

// The theme's style with a few options changed by a `with` expression; the rest stays as it was.
SlugGenerator mine = SlugGenerator.ForOrThrow(docker, style => style with { Casing = Casing.Kebab, TokenLength = 4, TokenChance = 100 });
Console.WriteLine(mine.Next());
```

For example:

```text
stargazing_blackwell
noble-forking-easley
benevolent-saha3317
```

Four things to note:

- **One namespace.** Everything you need to load and draw is in `Slugger`; the error model is in
  `Slugger.Errors`, for when you read a refusal in detail.
- **A plain name reports, `OrThrow` throws.** `SluggerTheme.LoadEmbedded` and `SlugGenerator.For`
  return an `Outcome<T>` and never throw for a refused theme; `LoadEmbeddedOrThrow` and `ForOrThrow`
  return the value or throw a `SluggerThemeException` (see [Errors](#errors)).
- **A generator starts from the theme's own style.** `docker` declares snake case, one term before
  the noun and a rare one-digit token, and its generator draws that way. The adjustment you pass
  receives that style: change what you need with `style with { ... }`, or return
  `GenerationOptions.Default` (with your changes) to ignore it. `GenerationOptions.Default` draws three
  terms — an adjective, a participle and the noun — in kebab case.
- **`GenerationOptions` is an immutable record.** `with` gives you a copy, so one set of options can
  be shared freely.

`SluggerTheme.EmbeddedNames` lists the themes compiled into the library: `docker`, `heroku` and
`slugger`.

## The API at a glance

The path from a theme to a slug runs through three types:

```text
SluggerTheme  →  SlugGenerator                  →  string
(load)           (the theme's style, adjusted,     (the slug, from Next)
                  reduced and checked once)
```

| To | Use | Namespace |
| --- | --- | --- |
| Load a theme | `SluggerTheme.LoadEmbedded`, `LoadFromFile`, `LoadFromJson`, `LoadFromStream` and their `OrThrow` forms | `Slugger` |
| Measure a theme, accepted or not | `SluggerTheme.AnalyzeEmbedded`, `AnalyzeFromFile`, `AnalyzeFromJson`, `AnalyzeFromStream`, returning a `ThemeAnalysis` | `Slugger` |
| Read what a theme says about itself | `SluggerTheme`, with `ThemeDefaults`, `ThemeMetadata` and `MaxLength` | `Slugger` |
| Say how to draw and how to write | `GenerationOptions`, `Casing`, `SegmentMode` | `Slugger` |
| Draw, from one theme or several | `SlugGenerator.For` or `ForOrThrow`, then `Next` | `Slugger` |
| Control the randomness | `IRandomSource`, `DefaultRandomSource` | `Slugger` |
| Know how many slugs your options leave | `SlugGenerator.Combinations` | `Slugger` |
| Know the floors a theme must clear | `ThemeFloors` | `Slugger` |
| Read a refusal | `SluggerThemeError`, `SluggerThemeException`; `Outcome<T>`, `Error`, `ErrorCode` | `Slugger`; `Slugger.Errors` |
| Branch on a refusal | `SluggerThemeError.Codes` | `Slugger` |

The rest of the engine is internal ([DEC0036](idr/DEC0036-api-publique-de-la-bibliotheque.md)): one
way to load, one way to draw, and nothing that works on a theme's words, which a consumer never sees
([DEC0034](idr/DEC0034-theme-public-opaque.md)).

A refactoring of the library's vocabulary is under way
([refactoring-in-progress.md](refactoring-in-progress.md)), and its types — `Theme`, `Word`, `Term`,
`Slug` and the rest — are internal until loading and generation use them.

## Loading themes

Every `Load*` method comes in two forms: `Load*` returns an `Outcome<SluggerTheme>` and never throws
for a refused theme, and `Load*OrThrow` returns the theme or throws (see [Errors](#errors)). The
same rule names everything else: a plain name reports, `OrThrow` throws.

### From a file or a string

```csharp
using Slugger;

// From a file: the theme is named after the file, without its extension.
SluggerTheme spices = SluggerTheme.LoadFromFileOrThrow("themes/spices.json");
Console.WriteLine($"{spices.Name}: {SlugGenerator.ForOrThrow(spices).Next()}");

// From a string: raw JSON has no file name, so you name it.
string json = """
    {
      "allowSmall": true,
      "adjectives": { "common": ["slow", "wide", "muddy"] },
      "nouns": [ { "value": "delta" }, { "value": "ford" }, { "value": "oxbow" } ]
    }
    """;
SluggerTheme rivers = SluggerTheme.LoadFromJsonOrThrow(json, "rivers");
Console.WriteLine($"{rivers.Name}: {SlugGenerator.ForOrThrow(rivers).Next()}");
```

With this `themes/spices.json`:

```json
{
  "allowSmall": true,
  "adjectives": {
    "common": ["warm", "golden", "bitter", "smoky"],
    "seed": ["cracked", "toasted"]
  },
  "nouns": [
    { "value": "saffron" },
    { "value": "sumac" },
    { "value": "cumin", "categories": ["seed"] },
    { "value": "fenugreek", "categories": ["seed"] }
  ],
  "defaults": { "sep": "_" }
}
```

it prints, for example:

```text
spices: toasted_fenugreek
rivers: slow-oxbow
```

`"allowSmall": true` keeps these examples short: without it, a theme needs at least 100 nouns, each
reaching at least 100 adjectives, and the load refuses anything smaller. A theme you ship should
clear those floors; [writing-a-theme.md](writing-a-theme.md) explains them, and `ThemeFloors` holds
their values. The `sep` in the `defaults` block is part of the theme's style, which a generator
applies: that is why `toasted_fenugreek` has an underscore and `slow-oxbow`, from a theme with no
style, the library's default hyphen.

A relative path is resolved against the current directory, not against your program's folder. For
a theme file copied next to your executable, build the path from `AppContext.BaseDirectory`.

To waive the floors for a theme that does not set `"allowSmall": true` itself, pass
`allowSmall: true` to any load, in either form: `LoadFromFileOrThrow(path, allowSmall: true)`.

A theme is also how to build one in memory: there is no constructor, so write its JSON and load it
with `LoadFromJson`, which holds it to the same rules as a file.

### From a resource embedded in your assembly

Pass the resource's stream to `LoadFromStream`, which reads it to its end and leaves it open.

```xml
<ItemGroup>
  <EmbeddedResource Include="Themes/spices.json" LogicalName="MyApp.Themes.spices.json" />
</ItemGroup>
```

```csharp
using System.Reflection;
using Slugger;

Assembly assembly = typeof(Program).Assembly;
using Stream stream = assembly.GetManifestResourceStream("MyApp.Themes.spices.json")
                      ?? throw new InvalidOperationException("The spices theme is not embedded.");

SluggerTheme spices = SluggerTheme.LoadFromStreamOrThrow(stream, "spices");
Console.WriteLine(SlugGenerator.ForOrThrow(spices).Next());
```

```text
toasted_cumin
```

### From a folder

The library has no folder API: enumerate the files and load each one.

```csharp
using Slugger;
using Slugger.Errors;

// No folder API: enumerate the files and load each one.
// Sorted, because each theme's share of the draw follows the list's order and a file system's order is not stable.
List<SluggerTheme> themes = [];
foreach (string path in Directory.EnumerateFiles("themes", "*.json").Order(StringComparer.Ordinal)) {
    Outcome<SluggerTheme> loaded = SluggerTheme.LoadFromFile(path);
    if (loaded.Error is { } refusal) {
        Console.Error.WriteLine($"{refusal.DiagnosticMessage}, skipped");
        continue;
    }

    themes.Add(loaded.GetResultOrThrow());
}

// Each theme weighs as much as it has nouns, so every noun keeps the same chance,
// and each is drawn in its own style.
SlugGenerator generator = SlugGenerator.ForOrThrow(themes);
DefaultRandomSource random = new(1);
for (int i = 0; i < 4; i++) {
    Console.WriteLine(generator.Next(random));
}
```

With a `themes` folder that holds the `spices.json` above and a `rivers.json` with the JSON of the
string example, it prints:

```text
wide-delta
warm_cumin
slow-oxbow
warm_saffron
```

**Sort the paths.** `Directory.EnumerateFiles` returns files in whatever order the file system
keeps them, and a generator gives each theme a slice of the draw in list order: the same seed over
the same files in another order draws different slugs. `StringComparer.Ordinal` sorts the same way
on every machine, which a culture-aware sort does not promise.

### What is the command's, not the library's

The command reads themes from `~/.slugger/themes` (or `--theme-dir`), lets a file there shadow a
built-in theme of the same name, and keeps saved defaults in `~/.config/slugger/config.json`. **The
library does none of that**: it never reads your home directory, `LoadEmbedded("docker")` always
returns the built-in theme, and a `docker.json` you load with `LoadFromFile` is simply another
theme that happens to have the same name. If you want shadowing, decide it in your own code.

## Reproducible slugs

To replay a run, share **one** seeded `DefaultRandomSource` across all its calls:

```csharp
using Slugger;

SluggerTheme  docker    = SluggerTheme.LoadEmbeddedOrThrow("docker");
SlugGenerator generator = SlugGenerator.ForOrThrow(docker);

DefaultRandomSource random = new(42);
for (int i = 0; i < 3; i++) {
    Console.WriteLine(generator.Next(random));
}
```

```text
mystifying_mirzakhani
analyzing_chandrasekhar
ethereal_chaplygin
```

This is exactly what the command prints for the same seed, because the command shares one source
across its run in the same way:

```console
$ slugger --theme docker --seed 42 --count 3
mystifying_mirzakhani
analyzing_chandrasekhar
ethereal_chaplygin
```

A source built anew for each call replays its first slug: `new DefaultRandomSource(42)` inside the
loop prints `mystifying_mirzakhani` three times. `Next()` without an argument draws from the shared,
unseeded `Random.Shared`, and never replays anything.

**A seed is not a promise across versions.** It reproduces a sequence for one version of the
library and one version of the theme. A word added to a theme, or a change to the order in which
the engine draws, changes what a seed gives. Do not store a seed expecting it to regenerate the
same names after an upgrade; store the names.

**Slugs are not secrets.** `DefaultRandomSource` wraps `System.Random`, which is not a
cryptographic generator, and the themes that ship with the library hold a few million combinations
each at most — few enough to try them all. Never use a slug as a password, an access token, an unguessable URL or anything else that
has to stay secret.

### A scripted source for tests

To pin an exact slug in a test, implement `IRandomSource` and hand back the draws you want. Make it
fail loudly when the engine asks for a draw you did not plan:

```csharp
using Slugger;

SluggerTheme  docker       = SluggerTheme.LoadEmbeddedOrThrow("docker");
SlugGenerator oneAdjective = SlugGenerator.ForOrThrow(docker, _ => GenerationOptions.Default with { SegmentMode = SegmentMode.Adjective });

// The first noun, then the first adjective that noun reaches.
Console.WriteLine(oneAdjective.Next(new ScriptedRandomSource(0, 0)));

// A draw the script did not plan is a failure, not a silent zero.
try {
    oneAdjective.Next(new ScriptedRandomSource(0));
} catch (InvalidOperationException exception) {
    Console.WriteLine(exception.Message);
}

sealed class ScriptedRandomSource(params int[] draws) : IRandomSource {

    private int _position;

    public int Next(int exclusiveUpperBound) {
        if (_position == draws.Length) {
            throw new InvalidOperationException($"Draw {_position + 1} was not scripted.");
        }

        int drawn = draws[_position++];
        if (drawn >= exclusiveUpperBound) {
            throw new InvalidOperationException($"Scripted {drawn}, but this draw must stay below {exclusiveUpperBound}.");
        }

        return drawn;
    }

}
```

```text
affable-agnesi
Draw 2 was not scripted.
```

The options here ignore docker's style on purpose: its rare token costs a draw of its own, which a
two-draw script would not plan. Which draw decides what, and in which order, is an implementation detail: a script written against
it may need updating with a new version of the library. When your test is about your own code
rather than a particular slug, a seeded `DefaultRandomSource` and an assertion on the shape of the
result age better.

## Errors

A refused theme is reported as one error with the code `THEME_REJECTED`, whose inner errors give
every reason, each with its own code. Apart from a file that is not valid JSON, which stops
everything, the load never stops at the first problem.

```csharp
using Slugger;
using Slugger.Errors;

string json = """
    {
      "adjectives": { "common": ["slow", "wide"] },
      "nouns": [ { "value": "delta", "categories": ["tidal"] }, { "value": "ford" } ]
    }
    """;

// The plain form reports: no exception, every reason at once.
Outcome<SluggerTheme> outcome = SluggerTheme.LoadFromJson(json, "rivers");
if (outcome.Error is { } refusal) {
    Console.WriteLine($"{refusal.Code}: {refusal.DiagnosticMessage}");
    foreach (Error reason in refusal.InnerErrors) {
        Console.WriteLine($"  {reason.Code}: {reason.DiagnosticMessage}");
    }
}

// The OrThrow form: the message names the theme, the reasons travel inside the exception.
try {
    SluggerTheme.LoadFromJsonOrThrow(json, "rivers");
} catch (SluggerThemeException exception) {
    Console.WriteLine($"{exception.Message} - {exception.Error.InnerErrors.Count} reasons");
}
```

```text
THEME_REJECTED: Theme "rivers" was refused
  THEME_UNKNOWN_CATEGORY: "delta" references category "tidal", which the theme does not declare (it declares common).
  THEME_TOO_FEW_NOUNS: 2 nouns, but a theme needs at least 100.
  THEME_POOL_TOO_SMALL: "delta" reaches 2 adjectives, but every noun needs at least 100.
  THEME_POOL_TOO_SMALL: "ford" reaches 2 adjectives, but every noun needs at least 100.
  THEME_CATEGORY_TOO_POOR: Category "tidal" totals 2 combinations, but every category needs at least 40,000.
Theme "rivers" was refused - 5 reasons
```

- **The `OrThrow` form raises `SluggerThemeException`.** Its `Message` only names the theme; log
  `exception.Error.InnerErrors`, or every reason is lost. The plain form returns the same error, a
  `SluggerThemeError`, and so is every reason inside it. A generator refused by `SlugGenerator.For`
  reports the same way.
- **Each `Error` has a `Code`, a `DiagnosticMessage` for whoever fixes the theme and a
  `ShortMessage`** — a plain sentence such as "The theme cannot be used." that you can show to an
  end user.
- **Branch on codes, never on messages.** Compare `Error.Code` with the constants of
  `SluggerThemeError.Codes`; the messages may be reworded in any version.
- **`IsFailure` tells the compiler that `Error` is set.** After `if (outcome.IsFailure)`,
  `outcome.Error.Code` reads without a nullable warning; `if (outcome.Error is { } refusal)` tests
  and unwraps in one step, which is what the examples here use.

The codes you may get when loading, all in `SluggerThemeError.Codes`:

| Kind | Codes |
| --- | --- |
| The whole refusal | `THEME_REJECTED` |
| A theme that is not there | `THEME_NOT_FOUND` |
| A broken file — never waived | `THEME_MALFORMED_JSON`, `THEME_MALFORMED_SECTION`, `THEME_MALFORMED_NOUN`, `THEME_UNKNOWN_CATEGORY`, `THEME_EXCLUSION_MATCHES_NOTHING`, `THEME_INCOMPATIBLE_ADJECTIVE_ABSENT`, `THEME_INCOMPATIBLE_PARTICIPLE_ABSENT`, `THEME_LONGER_THAN_PROMISED`, `THEME_PARTICIPLES_ABSENT`, `THEME_NO_NOUN` |
| A theme too small — waived by `allowSmall` | `THEME_TOO_FEW_NOUNS`, `THEME_POOL_TOO_SMALL`, `THEME_PARTICIPLE_POOL_TOO_SMALL`, `THEME_COMBINED_POOL_TOO_SMALL`, `THEME_INCOMPATIBILITY_STARVES_NOUN`, `THEME_CATEGORY_TOO_POOR` |
| Your options leave too little — from [`SlugGenerator.For`](#checked-once-when-the-generator-is-built) | `THEME_NOTHING_FITS_THE_LIMIT`, `THEME_NO_VALUE_SHORT_ENOUGH`, `THEME_LIMIT_STARVES_NOUN` |

`THEME_ALREADY_REGISTERED`, `THEME_NOT_A_FILE` and `THEME_NOT_SELECTABLE` belong to the command's
theme directory: no library method returns them, and they are not in `SluggerThemeError.Codes`.

**A missing theme is reported as not found.** `LoadEmbedded` with a name that is not compiled
in — names are case-sensitive, so `"Docker"` is one — and `LoadFromFile` with a file that does
not exist both report `THEME_NOT_FOUND` among the inner errors:

```csharp
using Slugger;
using Slugger.Errors;

Outcome<SluggerTheme> missing = SluggerTheme.LoadEmbedded("Docker");
if (missing.Error is { } refusal) {
    foreach (Error reason in refusal.InnerErrors) {
        Console.WriteLine($"{reason.Code}: {reason.DiagnosticMessage}");
    }
}
```

```text
THEME_NOT_FOUND: There is no built-in theme named "Docker". Built-in themes: docker, heroku, slugger.
```

For a file, the reason reads `"./absent.json" does not exist.`

Other exceptions you may run into:

- `ArgumentException` (or `ArgumentNullException`) from a `Load*` method given a null, empty or blank
  name or path. An I/O error while reading a file that exists, such as access being denied,
  propagates unchanged.
- `ArgumentOutOfRangeException` from `GenerationOptions` when a value is out of range, such as a
  `MaxLength` below one: it is thrown where the value is set, not when a slug is generated (see
  [Option values](#option-values)). A theme file whose `defaults` set `maxSegmentWords` below one
  never gets that far: the load refuses it with `THEME_MALFORMED_SECTION`.
- No exception from `Next`: a generator that was built always draws. A limit that leaves no noun to
  draw is refused when the generator is built, among the reasons of its `THEME_REJECTED`, with the
  code that names the limit: `THEME_NOTHING_FITS_THE_LIMIT` for `MaxLength` (`No slug of theme
  "docker" fits in 5 characters.`), `THEME_NO_VALUE_SHORT_ENOUGH` for `MaxSegmentWords`. With both
  set, the code names the word cap when it would leave no noun on its own, since no length could make
  up for it, and `MaxLength` otherwise. `THEME_NO_NOUN` is kept for a theme that holds no noun at all.

## Measuring a theme

A load answers *accepted or refused*. An analysis answers *by how much*: the margin on every rule the
theme is held to, what it would be refused for, and what is worth a second look - for a theme that
loads and, more usefully, for one that does not yet. It is what `slugger --analyze` writes its report
from, so a page that renders it shows the command's numbers
([DEC0038](idr/DEC0038-mesure-publique-d-un-theme.md)).

```csharp
using Slugger;

string json = """
    {
      "adjectives": { "common": ["slow", "wide"], "tidal": ["brackish"] },
      "nouns": [ { "value": "delta", "categories": ["tidal"] }, { "value": "ford" }, { "value": "ford" } ]
    }
    """;

ThemeAnalysis analysis = SluggerTheme.AnalyzeFromJson(json, "rivers");

Console.WriteLine($"{analysis.Name}: {(analysis.IsAccepted ? "accepted" : $"refused for {analysis.Refusals.Count} reasons")}");
if (analysis.Measurements is { } measured) {
    foreach (ThemeMargin margin in measured.Margins) {
        string subject = margin.Subject is null ? "" : $" ({margin.Subject})";
        Console.WriteLine($"  {margin.Rule,-25} {margin.WorstCase,6}{subject} against {margin.Limit:N0}: {margin.Margin:+#,0;-#,0;0}");
    }
    Console.WriteLine($"  {measured.Combinations:N0} combinations, duplicated: {string.Join(", ", measured.DuplicatedNouns)}");
}
```

```text
rivers: refused for 5 reasons
  DistinctNouns                  2 against 100: -98
  AdjectivesPerNoun              2 (ford) against 100: -98
  CombinationsPerCategory        3 (tidal) against 40,000: -39,997
  7 combinations, duplicated: ford
```

- **An analysis never throws for the theme's sake.** A theme that is refused is measured all the
  same; `Refusals` holds the `SluggerThemeError`s a load would report, sentence for sentence.
- **`Measurements` is null when there is nothing to measure**: a file that is not valid JSON, a
  section of the wrong shape, no noun at all. `Refusals` then says why.
- **`Margins` holds a row only for the rules that apply**: a rule the theme's segment mode does not
  hold it to, or a length it does not promise, has none. A positive `Margin` is room; a negative one
  is the shortfall. `ThemeRule.CharactersInTwoWords` and `CharactersInThreeWords` are ceilings, one
  row per length the theme promises in `maxLength` - a load checks both, whatever mode the theme
  draws - so their room is counted below the limit.
- **It measures the theme as it describes itself** - its own style and segment mode - which is what a
  load holds it to. `allowSmall: true` judges it with the size floors waived; `FloorsWaived` then
  says so, and the margins still compare it with the real floors.
- `Combinations` is counted as `SlugGenerator.Combinations` counts it, in the theme's own style.
  `LeastExposed` and `MostExposed` name the adjectives the fewest and the most nouns reach, and
  `UnreachableCategories` the categories no noun carries.

## Shaping slugs

Every property of `GenerationOptions` has its own documentation comment, shown by IntelliSense from
the preview after `1.0.0-preview.1`; this section covers what is easy to get wrong.

### A DNS label

A Kubernetes namespace, a subdomain or an S3 bucket name takes lowercase ASCII letters, digits and
hyphens, 63 characters at most:

```csharp
using Slugger;

SluggerTheme slugger = SluggerTheme.LoadEmbeddedOrThrow("slugger");

// Lowercase ASCII letters, digits and hyphens, 63 characters at most.
// Reduced to what fits and checked once, at startup; every label after it is cheap.
SlugGenerator labels = SlugGenerator.ForOrThrow(slugger, style => style with { Ascii = true, MaxLength = 63 });

DefaultRandomSource random = new(42);
for (int i = 0; i < 3; i++) {
    Console.WriteLine(labels.Next(random));
}
```

```text
athletic-stealing-lou-brock
sprightly-batting-grover-cleveland-alexander
bold-leaping-frank-thomas
```

- **Keep kebab case and `-`.** `slugger` states no style, so its generator keeps the library's
  kebab case. The docker style writes `_`, which a host name refuses, and camel case writes capitals:
  set `Casing = Casing.Kebab` when the theme you draw from has a style of its own.
- **Set `Ascii = true`.** Several themes hold accented words, and `FoldAccents` alone keeps letters
  such as `ø` or `ß` (see below).
- **`MaxLength = 63` never truncates**: it leaves out the words that would not fit, which is why
  the generator is built once — read [Length and word limits](#length-and-word-limits).

Expect building the generator to take about 65 ms here and each label after it about 50 µs:
`MaxLength` with `Ascii` is the most expensive combination (see [Cost and threads](#cost-and-threads)).
In a test, every slug drawn from the built-in themes and from every theme of the repository's
[themes/](../themes/) folder matched `^[a-z0-9]([a-z0-9-]*[a-z0-9])?$`, and each theme could be
drawn under `MaxLength = 63`.

### Accents

`FoldAccents` drops the accent from every letter that decomposes into a base letter and a mark: `é`
becomes `e`. Letters with no decomposition — `ø`, `ß`, `œ`, every non-Latin script — are kept.
`Ascii` folds, then drops every character that is still not ASCII, mangling a word rather than
letting it through. A term that folds to nothing is left out of the slug, so a theme written
entirely in a non-Latin script would come out empty.

```csharp
using Slugger;

// One adjective and one noun, accented as a theme may write them.
string json = """
    {
      "allowSmall": true,
      "adjectives": { "common": ["crème brûlée"] },
      "nouns": [ { "value": "smørrebrød" } ]
    }
    """;
SluggerTheme dishes = SluggerTheme.LoadFromJsonOrThrow(json, "dishes");

Func<GenerationOptions, GenerationOptions>[] variants = [
    style => style with { SegmentMode = SegmentMode.Adjective },
    style => style with { SegmentMode = SegmentMode.Adjective, FoldAccents = true },
    style => style with { SegmentMode = SegmentMode.Adjective, Ascii = true },
    style => style with { SegmentMode = SegmentMode.Adjective, Ascii = true, WordSeparator = "" }
];
foreach (Func<GenerationOptions, GenerationOptions> adjust in variants) {
    Console.WriteLine(SlugGenerator.ForOrThrow(dishes, adjust).Next());
}
```

```text
crème-brûlée-smørrebrød
creme-brulee-smørrebrød
creme-brulee-smrrebrd
cremebrulee-smrrebrd
```

`WordSeparator` replaces the space inside a compound term: `""` glues its words, `"_"` keeps the
boundary between terms visible (`gorgeous-john_doe`).

### Option values

`GenerationOptions` refuses what the command line refuses, as soon as the value is set — in an
object initializer or a `with` copy — with an `ArgumentOutOfRangeException` naming the property:

| Option | Accepted |
| --- | --- |
| `TokenChance` | 0 to 100 |
| `TokenLength` | 0 or more; 0 draws no token |
| `MaxLength`, `MaxSegmentWords` | null, or 1 or more |
| `Separator` | null (the casing's joiner), `""` (nothing between the terms), or one character |
| `WordSeparator` | null, `""` (glues the words), or one character |
| `Casing`, `SegmentMode` | a value the enumeration declares |

What it does not check is anything that depends on the theme:

| Option | Value | What happens |
| --- | --- | --- |
| `Separator` | any character, a space included | Written as it is: `sweet brahmagupta`. The command accepts it too. |
| `Separator` | set, with any `Casing` | Replaces the casing's joiner and keeps its capitals: `Camel` with `"-"` writes `keen-Yonath` ([DEC0030](idr/DEC0030-casse-et-separateur-orthogonaux.md)). |

What depends on the theme is checked when a generator is built, as the command checks it before it
draws: a segment mode the theme cannot feed is refused there. `SegmentMode.Participle` on `heroku` or
`slugger`, which lack the 100 participles per noun that mode requires, is refused with
`THEME_PARTICIPLE_POOL_TOO_SMALL`; only `docker` draws it. A theme file's own `defaults` are checked
when it loads, so `"tokenChance": 150` in a file is refused rather than handed to your adjustment.

### Several themes, each in its own style

`SlugGenerator.For(themes, adjust)` picks a theme by its share of the nouns, then draws inside it in
its own style, with the same adjustment applied to each. That is what the command's `--mimic-style`
does; the command without it switches the styles off when it draws from several themes
([DEC0004](idr/DEC0004-chaine-de-priorite-unique.md)). To get the command's default, ignore the
styles in the adjustment.

```csharp
using Slugger;

SluggerTheme[] themes = [SluggerTheme.LoadEmbeddedOrThrow("docker"), SluggerTheme.LoadEmbeddedOrThrow("heroku")];
DefaultRandomSource random = new(1);

// One set of options for every theme: no theme's style applies.
SlugGenerator plain = SlugGenerator.ForOrThrow(themes, _ => GenerationOptions.Default);
for (int i = 0; i < 3; i++) {
    Console.WriteLine(plain.Next(random));
}

// Each theme in its own style, each checked against what its options leave of it.
SlugGenerator styled = SlugGenerator.ForOrThrow(themes);
for (int i = 0; i < 3; i++) {
    Console.WriteLine(styled.Next(random));
}
```

```text
lavish-charting-bose
native-coiling-wind
blithe-drifting-meitner
quaint_yalow
timeless-willow-0137
experimenting_ritchie
```

## Length and word limits

`MaxLength` caps the number of characters of the finished slug, token included; `MaxSegmentWords`
caps the number of words in any one term. Neither ever cuts a word: they leave the words that would
break the limit out of the draw. Know three things before you set either.

- **`MaxLength` counts UTF-16 characters, not bytes** — `string.Length`. `é` counts once and takes
  two bytes in UTF-8. With `Ascii = true` the two counts agree.
- **Reducing the theme costs milliseconds.** The pools are rebuilt without the words that do not
  fit. A generator does it once, when it is built, and keeps the result. See
  [Cost and threads](#cost-and-threads) for figures. A theme whose style states `maxSegmentWords`
  sets `MaxSegmentWords` in the style a generator starts from, and pays the same cost.
- **What is left has to be checked.** A loaded theme cleared the floors as a whole. Under a limit, a
  noun can be left with a handful of adjectives and the slugs repeat sooner; a limit can leave no
  noun at all. A generator checks what is left when it is built, and refuses it as the command
  refuses the run.

### Checked once, when the generator is built

`SlugGenerator.For` takes the theme and your adjustment, reduces the theme to what the options leave,
and checks it the way the command does before its first draw: a limit, or a segment mode other than
the theme's own, holds it to the floors of the mode it draws
([DEC0031](idr/DEC0031-source-de-slugs-preparee-une-fois.md)). Build it when your application
starts, keep it, and draw with `Next`.

```csharp
using Slugger;
using Slugger.Errors;

SluggerTheme docker = SluggerTheme.LoadEmbeddedOrThrow("docker");

foreach (int cap in new[] { 25, 20 }) {
    Outcome<SlugGenerator> prepared = SlugGenerator.For(docker, style => style with { MaxLength = cap });

    if (prepared.Error is { } refused) {
        Console.WriteLine($"MaxLength = {cap}: {refused.DiagnosticMessage}, {refused.InnerErrors.Count} reasons");
        foreach (Error reason in refused.InnerErrors.Take(2)) {
            Console.WriteLine($"  {reason.Code}: {reason.DiagnosticMessage}");
        }

        continue;
    }

    SlugGenerator generator = prepared.GetResultOrThrow();
    Console.WriteLine($"MaxLength = {cap}: {generator.Next(new DefaultRandomSource(7))}, {generator.Combinations:N0} combinations");
}
```

```text
MaxLength = 25: quirky_greider, 69,053 combinations
MaxLength = 20: Theme "docker" was refused, 5 reasons
  THEME_COMBINED_POOL_TOO_SMALL: "chandrasekhar" reaches 49 words to put in front of it (49 in "adjectives", 0 in "participles"), but a theme drawing "either" needs at least 100 per noun.
  THEME_COMBINED_POOL_TOO_SMALL: "grothendieck" reaches 99 words to put in front of it (94 in "adjectives", 5 in "participles"), but a theme drawing "either" needs at least 100 per noun.
```

`SlugGenerator.ForOrThrow` throws the same refusal as a `SluggerThemeException`, as a load does. If
you loaded the theme with `allowSmall: true`, pass the same to `For`; a theme's own `"allowSmall": true`
is honoured without it. A theme drawn as it loaded — no limit, its own mode — is not checked again:
the load already did. `generator.Combinations` tells you how many combinations your options leave.

## Cost and threads

**Load a theme once and keep it.** Nothing is cached: every `Load*` call reads, parses, normalises
and validates the theme again. A `SluggerTheme` never changes after it is loaded, so a single
instance — a static field, a singleton in your container — can serve every thread.

**A generator is safe to share between threads**: build one at startup — a singleton in your
container — and draw from every thread. The random source is the part to watch:

- `Next()`, and `new DefaultRandomSource()`, draw from `Random.Shared`, which is thread-safe.
- **A seeded `DefaultRandomSource` is not thread-safe**: it wraps one `System.Random`. Shared by
  several threads it can break for good. Give each thread — each request, each job — its own, and
  pass it to `Next(IRandomSource)`.

In a test, one `DefaultRandomSource(42)` shared by 8 workers broke in two runs out of three and
returned `admiring_agnesi0` from then on, while parallel calls on one shared `SluggerTheme` without
a seed ran clean.

Orders of magnitude, from a Release build:

| Operation | Time | Allocated |
| --- | --- | --- |
| First load in a process (JIT included) | ≈170 ms | |
| Load a built-in theme again | 3 ms (`slugger`) to 25 ms (`docker`) | |
| Load a theme that declares `maxLength` | up to ≈200 ms for the largest themes of the repository | |
| Load a theme that does not | ≈6–15 ms (`jazz.json`) | |
| `SlugGenerator.For`, no limit, the theme drawn as it loaded | ≈0.03–3 ms | |
| `SlugGenerator.For` with `MaxSegmentWords`, reduced and checked once | ≈0.4–3 ms to reduce, plus the check | ≈0.5–2.6 MB |
| `SlugGenerator.For` under `MaxLength`, reduced and checked once | ≈30 ms (`docker` style) to ≈65 ms (`slugger`, `Ascii`) | ≈15–66 MB |
| `Next`, no limit | ≈3–4 µs | under 1 KB |
| `Next` under `MaxLength` or `MaxSegmentWords` | ≈1 µs; ≈50 µs with `Ascii` | under 1 KB; ≈100 KB with `Ascii` |

A theme file's `maxLength` block is a promise about its own slugs, checked at load against the
longest pair of words each noun can draw: it is the most expensive part of a load, and adding one to
`jazz.json` takes its load from about 8 ms to about 18 ms.

<details>
<summary>How these were measured</summary>

- **Machine:** .NET 10 (SDK 10.0.112), Linux x64, four cores of an Intel Xeon at 2.8 GHz; a Release
  build of both the library and the console program that measured it. Expect the same orders of
  magnitude on your machine, not the same numbers.
- **Times and allocations:** `Stopwatch` around warm calls, the median of repeated runs, and
  `GC.GetAllocatedBytesForCurrentThread` for allocations. The reductions were measured through the
  `SlugGenerator.Generate` of earlier previews, which reduced on every call what a generator now
  reduces once; the two rows for `For` and `Next` with no limit were measured on this version, in a
  four-core Linux container. The `MaxLength` range runs from the
  `docker` style under 63 characters (≈10 ms, ≈15 MB) to the [DNS example](#a-dns-label) and
  `docker` with one adjective, `Ascii` and a four-digit token under 63 characters (≈40–60 ms,
  ≈60–66 MB).
- **Threads:** 200,000 calls from 8 parallel workers on one shared theme, without a seed,
  raised no error. One `DefaultRandomSource(42)` shared by 8 workers for 1,000,000 calls broke in two
  runs out of three: it returned `admiring_agnesi0` for most of the calls, and went on returning it
  from a single thread afterwards.
- **DNS labels:** 20,000 draws per theme, with `Ascii = true` and kebab case, over the built-in
  themes and every theme of the repository's [themes/](../themes/) folder.
- **Trimming and Native AOT:** a console program that loads themes, generates slugs and reports a
  refusal, published with `PublishAot` (which also trims) and run against its JIT build with the
  same seed.
- **Repeats:** the medians in [Repeats and uniqueness](#repeats-and-uniqueness) come from 41 runs
  per row, each drawing until the first repeat.

</details>

## Repeats and uniqueness

**`Next` does not remember what it returned.** A theme holds a finite number of combinations,
and a draw that has already come out can come out again: by the birthday effect, the first repeat
arrives after roughly √N slugs for N combinations — much sooner than N.
`generator.Combinations` gives N for what the generator draws, the token aside; a token drawn on every
slug multiplies it by the number of values the token can take.

Measured with the code and themes of 1.0.0-preview.1 (`docker` has grown since: 69,142 combinations
in its own style today):

| Theme and options | Combinations | First repeat after |
| --- | --- | --- |
| `docker`, in its own style | 62,062 | ≈270–300 slugs |
| `heroku`, in its own style without the token | 45,382 | ≈290 |
| `slugger`, `GenerationOptions.Default` | 1,101,000 | ≈1,300–1,400 |
| `docker`, `GenerationOptions.Default` | 3,353,252 | ≈2,100 |
| `heroku`, in its own style (four-digit token) | 45,382 × 10,000 | ≈21,000 |
| `docker` style with `TokenLength = 4, TokenChance = 100` | 62,062 × 10,000 | ≈26,000 |

When names must be unique, **add a token** (`TokenLength = 4, TokenChance = 100`) to push the first
repeat into the tens of thousands, and **check against what is already taken** when a repeat would
do harm:

```csharp
using Slugger;

SluggerTheme  docker    = SluggerTheme.LoadEmbeddedOrThrow("docker");
SlugGenerator generator = SlugGenerator.ForOrThrow(docker);

long combinations = generator.Combinations;
Console.WriteLine($"{combinations:N0} combinations, a first repeat expected after about {Math.Sqrt(combinations):N0} slugs");

// Names that must not repeat: check against what is already taken, and draw again.
HashSet<string> taken = [];
for (int i = 0; i < 1_000; i++) {
    string name;
    do {
        name = generator.Next();
    } while (!taken.Add(name));
}
Console.WriteLine($"{taken.Count} distinct names");
```

```text
69,142 combinations, a first repeat expected after about 263 slugs
1000 distinct names
```

In production, bound the retry loop: as the names taken approach the number of combinations, each
new one takes more draws to find. `Combinations` follows the draw noun by noun — a noun that cannot feed the mode asked for
counts what it falls back to, and an incompatible pair is not counted — but it counts combinations,
not strings: two that write the same string count twice, so read it as an
order of magnitude.

## Stability

`Slugger` is a **preview** (`1.0.0-preview.1`), and a preview may break its API from one version to
the next. From `1.0.0`, every `1.x` version keeps this contract
([DEC0037](idr/DEC0037-contrat-de-compatibilite-de-la-1x.md)):

| What | In `1.x` |
| --- | --- |
| The public signatures | Stable. A version may add a type or a member, never remove or change one. |
| The error codes (`THEME_*`) | Stable. A version may add a code, never remove or rename one: branch on them. |
| The floors (`ThemeFloors`) | Fixed: no `1.x` version raises or lowers one. |
| The error messages | May be reworded in any version. Never branch on them. |
| The slugs a seed gives | The same for one version of the library and of the theme, not across versions. |

What to expect from the preview:

- **The surface in use** — `SluggerTheme`, `SlugGenerator`, `GenerationOptions`, `Casing`,
  `SegmentMode`, `ThemeDefaults`, `ThemeMetadata`, `MaxLength`, `ThemeFloors`, `IRandomSource`,
  `DefaultRandomSource`, `SluggerThemeError`, `SluggerThemeException` and the analysis model
  (`ThemeAnalysis`, `ThemeMeasurements`, `ThemeMargin`, `ThemeRule`, `WordExposure`), in `Slugger`,
  and the error model in `Slugger.Errors` — is the whole of the public surface, and what the command
  is built on.
- **One way to load, one way to draw** from the preview after `1.0.0-preview.1`
  ([DEC0036](idr/DEC0036-api-publique-de-la-bibliotheque.md)). What changes in your code:
  - **Namespaces.** Everything moved from `Slugger.Domain` and its sub-namespaces to `Slugger`; the
    error model is in `Slugger.Errors`. `using Slugger;` is enough to load and draw.
  - **Loading.** The `Themes` facade is gone: its methods are on `SluggerTheme`, and the naming is
    reversed. `Themes.LoadEmbedded`, which threw, is `SluggerTheme.LoadEmbeddedOrThrow`;
    `Themes.LoadEmbeddedResult`, which reported, is `SluggerTheme.LoadEmbedded`. The same for
    `LoadFromFile` and `LoadFromJson`, whose `OrThrow` forms now take `allowSmall` too.
    `Themes.ListEmbedded()` is `SluggerTheme.EmbeddedNames`.
  - **Drawing.** `SlugGenerator.Generate` and `SlugSource` are replaced by one type:
    `SlugGenerator.For(theme, adjust)` or `ForOrThrow`, then `Next()`. The theme's style now applies
    on its own: `GenerationOptions.Default.WithDefaultsOf(theme) with { MaxLength = 30 }` becomes
    `style => style with { MaxLength = 30 }`, and options that ignored the style become
    `_ => GenerationOptions.Default with { ... }`. `new ThemeCombinatorics(theme, options).Total(mode)`
    becomes `generator.Combinations`.
  - **Internal now:** `WeightedThemePicker` (pass several themes to `For`), `SlugFormatter`,
    `SlugBudget`, `WordNormalizer`, `ThemeCombinatorics`, `ThemeValidator` (its floors are
    `ThemeFloors.MinimumNouns` and the like), `GenerationOptions.WithDefaultsOf`, and `DomainError`
    and `DomainException`, which only the command raises.
- **A theme is opaque** ([DEC0034](idr/DEC0034-theme-public-opaque.md)). A load returns a
  `SluggerTheme` where it returned a `ThemeDocument`. What it states about itself — `Name`,
  `Metadata`, `Defaults`, `MaxLength` — can be read; its words cannot, and only the library builds
  one. `ThemeDocument`, `ThemeResolver`, `NounEntry`, `ThemeValidator.Validate` and
  `ThemeValidator.Remarks` are internal: build a theme in memory by passing its JSON to
  `SluggerTheme.LoadFromJson`, and check what your options leave of it with `SlugGenerator.For`.
- **A refused theme has its own error** ([DEC0035](idr/DEC0035-erreur-de-theme-du-theme-public.md)).
  `ThemeErrors`, in `Slugger.Domain.Validation`, is now `SluggerThemeError`, in `Slugger`: replace
  `ThemeErrors.Codes` with `SluggerThemeError.Codes`, and catch `SluggerThemeException` where you
  caught `DomainException`. The codes themselves are unchanged. Its factories are internal: a
  library reports a refusal, it does not ask you to build one. `THEME_ALREADY_REGISTERED`,
  `THEME_NOT_A_FILE` and `THEME_NOT_SELECTABLE`, which only the command's theme directory raises,
  are no longer in `Codes`.
- **The vocabulary types are internal** from the preview after `1.0.0-preview.1`. `Theme`,
  `ThemeName`, `Word`, `Term`, `Category`, `Chance`, `Token`, `TokenAlphabet`, `TokenLength`,
  `TokenMould`, `Epithet`, `Slug`, `Noun`, `Adjective`, `Participle`, their `*Error` and `*Exception`
  types and the `SlugFormatter.Format(Slug, ...)` overload were public in that preview, marked as
  not to be built on. Loading and generation never used them, and each one becomes public again in
  the version that wires it in.
- **The error types are the library's own** from the preview after `1.0.0-preview.1`: they came
  from the `FirstClassErrors` package and now live in `Slugger.Errors`
  ([DEC0029](idr/DEC0029-modele-d-erreurs-propre-au-moteur.md)). Replace `using FirstClassErrors;`
  with `using Slugger.Errors;`; the names you read — `Outcome<T>`, `Error`, `Code`,
  `InnerErrors`, `Context` — are unchanged. What the library never used is gone:
  `InstanceId`, `OccurredAt`, `Then`, `Recover`, `Finally`, and the means to build an error from
  scratch, such as `DomainError.Create`.
- **Trimming and Native AOT are not promised.** The package does not declare `IsTrimmable` or
  `IsAotCompatible`. A console program that uses it was published with `PublishAot` (which also
  trims) without a single trim or AOT warning, and the native executable printed the same slugs as
  the JIT build for the same seed — a measurement, not a guarantee.
- **The internals are not yours.** `Slugger.Domain`, `Slugger.Application` and `Slugger.Infrastructure`
  are `internal`;
  everything the command does beyond loading and generating — the theme directory, saved defaults,
  the clipboard — stays out of the library.

## See also

- [writing-a-theme.md](writing-a-theme.md) — the JSON format of a theme, its floors and how to read
  a refusal.
- [cli.md](cli.md) — the `slugger` command, built on this library.
- [architecture.md](architecture.md) — how the engine is built, and why.
- [ubiquitous-language.md](ubiquitous-language.md) — the words this page uses: slug, term, noun,
  epithet, token, category, pool, floor.
- [idr/](idr/) — the decision records, in French, that explain why the rules are what they are.
