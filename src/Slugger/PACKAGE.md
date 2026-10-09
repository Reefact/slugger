**Readable random names, generated in C#** — for containers, preview environments, jobs and test
data. Docker- and Heroku-style, drawn from themes in which every adjective fits its noun:
`singing-river`, never `singing-moon`.

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

- **Words that belong together.** An adjective is only drawn for a noun it can describe.
- **Bring your own words.** A theme is a JSON file: `Themes.LoadFromFile("jazz.json")`.
- **Checked, not trusted.** A theme too thin to stay varied is refused, with every reason at once.
- **Shaped for where it goes.** Separator, casing, ASCII, a token, a maximum length — and a seeded
  random source for reproducible tests.

Preview · .NET 10 · depends on FirstClassErrors (preview) and Value · Apache-2.0

**Learn more:**
[using the library](https://github.com/Reefact/slugger/blob/main/docs/library.md) ·
[writing a theme](https://github.com/Reefact/slugger/blob/main/docs/writing-a-theme.md) ·
[source and issues](https://github.com/Reefact/slugger)

Want names in a terminal or a script? [Slugger.Cli](https://www.nuget.org/packages/Slugger.Cli) is
the `slugger` command, built on this library.
