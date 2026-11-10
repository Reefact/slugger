# DEC0030 | La casse fixe les majuscules et un séparateur par défaut, que `sep` remplace toujours

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-11-10 | Accepté | Règle l'issue #47 | |

## Contexte

`--casing` acceptait `kebab`, `snake` et `camel`, et le formateur ne distinguait que `camel` du
reste : pour `kebab` comme pour `snake`, les termes étaient joints par `--sep`, `-` par défaut.
`--casing snake` écrivait donc `robust-rising-jacob-degrom`, et seule l'ajout de `--sep _` donnait du
snake_case. La documentation le reconnaissait (« `kebab` and `snake` render the same way »), et le
dépannage de `cli.md` conseillait d'ajouter `--sep _`.

Les deux thèmes embarqués qui fixent une casse fixent aussi le séparateur qui va avec — `docker`
`snake` et `_`, `heroku` `kebab` et `-` — ce qui masquait le défaut à l'usage courant, mais le
rendait visible dès qu'on demandait une autre casse : `--theme docker --casing kebab` écrivait
`humming_dirac`, le `sep` du thème restant en vigueur.

Une convention de nommage fixe deux choses : ses majuscules et ce qui joint les mots. Il n'existe
que quatre façons de mettre les majuscules — tout en minuscules, tout en majuscules, chaque mot
sauf le premier, chaque mot — et chaque convention connue est l'une d'elles croisée avec une
liaison : kebab-case (minuscules, `-`), snake_case (minuscules, `_`), CONSTANT_CASE (majuscules,
`_`), camelCase (chaque mot sauf le premier, rien), PascalCase (chaque mot, rien), Train-Case
(chaque mot, `-`), COBOL-CASE (majuscules, `-`), flatcase (minuscules, rien).

La chaîne de priorité (DEC0004) résout chaque levier indépendamment : ligne de commande, style du
thème, défauts sauvegardés, défauts intégrés.

## Décision

Dans ce contexte, nous décidons qu'une casse fixe les majuscules et un séparateur par défaut —
`kebab` `-`, `snake` et `constant` `_`, `camel` et `pascal` rien — et qu'un `sep` donné, où que ce
soit dans la chaîne de priorité, remplace ce séparateur quelle que soit la casse, la chaîne vide
comprise, sans toucher aux majuscules.

## Justification

Appliquer la casse à la lettre rend à chaque nom ce qu'il promet : `--casing snake` écrit
`robust_rising_jacob_degrom` sans autre option, ce que l'énumération décrivait déjà comme
l'intention (« as Docker writes it »).

Faire de `sep` un remplacement pur rend les deux réglages orthogonaux : la casse décide des
majuscules, `sep` de la liaison. Aucune combinaison n'est alors à refuser ni à ignorer en silence,
et cinq valeurs de casse suffisent à atteindre toutes les conventions : `--casing pascal --sep -`
donne Train-Case, `--casing kebab --sep ""` flatcase, `--casing constant --sep -` COBOL-CASE.

Qu'un `sep` donné n'importe où gagne garde la règle explicable en une phrase et laisse la chaîne de
priorité intacte : chaque levier reste résolu seul, et la casse ne fournit sa liaison que là où
aucun niveau n'en donne. Les thèmes `docker` et `heroku` déclarent désormais leur casse seule, leur
`sep` étant celui que leur casse donne déjà : leurs slugs ne changent pas, et une casse demandée en
ligne de commande apporte enfin sa propre liaison.

`constant` écrit aussi le jeton en majuscules : un `3fa` hexadécimal en minuscules au bout d'un
slug en majuscules mélangerait deux conventions.

## Alternatives envisagées

### Alternative 1 — Fusionner `kebab` et `snake` en une seule valeur

- **Description :** `--casing separated | camel`, la liaison restant l'affaire de `--sep` seul.
- **Pourquoi écartée :** honnête sur ce que faisait le code, mais cassante pour les scripts, les
  thèmes et les défauts sauvegardés qui écrivent `snake` ou `kebab`, et elle renonce aux noms que
  tout le monde emploie pour ces conventions.

### Alternative 2 — Une casse n'impose sa liaison qu'aux niveaux inférieurs au sien

- **Description :** une casse fixée à un niveau de la chaîne l'emporte sur un `sep` fixé plus bas,
  de sorte que `--casing kebab` donne `-` même sur un thème qui déclare `"sep": "_"`.
- **Pourquoi écartée :** lie deux leviers que la chaîne de priorité résout indépendamment, et la
  règle devient difficile à dire et à prévoir ; retirer le `sep` redondant des thèmes embarqués
  obtient le même résultat là où il comptait.

### Alternative 3 — Refuser `sep` avec `camel` et `pascal`

- **Description :** ces casses ne joignant rien, un `sep` serait une erreur.
- **Pourquoi écartée :** interdit des conventions légitimes — Train-Case, `robust_Rising_Jacob` —
  sans rien protéger.

### Alternative 4 — Une valeur de casse par convention nommée

- **Description :** `train`, `cobol`, `flat` à côté des cinq valeurs retenues.
- **Pourquoi écartée :** chacune n'est qu'une casse et une liaison déjà atteignables ; elles
  multiplieraient les valeurs à documenter sans rien rendre possible.

## Conséquences

### Positives

- `--casing snake` écrit du snake_case, et chaque casse écrit sa convention sans autre option.
- Toutes les conventions sont atteignables, `pascal` et `constant` comprises.
- `--sep ""` et `"sep": ""` écrivent les termes sans rien entre eux.

### Négatives

- `GenerationOptions.Separator` passe de `char` (défaut `-`) à `string?` (défaut nul, la liaison de
  la casse) : un changement de l'API publique, admis tant que `lib` est en préversion.
- Un `sep` sauvegardé par `--init` l'emporte désormais sur la liaison de `docker` : qui a sauvegardé
  `--sep -` obtient `humming-dirac` avec `--theme docker`, là où le `sep` du thème l'emportait
  auparavant sur les défauts sauvegardés.
- Un thème qui déclarait `casing: camel` avec un `sep` voyait ce `sep` ignoré ; il est maintenant
  écrit. De même, un `wordSep` est désormais écrit entre les mots d'un terme composé en `camel`.

### Risques

- Un thème utilisateur qui déclare `casing` et le `sep` correspondant garde ce `sep` sous une autre
  casse demandée en ligne de commande. C'est la règle, et `writing-a-theme.md` conseille de ne
  déclarer `sep` que là où le style s'écarte de sa casse.

### Actions de suivi

- Dire la règle là où chaque lecteur la cherche — fait dans `cli.md`, `writing-a-theme.md`,
  `theme-reference.md`, `library.md` et l'aide de `--casing` et `--sep`.
- Retirer le `sep` redondant de `docker` et `heroku` — fait, en version `1.0.1`.
