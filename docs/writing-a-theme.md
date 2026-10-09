# Writing a theme

A theme is a JSON file: a list of nouns, and the adjectives and participles that may stand in front
of them. No code and no recompiling — you point `slugger` at the file and draw.

This page takes you from a first file to a theme you can share: how to try it while you write it,
what each key does, the size rules every theme must clear and what each refusal means. Checking
that the pairs a theme draws actually make sense is a separate job, which no rule can do for you:
see [reviewing-a-theme.md](reviewing-a-theme.md). The words used here — term, epithet, pool,
floor — are defined in [ubiquitous-language.md](ubiquitous-language.md).

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
- [`maxLength`](#promising-a-length-maxlength), [`maxSegmentWords`](#promising-short-segments-maxsegmentwords),
  [`defaults`](#defaults-the-theme-style) and [`meta`](#meta-describing-the-theme)
- [Reading a refusal](#reading-a-refusal) and the [error catalogue](#error-catalogue)
- [The analysis report, annotated](#the-analysis-report-annotated)
- [Schema reference](#schema-reference)
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
theme "spices" is accepted as it is.
analysis of "spices" written to ./spices-analysis.md
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
version of the same theme, with short lists. The outputs further down this page come from the
complete version — 101 common adjectives, three categories, 106 nouns — so their numbers will not
match this excerpt, which the size rules refuse as it stands:

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
  *whether*; the report answers *by how much*, and it works on a refused theme too — that is when
  it helps most. See [the report, annotated](#the-analysis-report-annotated).
- **`--theme-dir .` makes the current folder the theme directory**, so `--theme spices` finds
  `spices.json` without installing anything. `--theme` takes the name, never the path.
- **In a terminal, the draw stays open**: press Enter for 20 more slugs, Ctrl+D to stop. Add
  `--oneshot` to draw once and return. Leave `--seed` out while you read: with a seed, every round
  repeats the same slugs.
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
- **A file named after a built-in theme replaces it**, and `--register` says so:

```console
$ slugger --register ./docker.json
theme "docker" registered.
warning: "docker" now shadows the built-in theme of the same name.
```

`--unregister docker` then removes your file and brings the built-in theme back. A built-in theme
itself cannot be unregistered:

```console
$ slugger --unregister heroku
"heroku" is embedded in the binary, so there is nothing to unregister - leave it out of --theme not to use it.
```

### Theme names

A theme's name is its file name without `.json`. Nothing inside the file names it — not even
`meta.title`, which is only a label for people.

- **`--theme`, `--theme-info` and `--unregister` take a name; `--analyze` and `--register` take a
  path.** A path given to `--theme` is looked up as a name and not found:

  ```console
  $ slugger --theme-dir . --theme ./spices.json --oneshot
  Theme "./spices.json" was refused for 1 reason:

    - No theme named "./spices.json". Available: docker, heroku, slugger, spices.
  ```

- **Write the name exactly as the file is named.** On Linux, `--theme Spices` does not find
  `spices.json`; the built-in names are always lowercase.
- **Never put a comma in a file name.** `--theme` splits its value on commas to draw from several
  themes, so `a,b.json` registers and shows in `--list-themes` but can never be drawn:
  `--theme a,b` asks for a theme `a` and a theme `b`.
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

**`common` is a floor that every noun stands on, not a fallback**
([DEC0002](idr/DEC0002-common-atteint-par-tout-nom.md)). Every noun reaches it *in addition to*
the categories it names:

| The noun names | It reaches |
| --- | --- |
| *(nothing)* | `common` |
| `["hot"]` | `hot` + `common` |
| `["hot", "seed"]` | `hot` + `seed` + `common` |

So no category has to reach 100 adjectives on its own: what counts is the sum with `common`. The
built-in `slugger` theme lives on that — five categories of 45 adjectives each, and 60 in `common`.

**Never write `common` on a noun.** It adds nothing, since the noun already reaches `common`, and
it gets the theme refused. A category that nouns name must reach 40,000 combinations from those
nouns alone ([the per-category floor](#the-per-category-floor)); `common` escapes that rule only as
long as no noun names it. Here one noun of the spices theme does:

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
`--register` points it out without refusing anything:

```console
$ slugger --register ./spices.json
theme "spices" registered.
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
  exclusions, so a noun emptied by its own `except` gets the theme refused, by name.
- **The key is not checked.** A misspelt `"excpet"` is ignored silently, like every
  [unknown key](#schema-reference): the theme loads and the word is drawn.

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
3, whose threshold is low and will stay low until the shipped themes have grown.

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
the moment it is mixed — so if you declare participles, give every noun at least 20. Here, the
smallest accepted theme with five participles and `"segmentMode": "either"`:

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

where a noun that reaches no participle counts 1 rather than 0. This product is taken **whatever
the segment mode**, because `--segment both` can reach it from any theme. A category's total is
the sum of `combinations(noun)` over **the nouns that name that category** in their `categories`,
and it must reach 40,000. A noun that names two categories counts in both.

Two kinds of category are never measured: `common`, unless a noun writes it (and then it is
measured like any other, [as above](#categories-which-epithet-for-which-noun)); and a category that
no noun names — the analysis report lists those, since their words are never drawn.

**An example.** Take the smallest accepted theme, add a `hot` section of 20 adjectives and a
`common` section of 24 participles, and write `"categories": ["hot"]` on some nouns. Each of those
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

The other checks are never lifted: a theme with no noun at all, a category a noun names that does
not exist, an `except` or `incompatible` word that the theme does not declare, a segment mode that
needs participles the theme does not have, a broken `maxLength` promise and a malformed file are
refused whatever the switches say. `allowSmall` accepts a small theme, not an incoherent one.

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

A value of several words is therefore perfectly normal: `"Herbes de Provence"`, `"Ras el-Hanout"`.

## Promising a length: `maxLength`

A slug ends up somewhere, and that place has rules. **63 characters** is the one that matters most:
it is the limit of a DNS label, so of an S3 bucket, a Kubernetes Service, a subdomain. **30** if
the target is a Heroku app or a GCP project.

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

The length is measured as the theme's own `defaults` format the slug, token included, and it counts
characters, not bytes: `é` counts once here and twice in UTF-8.

`maxLength` sits at the **root** of the file, next to `allowSmall`, not inside `defaults`: the
`defaults` are switched off as soon as several themes are drawn together, and a promise that
disappears when a theme is added is no promise.

**What it costs you:** the day you add a word that breaks the promise, the theme is **refused at
load**, and the message shows you the longest slug it can now produce. That is the point — the
problem reaches you rather than the registry that rejects your image six months later.

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
floors, the run is refused and says why:

```console
$ slugger --theme-dir . --theme spices --max-length 30 --oneshot
Theme "spices" was refused for 32 reasons:

  - Under 30 characters, "cardamom" reaches 15 participles behind "hard shelled", but a theme drawing "both" needs at least 20 per noun for every adjective it can draw - raise the limit, shorten the words, or draw one word instead of two.
  - Under 30 characters, "coriander" reaches 7 participles behind "hard shelled", but a theme drawing "both" needs at least 20 per noun for every adjective it can draw - raise the limit, shorten the words, or draw one word instead of two.
  - Under 30 characters, "mustard" reaches 10 participles behind "tongue numbing", but a theme drawing "both" needs at least 20 per noun for every adjective it can draw - raise the limit, shorten the words, or draw one word instead of two.
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

## Promising short segments: `maxSegmentWords`

A value of several words reaches the slug in several pieces: every boundary becomes a separator.
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
noun too few adjectives, or a category too few combinations, gets the run refused and says by how
much. `--analyze` accepts the option, and `--allow-small-theme` lifts the floors for a trial. The
value is a whole number of at least 1; `0` in a file crashes the program rather than being refused.

The cap goes in `defaults`, unlike `maxLength`: it is a matter of style, not a safety promise. It is
therefore switched off when several themes are drawn together, like everything in `defaults`.

### Lifting a theme's cap for one run

A theme's `defaults` apply **without being asked for**: a theme that writes `"maxSegmentWords": 1`
draws one word per term every time, even when the command line says nothing about it — and
`--analyze` measures it under that cap the same way. A compound noun written for that theme
therefore never comes out, unless the run asks explicitly for the opposite.

`--max-segment-words none` is that explicit opposite
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

This block holds the look of the style your theme imitates, not anyone's session preferences:

| Key | Command-line option | Values | Program default |
| --- | --- | --- | --- |
| `sep` | `--sep` | exactly one character — Docker writes `_`, Heroku and slugger `-` | `-` |
| `wordSep` | `--word-sep` | one character, or `""` to glue the words of a value together | the separator |
| `casing` | `--casing` | `kebab`, `snake` or `camel` | `kebab` |
| `foldAccents` | `--fold-accents` | `true` or `false`; rarely a theme's business, see [above](#what-happens-to-your-values) | `false` |
| `ascii` | `--ascii` | `true` or `false`; rarely a theme's business either | `false` |
| `segmentMode` | `--segment` | `adjective`, `participle`, `either`, `both` or `threeOrTwo`; also decides the [floors](#the-size-rules) | `both` |
| `maxSegmentWords` | `--max-segment-words` | a whole number, at least 1 | no cap |
| `tokenLength` | `--token-length` | how many characters the token has; `0` for none | `0` |
| `tokenHex` | `--token-hex` | `true` for a hexadecimal token rather than decimal | `false` |
| `tokenChance` | `--token-chance` | out of 100 slugs, how many get a token | `100` |
| `tokenGlued` | `--token-glued` | `true` glues the token to the last word: `focused_turing3` | `false` |

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
Conventions for moving them on when a theme changes are in
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
no `meta`, and says so:

```console
$ slugger --theme-dir . --theme-info spices
theme "spices"
  (no metadata declared)
```

## Reading a refusal

A theme is never refused one reason at a time
([DEC0006](idr/DEC0006-rapport-groupe-des-refus.md)). Reading the file and applying the rules report
together, so one run tells you everything the file needs:

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
- **Reasons are grouped by kind, and each kind names three cases at most**, then counts the rest:
  `... and 97 more of the same kind`. Fix the three, run again, and the next ones show.
- The same theme gets the same reasons from `--register`, from a draw and from `--analyze`, as
  long as the options are the same.

Two things are deliberately not reported. Broken JSON stops everything: nothing can be read from a
document that did not parse, so that is the only reason you get. And when a section that the rules
read is itself malformed, the rules skip it — `"nouns" must be an array of { value, categories }.`
already says it all.

## Error catalogue

Each entry shows what was written, what `slugger` answers, and the fix. The messages are the real
ones; the subjects come from the spices theme.

**A category that does not exist.** `{ "value": "turmeric", "categories": ["hott"] }`

```text
- "turmeric" references category "hott", which the theme does not declare (it declares common, hot, leaf, seed).
- Category "hott" totals 2,424 combinations, but every category needs at least 40,000.
```

Fix the spelling, or declare the category. The second line follows from the first: the unknown
category is measured too. Category names are compared exactly, so `"Hot"` gets the same two lines.

**A misspelt `except` word.** `"except": ["saffon"]`

```text
- "saffron" excludes "saffon", which the theme declares nowhere.
```

Fix the spelling. An exclusion must name a word the theme declares as an adjective or a participle.

**An `incompatible` pair written the wrong way round.** `"incompatible": { "burning": ["gentle"] }`

```text
- incompatible names "burning" as an adjective, which the theme declares nowhere in "adjectives". It is declared as a participle, so the pair may be the wrong way round: the key refuses, the words are refused.
- incompatible["burning"] refuses "gentle", which the theme declares nowhere in "participles". It is declared as an adjective, so the pair may be the wrong way round: the key refuses, the words are refused.
```

Swap them: the key is the adjective, the list holds participles. A word declared nowhere at all
(`"gentle": ["burnin"]`) gets the same kind of message, without the hint about direction.

**A noun starved by `incompatible`.** `"sleepy": ["waking", "dancing", "roaming", "wandering", "rising"]`

```text
- "saffron" reaches 19 participles beside "sleepy", but a theme drawing "both" needs at least 20 per noun for every adjective it can draw - either declare more participles for it, or drop the incompatibility.
```

Declare more participles for those nouns, or refuse fewer.

**A noun starved by `except`.** `"except": ["saffron", "warm"]` on a noun that reached exactly 100
adjectives:

```text
- "saffron" reaches 99 adjectives, but every noun needs at least 100.
```

Declare more adjectives that the noun reaches, or exclude fewer.

**A broken length promise.** `"maxLength": { "twoWords": 30 }`

```text
- maxLength.twoWords promises 30 characters, but the theme can produce "tongue_numbing_piment_d_espelette" at 33.
```

Shorten or remove the words in the quoted slug, or raise the promise.

**`common` written on a noun.** `{ "value": "turmeric", "categories": ["common"] }`

```text
- Category "common" totals 2,424 combinations, but every category needs at least 40,000.
```

Remove it: every noun reaches `common` already.

**A thin category.** Ten nouns name `hot`, each worth 120 × 24:

```text
- Category "hot" totals 28,800 combinations, but every category needs at least 40,000.
```

Add nouns to the category, add adjectives or participles to it, or merge it into another. See
[the per-category floor](#the-per-category-floor) for the arithmetic.

**A value with no letter or digit.** `"-"` in a list, or `{ "value": "!!!" }` as a noun:

```text
- "adjectives.common" must be an array of words, each holding a letter or a digit.
- nouns[0]: "!!!" holds no letter or digit.
```

Remove the entry. The first message does not say which entry it is: look for a value made of
punctuation only.

**A separator of two characters.** `"defaults": { "sep": "__" }`

```text
- "defaults.sep" must be a single character.
```

`sep` is exactly one character. `wordSep` accepts one character or `""`.

**A value of the wrong type.** `"allowSmall": "yes"`, `"meta": { "version": 1.0 }`,
`"categories": "hot"`:

```text
- "allowSmall" must be true or false.
- "meta.version" must be a string.
- nouns[20]: "categories" is not an array.
```

Use `true` or `false` without quotes, quote every `meta` value, and write a list even for one
category: `["hot"]`.

**A trailing comma, or any other broken JSON.**

```text
- The file is not valid JSON at line 2: The JSON array contains a trailing comma at the end which is not supported in this mode. Change the reader options. LineNumber: 1 | BytePositionInLine: 45.
```

Remove the comma after the last element of the list or object. Trust the first line number; the
`LineNumber` at the end counts from zero, and "Change the reader options" is not addressed to you.
Comments (`//`) are not allowed either.

**A missing or misspelt required key.** `"nons": [...]` instead of `"nouns"`:

```text
- "nouns" must be an array of { value, categories }.
```

A missing `adjectives` gives `"adjectives" must be an object of category to words.`

**A mode that needs participles, in a theme without any.** `"defaults": { "segmentMode": "either" }`
and no `participles`:

```text
- defaults.segmentMode asks for "either", but the theme declares no participle anywhere.
```

Declare participles, or drop `segmentMode`: a theme without participles draws one adjective anyway.

### Accepted without a word

These are not refused, which is exactly why they are dangerous:

- **Unknown keys are ignored silently**, at every level: the root, a noun, `defaults`, `maxLength`,
  `meta`. A misspelt `"excpet"` drops the exclusion, `"maxlength"` drops the promise,
  `"participels"` drops every participle (and usually triggers other refusals), `"casng"` drops the
  casing, `"categorie"` leaves the noun in `common` alone. Compare the spelling of every key with
  the [schema reference](#schema-reference).
- **`tokenChance` and `tokenLength` are not range-checked in a file**, although the command line
  checks its own options: `"tokenChance": 150` behaves like 100, and a negative `tokenLength` like
  0.
- **A `segmentMode` or `casing` written as a number** (`"7"`) is accepted and leads to undefined
  behaviour. Write the names as in the table. Case does not matter there: `"Either"` works.
- **A noun declared twice** (`"cumin"` and `"Cumin"`) is accepted. It is drawn twice as often as its
  neighbours, and only the analysis report points it out.
- **`"maxSegmentWords": 0`** is not refused: it crashes the program with an unhandled exception.
  Use 1 or more, or leave the key out.

## The analysis report, annotated

`--analyze` prints a verdict and writes the details next to the file. Here is the report for the
spices theme after four slips: a second `"Cumin"`, an unused `bark` category, `glowing` added to the
adjectives although it is also a participle, and an `incompatible` pair `leafy` / `burning` that no
noun can draw.

```console
$ slugger --analyze ./spices.json
theme "spices" is accepted as it is, with 2 remarks in the report.
analysis of "spices" written to ./spices-analysis.md
```

```markdown
# spices — theme analysis

**Accepted.** It loads as it is.

## Margins

| Rule | Worst case | Floor | Margin |
| --- | --- | --- | --- |
| Distinct nouns | 106 | 100 | +6 |
| Adjectives per noun | 101 (`saffron`) | 100 | +1 |
| Participles per noun | 24 (`saffron`) | 20 | +4 |
| Participles beside an adjective | 23 (`saffron` beside `sleepy`) | 20 | +3 |
| Characters | 45 (`tongue_numbing_smouldering_piment_d_espelette`) | 63 | +18 |
| Combinations per category | 62,556 (`seed`) | 40,000 | +22,556 |

Floors follow `segmentMode: both`: an adjective and a participle are drawn in front of the noun, so each pool carries its own floor.

## Duplicates

**1 noun declared more than once.** The draw indexes the list while the size rule counts distinct values, so each of these is drawn more often than its neighbours:

- `cumin`

## Categories nothing carries

1 category declared but carried by no noun, so their words never draw. Deliberate if you are keeping words aside; a typo otherwise:

- `bark`

## Exposure

How many nouns can reach one adjective, from `ridged` at 22 to `warm` at 107 — a spread of 5×.

A narrow category is decorative rather than wrong; this only says which ones are.

## Shape of the slug

- 8 of 146 adjectives are written in more than one word
- 6 of 107 nouns are
- the longest slug this theme can produce carries 6 segments, token aside

`--segment either` draws one word before the noun instead of two, if that is long for where the slug goes.

## Combinations

319,490 distinct slugs with an adjective and a participle in front, which is what `--segment both` reaches.

Above 40,000, so a suffix is a style choice here rather than a collision defence.

## Worth a second look

- "glowing" declared as both an adjective and a participle; a draw that lands on the same word twice writes it once.
- "leafy / burning" never drawn together by any noun, so the pair changes nothing; deliberate if you are writing ahead, a typo otherwise.
```

Section by section:

- **The verdict.** *Accepted*, or *Refused* followed by the reasons, grouped by kind as in the
  terminal.
- **Margins** — one row per rule, each with the **worst case**: the noun (or category) closest to
  failing, its count, the floor and the margin. A negative margin is in bold and is a refusal.
  - *Distinct nouns*: rule 1. Duplicates are counted once. This row counts the nouns of the file,
    even when a word cap or `--max-length` leaves fewer to draw from: when the two disagree, trust
    the verdict above the table.
  - *Adjectives per noun*: rule 2 under `adjective`, `both` and `threeOrTwo`. Here `saffron` is the
    poorest, because its `except` removes one word. Under `either`, a *Words before the noun* row
    holds the floor instead, and this row shows a dash.
  - *Participles per noun*: rule 3, under `both` and `threeOrTwo` (rule 2 under `participle`).
    *The theme declares none* when there are no participles.
  - *Participles beside an adjective*: the same, after `incompatible` — `sleepy` refuses `waking`,
    so `saffron` keeps 23. Shown under `both` and `threeOrTwo` when the theme declares pairs or
    the run passes `--max-length`.
  - *Characters*: the longest slug the theme can produce in its own mode and format, against the
    `maxLength` promise for that mode (or `--max-length`). A dash means nothing was promised.
  - *Combinations per category*: rule 4, with the poorest category. Absent when no noun names a
    category.
  - The sentence underneath says which mode the floors follow, and why.
- **Duplicates** — nouns whose cleaned value appears more than once. Not a refusal, but each copy
  makes the noun more likely to be drawn. Delete the copies.
- **Categories nothing carries** — categories declared in `adjectives` or `participles` that no noun
  names: their words are never drawn. Usually a typo in a noun's `categories`.
- **Exposure** — for each adjective, how many nouns can reach it; the report gives the least and the
  most reached. `ridged` reaches only the 22 seed nouns, `warm` reaches all of them. A large spread
  is not a fault — a narrow category is decorative — but it tells you which words are rare.
  Adjectives no noun reaches are left out, and `except` is not taken into account.
- **Shape of the slug** — how many adjectives and nouns have several words, and an upper bound on
  the number of segments a slug can have (the longest adjective, participle and noun added
  together, even if they never meet).
- **Combinations** — how many distinct slugs the theme can produce with an adjective and a
  participle in front (`--segment both`), and, when its own mode is another one, how many it
  produces in that mode. Below 40,000, consider a token in `defaults`. For a theme without
  participles, the first figure simply counts adjective–noun pairs.
- **Worth a second look** — remarks that refuse nothing: a word in both sections, an
  `incompatible` pair that can never apply (no noun reaches both words, or the theme draws one word
  in front of the noun). `--register` prints the same remarks as warnings. Duplicates and unused
  categories are not counted as remarks in the terminal line.

Four things to know about `--analyze`:

- **It exits with code 0 even when the theme would be refused** — the analysis itself succeeded.
  Read the verdict, not the exit code. It writes a report even for a file that does not exist or
  does not parse.
- **It overwrites** an existing `spices-analysis.md` without asking.
- **It ignores `--allow-small-theme`**: the margins always use the real floors. A theme that
  declares `"allowSmall": true` is reported as accepted, with its negative margins in bold.
- **The report does not record the options it was run with.** `--segment`, `--max-length` and
  `--max-segment-words` change what is measured, and so do your saved defaults (`--init`), but the
  report never lists them. Note the command next to the report if you keep it.

## Schema reference

| Key | Type | Required | Default | Checked | Not checked |
| --- | --- | --- | --- | --- | --- |
| `adjectives` | object: category → list of strings | yes | — | an object; each list holds strings with a letter or digit | empty lists, duplicates |
| `participles` | same as `adjectives` | no | none | same as `adjectives` | — |
| `nouns` | list of objects | yes | — | a list of objects; at least one noun, even with `allowSmall` | duplicate values (see the analysis) |
| `nouns[].value` | string | yes | — | non-empty, with a letter or digit | — |
| `nouns[].categories` | list of strings | no | `[]` (only `common`) | a list of strings; each category exists in `adjectives` or `participles` | case (compared exactly) |
| `nouns[].except` | list of strings | no | `[]` | each word is declared as an adjective or a participle | — |
| `incompatible` | object: adjective → list of participles | no | none | the key is a declared adjective, each value a declared participle | whether the pair can ever be drawn (a remark) |
| `maxLength` | object | no | no promise | an object | — |
| `maxLength.twoWords`, `maxLength.threeWords` | whole number | no | no promise | above zero; the theme can keep the promise | — |
| `defaults` | object | no | program defaults | an object | — |
| `defaults.sep` | string | no | `-` | exactly one character | — |
| `defaults.wordSep` | string | no | the separator | one character or `""` | — |
| `defaults.casing` | string | no | `kebab` | one of `kebab`, `snake`, `camel`, any case | a number written as a string |
| `defaults.segmentMode` | string | no | `both` | one of `adjective`, `participle`, `either`, `both`, `threeOrTwo`, any case; participles exist for `participle` and `either` | a number written as a string |
| `defaults.maxSegmentWords` | whole number | no | no cap | a whole number | `0` or less (crashes) |
| `defaults.tokenLength` | whole number | no | `0` | a whole number | the range (negative means no token) |
| `defaults.tokenChance` | whole number | no | `100` | a whole number | the range (above 100 means always) |
| `defaults.foldAccents`, `ascii`, `tokenHex`, `tokenGlued` | `true` or `false` | no | `false` | a boolean | — |
| `allowSmall` | `true` or `false` | no | `false` | a boolean | — |
| `meta` | object | no | none | an object | unknown keys |
| `meta.title`, `description`, `version`, `author`, `source`, `createdAt`, `publishedAt` | string | no | none | a string | the content (versions and dates are free text) |
| *any other key* | — | — | — | — | **ignored silently**, at every level |

Values in `adjectives`, `participles`, `nouns[].value`, `except` and `incompatible` are
[cleaned](#what-happens-to-your-values) before they are compared. Category names and the keys
themselves are not.

## Checking what a theme means

`--analyze` and `--register` check structure: the size of the pools, the lengths, the categories
that exist. Nothing in them knows that an orchid is not fragile, or that a polite English word
sounds wrong next to a given noun. That part is read in what the theme actually draws, and
[reviewing-a-theme.md](reviewing-a-theme.md) describes how.

## Where to find the built-in themes

The three themes compiled into `slugger` — `slugger`, `heroku` and `docker` — are ordinary theme
files in [src/Slugger/Infrastructure/Resources/](../src/Slugger/Infrastructure/Resources/). The
others the repository offers are in [themes/](../themes/), with what it takes to propose a new one
in [themes/README.md](../themes/README.md). All of them are worth reading as examples:
`heroku.json` for participles classified by capability, `docker.json` for `except`, `jazz.json`
for `incompatible`.

Why these choices: [the decision records](idr/), in French.
