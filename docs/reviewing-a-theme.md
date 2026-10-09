# Reviewing a theme

`--analyze` and `--register` check a theme's structure: the size of its pools, the length of its
slugs, the categories it names and declares. Nothing in them knows that an orchid is not fragile,
or that a polite English word sounds wrong next to a particular noun. That part cannot be measured
when the theme loads; it can only be judged by reading what the theme actually draws.

This page is the protocol for that reading. It assumes a theme that already loads — see
[writing-a-theme.md](writing-a-theme.md) for getting there.

The protocol has three phases, in this order and no other: while the categories themselves are
still changing, an automatic comparison has nothing stable to compare with.

Every example below comes from [`jazz.json`](../themes/jazz.json), which is in the repository: each
one is a fault this protocol actually found, in this order. The file now holds the correction — that
is what you will read in it, not the fault.

## The seven families

You are not looking for "problems". An open-ended search produces tunnel vision: you find one family,
and every later pass looks only for that one. You look for **these seven**, by name, and you go back
through the list on every pass.

| | What it is | Seen on `jazz` |
| --- | --- | --- |
| 1 | **Category leak** — a pool too wide lets a word reach a noun from another family | `reed-lined` on a double bass, `gut-strung` on a saxophone: a single `instrument` category for five families of instruments |
| 2 | **Physical impossibility** — the word describes a property the noun does not have | `pentatonic` on drum brushes (no pitch), `felt-hammered` on a Hammond organ (no hammers), `droning` on a banjo (no sustain) |
| 3 | **Checkable claim, wrong subject** — the word is not colour but a fact | `self-taught` on Coleman Hawkins, who studied at Washburn College; `twelve-bar` on Epistrophy, which is 32 bars long |
| 4 | **Anachronism** — the word and the noun both exist, but not at the same time | `bebop-fueled` on Louis Armstrong, who publicly rejected bebop; `avant-garde` on Wes Montgomery |
| 5 | **Self-reference** — the adjective repeats the noun | `flatted` on "flatted fifth", `muted` on "mute", `blue` on "Blue Monk" |
| 6 | **The adjective–participle pair** — each right on its own, wrong together | `metronomic-drifting`, `hushed-hollering`, `staccato-sustaining`, `breathless-breathing`, `swung-swinging` |
| 7 | **Wrong register** — grammatical and possible, yet nobody would say it | `walking` (as in a walking bass line) on a trill or a mouthpiece |

Families 1, 2 and 4 are fixed with `categories` and `except`; so is family 3, but it is *found*
another way (see phase 2); family 5 with `except`; family 6 is the reason `incompatible` exists;
family 7 is the only one that needs reading aloud.

Family 7 is also the only one where you can be wrong **the other way**, by correcting what was
right: a field's slang is not everyday English. On `jazz`, `wailing` applied to a drummer
was first marked wrong — you wail with a voice or a breath, not with drums — before checking showed
that *"the band was really wailing"* means playing loud and well, on any instrument. When a phrase
sounds odd in a field you know poorly, check the usage before you narrow a pool. A false positive
here costs the theme a word and fixes nothing.

## Phase 1 — one slug at a time, until a clean pass

Draw about **10 times as many slugs as the theme has nouns**, in its default mode. For a theme of
about 200 nouns, such as `flowers`, that is 2,000 to 2,500 slugs:

```bash
slugger --theme-dir . --theme flowers --count 2500 --oneshot > pass-01.txt
```

**Read them one by one.** Not grouped by noun, not skimmed: one slug, one verdict, the next.
Grouping by noun makes you read the noun and skim the two words in front of it — which is exactly
what lets families 3, 5 and 6 through, since they only show in the whole triple. A sample of 2,000
slugs is read in blocks of one or two hundred; it is not quick, and it is the part of the protocol
that finds the most.

Fix after each pass, draw a new sample, start again. Three rules hold this loop together:

