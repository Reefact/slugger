# Important Decision Records

The decisions that shaped `slugger`, one record each. **The records are written in French**, and
the index further down is too; this English part tells you how to use them, which ones constrain
which code and what each one decided, in a line.

- A record is history, not living documentation: it says what was true and known when the decision
  was made, and what the decision rules out.
- **An accepted record is never rewritten.** A decision that changes gets a new record, and the old
  record's status table gains a line naming its replacement — DEC0012, replaced by DEC0016, is the
  example here.
- Every record has the same sections: *Statut* (status), *Contexte* (the facts), *Décision* (one
  sentence), *Justification* (the arguments drawn from those facts), *Alternatives envisagées* (what
  was rejected and why) and *Conséquences* (positive, negative, risks, follow-up actions).
- The format comes from the maintainers'
  [IDR guideline](https://github.com/Reefact/guidelines/blob/main/important-decision-record-guideline.md),
  which lives in a private repository; the points above are what you need from it.
- The numbering belongs to this repository.

Read the record before you change the code it constrains. Most questions of the form "why is it
like this" are answered there. If your change contradicts a decision, it needs a new record, not
an edit to the old one; say so in your pull request.

## Finding the decision behind a file

The code names the decision it honours, in its comments and in the summaries of the tests that pin
it. To see which decisions a file names, and which files honour a decision:

```bash
grep -rn DEC00 src/Slugger/Domain/Validation/ThemeValidator.cs
grep -rln DEC0018 src tests
```

## Decisions by code area

| Area | Main files | Decisions |
| --- | --- | --- |
| Command line | `src/Slugger.Cli/CommandLine/`: `SluggerSettings`, `CommandLineReader`, `SluggerApp`, `CliErrors` | [DEC0019](DEC0019-ligne-de-commande-declaree-et-rendue-par-spectre.md), [DEC0006](DEC0006-rapport-groupe-des-refus.md), [DEC0024](DEC0024-aucun-plafond-explicite-qui-outrepasse-le-theme.md) |
| Precedence chain of options | `src/Slugger/Application/Options/`: `OptionResolver`, `SluggerOptions`, `SegmentWordsCap`, `MimicStyle`; `GenerationOptions.WithDefaultsOf` | [DEC0004](DEC0004-chaine-de-priorite-unique.md), [DEC0024](DEC0024-aucun-plafond-explicite-qui-outrepasse-le-theme.md) |
| Reading a theme file | `JsonThemeSerializer`, `ThemeLoader`, `ThemeDocument`, `ThemeMetadata`, `WordNormalizer.Canonicalize` | [DEC0006](DEC0006-rapport-groupe-des-refus.md), [DEC0008](DEC0008-reduction-des-caracteres-non-alphanumeriques.md), [DEC0011](DEC0011-exclusion-de-mots-par-nom.md), [DEC0017](DEC0017-refus-d-un-participe-a-cote-d-un-adjectif.md), [DEC0018](DEC0018-longueur-maximale-tenue-en-retirant-des-mots.md), [DEC0021](DEC0021-bloc-meta-descriptif-jamais-consulte.md), [DEC0022](DEC0022-dates-de-creation-et-de-publication-dans-meta.md), [DEC0023](DEC0023-plafond-de-mots-par-segment.md) |
| Validation | `ThemeValidator`, `ThemeCombinatorics`, `ThemeErrors` | [DEC0002](DEC0002-common-atteint-par-tout-nom.md), [DEC0003](DEC0003-validation-sur-le-pool-resolu.md), [DEC0006](DEC0006-rapport-groupe-des-refus.md), [DEC0013](DEC0013-mot-declare-dans-les-deux-sections.md), [DEC0015](DEC0015-tirage-pondere-du-mot-unique-de-either.md), [DEC0016](DEC0016-planchers-alignes-sur-le-mode-de-segment.md), [DEC0017](DEC0017-refus-d-un-participe-a-cote-d-un-adjectif.md), [DEC0018](DEC0018-longueur-maximale-tenue-en-retirant-des-mots.md), [DEC0020](DEC0020-absence-de-participe-tiree-comme-un-participe-de-plus.md), [DEC0023](DEC0023-plafond-de-mots-par-segment.md) |
| Resolution and drawing | `ThemeResolver`, `SlugGenerator`, `SlugBudget`, `WeightedThemePicker`, `SegmentMode`; `GenerateSlugsUseCase`, which validates a narrowed theme again | [DEC0001](DEC0001-restriction-des-adjectifs-par-categorie.md), [DEC0002](DEC0002-common-atteint-par-tout-nom.md), [DEC0011](DEC0011-exclusion-de-mots-par-nom.md), [DEC0013](DEC0013-mot-declare-dans-les-deux-sections.md), [DEC0015](DEC0015-tirage-pondere-du-mot-unique-de-either.md), [DEC0016](DEC0016-planchers-alignes-sur-le-mode-de-segment.md), [DEC0017](DEC0017-refus-d-un-participe-a-cote-d-un-adjectif.md), [DEC0018](DEC0018-longueur-maximale-tenue-en-retirant-des-mots.md), [DEC0020](DEC0020-absence-de-participe-tiree-comme-un-participe-de-plus.md), [DEC0023](DEC0023-plafond-de-mots-par-segment.md) |
| Rendering | `SlugFormatter`, `WordNormalizer` | [DEC0005](DEC0005-format-resolu-au-tirage.md), [DEC0008](DEC0008-reduction-des-caracteres-non-alphanumeriques.md), [DEC0009](DEC0009-pliage-des-accents-a-la-demande.md), [DEC0010](DEC0010-option-ascii-qui-defigure.md) |
| Analysis (`--analyze`) | `ThemeAnalyzer`, `AnalyzeThemeUseCase`, `ThemeAnalysisRenderer` | [DEC0014](DEC0014-rapport-d-analyse-d-un-theme.md), [DEC0003](DEC0003-validation-sur-le-pool-resolu.md) |
| Theme metadata (`--theme-info`) | `ThemeMetadata`, `ThemeInfoUseCase` | [DEC0021](DEC0021-bloc-meta-descriptif-jamais-consulte.md), [DEC0022](DEC0022-dates-de-creation-et-de-publication-dans-meta.md) |
| Vocabulary types, not yet wired in | `Word`, `Term`, `Category`, `Epithet`, `Adjective`, `Participle` | [DEC0001](DEC0001-restriction-des-adjectifs-par-categorie.md), [DEC0008](DEC0008-reduction-des-caracteres-non-alphanumeriques.md), [DEC0013](DEC0013-mot-declare-dans-les-deux-sections.md), [DEC0017](DEC0017-refus-d-un-participe-a-cote-d-un-adjectif.md), [DEC0020](DEC0020-absence-de-participe-tiree-comme-un-participe-de-plus.md) |
| Packages and public surface | `Slugger.csproj`, `Slugger.Cli.csproj`, `Directory.Packages.props`, `Themes.cs`, `NamespaceDependencyTests` | [DEC0007](DEC0007-surface-publique-reduite.md) |

[`../architecture.md`](../architecture.md) says where each of these files sits and how a slug
travels through them.

## Each decision in one line

- **DEC0001** — An adjective can be drawn for a noun only when the two share at least one category.
- **DEC0002** — Every noun reaches the `common` category on top of the ones it declares.
- **DEC0003** — The size rules are measured on the pool each noun and each category actually
  resolves to, not on the raw lists.
- **DEC0004** — Every option is resolved by one chain: the command line, then the drawn theme's
  `defaults`, then the defaults saved by `--init`, then the program's own default.
- **DEC0005** — The spaces inside a compound value are replaced when the slug is rendered, not when
  the theme is loaded.
- **DEC0006** — A refused theme or command line reports every reason in one run.
- **DEC0007** — The engine ships as one assembly in which only `Slugger.Domain` and the `Themes`
  facade are public.
- **DEC0008** — At load, every character that is neither a letter nor a digit becomes a word
  boundary.
- **DEC0009** — Accents are folded at rendering, when the run asks for it; a theme keeps its own
  spelling.
- **DEC0010** — `--ascii` guarantees an ASCII slug by dropping whatever is still not ASCII after
  folding, even when that disfigures a word.
- **DEC0011** — A noun can refuse named words (`except`), removed from its pools once its categories
  are resolved.
- **DEC0012** — A theme that declares participles must offer every noun at least twenty.
  *Replaced by DEC0016.*
- **DEC0013** — A word may be declared both as an adjective and as a participle: registering the
  theme says so, and a slug that draws it twice writes it once.
- **DEC0014** — `--analyze` measures a theme file and writes the report beside it; the library
  measures, the CLI writes.
- **DEC0015** — `either` draws its single word from the two pools taken together, weighted by their
  sizes.
- **DEC0016** — The per-noun floors apply to the pool that the theme's declared segment mode
  actually draws from.
- **DEC0017** — An adjective can refuse named participles beside it (`incompatible`), removed from
  the pool before the participle is drawn.
- **DEC0018** — A maximum slug length is met by removing words before the draw: a promise when a
  theme declares it, a ceiling when a run asks for it.
- **DEC0019** — The command line is declared once, on a type that Spectre binds the arguments to and
  draws `--help` from.
- **DEC0020** — A segment mode, `threeOrTwo`, draws "no participle" as one more candidate in the
  participle pool.
- **DEC0021** — A theme may describe itself in an optional `meta` block that generation never
  reads; the file name stays the theme's only identity.
- **DEC0022** — `meta` gains `createdAt` and `publishedAt`, read as plain strings and never parsed
  as dates.
- **DEC0023** — A maximum number of words per segment is met by removing values before the draw: a
  style when a theme declares it, a ceiling when a run asks for it.
- **DEC0024** — `--max-segment-words none` removes the cap explicitly, overriding the one a theme's
  `defaults` would apply.

## Adding a decision

Write a new `DECxxxx-<titre>.md` with the next free number, in French and in the format above, add
its row to the index below, its line to the list above and the areas it constrains to the table.
Name it in the code and in the tests that honour it, so that `grep` finds it.

## Index (en français)

Les décisions structurantes de `slugger`, au format défini par
[`important-decision-record-guideline.md`](https://github.com/Reefact/guidelines) du chapitre.

Un IDR est une **mémoire historique**, pas une documentation vivante : il décrit ce qui était
vrai et connu au moment de la décision. Un IDR accepté n'est pas réécrit — une décision qui
évolue donne lieu à un nouvel IDR, et le statut de l'ancien est mis à jour en conséquence.

La numérotation est propre à ce dépôt.

| # | Décision | Ce qu'elle contraint |
| --- | --- | --- |
| [DEC0001](DEC0001-restriction-des-adjectifs-par-categorie.md) | Restriction des adjectifs tirables par catégorie partagée avec le nom | Tout mécanisme de tirage |
| [DEC0002](DEC0002-common-atteint-par-tout-nom.md) | Adoption de « common » comme socle atteint par tout nom | La résolution des pools |
| [DEC0003](DEC0003-validation-sur-le-pool-resolu.md) | Validation de la taille d'un thème sur le pool résolu | Toute règle de validation future |
| [DEC0004](DEC0004-chaine-de-priorite-unique.md) | Adoption d'une chaîne de priorité unique pour toutes les options | Toute nouvelle option |
| [DEC0005](DEC0005-format-resolu-au-tirage.md) | Résolution du format au tirage plutôt qu'au chargement | Tout levier de format |
| [DEC0006](DEC0006-rapport-groupe-des-refus.md) | Rapport groupé de toutes les raisons d'un refus | Toute nouvelle erreur |
| [DEC0007](DEC0007-surface-publique-reduite.md) | Réduction de la surface publique à un seul assembly | Tout nouveau type, toute dépendance |
| [DEC0008](DEC0008-reduction-des-caracteres-non-alphanumeriques.md) | Réduction au chargement de tout caractère non alphanumérique en frontière de mot | Ce qu'une valeur de thème peut contenir |
| [DEC0009](DEC0009-pliage-des-accents-a-la-demande.md) | Pliage des accents décidé par l'exécution, jamais par le thème | Qui décide de l'alphabet d'un slug |
| [DEC0010](DEC0010-option-ascii-qui-defigure.md) | Adoption d'une option ASCII qui défigure plutôt que de renoncer | Ce qu'une exécution peut garantir |
| [DEC0011](DEC0011-exclusion-de-mots-par-nom.md) | Refus d'un mot par le nom lui-même, en plus du filtrage par catégories | Ce qu'un tirage peut associer |
| [DEC0012](DEC0012-plancher-de-participes-par-nom.md) | Instauration d'un plancher de participes par nom *(remplacé par DEC0016)* | Ce qu'un thème doit offrir pour être accepté |
| [DEC0013](DEC0013-mot-declare-dans-les-deux-sections.md) | Tolérance d'un mot déclaré dans les deux sections, signalée et absorbée | Ce qu'un slug peut répéter |
| [DEC0014](DEC0014-rapport-d-analyse-d-un-theme.md) | Mesure d'un thème par une commande dédiée, rendue par le CLI | Ce qu'un auteur peut savoir de son thème |
| [DEC0015](DEC0015-tirage-pondere-du-mot-unique-de-either.md) | Tirage du mot unique de « either » dans les deux pools réunis, pondéré | Ce que « either » veut dire |
| [DEC0016](DEC0016-planchers-alignes-sur-le-mode-de-segment.md) | Alignement des planchers de taille par nom sur le mode de segment déclaré | Ce qu'un thème doit offrir, selon ce qu'il tire |
| [DEC0017](DEC0017-refus-d-un-participe-a-cote-d-un-adjectif.md) | Refus d'un participe à côté d'un adjectif donné, retiré avant le tirage | Ce qu'un slug peut associer |
| [DEC0018](DEC0018-longueur-maximale-tenue-en-retirant-des-mots.md) | Longueur maximale tenue en retirant des mots avant le tirage | Où un slug peut être posé |
| [DEC0019](DEC0019-ligne-de-commande-declaree-et-rendue-par-spectre.md) | Ligne de commande déclarée une seule fois, lue et rendue par Spectre | Comment une option est déclarée, refusée et affichée |
| [DEC0020](DEC0020-absence-de-participe-tiree-comme-un-participe-de-plus.md) | Tirage de l'absence de participe comme un participe de plus, dans un mode dédié | Combien de mots un slug porte |
| [DEC0021](DEC0021-bloc-meta-descriptif-jamais-consulte.md) | Un bloc `meta` descriptif, jamais consulté par la génération | Ce qu'un thème peut dire de lui-même |
| [DEC0022](DEC0022-dates-de-creation-et-de-publication-dans-meta.md) | Dates de création et de publication dans `meta` | Ce qu'un thème peut dire de lui-même |
| [DEC0023](DEC0023-plafond-de-mots-par-segment.md) | Plafond de mots par segment, tenu en retirant des valeurs avant le tirage | Combien de mots un segment porte |
| [DEC0024](DEC0024-aucun-plafond-explicite-qui-outrepasse-le-theme.md) | Un plafond de mots explicitement absent, qui outrepasse celui du thème | Ce qu'un argument explicite peut dire d'un levier que les `defaults` d'un thème couvrent |

Pour écrire un thème plutôt que du code : [`../writing-a-theme.md`](../writing-a-theme.md).

## Ce qui n'est délibérément pas ici

Ces IDR remplacent `docs/slugger-spec.md`, supprimée : 63 % de ses 353 lignes paraphrasaient le
code et ne pouvaient que le suivre. Le contrat exact — chaque option, le schéma JSON,
ce que `--init` persiste, ce que dit chaque message — est dit par le code et pinné par les tests,
qui préviennent en rouge plutôt qu'en markdown périmé. Depuis DEC0019, les options sont en plus
dites par `slugger --help`, qui est tiré de leur déclaration. Git garde le reste
(`git show 96a83e7:docs/slugger-spec.md`).

N'ont pas donné lieu à un IDR, faute de contraindre une décision future :

- **L'internement des chaînes au chargement** — une optimisation invisible depuis l'extérieur.
- **La bascule REPL → oneshot** derrière un tube — un comportement déduit de l'environnement,
  jamais contesté.
- **Les deux moteurs de mutation** (Stryker et KillMutants côte à côte) — une expérience en
  cours. Elle deviendra une décision, ou disparaîtra.
