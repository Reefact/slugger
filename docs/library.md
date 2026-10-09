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
dotnet add package Slugger --prerelease
```

The package is a preview, so `--prerelease` is required: without it, `dotnet add package` finds no
version to install.

- **Target framework:** `net10.0` only.
- **Dependencies:** [FirstClassErrors](https://www.nuget.org/packages/FirstClassErrors), itself a
  preview, for `Outcome<T>` and the error types, and [Value](https://www.nuget.org/packages/Value),
  which provides the `ValueType<T>` base class of the types the
  [vocabulary refactoring](#the-api-at-a-glance) introduces. Both come in with the package.
- **Licence:** Apache-2.0.
- **Trimming and Native AOT:** no warning in a test publish, not promised — see [Stability](#stability).
- **IntelliSense:** the documentation comments this page refers to ship from the preview after
  `1.0.0-preview.1`. The `1.0.0-preview.1` on nuget.org predates them.

## Quick start

```csharp
using Slugger;
using Slugger.Domain;
using Slugger.Domain.Generation;

// Load once, keep it: every Load* call parses and validates the theme again.
ThemeDocument docker = Themes.LoadEmbedded("docker");

// The library's defaults: kebab-case, an adjective, a participle and the noun.
Console.WriteLine(SlugGenerator.Generate(docker, GenerationOptions.Default));

// The theme's own style, which is never applied unless you ask for it.
GenerationOptions dockerStyle = GenerationOptions.Default.WithDefaultsOf(docker);
Console.WriteLine(SlugGenerator.Generate(docker, dockerStyle));

// Any option changed with a `with` expression; the rest stays as it was.
GenerationOptions mine = dockerStyle with { Separator = '-', TokenLength = 4, TokenChance = 100 };
Console.WriteLine(SlugGenerator.Generate(docker, mine));
```

For example:

```text
candid-longing-mcclintock
gracious_gagarin
genuine-engelbart1704
```

Three things to note:

- **`GenerationOptions.Default` draws three terms** — an adjective, a participle and the noun — in
  kebab case, whatever the theme. The command draws the same way when no theme's style applies.
- **A theme's own style is opt-in.** `docker` declares snake case, one term before the noun and a
  rare one-digit token, but those only apply through `WithDefaultsOf(theme)`. Call it first, then
  change what you need with `with { ... }`: the other way round, the theme overrides whatever it
  has an opinion on.
- **`GenerationOptions` is an immutable record.** `with` gives you a copy, so one set of options can
  be shared freely.

`Themes.ListEmbedded()` returns the themes compiled into the library: `docker`, `heroku` and
`slugger`.

## The API at a glance

The path from a theme to a slug runs through four types:

```text
Themes  →  ThemeDocument  →  GenerationOptions  →  SlugGenerator.Generate  →  string
(load)     (the theme)       (how to draw and write)                          (the slug)
```

| To | Use | Namespace |
| --- | --- | --- |
| Load a theme | `Themes.LoadEmbedded`, `LoadFromFile`, `LoadFromJson` and their `*Result` forms | `Slugger` |
| Hold a loaded theme | `ThemeDocument`, with `NounEntry`, `ThemeDefaults`, `ThemeMetadata` and `MaxLength` | `Slugger.Domain` |
| Say how to draw and how to write | `GenerationOptions`, `Casing`, `SegmentMode` | `Slugger.Domain` |
| Generate | `SlugGenerator.Generate` | `Slugger.Domain.Generation` |
| Control the randomness | `IRandomSource`, `DefaultRandomSource` | `Slugger.Domain` |
| Draw from several themes | `WeightedThemePicker` | `Slugger.Domain.Resolution` |
| Measure how many slugs a theme gives | `ThemeCombinatorics` | `Slugger.Domain.Validation` |
| Check a theme, or what your options leave of it | `ThemeValidator`, with `ThemeResolver` and `SlugBudget` | `Validation`, `Resolution`, `Generation` |
| Branch on a refusal | `ThemeErrors.Codes` | `Slugger.Domain.Validation` |
| Write terms out yourself | `SlugFormatter.Format` | `Slugger.Domain.Generation` |

**Some public types are not used by the engine yet.** `Theme`, `ThemeName`, `Word`, `Term`,
`Category`, `Chance`, `Token`, `TokenAlphabet`, `TokenLength`, `TokenMould`, `Epithet`, `Slug`,
`Noun`, `Adjective`, `Participle`, the `*Error` and `*Exception` types that go with them and the
`SlugFormatter.Format(Slug, ...)` overload belong to a refactoring of the library's vocabulary that
is under way ([refactoring-in-progress.md](refactoring-in-progress.md)). Loading and generation
still work with `ThemeDocument` and strings, and these types may change or disappear before they
are wired in. Do not build on them yet; from the preview after `1.0.0-preview.1`, each one says so in
its IntelliSense documentation.

## Loading themes

Every `Load*` method comes in two forms: the throwing form returns the theme or throws, and the report
form (`Load*Result`) returns an `Outcome<ThemeDocument>` and never throws for a refused theme (see
[Errors](#errors)).

### From a file or a string

```csharp
using Slugger;
using Slugger.Domain;
using Slugger.Domain.Generation;

