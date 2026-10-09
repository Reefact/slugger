# Vocabulary

One word used to mean two things: "word" named both what is drawn from a theme and the literal unit
it is made of. This page fixes one word per level, and only one. The documentation, the messages and
the code all aim to use these words in exactly this sense.

> A **slug** is a **noun**, preceded by an optional **epithet** and followed by an optional
> **token**. The noun and the epithet are made of **terms** drawn from the vocabulary of a
> **theme**; a term holds one or more **words**. Once rendered, the slug is cut into **segments**.

## Words you will come across on the command line

The words below appear in messages, options and other pages. The terms they rest on — slug, noun,
epithet, term — are defined in [the next section](#the-words).

| Word | Meaning |
| --- | --- |
| category | A free label shared by nouns and epithets. An epithet is drawn only for a noun that shares one of its categories |
| `common` | The category every noun reaches on top of its own |
| pool | The epithets one noun can actually reach, once categories, `except`, `incompatible` and any length or word cap have been applied |
| floor | A minimum a theme must reach to be accepted: 100 nouns, 100 words before each noun, 20 participles per noun under `both`, 40,000 combinations per category |
| segment mode | What precedes the noun, chosen by `--segment` or `defaults.segmentMode`. Despite the name, it chooses the epithet, not a segment |
| `either` | One word before the noun, an adjective or a participle, drawn from both lists together |
| `both` | An adjective, then a participle. The default |
| `threeOrTwo` | Like `both`, except that the participle is sometimes left out |
| fold | Remove the accents that decompose (`é` → `e`), with `--fold-accents`. `ø`, `ß` and non-Latin letters are kept; `--ascii` is the option that guarantees ASCII |
| theme style | A theme's own `defaults` — separator, casing, token, segment mode… — applied when that theme is drawn alone |
| mimic style | Whether theme styles apply: `--mimic-style` keeps them even when several themes are drawn, `--mimic-style false` turns them off even for one |
| interactive loop | What `slugger` does when standard input and standard output are both a terminal and `--oneshot` is absent: it draws a round, draws another each time you press Enter and stops on Ctrl+D |
| one-shot | `--oneshot`: draw once and exit. What scripts should always pass |
| register | `--register`: validate a theme file and copy it into the theme directory, so that `--theme` finds it by name |
| shadow | A registered theme with the same name as a built-in one replaces it; `--register` warns when it happens |

## The words

### slug

What a draw produces: a **noun**, an optional **epithet** in front of it, an optional **token**
behind it. It is the unit delivered — what becomes a container name, a git branch, a subdomain.

A slug is not a string: it becomes one at **rendering**, which chooses the separator, the casing and
the folding of accents. The same slug rendered twice in two different ways gives two strings.

### term

The generic name for a **noun**, an **adjective** or a **participle** — what is drawn from a theme's
vocabulary. It is the unit of the draw: a term is drawn whole, never in part.

A term holds one or more words. `cobaltite` holds one, `sharp faced` two, and each of them is
**a single term**.

### word

A word in the literal sense. In a theme file, the words of a term are separated by a space —
`"value": "rock crystal"` declares a term of two words — or by any other character that is neither
a letter nor a digit, which loading turns into the same boundary.

### noun

The central term. Always present, always last.

### epithet

The words that describe the noun, in front of it: in `toasted_waking_clove`, the epithet is
`toasted waking`. In full: **what qualifies the noun**, whole — an adjective, a participle or both.
It therefore holds one or two terms, never zero: a slug without one does not have an empty
epithet, it has none.

> **Why this word.** In French grammar, *épithète* is the function of a word attached directly to
> a noun — English grammar says attributive. It names the role the adjective and the participle
> share, where *prefix* would only have named their place. Grammar gives the function to one word;
> here it covers both when there are two. A plural would have been more exact, but reads badly.

### adjective

An epithet drawn from the theme's list of adjectives. Which adjectives a noun can reach depends on
that noun alone — its categories ([DEC0001](idr/DEC0001-restriction-des-adjectifs-par-categorie.md))
and its exclusions ([DEC0011](idr/DEC0011-exclusion-de-mots-par-nom.md)) — and never on another
term of the slug.

### participle

A verb form used like an adjective: `rising`, `frozen`, `whispering`. In a slug, a participle is an
epithet drawn from the theme's list of participles. Its pool depends on the noun **and** on the
adjective already drawn, which can refuse some participles
([DEC0017](idr/DEC0017-refus-d-un-participe-a-cote-d-un-adjectif.md)) — and under `threeOrTwo`,
drawing no participle at all is itself one of the outcomes
([DEC0020](idr/DEC0020-absence-de-participe-tiree-comme-un-participe-de-plus.md)).

That is the whole difference between the two, and it lies in the **draw**, not in the slug. Once
drawn, an adjective and a participle are two terms in front of the noun, and
`dazzling-flaring-olivine` does not say which is which.

### token

The characters at the end, drawn at random rather than from the vocabulary. Neither a term nor a
word: it does not come from the theme.

### mould

What a token is drawn from: the alphabet its characters are taken from, and how many characters it
has. The mould says **what a token looks like** — four hexadecimal characters — and nothing else.
Whether a token appears at all is not the mould's concern: that is the slug's question, and a
**chance** answers it.

### chance

Out of a hundred slugs, how many get a token. A whole number: a chance counts in hundredths, nothing
finer.

### theme

The vocabulary terms are drawn from: one JSON file.

### segment

What sits between two separator characters in the **rendered** slug. The first segment is followed
by a separator, the last one is preceded by one.

A segment is a property of **rendering**, not of the slug: for the same draw, the number of segments
changes with the options. You say "this slug, rendered in camel case, has one segment" — never
"this slug has one segment".

## The three counts, on an example

The adjective `sharp faced` and the noun `cobaltite`, drawn from `mineralogy`. The draw does not
change; only the rendering does.

| Rendering | Segments | Terms | Words |
| --- | --- | --- | --- |
| `sharpfaced-cobaltite` (`--word-sep ""`) | 2 | 2 | 3 |
| `sharp-faced-cobaltite` (default) | 3 | 2 | 3 |
| `sharp_faced-cobaltite` (`--word-sep _`) | 3 | 2 | 3 |
| `sharpFacedCobaltite` (`--casing camel`) | 1 | 2 | 3 |

The Terms and Words columns are facts of the draw: they do not change. The Segments column is a
fact of the rendering, and it takes three values for the same slug.

That is the reason for the distinction: **terms and words belong to the draw, segments to the
rendering.** Anything decided before the draw — a cap, a floor, a length promise — is counted in
terms and words, never in segments.

It is also why a segment cannot lead you back to the terms: `sharp-faced-cobaltite` is written the
same whether `sharp faced` + `cobaltite`, `sharp` + `faced cobaltite` or a single term was drawn.
Rendering loses the structure; `--word-sep` is there to make it readable again.

## What does not belong to this vocabulary

Whatever receives the slug has its own vocabulary, and it should not be borrowed:

| | Its unit |
| --- | --- |
| DNS (RFC 1035) | **label** — a whole slug *is* a label, hence the 63 characters `docker` promises. A label is limited to 63 octets, and `maxLength` counts characters, so an accented slug takes more octets than its length says |
| URI (RFC 3986) | path **segment**, delimited by `/` |
| git ref | **component**, delimited by `/` |

RFC 3986's *segment* is indeed the ancestor of ours, but it is defined by its delimiter and nothing
else. Ours describes a rendering, never a draw.

## Where each word lives in the code

Every type below is in the `Slugger.Domain` namespace. Several of them are new and not yet used by
the generator — see [refactoring-in-progress.md](refactoring-in-progress.md).

| Word | Type | Note |
| --- | --- | --- |
| slug | `Slug` | |
| term | `Term` | |
| word | `Word` | |
| noun | `Noun` | Wraps a `Term`. `NounEntry` is something else: an entry of a theme file's noun list, with its categories and exclusions |
| epithet | `Epithet` | |
| adjective | `Adjective` | Wraps a `Term` |
| participle | `Participle` | Wraps a `Term` |
| token | `Token` | |
| mould | `TokenMould` | Made of a `TokenAlphabet` and a `TokenLength` |
| chance | `Chance` | |
| theme | `Theme`, `ThemeName` | `Theme` is the entity and `ThemeName` its identity. `ThemeDocument` is the file as loaded, which every `Themes.Load*` method returns |
| category | `Category` | |
| segment | — | No type: a segment exists only in the rendered string, which `SlugFormatter` (in `Slugger.Domain.Generation`) produces |

`Noun`, `Adjective` and `Participle` each wrap a `Term` rather than derive from it, so that the
compiler refuses an adjective where a participle is expected even when the two hold the same term.

## Where the code does not follow yet

This page is the reference; the code came before it and did not wait for it. Three names today say
"word" for a term, or "segment" for a term or an epithet:

- `MaxLength.TwoWords` / `ThreeWords` — and the `twoWords` / `threeWords` keys of `maxLength` —
  count **terms**
- `--max-segment-words` caps the **words of a term**
- `SegmentMode` chooses which **epithets** precede the noun

Renaming them touches a public option and published JSON keys: that is a decision, so it needs a
decision record, and it is not taken here.
