# The vocabulary refactoring, where it stands

**This page is temporary.** It exists because the work below spans more branches than one sitting,
and it goes away when the last item of *What is left* is done. It is not a decision record: what
gets settled along the way belongs in `docs/idr/`, and the words themselves belong in
`docs/ubiquitous-language.md`.

Branch: `claude/marvelous-waiting-beyond-turret`. Green at 609 tests, build clean under
`GITHUB_ACTIONS=true`, `sh .claude/hooks/coding-rules.sh --all` silent, and the CLI runs —
`--theme docker --count 3`, `--list-themes` and the `--theme '*'` branch draw all answer.

## Why it started

`MaxLength(int? TwoWords, int? ThreeWords)` could not be read. `For(1)` gives `TwoWords` and
`For(2)` gives `ThreeWords`, because one counts the words in the slug and the other counts the words
before the noun - two conventions in one type, and no name saying which is which. That is a
modelling problem rather than a naming one, and it was not the only place the code counted "words"
without saying whose.

So the vocabulary was settled first, in `docs/ubiquitous-language.md`, before anything was written:

> Un **slug** est un **nom**, précédé d'une **épithète** optionnelle et suivi d'un **jeton**
> optionnel.

Twelve words, one per level: slug, terme, mot, nom, épithète, adjectif, participe, jeton, moule,
chance, thème, segment. Each of the types below is one of them.

## What is built

**The value objects**, each a `[ValueObject]` deriving from `Value`'s `ValueType<T>`, with a
`From` that reports and a `FromOrThrow` that raises, its own `<Concept>Error` / `<Concept>Exception`
pair beside it, and a `Dehydrate()` that is the one door out:

`Word`, `Term`, `Category`, `Chance`, `Token`, `TokenAlphabet`, `TokenLength`, `TokenMould`,
`Epithet`, `Slug`, `ThemeName`. Plus three `[SemanticObject]`s - `Noun`, `Adjective`, `Participle` -
which wrap a `Term` and hand it back through `Value`, so the compiler refuses
`ParticiplePool(noun, participle)` where two terms would have passed for one another.

**The theme as an entity.** `Theme` carries `[Entity]`: it is the `ThemeName` it is known by, not
the words it holds, and its constructor is `internal` because a theme is read, refused where it
breaks a rule, and only then made. `ICatalog` is the `[Repository]` the domain reaches one through,
with the two doors a repository needs - `Get` reports, `GetOrThrow` raises.

**The rules measured rather than written down.** `tests/Slugger.ArchitectureTests/` holds
`ValueObjectRulesTests` (five rules, applied only to what carries the marker),
`EntityRulesTests` (three), `DehydrationTests` (Cecil on the call sites, not the signatures),
`LayeringTests`, `NamespaceDependencyTests` and `ClipboardDependencyTests`.

**The house rules the session produced** are all in `CLAUDE.md` - consecutive guards, a comment on
an `if` becoming a named predicate, one dot per line and where not to apply it, early return over
nesting, decomposing a compound condition, and *a rule lives in this file, never in the code*.
`.claude/hooks/coding-rules.sh` checks two of them per edit and `--all` sweeps the tree, because the
per-edit hook never sees a file written by a heredoc.

## What the introduction has actually reached

**One overload deep.** `SlugFormatter.Format(Slug, GenerationOptions)` exists and is the single
named exception to the dehydration rule - writing a slug out *is* crossing the boundary, and
`SlugBudget` formats to measure, so the formatter cannot live outside the domain either.

Everything else still runs on strings. `SlugGenerator` draws strings, `ThemeResolver` resolves
pools of strings, and `ThemeDocument` (the old `Theme`, renamed) is what a catalog hands back. The
types above are built and pinned by tests; they are not yet what the engine is made of.

## What is left, in order

1. **Migrate `ThemeResolver`.** Start here rather than designing `Theme`'s surface up front: the
   resolver is the most constrained caller, so what it needs is what the entity owes. Expect the
   pools to move into `Theme` - adjectives and participles already unioned across the noun's
   categories and already minus its exclusions - because that is what the resolver memoises today
   and what "un nom dans le domaine ne porte pas les exclusions" means in code.
2. **Implement `ICatalog`** over the existing catalogs (embedded, file system, chained), turning
   the `ThemeDocument` they read into a `Theme`. That is where validation ends up living, which is
   what lets `Get` promise a whole aggregate while still reporting a file edited by hand.
3. **Move `ThemeDocument` into `Infrastructure`.** It is the shape of the file, and once the
   entity is what the domain works with there is no reason for it to sit in `Domain/`.
4. **Make `ThemeErrors` singular and move it out of `Domain/Validation/`.** It is the last
   catalogue still plural and still filed away, against the rule the others were changed to follow.
   Careful: `ThemeError.NoNounToDrawFrom` would duplicate an existing `ErrorCode`, so the old
   catalogue migrates into the new one rather than the two coexisting.
5. **Retire `IThemeCatalog`** once `ICatalog` covers it, so the two names stop sitting side by side.
6. **Rewrite this branch's history** before it is proposed - agreed during the session, not done.

## Open questions, none of them blocking

- **`Category` folds its case; the theme files do not.** Wiring it in changes behaviour - a noun
  labelled `"Common"` reaches `common` where today it silently reaches nothing. That is a fix, but
  it is a visible one and wants a DEC before it lands.
- **`ThemeName` keeps its case, deliberately**, unlike `Category`: a directory on Linux tells
  `Docker` from `docker` and one on Windows does not, so folding it would make the domain answer
  for a file it cannot open. Written down here because the asymmetry looks like an oversight and
  is not.
- **The `Value` package in the engine's dependency whitelist** may want a DEC of its own.
- **The mutation figures in `CLAUDE.md` predate DEC0019** and now also predate this work. Re-run
  both engines before comparing anything, and do not read a few points as a change.
- **`claude/dreamy-mayer-vnicqe`** still exists on the remote; deleting it returned 403 through the
  proxy.