// From a file: the theme is named after the file, without its extension.
ThemeDocument spices = Themes.LoadFromFile("themes/spices.json");
Console.WriteLine($"{spices.Name}: {SlugGenerator.Generate(spices, GenerationOptions.Default)}");

// From a string: raw JSON has no file name, so you name it.
string json = """
    {
      "allowSmall": true,
      "adjectives": { "common": ["slow", "wide", "muddy"] },
      "nouns": [ { "value": "delta" }, { "value": "ford" }, { "value": "oxbow" } ]
    }
    """;
ThemeDocument rivers = Themes.LoadFromJson(json, "rivers");
Console.WriteLine($"{rivers.Name}: {SlugGenerator.Generate(rivers, GenerationOptions.Default)}");
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
spices: smoky-cumin
rivers: wide-ford
```

`"allowSmall": true` keeps these examples short: without it, a theme needs at least 100 nouns, each
reaching at least 100 adjectives, and the load refuses anything smaller. A theme you ship should
clear those floors; [writing-a-theme.md](writing-a-theme.md) explains them. The `sep` in the
`defaults` block is part of the theme's style: it only applies through `WithDefaultsOf`, which is why
`smoky-cumin` has a hyphen.

A relative path is resolved against the current directory, not against your program's folder. For
a theme file copied next to your executable, build the path from `AppContext.BaseDirectory`.

The throwing form has no `allowSmall` argument. To waive the floors for a theme that does not
set `"allowSmall": true` itself, use `LoadFromFileResult(path, allowSmall: true)` or its siblings.

### From a resource embedded in your assembly

There is no `Stream` overload: read the resource into a string and pass it to `LoadFromJson`.

```xml
<ItemGroup>
  <EmbeddedResource Include="Themes/spices.json" LogicalName="MyApp.Themes.spices.json" />
</ItemGroup>
```

```csharp
using System.Reflection;
using Slugger;
using Slugger.Domain;
using Slugger.Domain.Generation;

Assembly assembly = typeof(Program).Assembly;
using Stream stream = assembly.GetManifestResourceStream("MyApp.Themes.spices.json")
                      ?? throw new InvalidOperationException("The spices theme is not embedded.");
using StreamReader reader = new(stream);

ThemeDocument spices = Themes.LoadFromJson(reader.ReadToEnd(), "spices");
Console.WriteLine(SlugGenerator.Generate(spices, GenerationOptions.Default.WithDefaultsOf(spices)));
```

```text
toasted_fenugreek
```

### From a folder

The library has no folder API: enumerate the files and load each one.

```csharp
using FirstClassErrors;
using Slugger;
using Slugger.Domain;
using Slugger.Domain.Generation;
using Slugger.Domain.Resolution;

// No folder API: enumerate the files and load each one.
// Sorted, because the picker's weights follow the list's order and a file system's order is not stable.
List<ThemeDocument> themes = [];
foreach (string path in Directory.EnumerateFiles("themes", "*.json").Order(StringComparer.Ordinal)) {
    Outcome<ThemeDocument> loaded = Themes.LoadFromFileResult(path);
    if (loaded.Error is { } refusal) {
        Console.Error.WriteLine($"{refusal.DiagnosticMessage}, skipped");
        continue;
    }

    themes.Add(loaded.GetResultOrThrow());
}