- **Restart from the list, not from the last find.** The pass after a discovery is the worst of all:
  you spend it looking for what you just found. Go back through the seven families in order.
- **A clean pass, or nothing.** The phase ends on a pass that finds **nothing** — not on a pass whose
  finds you have fixed. Until you have read a whole sample without noting anything, you are not
  converging, you are stopping.
- **Write down the yield of every pass.** A falling series says you are converging; a flat one says
  you are rereading the same thing. On `jazz`, ten passes: 38, 2, 29, 1, 3, 3, 11, 3, 3, then
  nothing — and the tenth pass closed the phase, not the sixth, whose three finds had only been
  fixed. A peak in the middle of a descent means a family has just been looked at for the first
  time: the 29 was the adjective–participle pair, the 11 was the `deep-cut` category compared with
  the titles it reached.

This loop is what shapes the taxonomy — which categories exist, and how fine they are. Not the
other way round. A category is not created because it would look tidy, but because a word was
reaching a noun it could not describe.

## Phase 2 — the truth table, on every axis

Build a table **independent of the file**: for each noun, which traits are really true of it,
according to what you know of the subject — never according to what the JSON already says. Then
compare it with `categories`. A **false positive** — a label that lets in a word that should not
apply — is the serious bug, and is fixed as soon as you are confident. A **false negative** — a real
trait left undeclared — only narrows a pool: note it, and do not force it when the case is doubtful.

Two requirements make this phase pay off:

**Cover every kind of noun, not the one that already paid off.** On `jazz`, the table was first built
for the theme's 48 instruments — and declared finished. Its 67 musicians were only audited on the
next pass, and that is where `scatting` turned up on Count Basie, a pianist, because the vocal pool
reached everybody.

**Put every word of every pool through the fact test.** The question is: *could someone contradict
me with a source?* "Tormented" — no, it is a reading, and a reader who disagrees has no source to
prove it wrong.
"Self-taught" — yes, there is a biography. Any word that passes this test is a **claim**, and a
claim must be true of **every** noun it reaches. So it is audited noun by noun, exhaustively, never
by sampling: a word that is wrong on three nouns out of a hundred will hardly ever come out of a
draw, and will come out in front of the first reader who knows the subject. This test, applied
late, is what found the last two faults in `jazz`.

This pass covers all the nouns, including those a draw would rarely bring up. That is what makes it
complementary to the sample reviews rather than redundant with them.

## Phase 3 — a check the fix cannot satisfy

**Never check a fix by searching for the pattern you just fixed.** That only proves the fix, and by
construction it finds nothing else. You spent phase 1 discovering that you do not know in advance
what you are looking for: do not go back to a check that assumes you do.

Instead, take every slug apart into **adjective + participle + noun**, using the theme file's own
lists, and check that each part is in a category its noun reaches, is not in that noun's `except`
and does not form a pair that `incompatible` refuses. The filter does not depend on what you just
fixed: it holds for everything the file declares, including what you will declare later. A sample
of 10,000 slugs goes through it in a second, and covers what no reading covers.

**This check is a script you write.** `slugger` has no command for it. The repository has its own
version, in C#: [`SlugDecomposer`](../tests/Slugger.UnitTests/SlugDecomposer.cs), which
[`DrawnSlugTests`](../tests/Slugger.UnitTests/DrawnSlugTests.cs) runs on a couple of hundred draws of
every built-in theme and every theme in `themes/` whenever the test suite runs. That test is a safety net for the
repository, not a substitute for your own run on a large sample.

**Keep it exact: a prefix is not a word.** On `jazz`, a first version flagged
`blue-note-returning-blue-train` as breaking the exclusion of `blue` — the adjective drawn was
`blue note`, and on *Blue Train*, Coltrane's only album as a leader for **Blue Note**, it is even
right. Cutting a slug at its separators cannot tell `blue note` from `blue` + `note`. Ask `slugger`
to mark the term boundaries instead: with `--sep '|' --word-sep ' '`, every term is one segment and
its own words stay separated by spaces, so `lyrical|wavering|blue note` splits without guessing.
`--casing kebab` keeps a theme that writes camel case from gluing everything together, and
`--token-length 0` keeps a theme's token off the end:

