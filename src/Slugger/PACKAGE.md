**Readable random names, generated in C#** — for containers, preview environments, jobs and test
data, Docker- and Heroku-style.

Not a URL slugifier: to turn a title into `hello-world`, see
[Slugify.Core](https://www.nuget.org/packages/Slugify.Core).

```csharp
using Slugger;
using Slugger.Domain;
using Slugger.Domain.Generation;

foreach (string name in Themes.ListEmbedded()) {
    ThemeDocument     theme = Themes.LoadEmbedded(name);
    GenerationOptions style = GenerationOptions.Default.WithDefaultsOf(theme);

    Console.WriteLine($"{name,-8} {SlugGenerator.Generate(theme, style)}");
}
```

```text
docker   tidy_shtern
heroku   calm-wood-7821
slugger  timeless-continuing-carl-hubbell
```

## The words go together

Glue a random adjective to a random noun and sooner or later you print `decaf-otter`. A slugger
theme sorts its words into categories, and an adjective is only drawn for a noun it can describe:

```json
{
  "adjectives": { "common": ["quiet", "lucky"], "drink": ["iced", "decaf"], "animal": ["furry"] },
  "nouns": [
    { "value": "espresso", "categories": ["drink"] },
    { "value": "otter",    "categories": ["animal"] }
  ]
}
```

`iced-espresso`, `furry-otter`, `lucky-otter` — never `decaf-otter`. (Abridged: a theme needs 100
nouns to load, or `"allowSmall": true`.)

## Your vocabulary, checked before use

A theme is a JSON file you load with `Themes.LoadFromFile("jazz.json")` — jazz, cocktails,
cyberpunk and more [live in the repository](https://github.com/Reefact/slugger/tree/main/themes).
A theme too thin to stay varied is refused, and the refusal lists every reason at once, as an
`Outcome` or an exception.

## Shaped for where it goes

Separator, casing, accent folding or strict ASCII, a decimal or hexadecimal token, a maximum length
met by leaving long words out rather than truncating — and a seeded random source, so a test gets
the same names on every run.

Preview · .NET 10 · depends on FirstClassErrors (preview) and Value · Apache-2.0

**Learn more:**
[using the library](https://github.com/Reefact/slugger/blob/main/docs/library.md) ·
[writing a theme](https://github.com/Reefact/slugger/blob/main/docs/writing-a-theme.md) ·
[source and issues](https://github.com/Reefact/slugger)

Want names in a terminal or a script? [Slugger.Cli](https://www.nuget.org/packages/Slugger.Cli) is
the `slugger` command, built on this library.
