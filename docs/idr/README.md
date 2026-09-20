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