// Each theme weighs as much as it has nouns, so every noun keeps the same chance.
WeightedThemePicker picker = new(themes);
DefaultRandomSource random = new(1);
for (int i = 0; i < 4; i++) {
    Console.WriteLine(SlugGenerator.Generate(picker, GenerationOptions.Default, random));
}
```

With a `themes` folder that holds the `spices.json` above and a `rivers.json` with the JSON of the
string example, it prints:

```text
wide-delta
warm-cumin
slow-oxbow
warm-saffron
```

**Sort the paths.** `Directory.EnumerateFiles` returns files in whatever order the file system
keeps them, and `WeightedThemePicker` gives each theme a slice of the draw in list order: the same
seed over the same files in another order draws different slugs. `StringComparer.Ordinal` sorts the
same way on every machine, which a culture-aware sort does not promise.

### What is the command's, not the library's

The command reads themes from `~/.slugger/themes` (or `--theme-dir`), lets a file there shadow a
built-in theme of the same name, and keeps saved defaults in `~/.config/slugger/config.json`. **The
library does none of that**: it never reads your home directory, `LoadEmbedded("docker")` always
returns the built-in theme, and a `docker.json` you load with `LoadFromFile` is simply another
theme that happens to have the same name. If you want shadowing, decide it in your own code.

## Reproducible slugs

### The `Seed` trap

`GenerationOptions.Seed` looks like the way to get a reproducible run. It is not: the two-argument
`SlugGenerator.Generate(theme, options)` builds a **new** random source from the seed on every call,
so every call returns the **same slug**. Share one seeded `DefaultRandomSource` instead:

```csharp
using Slugger;
using Slugger.Domain;
using Slugger.Domain.Generation;

ThemeDocument docker = Themes.LoadEmbedded("docker");
GenerationOptions dockerStyle = GenerationOptions.Default.WithDefaultsOf(docker);

// The trap: Seed builds a new random source on every call, so every call draws the same slug.
GenerationOptions seeded = dockerStyle with { Seed = 42 };
for (int i = 0; i < 3; i++) {
    Console.WriteLine(SlugGenerator.Generate(docker, seeded));
}

Console.WriteLine();

// The right way: one seeded source, shared by every call of the run.
DefaultRandomSource random = new(42);
for (int i = 0; i < 3; i++) {
    Console.WriteLine(SlugGenerator.Generate(docker, dockerStyle, random));
}
```

```text
interesting_mirzakhani
interesting_mirzakhani
interesting_mirzakhani

interesting_mirzakhani
reaching_chandrasekhar
ethereal_chaplygin
```

The second sequence is exactly what the command prints for the same seed, because the command
shares one source across its run in the same way:

```console
$ slugger --theme docker --seed 42 --count 3
interesting_mirzakhani
reaching_chandrasekhar
ethereal_chaplygin
```

The overloads that take an `IRandomSource` ignore `Seed`.

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
using Slugger.Domain;
using Slugger.Domain.Generation;

ThemeDocument docker = Themes.LoadEmbedded("docker");
GenerationOptions oneAdjective = GenerationOptions.Default with { SegmentMode = SegmentMode.Adjective };

// The first noun, then the first adjective that noun reaches.
Console.WriteLine(SlugGenerator.Generate(docker, oneAdjective, new ScriptedRandomSource(0, 0)));

// A draw the script did not plan is a failure, not a silent zero.
try {
    SlugGenerator.Generate(docker, oneAdjective, new ScriptedRandomSource(0));
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

Which draw decides what, and in which order, is an implementation detail: a script written against
it may need updating with a new version of the library. When your test is about your own code
rather than a particular slug, a seeded `DefaultRandomSource` and an assertion on the shape of the
result age better.

## Errors

A refused theme is reported as one error with the code `THEME_REJECTED`, whose inner errors give
every reason, each with its own code. Apart from a file that is not valid JSON, which stops
everything, the load never stops at the first problem.

```csharp
using FirstClassErrors;
using Slugger;
using Slugger.Domain;
using Slugger.Domain.Validation;