```bash
slugger --theme-dir . --theme jazz --count 10000 --oneshot \
  --sep '|' --word-sep ' ' --casing kebab --token-length 0 > sample.txt
python3 check_slugs.py jazz.json < sample.txt
```

`check_slugs.py` can be as short as this. It cleans the file's values the way `slugger` does
(lowercase, anything that is not a letter or a digit becomes a space), rebuilds each noun's pools
from its categories plus `common` minus its `except`, and prints every slug that no decomposition
allows.
It prints nothing when every slug passes.

<details>
<summary><code>check_slugs.py</code></summary>

```python
import json, sys

def clean(value):
    out, boundary = [], False
    for ch in value:
        if ch.isalnum():
            out.append(ch.lower()); boundary = False
        elif not boundary:
            out.append(" "); boundary = True
    return "".join(out).strip()

theme = json.load(open(sys.argv[1], encoding="utf-8"))
sections = {s: {c: {clean(w) for w in ws} for c, ws in theme.get(s, {}).items()}
            for s in ("adjectives", "participles")}
nouns = {}
for n in theme["nouns"]:
    nouns.setdefault(clean(n["value"]), []).append(n)
refused = {clean(a): {clean(p) for p in ps} for a, ps in theme.get("incompatible", {}).items()}

def reach(section, noun):
    words = set()
    for c in noun.get("categories", []) + ["common"]:
        words |= sections[section].get(c, set())
    return words - {clean(w) for w in noun.get("except", [])}

def fault(terms):
    *before, value = terms
    if value not in nouns:
        return f"{value!r} is not a noun of the theme"
    for noun in nouns[value]:
        adjectives, participles = reach("adjectives", noun), reach("participles", noun)
        if len(before) == 2:
            adjective, participle = before
            if adjective in adjectives and participle in participles - refused.get(adjective, set()):
                return None
        elif len(before) == 1:
            if before[0] in adjectives | participles:
                return None
        else:
            return None
    return f"{' + '.join(before)} cannot stand in front of {value!r}"

for line in sys.stdin:
    slug = line.strip()
    if slug and (problem := fault(slug.split("|"))):
        print(f"{slug}: {problem}")
```

</details>

It needs Python 3.8 or later. If the theme's own `defaults` fold accents or force ASCII, the drawn
words no longer match the file: draw with `--mimic-style false` and pass the theme's segment mode
(`--segment`) and word cap (`--max-segment-words`) yourself.

Then read one last fresh sample, by hand, in **every mode the theme explicitly promises**: its
default mode; `--max-segment-words none` if the theme caps the words per term but still holds
multi-word terms; `--segment either` if the theme still makes sense that way. Count **25 to 30 times
the number of nouns per mode** — on `flowers`, around 10,000 slugs in all. Its job is to confirm
that nothing broke. If it still finds something of substance, the taxonomy was not stable: back to
phase 1.

## What makes the protocol fail

Four ways of believing you followed it, all seen on `jazz`:

- **Declaring convergence without a clean pass.** A pass that finds and fixes is not a pass that
  converges. You stop on a clean pass, not on a fixed one.
- **Narrowing the field to what already paid off.** After the instruments, the next passes read only
  instruments — the musicians, the titles and the technical terms stayed untouched for three more
  passes.
- **Taking a `grep` for a check.** Searching for the pattern you fixed, finding it gone and calling
  that phase 3.
- **Treating a claim as colour.** The whole pool reads like poetry, so the checkable word hidden in
  it reads like poetry too — until it meets the reader who knows the subject.

None of the samples, nor the phase 2 truth table, belongs in the repository: they are as disposable
as the report `--analyze` writes next to the theme. Only the corrected theme stays.
