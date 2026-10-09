**Readable random names in your terminal** — for containers, git branches, preview environments
and test data, Docker- and Heroku-style.

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

In a terminal, `slugger` stays open: Enter draws another round, Ctrl+D quits. `--oneshot` draws once
and exits, which is what a script or a `$(...)` wants.

## The words go together

Glue a random adjective to a random noun and sooner or later you print `thundering-moon`. In a
slugger theme, an adjective is only drawn for a noun it can describe: a river can be `singing`, the
moon cannot. Docker has to hard-code a refusal for `boring_wozniak`; slugger's `docker` theme simply
declares it.

## Any vocabulary you like

A theme is a plain JSON file. Three ship inside the tool, more
[wait in the repository](https://github.com/Reefact/slugger/tree/main/themes):

| Theme | Sounds like |
| --- | --- |
| `heroku` | `jagged-thunder-2506` |
| `jazz` | `loosely-arranged-whispering-take-five` |
| `code-review` | `melancholy-coordinated-value-object` |
| `cyberpunk` | `liquid-cooled-pursuing-combat-drone` |

## Names that fit where they go

```console
$ slugger --ascii --max-length 63            # a Kubernetes namespace
atmospheric-welcoming-ebbets
$ slugger --casing camel --token-length 4    # an identifier with a suffix
craftyPersistingSpikes1512
```

Under a length limit the words that would not fit are left out of the draw — a name is never
truncated. And a theme too thin to stay varied is refused before the first draw, with every reason
at once.

Try it without installing, with the .NET 10 SDK: `dnx Slugger.Cli --yes -- --theme heroku --oneshot`

**Learn more:**
[using the command](https://github.com/Reefact/slugger/blob/main/docs/cli.md) ·
[writing a theme](https://github.com/Reefact/slugger/blob/main/docs/writing-a-theme.md) ·
[source and issues](https://github.com/Reefact/slugger)

Generating names from C#? [Slugger](https://www.nuget.org/packages/Slugger) is the library behind
this command.
