# Writing a theme

A theme is a JSON file: a list of nouns, and the adjectives and participles that may stand in front
of them. No code and no recompiling — you point `slugger` at the file and draw.

This page takes you from a first file to a theme you can share: how to try it while you write it,
what each key does, the size rules every theme must clear and how to read a refusal. When you only
need to look something up — a key, a message, a line of the analysis report — go to
[theme-reference.md](theme-reference.md). Checking that the pairs a theme draws actually make sense
is a separate job, which no rule can do for you: see [reviewing-a-theme.md](reviewing-a-theme.md).
The words used here — term, epithet, pool, floor — are defined in
[ubiquitous-language.md](ubiquitous-language.md).

You need the `slugger` command: see [Install](../README.md#install). Its options are described in
[cli.md](cli.md). The decision records linked from this page explain why things work the way they
do; they are written in French. The slugs shown here are examples: every run draws at random, so
yours will differ.

- [Your first theme](#your-first-theme)
- [Trying a theme while you write it](#trying-a-theme-while-you-write-it)
- [Installing a theme](#installing-a-theme) and [theme names](#theme-names)
- [Categories](#categories-which-epithet-for-which-noun), [participles](#participles),
  [`except`](#ruling-out-a-word-for-one-noun-except) and
  [`incompatible`](#ruling-out-a-participle-beside-an-adjective-incompatible)
- [The size rules](#the-size-rules)
- [What happens to your values](#what-happens-to-your-values)
- [`maxLength`](#promising-a-length-maxlength),
  [`maxSegmentWords`](#capping-the-words-of-a-term-maxsegmentwords),
  [`defaults`](#defaults-the-theme-style) and [`meta`](#meta-describing-the-theme)
- [Reading a refusal](#reading-a-refusal)
- [Where to find the built-in themes](#where-to-find-the-built-in-themes)

## Your first theme

### A tiny one, to try things

```json
{
  "adjectives": { "common": ["warm", "smoky", "bright"] },
  "nouns": [{ "value": "saffron" }, { "value": "cumin" }]
}
```

Save it as `spices.json`. `adjectives` and `nouns` are the only keys a theme must have. This one is
far below the [size rules](#the-size-rules), so `slugger` refuses it:

```console
$ slugger --theme-dir . --theme spices --oneshot
Theme "spices" was refused for 3 reasons:

  - 2 nouns, but a theme needs at least 100.
  - "saffron" reaches 3 adjectives, but every noun needs at least 100.
  - "cumin" reaches 3 adjectives, but every noun needs at least 100.
```

`--allow-small-theme` lifts the size rules for one run, which is enough to watch the file work:

```console
$ slugger --theme-dir . --theme spices --allow-small-theme --count 4 --oneshot
bright-saffron
warm-cumin
bright-saffron
bright-saffron
```

### The smallest theme that is accepted

A theme is accepted with **100 distinct nouns, each reaching at least 100 adjectives**. The
simplest way there is 100 adjectives under `common`, which every noun reaches, and 100 nouns with
no category. Abbreviated, it looks like this:

```json
{
  "adjectives": {
    "common": ["warm", "bright", "bold", "smoky", "sweet", "bitter", "earthy", "fragrant"]
  },
  "nouns": [
    { "value": "saffron" }, { "value": "cumin" }, { "value": "turmeric" }, { "value": "paprika" }
  ]
}
```

<details>
<summary>The complete file: 100 adjectives and 100 nouns</summary>

```json
{
  "adjectives": {
    "common": [
      "warm", "bright", "bold", "smoky", "sweet", "bitter", "earthy", "fragrant", "pungent", "sharp",
      "mellow", "golden", "dusky", "fiery", "gentle", "rich", "deep", "subtle", "zesty", "tangy",
      "robust", "heady", "floral", "woody", "nutty", "citrus", "musky", "resinous", "peppery", "dry",
      "fresh", "ancient", "rare", "humble", "noble", "wild", "crisp", "toasted", "roasted", "ground",
      "whole", "cracked", "crushed", "dusty", "silky", "velvet", "amber", "crimson", "ochre", "russet",
      "tawny", "ruby", "scarlet", "coppery", "bronze", "tender", "fierce", "quiet", "loud", "lively",
      "lazy", "brisk", "shy", "brave", "proud", "lucky", "merry", "jolly", "eager", "calm",
      "vivid", "stark", "lush", "lean", "spare", "plump", "round", "long", "slender", "tiny",
      "great", "grand", "little", "hidden", "secret", "distant", "northern", "southern", "eastern", "western",
      "royal", "honest", "clever", "curious", "dreamy", "sleepy", "restless", "steady", "swift", "slow"
    ]
  },
  "nouns": [
    { "value": "saffron" }, { "value": "cumin" }, { "value": "turmeric" }, { "value": "paprika" }, { "value": "cinnamon" },
    { "value": "clove" }, { "value": "nutmeg" }, { "value": "mace" }, { "value": "cardamom" }, { "value": "anise" },
    { "value": "fennel" }, { "value": "coriander" }, { "value": "caraway" }, { "value": "dill" }, { "value": "mustard" },
    { "value": "fenugreek" }, { "value": "sumac" }, { "value": "allspice" }, { "value": "pepper" }, { "value": "peppercorn" },
    { "value": "chilli" }, { "value": "cayenne" }, { "value": "ginger" }, { "value": "galangal" }, { "value": "garlic" },
    { "value": "onion" }, { "value": "shallot" }, { "value": "vanilla" }, { "value": "tamarind" }, { "value": "asafoetida" },
    { "value": "ajwain" }, { "value": "nigella" }, { "value": "sesame" }, { "value": "poppy" }, { "value": "juniper" },
    { "value": "bay" }, { "value": "basil" }, { "value": "oregano" }, { "value": "thyme" }, { "value": "rosemary" },
    { "value": "sage" }, { "value": "marjoram" }, { "value": "tarragon" }, { "value": "chervil" }, { "value": "lovage" },
    { "value": "savory" }, { "value": "mint" }, { "value": "parsley" }, { "value": "cilantro" }, { "value": "lemongrass" },
    { "value": "curry" }, { "value": "harissa" }, { "value": "berbere" }, { "value": "dukkah" }, { "value": "baharat" },
    { "value": "advieh" }, { "value": "amchur" }, { "value": "annatto" }, { "value": "achiote" }, { "value": "mahlab" },
    { "value": "mastic" }, { "value": "sansho" }, { "value": "cubeb" }, { "value": "wasabi" }, { "value": "horseradish" },
    { "value": "liquorice" }, { "value": "cassia" }, { "value": "cocoa" }, { "value": "chicory" }, { "value": "sorrel" },
    { "value": "sassafras" }, { "value": "epazote" }, { "value": "culantro" }, { "value": "perilla" }, { "value": "shiso" },
    { "value": "yuzu" }, { "value": "pimento" }, { "value": "aleppo" }, { "value": "urfa" }, { "value": "kashmiri" },
    { "value": "habanero" }, { "value": "chipotle" }, { "value": "ancho" }, { "value": "guajillo" }, { "value": "pasilla" },
    { "value": "jalapeno" }, { "value": "serrano" }, { "value": "poblano" }, { "value": "borage" }, { "value": "hyssop" },
    { "value": "chive" }, { "value": "angelica" }, { "value": "woodruff" }, { "value": "verbena" }, { "value": "costmary" },
    { "value": "lavender" }, { "value": "zedoary" }, { "value": "spikenard" }, { "value": "mugwort" }, { "value": "rue" }
  ]
}
```

</details>

```console
$ slugger --analyze ./spices.json
Theme "spices" is accepted as it is.
Analysis of "spices" written to ./spices-analysis.md
$ slugger --theme-dir . --theme spices --count 5 --oneshot
gentle-cassia
scarlet-caraway
musky-sumac
ruby-culantro
round-allspice
```

It passes because it declares no participle, so a single adjective goes in front of each noun and
the floors are 100 nouns and 100 adjectives per noun; and because no noun names a category, so no
category has a floor to clear. It sits exactly on both floors: one noun or one adjective fewer, and
it is refused.

Everything else is optional: categories, so that each adjective fits its noun; participles, for a
third word; exclusions; a length promise; a style; a description. Here is the shape of a fuller
version of the same theme, with short lists. The complete file is
[examples/spices.json](examples/spices.json) — 101 common adjectives, three categories, 106 nouns —
and the outputs further down this page come from it. The excerpt alone is refused by the size
rules:

```json
{
  "meta": {
    "title": "Spices",
    "description": "Spices, herbs and chillies, with the heat where it belongs",
    "version": "1.0.0",
    "author": "Jo Example",
    "source": "https://example.com/themes/spices.json",
    "createdAt": "2026-10-09",
    "publishedAt": "2026-10-09"
  },
  "adjectives": {
    "common": ["warm", "bright", "gentle", "sleepy", "saffron"],
    "hot": ["blistering", "red-hot", "tongue-numbing"],
    "seed": ["ridged", "hard-shelled"],
    "leaf": ["leafy", "feathery"]
  },
  "participles": {
    "common": ["simmering", "rising", "waking"],
    "hot": ["burning", "blazing", "searing", "scalding", "smouldering"],
    "leaf": ["wilting", "unfurling"]
  },
  "incompatible": {
    "gentle": ["burning", "blazing", "searing", "scalding"],
    "sleepy": ["waking"]
  },
  "nouns": [
    { "value": "saffron", "except": ["saffron"] },
    { "value": "chilli", "categories": ["hot"] },
    { "value": "mustard", "categories": ["hot", "seed"] },
    { "value": "basil", "categories": ["leaf"] },
    { "value": "Piment d'Espelette", "categories": ["hot"] },
    { "value": "vanilla" }
  ],
  "defaults": { "sep": "_" },
  "maxLength": { "twoWords": 40, "threeWords": 63 }
}
```

Write the file to be read. Repeating a word in several categories, or from one theme to another,
costs nothing.

## Trying a theme while you write it

Two commands, run from the folder that holds the file, are the whole loop:

```bash
slugger --analyze ./spices.json                   # verdict here, measurements in spices-analysis.md
slugger --theme-dir . --theme spices --count 20   # draw from the file as it is now
```

- **`--analyze` takes a path.** It tells you whether the theme would be accepted and writes
  `spices-analysis.md` next to the file, with your margin on every floor. A refusal answers
  *whether*; the report answers *by how much*, and it still measures a theme that the size rules
  refuse — that is when it helps most. It measures a theme with a
  [coherence error](#coherence-errors) too, and lists the same reasons as `--register`. Only a
  malformed file or section, or a theme with no noun, leaves nothing to measure: the report then
  lists the errors alone, and you run `--analyze` again once they are fixed. Every line of the
  report is explained in [theme-reference.md](theme-reference.md#the-analysis-report-annotated).
- **`--theme-dir .` makes the current folder the theme directory**, so `--theme spices` finds
  `spices.json` without installing anything. `--theme` takes the name, never the path.
- **In a terminal, the draw stays open**: press Enter for 20 more slugs, Ctrl+D to stop. Add
  `--oneshot` to draw once and return. With `--seed`, the rounds carry on one sequence, so the same
  command replays the same session.
- **While the theme is below the floors**, add `--allow-small-theme` to the draw. `--analyze`
  ignores that option and always applies the real floors.

Edit, run both again, read. Once the analysis says *accepted* and the slugs read well, install it.

## Installing a theme

```bash
slugger --register ./spices.json   # validate, then copy into the theme directory
slugger --theme spices             # from any folder, from now on
slugger --list-themes              # every theme available: built-in and registered
slugger --theme-info spices        # what the theme says about itself
slugger --unregister spices        # remove the copy
```

The theme directory is `~/.slugger/themes` (under your user profile on Windows), or the folder
`--theme-dir` names.

- **`--register` validates exactly as any load does**, and copies nothing if the file is refused.
- **It copies the file.** Later edits to your original do not reach the registered copy, and
  registering a name that already exists is refused rather than overwritten: run
  `--unregister spices` first, then `--register` again.
- **A small theme can be registered** with `--allow-small-theme`, but every run that draws from it
  needs the option again. If the theme is meant to stay small, write `"allowSmall": true` in the
  file instead (see [the size rules](#the-size-rules)).
- **A file with the same name as a built-in theme replaces it**, and `--register` says so:

```console
$ slugger --register ./docker.json
Theme "docker" registered.
warning: "docker" now shadows the built-in theme of the same name.
```

`--unregister docker` then removes your file and brings the built-in theme back. A built-in theme
itself cannot be unregistered:

```console
$ slugger --unregister heroku
"heroku" is embedded in the binary, so there is nothing to unregister - to stop using it, leave it out of --theme.
```

### Theme names

A theme's name is its file name without `.json`. Nothing inside the file names it — not even
`meta.title`, which is only a label for people.

- **`--theme`, `--theme-info` and `--unregister` take a name; `--analyze` and `--register` take a
  path.** A path given to `--theme` is looked up as a name and not found, and slugger says where the
  folder goes instead:

  ```console
  $ slugger --theme-dir . --theme ./spices.json --oneshot
  Theme "./spices.json" could not be found. Available: docker, heroku, slugger, spices.
  --theme takes a theme name; to draw from a folder, use --theme-dir <folder> --theme <name>
  ```

- **Write the name exactly as the file is named.** On Linux, `--theme Spices` does not find
  `spices.json`; the built-in names are always lowercase.
- **Never put a comma in a file name.** `--theme` splits its value on commas to draw from several
  themes, so a theme named `a,b` could never be drawn: `--theme a,b` asks for a theme `a` and a
  theme `b`. `--register` refuses such a file, and a file named `*.json` too, since `--theme '*'`
  means every theme:

  ```console
  $ slugger --register ./a,b.json
  Theme "a,b" cannot be registered: --theme splits its value on commas, so no --theme could ever select it. Rename the file.
  ```

  Copied into a theme directory by hand, such a file is left out of `--list-themes` and of
  `--theme '*'`, and every run says why on standard error:

  ```text
  warning: /home/jo/.slugger/themes/a,b.json is ignored: --theme splits its value on commas, so no --theme could ever select it. Rename the file.
  ```
- Lowercase letters, digits and hyphens are the safe choice: `french-gastronomy`, `spices`.

## Categories: which epithet for which noun

Categories are the heart of the file, and the reason `slugger` exists. An adjective can only be
drawn for a noun when the two share a category; without them you get `thundering-moon` as readily
as `weeping-willow` ([DEC0001](idr/DEC0001-restriction-des-adjectifs-par-categorie.md)).

A category is a key in `adjectives` (or `participles`) and a label in a noun's `categories`:

```json
"adjectives": {
  "common": ["warm", "bright"],
  "hot": ["blistering", "red-hot", "tongue-numbing"]
},
"nouns": [
  { "value": "vanilla" },
  { "value": "chilli", "categories": ["hot"] }
]
```

**`common` is a base that every noun stands on, not a fallback**
([DEC0002](idr/DEC0002-common-atteint-par-tout-nom.md)). Every noun reaches it *in addition to*
the categories it names:

| The noun names | It reaches |
| --- | --- |
| *(nothing)* | `common` |
| `["hot"]` | `hot` + `common` |
| `["hot", "seed"]` | `hot` + `seed` + `common` |

So no category has to reach 100 adjectives on its own: what counts is the sum with `common`. The
built-in `slugger` theme relies on that — five categories of 45 adjectives each, and 60 in `common`.

**Never write `common` on a noun.** It adds nothing, since the noun already reaches `common`, and
it gets the theme refused. A category that nouns name must reach 40,000 combinations from those
nouns alone ([the per-category floor](#the-per-category-floor)); `common` escapes that rule only as
long as no noun names it. Here is the complete spices file with `"categories": ["common"]` written
on `turmeric`:

```console
$ slugger --register ./spices.json
Theme "spices" was refused for 1 reason:

  - Category "common" totals 2,424 combinations, but every category needs at least 40,000.
```

Apart from `common`, a category name is free text with no meaning to the program. Two themes can
both have a `hot` category with nothing in common: a draw never leaves its own file. Names are
compared exactly — `Hot` is not `hot` — and **every category a noun names must exist** as a key in
`adjectives` or in `participles`; either one is enough.

## Participles

`participles` is optional. It has exactly the shape of `adjectives`, and adds a third word to the
slug: `warm_rising_mustard`. Declaring it is enough; no option is needed, because the default
[segment mode](#the-size-rules), `both`, puts an adjective and a participle in front of the noun.

**Classify participles by what the noun can physically do, not by subject.** That is what makes
them plausible. The built-in `heroku` theme uses `mobile` (it moves), `sonore` (it makes a sound),
`lumineux` (it gives light), `vivant` (it is alive), `chaleur` (heat) and `eau` (water) — its
category names are French, which the program does not care about:

- `moon` is `["lumineux", "mobile"]`, without `sonore` — so `thundering-moon` is never drawn,
  while `waning-moon` (through `common`) can be.
- `willow` is `["vivant"]` — `weeping-willow`, a real English idiom, can be drawn.
- `river` is `["eau", "mobile", "sonore"]` — `thundering-river`, `humming-river` and
  `swirling-river` all sound right.

Sorting the same words by subject ("astronomy", "weather") would have filtered nothing.

If a drawn noun reaches no participle, the slug falls back to the adjective alone, without an
error — although the [participle floor](#the-size-rules) normally prevents it. A theme whose
`defaults.segmentMode` asks for `participle` or `either` but which declares no participle at all is
refused.

**The same word may appear in both sections** — `glowing` is an adjective and a present
participle. It is allowed ([DEC0013](idr/DEC0013-mot-declare-dans-les-deux-sections.md)), and
`--register` points it out without refusing anything. With `glowing` added to the `common`
adjectives of the complete spices file:

```console
$ slugger --register ./spices.json
Theme "spices" registered.
warning: "glowing" declared as both an adjective and a participle; a draw that lands on the same word twice writes it once.
```

When a draw does land on the same word twice, the slug writes it once — `glowing_cumin` rather than
`glowing_glowing_cumin`. That slug has lost a word, hence the warning.

## Ruling out a word for one noun: `except`

Categories keep an adjective away from a noun it cannot describe. They do not keep it away from a
noun it describes perfectly well and still insults: Docker's own generator refuses
`boring_wozniak` in its code for exactly that reason. `except` says it in the theme, noun by noun
([DEC0011](idr/DEC0011-exclusion-de-mots-par-nom.md)). The built-in `docker` theme has:

```json
{ "value": "wozniak", "except": ["boring", "condescending", "calculating", "ornery", "sly", "crafty"] }
```

`boring` stays available to every other noun; it simply never reaches this one. The exclusion
applies **to adjectives and participles alike**: what makes a word unwelcome next to a noun is the
word, not its grammar, and `boring` is a present participle too. In the spices theme, `saffron` is
both a colour adjective in `common` and a noun, and `"except": ["saffron"]` on the noun is what
keeps the adjective away from the noun.

- **A word the theme declares nowhere is refused**, not ignored. A safety list that lets a typo
  through is worse than no list: `saffon` would leave the noun looking protected when it is not.
- **You cannot exclude too much without noticing.** The 100-adjective floor is measured after the
  exclusions, so a noun emptied by its own `except` gets the theme refused, and the refusal names
  it.
- **The key is not checked.** A misspelt `"excpet"` is ignored silently, like every
  [unknown key](theme-reference.md#schema): the theme loads and the word is drawn.

## Ruling out a participle beside an adjective: `incompatible`

`except` keeps a word away from a noun. It says nothing about the pair formed by the **two** words
in front of the noun under `both`: `gentle` is a fine adjective for a spice, `burning` a fine
participle for a chilli, and `gentle_burning_chilli` contradicts itself.

`incompatible` declares those pairs once, for the whole theme
([DEC0017](idr/DEC0017-refus-d-un-participe-a-cote-d-un-adjectif.md)):

```json
"incompatible": {
  "gentle": ["burning", "blazing", "searing", "scalding"],
  "sleepy": ["waking"]
}
```

The key is an adjective and the values are participles. `slugger` draws the adjective **first**,
then the participle from what is left — so a refused pair is never drawn at all, rather than drawn
and thrown away.

- **Direction matters.** `gentle` refuses `burning`; if `burning` were also an adjective, it would
  refuse nothing. Write the other direction too if you want it.
- **A reversed pair is refused at load, and the message says so**: when your key is declared in
  `participles` rather than `adjectives`, that is almost always the cause.
- **The participle floor is measured after the subtraction**, for the worst pair. A noun that
  reaches 24 participles, beside an adjective that refuses 5 of them, has 19 for that draw — and
  the theme is refused, naming the noun **and** the adjective.
- **It only matters under `both`** (and `threeOrTwo`). The other modes put a single word in front
  of the noun, so two words never meet. A pair declared anyway is pointed out without refusing
  anything — as is a pair that no noun can bring together.

That makes three ways for a word to vanish from a draw: categories, `except` and `incompatible`.
When a word never comes out, check all three; the analysis report helps by listing the categories
no noun names and the pairs that can never apply.

## The size rules

A theme is measured on the **pools it actually resolves**, not on the length of its lists
([DEC0003](idr/DEC0003-validation-sur-le-pool-resolu.md)): a file of 500 adjectives, 480 of them in
one category, leaves the other nouns a dozen choices each, and a global count would never see it.

1. At least **100 distinct nouns**.
2. Every noun reaches at least **100 words to put in front of it**.
3. **Under `both` and `threeOrTwo` only**, every noun also reaches at least **20 participles** —
   and still 20 beside every adjective it can draw, once `incompatible` has been applied.
4. Every category that a noun names totals at least **40,000 combinations**.

### What the per-noun floor counts

"Words to put in front of it" depends on the segment mode, because the mode decides what is drawn
([DEC0016](idr/DEC0016-planchers-alignes-sur-le-mode-de-segment.md)). The mode is
`defaults.segmentMode` when the theme declares one, `both` otherwise. The key and the option say
"segment" for historical reasons: what they choose is the epithet.

| Segment mode | Rule 2: the 100 words are | Rule 3 |
| --- | --- | --- |
| `adjective` | its adjectives | — |
| `participle` | its participles | — |
| `either` | its adjectives **plus** its participles, as one pool | — |
| `both` (default) | its adjectives | 20 participles as well |
| `threeOrTwo` | its adjectives | 20 participles as well, as under `both` |
| *(the theme declares no participle)* | its adjectives | — |

Under `either`, one word precedes the noun, drawn from both sections together in proportion to
their size ([DEC0015](idr/DEC0015-tirage-pondere-du-mot-unique-de-either.md)): 178 adjectives and
20 participles make a pool of 198 in which a participle comes out about one time in ten. That is
why the two together must reach 100, rather than each on its own.

Under `both`, the participle is a **second** word beside the adjective, as visible in the slug as
the adjective: a noun that reaches three participles repeats its middle word endlessly. Hence rule
3, whose floor is low and will stay low until the shipped themes have grown.

Under `threeOrTwo`, the participle is drawn from a pool with **one more candidate** than you
declare, and that candidate is no participle at all
([DEC0020](idr/DEC0020-absence-de-participe-tiree-comme-un-participe-de-plus.md)): a noun that
reaches 25 participles draws from 26, and the 26th outcome writes a slug of two words. The floors
are those of `both`, because the absence takes a share of the draws, never a share of the pool.

Under `adjective`, a thin `participles` section refuses nothing: it is never drawn. The analysis
report still shows it, with a dash where the floor would be.

**A run can ask for another mode, and the floors follow it.** `--segment both` on a theme written
for `either` measures the theme under `both`, and refuses it if its nouns lack participles. The same
happens when your theme is drawn **with other themes** (`--theme spices,docker` or `--theme '*'`):
theme styles are switched off, so the mode falls back to `both` unless the command line says
otherwise. A theme written for `either` with a handful of participles works alone and is refused
the moment it is mixed — so if you declare participles, give every noun at least 20. Here is the
smallest accepted theme, given five participles and `"segmentMode": "either"`:

```json
"participles": { "common": ["simmering", "steeping", "blooming", "crackling", "drifting"] },
"defaults": { "segmentMode": "either" }
```

On its own it draws one word in front of each noun. Drawn with `docker`, it is refused:

```console
$ slugger --theme-dir . --theme spices,docker --oneshot
Theme "spices" was refused for 100 reasons:

  - "saffron" reaches 5 participles, but a theme drawing "both" needs at least 20 per noun.
  - "cumin" reaches 5 participles, but a theme drawing "both" needs at least 20 per noun.
  - "turmeric" reaches 5 participles, but a theme drawing "both" needs at least 20 per noun.
    ... and 97 more of the same kind
```

### The per-category floor

For each noun, the number of slugs it can produce is:

```text
combinations(noun) = adjectives it reaches × participles it reaches
```

where a noun that reaches no participle counts 1 rather than 0. This product is computed
**whatever the segment mode**, because `--segment both` can reach it from any theme. A category's total is
the sum of `combinations(noun)` over **the nouns that name that category** in their `categories`,
and it must reach 40,000. A noun that names two categories counts in both.

Two kinds of category are never measured: `common`, unless a noun writes it (and then it is
measured like any other, [as above](#categories-which-epithet-for-which-noun)); and a category that
no noun names — the analysis report lists those, since their words are never drawn.

**An example.** Take the smallest accepted theme, add the 20 `hot` adjectives and the 24 `common`
participles of [the complete spices file](examples/spices.json), and write
`"categories": ["hot"]` on some nouns. Each of those
nouns reaches 100 + 20 = 120 adjectives and 24 participles, so it brings 120 × 24 = 2,880
combinations to `hot`. With 13 hot nouns, the theme is refused:

```console
$ slugger --theme-dir . --theme spices --oneshot
Theme "spices" was refused for 1 reason:

  - Category "hot" totals 37,440 combinations, but every category needs at least 40,000.
```

With a 14th, `hot` totals 40,320 and the theme is accepted. Without the participles, each hot noun
brings only 120, and the same category needs **334 nouns**. That is the practical lesson: a
category without participles is very expensive. Roughly, a category needs
`40,000 ÷ (adjectives × participles)` nouns:

| A noun of the category reaches | Nouns the category needs |
| --- | --- |
| 120 adjectives, no participle | 334 |
| 120 adjectives, 24 participles | 14 |
| 120 adjectives, 40 participles | 9 |
| 150 adjectives, 30 participles | 9 |

### Why these numbers, and how to lift them

40,000 is roughly where a name generator needs a numeric suffix to avoid collisions: Docker's own
generator has 108 adjectives × 236 names = 25,488 combinations and Heroku's 91 × 95 = 8,645, and
both add a suffix. The three built-in themes clear it without help.

If your theme is below the floors and you mean it to be, two switches lift rules 1 to 4:

- `"allowSmall": true` in the file — declared once by its author, valid for good;
- `--allow-small-theme` on the command line — for one run, to try a theme still being written.

### Coherence errors

Neither switch lifts the other checks, which find a file that contradicts itself rather than a
small one. This documentation calls them **coherence errors**:

- a theme with no noun at all;
- a category that a noun names and the theme does not declare;
- a word in `except` or `incompatible` that the theme does not declare;
- a `segmentMode` that needs participles the theme does not have;
- a `maxLength` promise the theme cannot keep;
- a malformed file or section: broken JSON, a missing required key, a value of the wrong type or
  out of range.

They are refused whatever the switches say: `allowSmall` accepts a small theme, not an incoherent
one. `--analyze` still measures a theme refused for a coherence error: it lists the same reasons
as `--register` and shows the margins beside them. The exceptions are a malformed file or section
and a theme with no noun, which leave nothing to measure: the report and the terminal list the
errors alone, and once they are fixed, `--analyze` shows the margins. The report says *the file
was read, but these errors leave nothing that can be measured*, or, for a file that is not valid
JSON, that it *could not be read*.

## What happens to your values

Write your values the way they are really written — `"Piment d'Espelette"`, `"Ras el-Hanout"`,
`"Salt & Pepper"`. They are cleaned when the theme is loaded
([DEC0008](idr/DEC0008-reduction-des-caracteres-non-alphanumeriques.md)): everything is lowercased,
and **anything that is neither a letter nor a digit becomes a word boundary**. Consecutive
boundaries count as one, and boundaries at either end disappear.

| Written in the file | After loading |
| --- | --- |
| `"Piment d'Espelette"` | `piment d espelette` |
| `"Ras el-Hanout"` | `ras el hanout` |
| `"Salt & Pepper"` | `salt pepper` |
| `"St. John's Wort"` | `st john s wort` |
| `"Five-Spice!"` | `five spice` |
| `"Blend 21"` | `blend 21` |

Messages quote values in this cleaned form: a refusal talks about `"piment d espelette"`, not
`"Piment d'Espelette"`. Note what the apostrophe does: `Za'atar` becomes two words, `za atar`.

**Accents are kept** — `" Crème    Brûlée "` becomes `crème brûlée`, never `creme brulee`: an
accented letter is a letter, and so is a letter of any other alphabet. Write words as they are
spelled. If a slug has to live somewhere that cannot take your alphabet, that is for the run to
decide, not the theme ([DEC0009](idr/DEC0009-pliage-des-accents-a-la-demande.md)): `--fold-accents`
turns `é` into `e` and `ō` into `o` when the slug is formed.

`--fold-accents` only folds what decomposes, which means the accents of the Latin alphabet. `ø`,
`ß` and `œ` do not decompose, nor does any non-Latin script: they pass through unchanged. Folding
never guarantees an ASCII slug. When the destination demands one, `--ascii` promises the result
rather than the mechanism — and mangles what it cannot fold
([DEC0010](idr/DEC0010-option-ascii-qui-defigure.md)). With an adjective `warm` in front:

| Value | *(nothing)* | `--fold-accents` | `--ascii` |
| --- | --- | --- | --- |
| `Crème Brûlée` | `warm-crème-brûlée` | `warm-creme-brulee` | `warm-creme-brulee` |
| `Shichimi Tōgarashi` | `warm-shichimi-tōgarashi` | `warm-shichimi-togarashi` | `warm-shichimi-togarashi` |
| `Søren Straße` | `warm-søren-straße` | `warm-søren-straße` | `warm-sren-strae` |
| `唐辛子` | `warm-唐辛子` | `warm-唐辛子` | `warm` |

`--ascii` includes the folding, so the two are never needed together. A term with nothing left is
dropped from the slug rather than joined as an empty segment — but **when nothing survives at all,
the slug is empty**. That is the accepted price of the option; use it only where nothing else will
do.

A value that holds no letter and no digit is refused, since nothing would be left to draw.

The word boundaries survive until the slug is formed, where the separator replaces them — or
`--word-sep`, if you want them to become something else:

| | `warm` + `"Piment d'Espelette"` | `warm` + `"Herbes de Provence"` |
| --- | --- | --- |
| default | `warm-piment-d-espelette` | `warm-herbes-de-provence` |
| `--word-sep _` | `warm-piment_d_espelette` | `warm-herbes_de_provence` |
| `--word-sep ''` | `warm-pimentdespelette` | `warm-herbesdeprovence` |

A multi-word term is therefore perfectly normal: `"Herbes de Provence"`, `"Ras el-Hanout"`.

## Promising a length: `maxLength`

A slug ends up somewhere, and that place has rules. **63 characters** is the one that matters most:
it is the limit of a DNS label, and therefore of an S3 bucket name, a Kubernetes Service name or a
subdomain. **30** if the target is a Heroku app or a GCP project.

Neither Docker nor Heroku truncates anything: their names fit because their words are short.
`maxLength` writes that discipline into the file
([DEC0018](idr/DEC0018-longueur-maximale-tenue-en-retirant-des-mots.md)):

```json
"maxLength": {
  "twoWords": 40,
  "threeWords": 63
}
```

- **`twoWords`** covers every slug with one word in front of the noun: segment modes `adjective`,
  `participle` and `either`.
- **`threeWords`** covers slugs with two words in front of the noun: segment mode `both`.
- `threeOrTwo` produces both shapes, so promise both keys.
- A missing key promises nothing, which is not the same as promising infinity.
- The values are whole numbers above zero. Both keys are checked whatever the theme's own mode.

The length is measured on the slug as the theme's own `defaults` would format it, token included,
and it counts characters, not bytes: `é` counts once here and twice in UTF-8.

`maxLength` sits at the **root** of the file, next to `allowSmall`, not inside `defaults`: the
`defaults` are switched off as soon as several themes are drawn together, and a promise that
disappears when a theme is added is no promise.

**What it costs you:** the day you add a word that breaks the promise, the theme is **refused at
load**, and the message shows you the longest slug it can now produce. That is the point — the
problem reaches you rather than the registry that rejects your image six months later. Here is the
complete spices file with its `twoWords` promise lowered to 30:

```console
$ slugger --register ./spices.json
Theme "spices" was refused for 1 reason:

  - maxLength.twoWords promises 30 characters, but the theme can produce "tongue_numbing_piment_d_espelette" at 33.
```

The built-in `docker.json` promises 63 and its longest slug is 28 characters; `heroku.json` promises
30 and its longest is 27 (`crystalline-wildflower-0000`). Three characters of margin: a noun of 14
characters, or an adjective of 15, would get `heroku.json` refused.

### At run time: `--max-length`

`--max-length` does not truncate either: it **removes from the draw** every word that would not
fit, then validates what is left like any other theme. When the reduced theme no longer clears its
floors, the run is refused, and the message says why. With the complete spices file:

```console
$ slugger --theme-dir . --theme spices --max-length 30 --oneshot
Theme "spices" was refused for 32 reasons:

  - Under 30 characters, "cardamom" reaches 15 participles after "hard shelled", but a theme drawing "both" needs at least 20 per noun for every adjective it can draw - raise the limit, shorten the words, or draw one word instead of two.
  - Under 30 characters, "coriander" reaches 7 participles after "hard shelled", but a theme drawing "both" needs at least 20 per noun for every adjective it can draw - raise the limit, shorten the words, or draw one word instead of two.
  - Under 30 characters, "mustard" reaches 10 participles after "tongue numbing", but a theme drawing "both" needs at least 20 per noun for every adjective it can draw - raise the limit, shorten the words, or draw one word instead of two.
    ... and 26 more of the same kind
  - "piment d espelette" reaches 17 adjectives, but every noun needs at least 100.
  - "shichimi tōgarashi" reaches 17 adjectives, but every noun needs at least 100.
  - "herbes de provence" reaches 18 adjectives, but every noun needs at least 100.
```

The last suggestion is the right one here: `--segment either --max-length 30` is accepted, because
one word in front of the noun leaves far more room. `--analyze` accepts both options, which answers
the question without drawing anything:

```bash
slugger --analyze ./spices.json --max-length 30 --segment either
```

## Capping the words of a term: `maxSegmentWords`

A multi-word term reaches the slug in several pieces: every boundary becomes a separator.
`tongue-numbing` + `smouldering` + `Piment d'Espelette` is written:

```text
tongue_numbing_smouldering_piment_d_espelette
```

Six pieces for three terms, and nothing says where each term starts. The built-in `docker` and
`heroku` themes have no such issue: none of their nouns has two words, which is what gives
`focused_turing` its shape.

`maxSegmentWords` writes that discipline into the file
([DEC0023](idr/DEC0023-plafond-de-mots-par-segment.md)):

```json
"defaults": { "maxSegmentWords": 1 }
```

and `--max-segment-words` asks for it at run time, from any theme:

```bash
slugger --theme spices --max-segment-words 1
```

Like `--max-length`, **it removes, it never cuts**: `piment d espelette` leaves the draw under a
one-word cap; it does not come out as `piment`. The count is per term — the noun, the adjective,
the participle — never for the whole slug: a three-word noun does not use up the adjective's room,
it is simply left out. The noun is measured like the others, and that is where it shows most,
since the noun is nearly always the long part.

What remains is validated like any theme, with the same messages as for a file: a cap that leaves a
noun too few adjectives, or a category too few combinations, gets the run refused, and the message
says by how much. `--analyze` accepts the option, and `--allow-small-theme` lifts the floors for a
trial. The value is a whole number of at least 1; a file that writes `0` is refused.

The cap goes in `defaults`, unlike `maxLength`: it is a matter of style, not a safety promise. It is
therefore switched off when several themes are drawn together, like everything in `defaults`.

### Lifting a theme's cap for one run

A theme's `defaults` apply **without being asked for**: a theme that writes `"maxSegmentWords": 1`
draws one word per term every time, even when the command line says nothing about it — and
`--analyze` measures it under that cap the same way. A multi-word noun written for that theme
therefore never comes out, unless the run asks explicitly for the opposite.

`--max-segment-words none` asks for exactly that
([DEC0024](idr/DEC0024-aucun-plafond-explicite-qui-outrepasse-le-theme.md)):

```bash
slugger --theme quantum-physics --max-segment-words none
```

[`quantum-physics`](../themes/quantum-physics.json) promises Docker's shape by default
(`maxSegmentWords: 1`), and still holds many multi-word nouns — `black hole`, `bell pair` —
written for that vocabulary rather than for three one-word segments. `none` brings them back for
one run without touching the rest of the theme's style: its separator, casing and segment mode
stay its own. That is the difference from `--mimic-style false`, which would discard all of it
along with the cap.

## `defaults`: the theme style

This block describes the style your theme imitates, not anyone's session preferences:

| Key | Command-line option | Values | Program default |
| --- | --- | --- | --- |
| `sep` | `--sep` | exactly one character — Docker writes `_`, Heroku and slugger `-` | `-` |
| `wordSep` | `--word-sep` | one character, or `""` to glue the words of a multi-word term together | the separator |
| `casing` | `--casing` | `kebab`, `snake` or `camel` | `kebab` |
| `foldAccents` | `--fold-accents` | `true` or `false`; rarely a theme's business, see [above](#what-happens-to-your-values) | `false` |
| `ascii` | `--ascii` | `true` or `false`; rarely a theme's business either | `false` |
| `segmentMode` | `--segment` | `adjective`, `participle`, `either`, `both` or `threeOrTwo`; also decides the [floors](#the-size-rules) | `both` |
| `maxSegmentWords` | `--max-segment-words` | a whole number, at least 1 | no cap |
| `tokenLength` | `--token-length` | how many characters the token has, 0 or more; `0` for none | `0` |
| `tokenHex` | `--token-hex` | `true` for a hexadecimal token rather than decimal | `false` |
| `tokenChance` | `--token-chance` | out of 100 slugs, how many get a token: 0 to 100 | `100` |
| `tokenGlued` | `--token-glued` | `true` glues the token to the last word: `focused_turing3` | `false` |

A value outside these bounds is refused at load, as the command-line option refuses it: a
`tokenChance` above 100, a negative `tokenLength`, a `maxSegmentWords` below 1.

`casing` only changes letters. `snake` with the default separator still writes
`warm-piment-d-espelette`: pair it with `"sep": "_"`. `camel` writes no separator at all.

Leave out what belongs to a session: `--count`, `--seed`, `--oneshot`, `--clipboard`.

**The theme style beats the user's saved defaults** when the theme is drawn alone: asking for
`--theme docker` asks for its format as much as its words (see [cli.md](cli.md#who-wins) for the
whole order). That is why you should **not** copy the program's defaults into the block — `"sep":
"-"`, `"casing": "kebab"`, `"segmentMode": "both"` or `"tokenLength": 0`. A block that says nothing
new only overrides the saved preferences of whoever uses your theme. The built-in `slugger.json`
has no `defaults` block at all, for exactly that reason.

When several themes are drawn together, every theme's style is switched off, unless the user adds
`--mimic-style` ([cli.md](cli.md#a-themes-own-style)).

## `meta`: describing the theme

A fully optional block, for whoever shares or reuses a theme rather than for the engine: nothing in
it affects a draw ([DEC0021](idr/DEC0021-bloc-meta-descriptif-jamais-consulte.md),
[DEC0022](idr/DEC0022-dates-de-creation-et-de-publication-dans-meta.md)).

```json
"meta": {
  "title": "Spices",
  "description": "Spices, herbs and chillies, with the heat where it belongs",
  "version": "1.0.0",
  "author": "Jo Example",
  "source": "https://example.com/themes/spices.json",
  "createdAt": "2026-10-09",
  "publishedAt": "2026-10-09"
}
```

| Key | What it is |
| --- | --- |
| `title` | A readable name for display. Never an identifier: `spices.json` is still asked for as `spices` |
| `description` | What the theme is, or what it is for |
| `version` | Free text, never compared or enforced |
| `author` | Who wrote it |
| `source` | Where to find the original — the URL of the **file**, not of the repository: whoever reads this already holds a copy and wants to know where it came from. A built-in theme has no copy to trace and leaves the key out |
| `createdAt` | When the theme was first written |
| `publishedAt` | When this `version` was published |

Every key is optional, `meta` included, and every value present must be a string — a number is
refused like any other malformed section. `version` and the two dates are free text: nothing reads
them as a version or a date, nothing compares them. Unknown keys inside `meta` are ignored.
Conventions for updating them when a theme changes are in
[themes/README.md](../themes/README.md#versions-and-dates).

`--theme-info` prints the block without measuring anything — unlike `--analyze`, it does not apply
the size rules. Like `--theme`, it takes a theme **name**, never a path:

```console
$ slugger --theme-dir . --theme-info spices
theme "spices"
  title: Spices
  description: Spices, herbs and chillies, with the heat where it belongs
  version: 1.0.0
  author: Jo Example
  createdAt: 2026-10-09
  publishedAt: 2026-10-09
  source: https://example.com/themes/spices.json
```

A missing key is not printed at all, rather than printed empty. The smallest theme above declares
no `meta`, and `--theme-info` says so:

```console
$ slugger --theme-dir . --theme-info spices
theme "spices"
  (no metadata declared)
```

## Reading a refusal

A theme is never refused one reason at a time
([DEC0006](idr/DEC0006-rapport-groupe-des-refus.md)). Parsing and validation report their findings
together, so one run tells you everything that needs fixing:

```json
{
  "adjectives": { "seed": ["ridged", "oval"], "common": ["warm", "bright"] },
  "nouns": [
    { "value": "cumin", "categories": ["seed"] },
    { "value": "chilli", "categories": ["hot"] },
    { "value": "" }
  ],
  "defaults": { "sep": "--", "casing": "upper" }
}
```

```console
$ slugger --register ./draft.json
Theme "draft" was refused for 9 reasons:

  - nouns[2]: no non-empty "value".
  - "defaults.sep" must be a single character.
  - "defaults.casing" must be one of kebab, snake, camel.
  - "chilli" references category "hot", which the theme does not declare (it declares common, seed).
  - 2 nouns, but a theme needs at least 100.
  - "cumin" reaches 4 adjectives, but every noun needs at least 100.
  - "chilli" reaches 2 adjectives, but every noun needs at least 100.
  - Category "hot" totals 2 combinations, but every category needs at least 40,000.
  - Category "seed" totals 4 combinations, but every category needs at least 40,000.
```

- **Every reason names its subject** — the noun, the category, the key — because a number alone
  does not say what to fix. Values are quoted in their [cleaned form](#what-happens-to-your-values).
- **A noun is located by its index in `nouns`, counting from 0**: `nouns[2]` is the third entry.
- **Reasons are grouped by kind, and each kind names three cases at most**, then counts the rest:
  `... and 97 more of the same kind`. Fix those three and run again: the next ones show.
- **`--register` and a draw give the same reasons.** So does `--analyze` with the same options,
  except for a malformed file or section, or a theme with no noun: the analysis then lists the
  errors of the file alone, and measures the size rules only once they are fixed.

Two things are deliberately not reported. Broken JSON stops everything: nothing can be read from a
document that did not parse, so that is the only reason you get. And when a section that the rules
read is itself malformed, the rules skip it — `"nouns" must be an array of { value, categories }.`
already says it all.

Every message, with what causes it and how to fix it, is in the
[error catalogue](theme-reference.md#error-catalogue).

## Checking what a theme means

`--analyze` and `--register` check structure: the size of the pools, the lengths, the categories
that exist. Nothing in them knows that an orchid is not fragile, or that a polite English word
sounds wrong next to a given noun. That part can only be judged by reading what the theme actually
draws, and [reviewing-a-theme.md](reviewing-a-theme.md) describes how.

## Where to find the built-in themes

The three themes compiled into `slugger` — `slugger`, `heroku` and `docker` — are ordinary theme
files in [src/Slugger/Infrastructure/Resources/](../src/Slugger/Infrastructure/Resources/). The
others the repository offers are in [themes/](../themes/), with what it takes to propose a new one
in [themes/README.md](../themes/README.md). All of them are worth reading as examples:
`heroku.json` for participles classified by capability, `docker.json` for `except`, `jazz.json`
for `incompatible`.

Why these choices: [the decision records](idr/), in French.
