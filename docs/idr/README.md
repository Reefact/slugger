# Important Decision Records

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

Pour écrire un thème plutôt que du code : [`../writing-a-theme.md`](../writing-a-theme.md).

## Ce qui n'est délibérément pas ici

Ces IDR remplacent `docs/slugger-spec.md`, supprimée : 63 % de ses 353 lignes paraphrasaient le
code et ne pouvaient que le suivre. Le contrat exact — les vingt flags, le schéma JSON, ce que
`--init` persiste, ce que dit chaque message — est dit par le code et pinné par les tests, qui
préviennent en rouge plutôt qu'en markdown périmé. Git garde le reste
(`git show 96a83e7:docs/slugger-spec.md`).

N'ont pas donné lieu à un IDR, faute de contraindre une décision future :

- **L'internement des chaînes au chargement** — une optimisation invisible depuis l'extérieur.
- **La bascule REPL → oneshot** derrière un tube — un comportement déduit de l'environnement,
  jamais contesté.
- **Les deux moteurs de mutation** (Stryker et KillMutants côte à côte) — une expérience en
  cours. Elle deviendra une décision, ou disparaîtra.
