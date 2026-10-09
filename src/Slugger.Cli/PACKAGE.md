**Readable random names in your terminal** — for containers, git branches, preview environments
and test data. Docker- and Heroku-style, drawn from themes in which every adjective fits its noun:
`singing-river`, never `singing-moon`.

Not a URL slugifier: to turn `Hello World` into `hello-world`, see
[Slugify.Core](https://www.nuget.org/packages/Slugify.Core).

```console
$ dotnet tool install --global Slugger.Cli
$ slugger --theme docker --count 3
devoted_noether
smiling_visvesvaraya
suave_kapitsa
$ git switch -c "feature/$(slugger --oneshot)"
Switched to a new branch 'feature/seasoned-persisting-pickoff'
```

- **Words that belong together.** An adjective is only drawn for a noun it can describe.
- **Bring your own words.** A theme is a JSON file. Three are built in; jazz, cocktails, minerals
  and more [wait in the repository](https://github.com/Reefact/slugger/tree/main/themes).
- **Checked, not trusted.** A theme too thin to stay varied is refused, with every reason at once.
- **Fits where it lands.** `--max-length 63 --ascii` gives a valid DNS label — by leaving long
  words out, never by truncating.

Try it without installing, with the .NET 10 SDK: `dnx Slugger.Cli --yes -- --theme heroku --oneshot`

**Learn more:**
[using the command](https://github.com/Reefact/slugger/blob/main/docs/cli.md) ·
[writing a theme](https://github.com/Reefact/slugger/blob/main/docs/writing-a-theme.md) ·
[source and issues](https://github.com/Reefact/slugger)

Generating names from C#? [Slugger](https://www.nuget.org/packages/Slugger) is the library behind
this command.
