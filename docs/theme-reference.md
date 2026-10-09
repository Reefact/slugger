# Theme file reference

This page is for looking things up while you write a theme: every key a theme file can hold, every
message `slugger` can answer with and every line of the analysis report. The guide that explains
them in order, with the reasons behind them, is [writing-a-theme.md](writing-a-theme.md). The
examples use [examples/spices.json](examples/spices.json), the complete version of the theme that
the guide builds.

- [Schema](#schema)
- [Error catalogue](#error-catalogue)
- [The analysis report, annotated](#the-analysis-report-annotated)

## Schema

Each key links to the section of the guide that explains it.

| Key | Type | Required | Default | Checked | Not checked |
| --- | --- | --- | --- | --- | --- |
| [`adjectives`](writing-a-theme.md#categories-which-epithet-for-which-noun) | object: category → list of strings | yes | — | an object; each list holds strings with a letter or digit | empty lists, duplicates |
| [`participles`](writing-a-theme.md#participles) | same as `adjectives` | no | none | same as `adjectives` | — |
| [`nouns`](writing-a-theme.md#your-first-theme) | list of objects | yes | — | a list of objects; at least one noun, even with `allowSmall` | duplicate values (see [the analysis](#the-analysis-report-annotated)) |
| [`nouns[].value`](writing-a-theme.md#what-happens-to-your-values) | string | yes | — | non-empty, with a letter or digit | — |
| [`nouns[].categories`](writing-a-theme.md#categories-which-epithet-for-which-noun) | list of strings | no | `[]` (only `common`) | a list of strings; each category exists in `adjectives` or `participles` | case (compared exactly) |
| [`nouns[].except`](writing-a-theme.md#ruling-out-a-word-for-one-noun-except) | list of strings | no | `[]` | each word is declared as an adjective or a participle | — |
| [`incompatible`](writing-a-theme.md#ruling-out-a-participle-beside-an-adjective-incompatible) | object: adjective → list of participles | no | none | the key is a declared adjective, each value a declared participle | whether the pair can ever be drawn (a remark) |
| [`maxLength`](writing-a-theme.md#promising-a-length-maxlength) | object | no | no promise | an object | — |
| `maxLength.twoWords`, `maxLength.threeWords` | whole number | no | no promise | above zero; the theme can keep the promise | — |
| [`defaults`](writing-a-theme.md#defaults-the-theme-style) | object | no | program defaults | an object | — |
| `defaults.sep` | string | no | `-` | exactly one character | — |
| `defaults.wordSep` | string | no | the separator | one character or `""` | — |
| `defaults.casing` | string | no | `kebab` | one of `kebab`, `snake`, `camel`, in any case | a number written as a string |
| [`defaults.segmentMode`](writing-a-theme.md#what-the-per-noun-floor-counts) | string | no | `both` | one of `adjective`, `participle`, `either`, `both`, `threeOrTwo`, in any case; participles exist for `participle` and `either` | a number written as a string |
| [`defaults.maxSegmentWords`](writing-a-theme.md#capping-the-words-of-a-term-maxsegmentwords) | whole number | no | no cap | a whole number | `0` or less (crashes) |
| `defaults.tokenLength` | whole number | no | `0` | a whole number | the range (negative means no token) |
| `defaults.tokenChance` | whole number | no | `100` | a whole number | the range (above 100 means always) |
| `defaults.foldAccents`, `ascii`, `tokenHex`, `tokenGlued` | `true` or `false` | no | `false` | a boolean | — |
| [`allowSmall`](writing-a-theme.md#why-these-numbers-and-how-to-lift-them) | `true` or `false` | no | `false` | a boolean | — |
| [`meta`](writing-a-theme.md#meta-describing-the-theme) | object | no | none | an object | unknown keys |
| `meta.title`, `description`, `version`, `author`, `source`, `createdAt`, `publishedAt` | string | no | none | a string | the content (versions and dates are free text) |
| *any other key* | — | — | — | — | **ignored silently**, at every level |

Values in `adjectives`, `participles`, `nouns[].value`, `except` and `incompatible` are
[cleaned](writing-a-theme.md#what-happens-to-your-values) before they are compared. Category names
and the keys themselves are not.

## Error catalogue

Each entry shows a change to [the complete spices file](examples/spices.json), unless it says
otherwise, then what `slugger` answers and how to fix it. Only the line that matters is shown; the
messages are the real ones, as `--register` or a draw prints them.

Entries marked *coherence error* are the checks that `allowSmall` never lifts
([the list](writing-a-theme.md#coherence-errors)). While a file has one, `--analyze` reports the
coherence errors alone, with no margins: fix them first, then analyse again. In messages that
locate a noun as `nouns[i]`, the index counts from 0; after an entry made only of punctuation, the
indexes of the entries that follow are one too low — a known fault.

**A category that does not exist** *(coherence error)*. `{ "value": "turmeric", "categories": ["hott"] }`

```text
- "turmeric" references category "hott", which the theme does not declare (it declares common, hot, leaf, seed).
- Category "hott" totals 2,424 combinations, but every category needs at least 40,000.
```

Fix the spelling, or declare the category. `--register` and a draw print both lines: the unknown
category is measured too. `--analyze` prints only the first. Category names are compared exactly,
so `"Hot"` gets the same two lines.

**A misspelt `except` word** *(coherence error)*. `"except": ["saffon"]`

```text
- "saffron" excludes "saffon", which the theme declares nowhere.
```

Fix the spelling. An exclusion must name a word the theme declares as an adjective or a participle.

**An `incompatible` pair written the wrong way round** *(coherence error)*.
`"incompatible": { "burning": ["gentle"] }`

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

**A noun starved by `except`.** `"except": ["saffron", "warm"]` on `saffron`, which reached exactly
100 adjectives:

```text
- "saffron" reaches 99 adjectives, but every noun needs at least 100.
```

Declare more adjectives that the noun reaches, or exclude fewer.

**A broken length promise** *(coherence error)*. `"maxLength": { "twoWords": 30 }`

```text
- maxLength.twoWords promises 30 characters, but the theme can produce "tongue_numbing_piment_d_espelette" at 33.
```

Shorten or remove the words in the quoted slug, or raise the promise.

**`common` written on a noun.** `{ "value": "turmeric", "categories": ["common"] }`

```text
- Category "common" totals 2,424 combinations, but every category needs at least 40,000.
```

Remove it: every noun reaches `common` already.

**A thin category.** In the example of
[the per-category floor](writing-a-theme.md#the-per-category-floor), with ten nouns that name
`hot`, each worth 120 × 24:

```text
- Category "hot" totals 28,800 combinations, but every category needs at least 40,000.
```

Give the category more nouns or more words, or merge it into another.

**A value with no letter or digit** *(coherence error)*. `"-"` in a list, or `{ "value": "!!!" }`
as the first noun:

```text
- "adjectives.common" must be an array of words, each holding a letter or a digit.
- nouns[0]: "!!!" holds no letter or digit.
```

Remove the entry. The first message does not say which entry it is: look for a value made of
punctuation only.

**A separator of two characters** *(coherence error)*. `"defaults": { "sep": "__" }`

```text
- "defaults.sep" must be a single character.
```

`sep` is exactly one character. `wordSep` accepts one character or `""`.

**A value of the wrong type** *(coherence error)*. `"allowSmall": "yes"`,
`"meta": { "version": 1.0 }`, `"categories": "hot"` on `chilli`:

```text
- "allowSmall" must be true or false.
- "meta.version" must be a string.
- nouns[20]: "categories" is not an array.
```

Write `true` or `false` without quotes and quote every `meta` value. A noun's `categories` is a list
even for one category: `["hot"]`. `nouns[20]` is the 21st entry, `chilli`.

**A trailing comma, or any other broken JSON** *(coherence error)*. In a small file whose second
line is `"adjectives": { "common": ["warm", "smoky",] },`:

```text
- The file is not valid JSON at line 2: The JSON array contains a trailing comma at the end which is not supported in this mode. Change the reader options. LineNumber: 1 | BytePositionInLine: 45.
```

Remove the comma after the last element of the list or object. Trust the first line number; the
`LineNumber` at the end counts from zero, and "Change the reader options" is not addressed to you.
Comments (`//`) are not allowed either.

**A missing or misspelt required key** *(coherence error)*. `"nons": […]` instead of `"nouns"`:

```text
- "nouns" must be an array of { value, categories }.
```

A missing `adjectives` gives `"adjectives" must be an object of category to words.`

**A mode that needs participles, in a theme without any** *(coherence error)*.
`"defaults": { "segmentMode": "either" }`, with `participles` and `incompatible` removed:

```text
- defaults.segmentMode asks for "either", but the theme declares no participle anywhere.
```

Declare participles, or drop `segmentMode`: a theme without participles draws one adjective anyway.

### Accepted without a word

These are not refused, which is exactly why they are dangerous:

- **Unknown keys are ignored silently**, at every level: the root, a noun, `defaults`, `maxLength`,
  `meta`. A misspelt `"excpet"` drops the exclusion, `"maxlength"` drops the promise,
  `"participels"` drops every participle (and usually causes other refusals), `"casng"` drops the
  casing and `"categorie"` leaves the noun with `common` alone. Compare the spelling of every key
  with the [schema](#schema).
- **`tokenChance` and `tokenLength` are not range-checked in a file**, although the command line
  checks its own options: `"tokenChance": 150` behaves like 100, and a negative `tokenLength` like
  0.
- **A `segmentMode` or `casing` written as a number** (`"7"`) is accepted and leads to undefined
  behaviour. Write the names as in the schema. Case does not matter there: `"Either"` works.
- **A noun declared twice** (`"cumin"` and `"Cumin"`) is accepted. It is drawn twice as often as its
  neighbours, and only the analysis report points it out.
- **`"maxSegmentWords": 0`** is not refused: it crashes the program with an unhandled exception.
  Use 1 or more, or leave the key out.

## The analysis report, annotated

`--analyze` prints a verdict and writes the details next to the file. Here is the report for
[the complete spices file](examples/spices.json) after four slips: `glowing` appended to the
`common` adjectives although it is also a participle, a category `"bark": ["corky", "furrowed"]`
that no noun names, `"leafy": ["burning"]` added to `incompatible` although no noun reaches both
words, and a second `{ "value": "Cumin", "categories": ["seed"] }` at the end of `nouns`.

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
  terminal. When the reasons include a [coherence error](writing-a-theme.md#coherence-errors), only
  the coherence errors are listed and the report stops there: *The document could not be read, so
  there is nothing to measure.*
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
  - *Characters*: the longest slug the theme can produce in its own mode and format, next to the
    `maxLength` promise for that mode (or `--max-length`). A dash means nothing was promised.
  - *Combinations per category*: rule 4, with the poorest category. Absent when no noun names a
    category.
  - The sentence underneath says which mode the floors follow, and why.
- **Duplicates** — nouns whose cleaned value appears more than once. Not a refusal, but each copy
  makes the noun more likely to be drawn. Delete the copies.
- **Categories nothing carries** — categories declared in `adjectives` or `participles` that no noun
  names: their words are never drawn. Usually a typo in a noun's `categories`.
- **Exposure** — for each adjective, how many nouns can reach it; the report gives the least and the
  most reached. `ridged` reaches only the 22 nouns that name `seed`, `warm` reaches all of them. A
  large spread is not a fault — a narrow category is decorative — but it tells you which words are
  rare. Adjectives no noun reaches are left out, and `except` is not taken into account.
- **Shape of the slug** — how many adjectives and nouns are multi-word terms, and an upper bound on
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

Five things to know about `--analyze`:

- **A coherence error hides the margins.** While the file has one, the report lists the coherence
  errors and measures nothing else — not even the size rules that `--register` would report next to
  them. Fix the coherence errors, then run `--analyze` again to see the margins.
- **It exits with code 0 even when the theme would be refused** — the analysis itself succeeded.
  Read the verdict, not the exit code. It writes a report even for a file that does not exist or
  does not parse.
- **It overwrites** an existing `spices-analysis.md` without asking.
- **It ignores `--allow-small-theme`**: the margins always use the real floors. A theme that
  declares `"allowSmall": true` is reported as accepted, with its negative margins in bold.
- **The report does not record the options it was run with.** `--segment`, `--max-length` and
  `--max-segment-words` change what is measured, and so do your saved defaults (`--init`), but the
  report never lists them. Note the command next to the report if you keep it.
