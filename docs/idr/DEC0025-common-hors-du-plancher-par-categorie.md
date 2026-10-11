# DEC0025 | `common` jamais mesuré par le plancher par catégorie

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-10-11 | Accepté | Issue #35 | |

## Contexte

DEC0002 fait de `common` le socle de tout nom : chaque nom l'atteint en plus des catégories qu'il
déclare, sans avoir à l'écrire. DEC0003 mesure les règles de taille sur le pool résolu.

Le plancher par catégorie (40 000 combinaisons) ne mesure pourtant que les catégories qu'un nom
**écrit** dans `categories`, et `ThemeCombinatorics.CombinationsForCategory` ne somme que les noms
qui l'écrivent. Tant qu'aucun nom n'écrit `common`, `common` n'est jamais mesuré. Dès qu'un nom
l'écrit, `common` totalise les combinaisons de ce seul nom, et le thème est refusé : sur le fichier
d'exemple `spices`, écrire `"categories": ["common"]` sur `turmeric` donnait *« Category "common"
totals 2,424 combinations, but every category needs at least 40,000. »*, alors que le même fichier
sans ce mot est accepté et que le mot ne change rien à ce qui est tiré.

Écrire la vérité plus souvent n'en sort jamais : sur le plus petit thème accepté (100 noms, 100
adjectifs dans `common`), les 100 noms écrivant `common` donnent 100 × 100 = 10 000 combinaisons,
toujours sous le plancher, quand ne rien écrire passe. Et le message envoie l'auteur « enrichir
`common` », quand le remède est d'effacer un mot. Le guide documentait le piège plutôt que de le
lever.

## Décision

Dans ce contexte, nous décidons que le plancher par catégorie ne mesure jamais `common`, qu'un nom
l'écrive ou non ; un nom qui l'écrit vaut une remarque, qui ne refuse rien.

## Justification

**Une catégorie que tout nom atteint ne peut pas être une branche pauvre du thème.** Le plancher
existe pour qu'aucune branche ne soit pauvre à elle seule (DEC0003) ; `common` n'est pas une branche
mais le tronc (DEC0002). Le mesurer seulement quand des noms l'écrivent mesure la façon dont le
fichier est écrit, pas ce qu'il tire - l'inverse de ce que DEC0003 demande.

**Aucun thème accepté aujourd'hui ne devient refusé.** La règle ne fait que retirer une mesure ; un
thème qui passait passe toujours, un thème refusé pour ce seul motif passe désormais.

**L'auteur est quand même prévenu.** Le mot ne change rien, et c'est exactement ce que les
remarques de `--register` et de `--analyze` disent déjà d'un mot déclaré dans les deux sections ou
d'une paire `incompatible` qui ne s'applique jamais : accepté, mais à regarder.

**Le rapport d'analyse suit la même liste.** `ThemeValidator.CategoriesUnderTheFloor` est la seule
définition des catégories mesurées, et `ThemeAnalyzer` la lit pour sa ligne *Combinations per
category* ; sans cela, le rapport présenterait `common` comme la catégorie la plus pauvre d'un thème
que son propre verdict accepte.

## Alternatives envisagées

### Alternative 1 — Refuser `"common"` dans `categories`

- **Description :** un refus qui dit pourquoi : *« `common` is reached by every noun; leave it out
  of "categories". »*
- **Pourquoi écartée :** le sens du fichier est sans ambiguïté, et un gros thème tiers qui écrit
  `common` sur tous ses noms, accepté aujourd'hui parce que ses noms totalisent 40 000 combinaisons,
  deviendrait refusé. Cela renomme le piège sans le lever.

### Alternative 2 — Mesurer `common` sur tous les noms

- **Description :** compter chaque nom pour `common`, puisque chaque nom l'atteint.
- **Pourquoi écartée :** `common` vaudrait alors le total du thème, qui est toujours au-dessus du
  plancher dès que les autres règles passent ; une mesure qui ne peut jamais échouer n'est qu'un
  calcul de plus.

## Conséquences

### Positives

- Un nom qui écrit `common` ne fait plus refuser son thème, et l'auteur apprend que le mot est
  inutile au lieu de lire qu'il doit enrichir `common`.
- Le verdict et le rapport d'analyse mesurent la même liste de catégories.

### Négatives

- Une remarque de plus dans la sortie de `--register` pour un thème tiers qui écrit `common`
  partout.

### Risques

- Aucun identifié : la règle retire une mesure sans en ajouter.

### Actions de suivi

- Retirer du catalogue d'erreurs de `docs/theme-reference.md` l'entrée « `common` written on a
  noun », qui décrivait le refus, et dire dans `docs/writing-a-theme.md` que le mot est inutile
  plutôt que dangereux.