string json = """
    {
      "adjectives": { "common": ["slow", "wide"] },
      "nouns": [ { "value": "delta", "categories": ["tidal"] }, { "value": "ford" } ]
    }
    """;

// The report form: no exception, every reason at once.
Outcome<ThemeDocument> outcome = Themes.LoadFromJsonResult(json, "rivers");
if (outcome.Error is { } refusal) {
    Console.WriteLine($"{refusal.Code}: {refusal.DiagnosticMessage}");
    foreach (Error reason in refusal.InnerErrors) {
        Console.WriteLine($"  {reason.Code}: {reason.DiagnosticMessage}");
    }
}

// The throwing form: the message names the theme, the reasons travel inside the exception.
try {
    Themes.LoadFromJson(json, "rivers");
} catch (DomainException exception) when (exception.Error.Code == ThemeErrors.Codes.Rejected) {
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

- **The throwing form raises `FirstClassErrors.DomainException`.** Its `Message` only names the
  theme; log `exception.Error.InnerErrors`, or every reason is lost.
- **Each `Error` has a `Code`, a `DiagnosticMessage` for whoever fixes the theme and a
  `ShortMessage`** — a plain sentence such as "The theme cannot be used." that you can show to an
  end user.
- **Branch on codes, never on messages.** Compare `Error.Code` with the constants of
  `ThemeErrors.Codes`; the messages may be reworded in any version.
- **Read `outcome.Error` through a pattern.** `Outcome<T>.Error` is declared nullable, and
  FirstClassErrors does not tell the compiler that `IsFailure` means it is set: after
  `if (outcome.IsFailure)`, reading `outcome.Error.Code` gives warning CS8602.
  `if (outcome.Error is { } refusal)` tests and unwraps in one step.

The codes you may get when loading, all in `ThemeErrors.Codes`:

| Kind | Codes |
| --- | --- |
| The whole refusal | `THEME_REJECTED` |
| A theme that is not there | `THEME_NOT_FOUND` |
| A broken file — never waived | `THEME_MALFORMED_JSON`, `THEME_MALFORMED_SECTION`, `THEME_MALFORMED_NOUN`, `THEME_UNKNOWN_CATEGORY`, `THEME_EXCLUSION_MATCHES_NOTHING`, `THEME_INCOMPATIBLE_ADJECTIVE_ABSENT`, `THEME_INCOMPATIBLE_PARTICIPLE_ABSENT`, `THEME_LONGER_THAN_PROMISED`, `THEME_PARTICIPLES_ABSENT`, `THEME_NO_NOUN` |
| A theme too small — waived by `allowSmall` | `THEME_TOO_FEW_NOUNS`, `THEME_POOL_TOO_SMALL`, `THEME_PARTICIPLE_POOL_TOO_SMALL`, `THEME_COMBINED_POOL_TOO_SMALL`, `THEME_INCOMPATIBILITY_STARVES_NOUN`, `THEME_CATEGORY_TOO_POOR` |
| Your options leave too little — from the [startup check](#check-a-limit-once-at-startup) | `THEME_NOTHING_FITS_THE_LIMIT`, `THEME_NO_VALUE_SHORT_ENOUGH`, `THEME_LIMIT_STARVES_NOUN` |

`THEME_ALREADY_REGISTERED`, `THEME_NOT_A_FILE` and `THEME_NOT_SELECTABLE` belong to the command's
theme directory; no library method returns them.

**A missing theme is reported as not found.** `LoadEmbeddedResult` with a name that is not compiled
in — names are case-sensitive, so `"Docker"` is one — and `LoadFromFileResult` with a file that does
not exist both report `THEME_NOT_FOUND` among the inner errors:

```csharp
using FirstClassErrors;
using Slugger;
using Slugger.Domain;

Outcome<ThemeDocument> missing = Themes.LoadEmbeddedResult("Docker");
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
- `ArgumentOutOfRangeException` from `Generate` when `MaxLength` or `MaxSegmentWords` is below one.
  A theme file whose `defaults` set `maxSegmentWords` below one never gets that far: the load
  refuses it with `THEME_MALFORMED_SECTION`.
- `DomainException` from `Generate` when a limit leaves no noun to draw, with the code that names
  the limit: `THEME_NOTHING_FITS_THE_LIMIT` for `MaxLength` (`No slug of theme "docker" fits in 5
  characters.`), `THEME_NO_VALUE_SHORT_ENOUGH` for `MaxSegmentWords`. `THEME_NO_NOUN` is kept for a
  theme that holds no noun at all.

## Shaping slugs

Every property of `GenerationOptions` has its own documentation comment, shown by IntelliSense from
the preview after `1.0.0-preview.1`; this section covers what is easy to get wrong.

### A DNS label

A Kubernetes namespace, a subdomain or an S3 bucket name takes lowercase ASCII letters, digits and
hyphens, 63 characters at most:

```csharp
using Slugger;
using Slugger.Domain;
using Slugger.Domain.Generation;

ThemeDocument slugger = Themes.LoadEmbedded("slugger");

// Lowercase ASCII letters, digits and hyphens, 63 characters at most.
GenerationOptions dnsLabel = GenerationOptions.Default with { Ascii = true, MaxLength = 63 };

DefaultRandomSource random = new(42);
for (int i = 0; i < 3; i++) {
    Console.WriteLine(SlugGenerator.Generate(slugger, dnsLabel, random));
}
```

```text
athletic-stealing-lou-brock
sprightly-batting-grover-cleveland-alexander
bold-leaping-frank-thomas
```

- **Keep kebab case and `-`.** The docker style writes `_`, which a host name refuses, and camel
  case writes capitals.
- **Set `Ascii = true`.** Several themes hold accented words, and `FoldAccents` alone keeps letters
  such as `ø` or `ß` (see below).
- **`MaxLength = 63` never truncates**: it leaves out the words that would not fit. It is costly —
  read [Length and word limits](#length-and-word-limits) and check it once at startup.

Expect each call of this example to take about 50 ms and allocate about 60 MB: `MaxLength` with
`Ascii` is the most expensive combination (see [Cost and threads](#cost-and-threads)). In a test,
every slug drawn from the built-in themes and from every theme of the repository's
[themes/](../themes/) folder matched `^[a-z0-9]([a-z0-9-]*[a-z0-9])?$`, and each theme passed the
startup check under `MaxLength = 63`.

### Accents

`FoldAccents` drops the accent from every letter that decomposes into a base letter and a mark: `é`
becomes `e`. Letters with no decomposition — `ø`, `ß`, `œ`, every non-Latin script — are kept.
`Ascii` folds, then drops every character that is still not ASCII, mangling a word rather than
letting it through. A term that folds to nothing is left out of the slug, so a theme written
entirely in a non-Latin script would come out empty.

```csharp
using Slugger.Domain;
using Slugger.Domain.Generation;

// Terms as a theme holds them: lowercase, the words of a compound one separated by a space.
string[] terms = ["brûlé", "smørrebrød", "crème fraîche"];

Console.WriteLine(SlugFormatter.Format(terms, null, GenerationOptions.Default));
Console.WriteLine(SlugFormatter.Format(terms, null, GenerationOptions.Default with { FoldAccents = true }));
Console.WriteLine(SlugFormatter.Format(terms, null, GenerationOptions.Default with { Ascii = true }));
Console.WriteLine(SlugFormatter.Format(terms, null, GenerationOptions.Default with { Ascii = true, WordSeparator = "" }));
```

```text
brûlé-smørrebrød-crème-fraîche
brule-smørrebrød-creme-fraiche
brule-smrrebrd-creme-fraiche
brule-smrrebrd-cremefraiche
```

`WordSeparator` replaces the space inside a compound term: `""` glues its words, `"_"` keeps the
boundary between terms visible (`gorgeous-john_doe`).

### Option values that are not checked

`GenerationOptions` takes what you give it. Only the two limits refuse a value, and only when a
slug is generated:

| Option | Value | What happens |
| --- | --- | --- |
| `TokenChance` | above 100, such as 150 | Every slug gets a token, as with 100. |
| `TokenChance` | zero or less | No token. |
| `TokenLength` | negative | No token, as with zero. |
| `Separator` | any character, a space included | Written as it is: `sweet brahmagupta`. |
| `Casing` | `Snake` with the default separator | Written like kebab: `keen-yonath`. Set `Separator = '_'` too. |
| `SegmentMode` | `Participle`, on any built-in theme | Drawn, although no built-in theme has the 100 participles per noun the command requires for that mode. |
| `MaxLength`, `MaxSegmentWords` | zero or less | `Generate` throws `ArgumentOutOfRangeException`. |

The command refuses the out-of-range numbers on its command line, and checks the segment mode and
the limits before it draws. The library leaves all of it to you: the
[startup check](#check-a-limit-once-at-startup) below covers the segment mode and the two limits.
A theme file's own `defaults` are another matter: the load checks their bounds, so
`"tokenChance": 150` in a file is refused rather than passed on by `WithDefaultsOf`.

### Several themes, each in its own style

`Generate(WeightedThemePicker, options, random)` applies the same options to every theme, so no
theme's style applies. To keep each one's style — what the command's `--mimic-style` does — pick
the theme yourself and pass the options for it. The draws are the same as through the picker
overload.

```csharp
using Slugger;
using Slugger.Domain;
using Slugger.Domain.Generation;
using Slugger.Domain.Resolution;

ThemeDocument[] themes = [Themes.LoadEmbedded("docker"), Themes.LoadEmbedded("heroku")];
WeightedThemePicker picker = new(themes);
DefaultRandomSource random = new(1);

// One set of options for every theme: no theme's style applies.
for (int i = 0; i < 3; i++) {
    Console.WriteLine(SlugGenerator.Generate(picker, GenerationOptions.Default, random));
}

// Each theme in its own style: pick the theme yourself, then generate inside it.
Dictionary<ThemeDocument, GenerationOptions> styles =
    themes.ToDictionary(theme => theme, theme => GenerationOptions.Default.WithDefaultsOf(theme));
for (int i = 0; i < 3; i++) {
    ThemeDocument drawn = picker.Pick(random);
    Console.WriteLine(SlugGenerator.Generate(drawn, styles[drawn], random));
}
```

```text
lavish-refactoring-bose
native-coiling-wind
blithe-blazing-meitner
quaint_yalow
timeless-willow-0137
stoic_ritchie
```

## Length and word limits

`MaxLength` caps the number of characters of the finished slug, token included; `MaxSegmentWords`
caps the number of words in any one term. Neither ever cuts a word: they leave the words that would
break the limit out of the draw. Know three things before you set either.

- **`MaxLength` counts UTF-16 characters, not bytes** — `string.Length`. `é` counts once and takes
  two bytes in UTF-8. With `Ascii = true` the two counts agree.
- **They cost milliseconds per slug.** Each call that sets one rebuilds the theme's pools without
  the words that do not fit. The command does it once per run; the library has no public way to
  keep the reduced theme between calls, so it does it on every call. See
  [Cost and threads](#cost-and-threads) for figures. A theme whose style states
  `maxSegmentWords` sets `MaxSegmentWords` through `WithDefaultsOf`, and pays the same cost.
- **Nothing checks what is left.** A loaded theme cleared the floors as a whole. Under a limit, a
  noun can be left with a handful of adjectives, the slugs repeat sooner and nothing tells you; a
  limit that leaves no noun at all makes `Generate` throw. The command checks this before its first
  draw and refuses the run; the library does not.

### Check a limit once at startup

Build a `ThemeResolver` from the options you will generate with — the same segment mode, a
`SlugBudget` for the length limit, the same word limit — and give it to `ThemeValidator.Validate`.
That is what the command does. Run it once, when your application starts, and refuse to start
rather than generate from a theme your options have gutted.

```csharp
using FirstClassErrors;
using Slugger;
using Slugger.Domain;
using Slugger.Domain.Generation;
using Slugger.Domain.Resolution;
using Slugger.Domain.Validation;

ThemeDocument docker = Themes.LoadEmbedded("docker");

foreach (int cap in new[] { 25, 20 }) {
    GenerationOptions options = GenerationOptions.Default.WithDefaultsOf(docker) with { MaxLength = cap };
    IReadOnlyList<DomainError> refusals = WhatTheOptionsBreak(docker, options);

    Console.WriteLine($"MaxLength = {cap}: {refusals.Count} reasons");
    foreach (DomainError reason in refusals.Take(2)) {
        Console.WriteLine($"  {reason.Code}: {reason.DiagnosticMessage}");
    }
}

// Run once at startup, with the options you will generate with.
static IReadOnlyList<DomainError> WhatTheOptionsBreak(ThemeDocument theme, GenerationOptions options) {
    SlugBudget?   budget  = options.MaxLength is { } cap ? new SlugBudget(cap, options) : null;
    ThemeResolver reduced = new(theme, options.SegmentMode, budget, options.MaxSegmentWords);

    return ThemeValidator.Validate(reduced);
}
```

```text
MaxLength = 25: 0 reasons
MaxLength = 20: 5 reasons
  THEME_COMBINED_POOL_TOO_SMALL: "chandrasekhar" reaches 49 words to put in front of it (49 in "adjectives", 0 in "participles"), but a theme drawing "either" needs at least 100 per noun.
  THEME_COMBINED_POOL_TOO_SMALL: "grothendieck" reaches 98 words to put in front of it (94 in "adjectives", 4 in "participles"), but a theme drawing "either" needs at least 100 per noun.
```

To fail at startup, throw what the load itself would have thrown:
`throw ThemeErrors.Rejected(theme.Name, refusals).ToException();`. If you loaded the theme with
`allowSmall: true`, pass the same to `Validate`; a theme's own `"allowSmall": true` is honoured
without it.

The check also covers `SegmentMode` and `MaxSegmentWords` on their own: a mode other than the
theme's own is held to that mode's floors, which is how the command refuses `participle` on the
built-in themes. `new ThemeCombinatorics(reduced).Total(options.SegmentMode)` then tells you how many
combinations your options leave.

## Cost and threads

**Load a theme once and keep it.** Nothing is cached: every `Load*` call reads, parses, normalises
and validates the theme again. A `ThemeDocument` never changes after it is built, so a single
instance — a static field, a singleton in your container — can serve every thread.

**Generation is safe to call from several threads** on the same `ThemeDocument` and the same
options. The random source is the part to watch:

- The two-argument `Generate` without a `Seed`, and `new DefaultRandomSource()`, draw from
  `Random.Shared`, which is thread-safe.
- **A seeded `DefaultRandomSource` is not thread-safe**: it wraps one `System.Random`. Shared by
  several threads it can break for good. Give each thread — each request, each job — its own.
- A `ThemeResolver` fills a cache as you ask it questions: use one instance from one thread at a
  time.

In a test, one `DefaultRandomSource(42)` shared by 8 workers broke in two runs out of three and
returned `admiring_agnesi0` from then on, while parallel calls on one shared `ThemeDocument` without
a seed ran clean.

Orders of magnitude, from a Release build:

| Operation | Time | Allocated |
| --- | --- | --- |
| First load in a process (JIT included) | ≈170 ms | |
| Load a built-in theme again | 3 ms (`slugger`) to 25 ms (`docker`) | |
| Load a theme that declares `maxLength` | up to ≈1.3 s for the largest themes of the repository | |
| Load a theme that does not | ≈6–15 ms (`jazz.json`) | |
| `Generate`, no limit | ≈10 µs | ≈15 KB |
| `Generate` with `MaxSegmentWords` | ≈0.4–3 ms | ≈0.5–2.6 MB |
| `Generate` with `MaxLength`, depending on the theme and the mode (most with `Ascii`) | ≈10–60 ms | ≈15–66 MB |
| Startup check under `MaxLength` | ≈25 ms (`docker` style) to ≈300 ms (`slugger`) | |

A theme file's `maxLength` block is a promise about its own slugs, checked at load against every
pair of words each noun can draw: it is the most expensive part of a load, and adding one to
`jazz.json` takes its load from about 15 ms to about 140 ms.

<details>
<summary>How these were measured</summary>

- **Machine:** .NET 10 (SDK 10.0.112), Linux x64, four cores of an Intel Xeon at 2.8 GHz; a Release
  build of both the library and the console program that measured it. Expect the same orders of
  magnitude on your machine, not the same numbers.
- **Times and allocations:** `Stopwatch` around warm calls, the median of repeated runs, and
  `GC.GetAllocatedBytesForCurrentThread` for allocations. The `MaxLength` range runs from the
  `docker` style under 63 characters (≈10 ms, ≈15 MB) to the [DNS example](#a-dns-label) and
  `docker` with one adjective, `Ascii` and a four-digit token under 63 characters (≈40–60 ms,
  ≈60–66 MB).
- **Threads:** 200,000 calls from 8 parallel workers on one shared `ThemeDocument`, without a seed,
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

**`Generate` does not remember what it returned.** A theme holds a finite number of combinations,
and a draw that has already come out can come out again: by the birthday effect, the first repeat
arrives after roughly √N slugs for N combinations — much sooner than N.
`ThemeCombinatorics(theme).Total(mode)` gives N for a theme and a segment mode, the token aside; a
token drawn on every slug multiplies it by the number of values the token can take.

Measured with the code of 1.0.0-preview.1:

| Theme and options | Combinations (`Total`) | First repeat after |
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
using Slugger.Domain;
using Slugger.Domain.Generation;
using Slugger.Domain.Validation;

ThemeDocument docker = Themes.LoadEmbedded("docker");
GenerationOptions dockerStyle = GenerationOptions.Default.WithDefaultsOf(docker);

long combinations = new ThemeCombinatorics(docker).Total(dockerStyle.SegmentMode);
Console.WriteLine($"{combinations:N0} combinations, a first repeat expected after about {Math.Sqrt(combinations):N0} slugs");

// Names that must not repeat: check against what is already taken, and draw again.
HashSet<string> taken = [];
for (int i = 0; i < 1_000; i++) {
    string name;
    do {
        name = SlugGenerator.Generate(docker, dockerStyle);
    } while (!taken.Add(name));
}
Console.WriteLine($"{taken.Count} distinct names");
```

```text
62,062 combinations, a first repeat expected after about 249 slugs
1000 distinct names
```

In production, bound the retry loop: as the names taken approach the number of combinations, each
new one takes more draws to find. `Total` counts combinations, not strings — it does not subtract
incompatible pairs, and two combinations that write the same string count twice — so read it as an
order of magnitude.

## Stability

`Slugger` is a **preview** (`1.0.0-preview.1`), and a preview may break its API from one version to
the next. What to expect:

- **The surface in use** — `Themes`, `ThemeDocument`, `GenerationOptions`, `SlugGenerator`,
  `IRandomSource`, `DefaultRandomSource`, `WeightedThemePicker`, `ThemeCombinatorics`,
  `ThemeValidator`, `ThemeResolver`, `SlugBudget`, `SlugFormatter`, `ThemeErrors` and the enums and
  records around them — is what the engine runs on today, and what the command is built on.
- **The surface in transition.** The types listed under [The API at a glance](#the-api-at-a-glance)
  as not used yet are the first part of a refactoring of the library's vocabulary. The plan in
  [refactoring-in-progress.md](refactoring-in-progress.md) also moves `ThemeDocument` out of
  `Slugger.Domain` once the `Theme` entity takes its place, and renames and moves `ThemeErrors`:
  expect code that names them to need changes when that lands.
- **Seeds** reproduce a sequence for one version of the library and of the theme, not across
  versions.
- **Trimming and Native AOT are not promised.** The package does not declare `IsTrimmable` or
  `IsAotCompatible`. A console program that uses it was published with `PublishAot` (which also
  trims) without a single trim or AOT warning, and the native executable printed the same slugs as
  the JIT build for the same seed — a measurement, not a guarantee.
- **Messages** may be reworded at any time; error **codes** are what to branch on.
- **The floors may rise.** The participle floor (`ThemeValidator.MinimumParticiplePoolPerNoun`) is
  meant to go up as the built-in themes grow, and never down: a custom theme accepted today, if it
  only just clears a floor, may be refused at load by a later version. Keep some margin, and load
  your themes in a test so that an upgrade tells you.
- **The internals are not yours.** `Slugger.Application` and `Slugger.Infrastructure` are `internal`;
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
