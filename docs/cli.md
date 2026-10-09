# Using the `slugger` command

`slugger` prints names like `humming_dirac` or `silvery-mud-7643`, drawn from a **theme**: a
vocabulary of nouns and the adjectives and participles that fit them. This page is for anyone who
runs the command — in a terminal, in a script, in CI. To write your own theme, see
[writing-a-theme.md](writing-a-theme.md); to call the engine from C#, see
[library.md](library.md).

- [Install](#install)
- [First slugs](#first-slugs)
- [Recipes](#recipes)
- [Choosing themes](#choosing-themes)
- [Shaping a slug](#shaping-a-slug)
- [Personal defaults](#personal-defaults)
- [Scripts and CI](#scripts-and-ci)
- [Option reference](#option-reference)
- [Troubleshooting](#troubleshooting)

## Install

`slugger` is a .NET tool: installing it takes the [.NET 10](https://dotnet.microsoft.com/download)
SDK, and running it the .NET 10 runtime.

```bash
dotnet tool install --global Slugger.Cli      # then: slugger
dotnet tool update  --global Slugger.Cli
dotnet tool uninstall --global Slugger.Cli
```

To pin it to one repository instead, install it as a local tool; it is then run through `dotnet`:

```bash
dotnet new tool-manifest            # once per repository
dotnet tool install Slugger.Cli
dotnet slugger --theme docker --oneshot
```

And to run it once without installing anything — handy on a CI runner — use `dnx`, which ships with
the .NET 10 SDK:

```bash
dnx Slugger.Cli --yes -- --theme docker --oneshot
```

## First slugs

```console
$ slugger --theme docker --count 3
humming_dirac
hale_merkle
blithe_bohr
```

With no option at all, `slugger` draws one slug from its own theme, `slugger`, whose nouns come
from baseball:

```console
$ slugger
robust-rising-jacob-degrom
```

**In a terminal, `slugger` stays open**: every Enter draws another round, and Ctrl+D quits (Ctrl+C
too). No prompt is shown — it simply waits. `--oneshot` turns that off, and so does anything that
is not a terminal on standard input: a pipe into `slugger`, a script, a CI runner. See
[Scripts and CI](#scripts-and-ci) for the one case that catches people out.

## Recipes

A git branch:

```console
$ git switch -c "feat/$(slugger --oneshot)"
Switched to a new branch 'feat/natural-soaring-chief-bender'
```

A Docker container — Docker's own naming style, from the built-in `docker` theme:

```bash
docker run --name "$(slugger --theme docker --oneshot)" nginx
```

A Kubernetes namespace, a DNS label or an S3 bucket — lowercase ASCII letters, digits and hyphens,
63 characters at most:

```console
$ slugger --ascii --max-length 63 --count 3 --oneshot
atmospheric-welcoming-ebbets
classic-remaining-richie-ashburn
remarkable-thriving-tom-seaver
```

`--ascii` matters here: several themes hold accented words (`saucisse de montbéliard`), which a DNS
label refuses. `--max-length` never truncates — it leaves out the words that would not fit, see
[Length](#length). For a Helm release name, use `--max-length 53`.

A name that has to be unique — `slugger` does not check uniqueness, and a theme's vocabulary is
finite: with the `docker` style, the first repeat typically comes after a few hundred names. A
four-digit token pushes it to tens of thousands:

```console
$ slugger --theme docker --token-length 4 --token-chance 100 --count 2 --oneshot
docile_proskuriakova9138
admiring_hopper7005
```

## Choosing themes

Three themes are built in: `slugger` (the default), `docker` and `heroku`. `--list-themes` prints
every theme available, one per line.

```console
$ slugger --list-themes
docker
heroku
slugger
```

The repository carries more themes that are not built in — cocktails, jazz, mineralogy, Icelandic
place names and others, listed in [themes/](../themes/). Download one and register it:

```console
$ curl -sSfLO https://raw.githubusercontent.com/Reefact/slugger/main/themes/jazz.json
$ slugger --register ./jazz.json
theme "jazz" registered.
$ slugger --theme jazz --oneshot
faded-quivering-chord-change
```

`--register` validates the file and copies it into the theme directory — `~/.slugger/themes` by
default, `%USERPROFILE%\.slugger\themes` on Windows; `--theme-dir` points elsewhere. A theme that is already registered is refused rather than
overwritten: `--unregister jazz` first. A registered file named like a built-in theme shadows it,
and `--register` says so.

To draw from a folder of themes without registering them, point `--theme-dir` at it:

```bash
slugger --theme-dir ~/src/slugger/themes --theme jazz
```

A theme is asked for by its **name** — its file name without `.json`, case-sensitive — never by a
path. `--theme-info jazz` prints what the file says about itself (title, description, author...).

### Several themes at once

`--theme` repeats, takes a comma-separated list, and `'*'` stands for every theme available. Quote
the star, or your shell expands it into file names first.

```bash
slugger --theme docker,heroku --count 4
slugger --theme docker --theme heroku --count 4      # the same
slugger --theme '*' --count 4
```

Each slug comes from one theme, picked in proportion to the theme's size.

### A theme's own style

A theme can declare its own style. `docker` writes `snake_case`, puts one word before the noun and
adds a one-digit token to about one name in a hundred; `heroku` writes `kebab-case` with one word
before the noun and a four-digit token. **When one theme is drawn, its style applies by itself.** When several are drawn,
styles are switched off and every slug is shaped by your options and the program's defaults:

```console
$ slugger --theme docker,heroku --count 4 --oneshot
calm-iterating-ishizaka
vivacious-wandering-hertz
vibrant-joking-lovelace
indigo-deepening-frost
```

`--mimic-style` keeps each theme's style even when several are drawn; `--mimic-style false`
ignores the style even of a single theme.

```console
$ slugger --theme docker,heroku --mimic-style --count 4 --oneshot
iterating_ishizaka
civil_visvesvaraya
drowsy-fjord-2665
lingering-fen-5153
```

## Shaping a slug

A slug is a **noun**, optionally preceded by an **epithet** (an adjective, a participle or both)
and followed by a **token**.

### What precedes the noun

`--segment` chooses it. The same draw under each mode (`--theme heroku --mimic-style false
--seed 7`):

| `--segment` | Example | What precedes the noun |
| --- | --- | --- |
| `adjective` | `rustic-tooth` | One adjective |
| `either` | `bleak-tooth` | One adjective or one participle, from both lists together |
| `both` (default) | `rustic-unveiling-tooth` | An adjective, then a participle |
| `threeOrTwo` | `rustic-unveiling-tooth` | Like `both`, but sometimes the participle is left out |
| `participle` | — | One participle. Every shipped theme is refused in this mode: none declares enough participles per noun. |

### Separator and casing

```console
$ slugger --casing camel
craftyPersistingSpikes
$ slugger --sep _
crafty_persisting_spikes
```

`--casing` takes `kebab`, `snake` or `camel`. Today `kebab` and `snake` render the same way: the
**separator** decides between `-` and `_`, so for `snake_case` write `--sep _`. `camel` drops the
separator altogether.

A value that starts with a dash must be glued to its option — `--sep=-`, `--word-sep=-` — because
`--sep -` reads the dash as the start of another option.

Some terms hold several words — `jacob degrom`, `rock crystal`. `--word-sep` joins those words, and
defaults to the separator:

```console
$ slugger --seed 1
robust-rising-jacob-degrom
$ slugger --seed 1 --word-sep ""
robust-rising-jacobdegrom
$ slugger --seed 1 --word-sep _
robust-rising-jacob_degrom
```

`--max-segment-words 1` leaves out every term of more than one word instead — the shape Docker's
names have. Like `--max-length`, it can leave too few words behind: `slugger`'s own theme is refused
under it, `docker` is not. `--max-segment-words none` lifts a cap that a theme's own style sets.

### Token

```console
$ slugger --token-length 4
competitive-leaping-john-smoltz-9138
$ slugger --token-length 4 --token-hex --token-glued
competitive-leaping-john-smoltzf14c
```

`--token-chance` says how many slugs out of a hundred get one (100 by default). Mind a theme's own
style: `docker` sets the chance to 1, so `--theme docker --token-length 4` almost never shows a
token until you add `--token-chance 100`.

### Accents

Themes keep words as they are written: `crème brûlée`, `søren`. Two options take accents out:

- `--fold-accents` removes the diacritics it can (`é` → `e`, `ç` → `c`) and leaves the rest:
  `ø`, `ß` and non-Latin scripts pass through unchanged.
- `--ascii` guarantees an ASCII slug, whatever it costs the words: what cannot be folded is dropped
  (`søren` → `sren`), and a term with nothing left disappears from the slug.

### Length

`--max-length 63` promises that no slug exceeds 63 characters. It does so by **leaving out** every
word that could produce a longer slug — never by truncating — and then checks that what remains is
still a valid theme. Ask for too little and the run is refused, with the reason:

```console
$ slugger --max-length 40
Theme "slugger" was refused for 6 reasons:

  - "grover cleveland alexander" reaches 41 adjectives, but every noun needs at
least 100.
  ...
```

Each theme has its own floor: `slugger` needs about 50 characters in its default mode, `docker` and
`heroku` accept 25. Drawing one word in front of the noun leaves more room:

```console
$ slugger --max-length 40 --segment adjective --count 2
robust-jacob-degrom
proud-wild-pitch
```

## Personal defaults

`--init` saves the other options of the same command line as your defaults, and quits:

```console
$ slugger --init --sep _ --token-length 3
defaults saved.
$ slugger
robust_rising_jacob_degrom_764
```

A second `--init` adds to the saved defaults rather than replacing them. The file is
`~/.config/slugger/config.json` on Linux and macOS, `%USERPROFILE%\.config\slugger\config.json` on
Windows, or `$XDG_CONFIG_HOME/slugger/config.json` when that variable is set:

```json
{
  "Separator": "_",
  "TokenLength": 3
}
```

Let `--init` write it: the keys are case-sensitive, and a key spelled differently (`"sep"`,
`"casing"`) is ignored without a warning. Save `--theme-dir` as an absolute path — a relative one
is kept as written and then resolved from wherever you run `slugger`.

To start again, delete the file. That is also the only way to turn a saved flag off:
`--clipboard false` is accepted on the command line but does not override a saved `--clipboard`.
A file that is not valid JSON is ignored without a warning.

### Who wins

From strongest to weakest:

1. what this command line says;
2. the drawn theme's own style — when a single theme is drawn, or with `--mimic-style`;
3. your saved defaults;
4. the program's defaults.

So asking for a theme asks for its style too, over your defaults:

```console
$ slugger --theme heroku                       # heroku's style beats the saved "_"
silvery-mud-7643
$ slugger --theme heroku --mimic-style false   # your defaults apply again
dappled_waxing_mud_764
```

## Scripts and CI

**Use `--oneshot` whenever a terminal may be attached.** The interactive loop starts whenever
standard input is a terminal, whatever standard output is — so in an interactive shell,
`name=$(slugger)` and `slugger | head -1` wait silently for an Enter. On a CI runner, standard input
is not a terminal and `slugger` generates once by itself, but `--oneshot` costs nothing and makes
the script portable. You can also save it: `slugger --init --oneshot`.

```bash
slugger --count 3 --oneshot | while read -r name; do
  echo "would create: $name"
done
```

- **Standard output** holds the slugs, one per line, and nothing else. `--list-themes` prints one
  theme name per line, so `slugger --list-themes | xargs -n1 slugger --oneshot --theme` draws one
  slug from each theme.
- **Standard error** holds refusals and warnings.
- **Exit code** 0 when the command did what was asked, 1 when it refused — an unknown option, a
  bad value, a theme that fails validation. A crash, such as `--clipboard` without `xsel` on Linux,
  exits with another code (134), so test for success rather than for 1. `--analyze` exits 0 even for a refused theme: the
  analysis worked, and its verdict is in the report.
- **`--theme '*'`** loads and validates every theme on each run, which takes seconds with many
  themes: draw what you need in one call with `--count` rather than in a loop.
- **`--seed`** makes a run reproducible: the same seed, options and themes give the same slugs.
- **`--count`** does not guarantee distinct slugs.

When the command line is wrong, every reason is reported at once rather than the first one:

```console
$ slugger --casing SHOUT --thme docker --count abc
The command line was refused for 3 reasons:

  - "--thme" is not an option slugger has. "--help" lists the ones it does.
  - "--casing" accepts kebab, snake, camel, and "SHOUT" is none of them.
  - "--count" needs a whole number, and "abc" is not one.
```

### Clipboard

`--clipboard` copies the **last** slug of the run to the clipboard. On Linux it needs `xsel`
(`sudo apt install xsel`); without it the command crashes, and if `--clipboard` was saved with
`--init`, every run does — delete the config file or install `xsel`.

## Option reference

`slugger --help` prints the same list.

**Choosing themes**

| Option | Default | |
| --- | --- | --- |
| `--theme <NAME>` | `slugger` | Theme to draw from. Repeatable, comma-separated, `'*'` for all. |
| `--theme-dir <PATH>` | `~/.slugger/themes` (`%USERPROFILE%\.slugger\themes` on Windows) | Where registered themes live. |
| `--mimic-style [true\|false]` | on for one theme, off for several | Whether the drawn theme's own style applies. |
| `--allow-small-theme` | off | Waive the minimum size rules for this run. |

**Shaping the slug**

| Option | Default | |
| --- | --- | --- |
| `--segment <MODE>` | `both` | `adjective`, `participle`, `either`, `both` or `threeOrTwo`. |
| `--sep <CHARACTER>` | `-` | What joins the terms. |
| `--word-sep <CHARACTER>` | the separator | What joins the words of a multi-word term; `""` glues them. |
| `--casing <CASING>` | `kebab` | `kebab`, `snake` or `camel`. |
| `--token-length <DIGITS>` | `0` | Length of the trailing token; 0 for none. |
| `--token-chance <PERCENT>` | `100` | How many slugs out of 100 get a token. |
| `--token-hex` | off | Hexadecimal token instead of decimal. |
| `--token-glued` | off | No separator before the token. |
| `--fold-accents` | off | Remove the diacritics that can be removed. |
| `--ascii` | off | Force an ASCII slug, dropping what cannot be folded. |
| `--max-length <CHARACTERS>` | none | Longest slug allowed; leaves words out, never truncates. |
| `--max-segment-words <WORDS>` | none | Most words a term may hold, or `none` to lift a theme's cap. |

A theme's own style may change these defaults when that theme is drawn alone.

**Running**

| Option | Default | |
| --- | --- | --- |
| `--count <N>` | `1` | How many slugs one round generates. |
| `--seed <N>` | random | Seed for a reproducible run. |
| `--oneshot` | off | Generate once and quit, instead of waiting for Enter. |
| `--clipboard` | off | Copy the last slug to the clipboard. |

**Commands** — each does one thing and quits

| Option | |
| --- | --- |
| `--list-themes` | List the themes available. |
| `--theme-info <NAME>` | Show a theme's metadata. |
| `--register <PATH>` | Validate a theme file and copy it into the theme directory. |
| `--unregister <NAME>` | Remove a registered theme. |
| `--analyze <PATH>` | Measure a theme file and write a report next to it — see [writing-a-theme.md](writing-a-theme.md). |
| `--init` | Save the other options as your defaults. |
| `-h`, `--help` / `-v`, `--version` | Help and version. |

## Troubleshooting

| Symptom | Cause and fix |
| --- | --- |
| `$(slugger)` or `slugger \| ...` hangs | The interactive loop is waiting for Enter: add `--oneshot`. |
| `Option does not have a name.` | A value starting with a dash: write `--sep=-`. |
| `Unknown command 'something.json'` | An unquoted `*`: write `--theme '*'`. |
| `--casing snake` still prints dashes | The separator decides: add `--sep _`. |
| `No theme named "x"` | Names are case-sensitive file names; `--list-themes` shows them. A path is not a name: use `--theme-dir`. |
| No token with `--theme docker --token-length 4` | `docker`'s style sets the token chance to 1: add `--token-chance 100`. |
| `--max-length` refuses the theme | Too few words fit: raise the limit, or draw one word before the noun with `--segment adjective`. |
| `--segment participle` refuses every theme | No shipped theme declares enough participles per noun; use `either`. |
| `--clipboard` crashes on Linux | Install `xsel`. |
| A saved option will not go away | Delete the config file — see [Personal defaults](#personal-defaults). |
