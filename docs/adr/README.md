# Décisions d'architecture

Les décisions qui font que `slugger` est `slugger`, et qui contraignent ce qu'on peut lui
ajouter. Une par fichier, numérotée, jamais réécrite : une décision qu'on change reçoit un
nouvel ADR qui déclare l'ancien remplacé.

| # | Décision | Ce qu'elle contraint |
| --- | --- | --- |
| [0001](0001-un-adjectif-plausible-pour-son-nom.md) | Un adjectif doit être plausible pour son nom | Tout mécanisme de tirage |
| [0002](0002-common-est-un-socle-partage.md) | `common` est un socle partagé, pas un repli | La résolution des pools |
| [0003](0003-la-validation-juge-le-pool-resolu.md) | La validation juge le pool résolu | Toute règle de validation future |
| [0004](0004-une-seule-chaine-de-priorite.md) | Une seule chaîne de priorité pour toute option | Toute nouvelle option |
| [0005](0005-le-format-se-decide-au-tirage.md) | Le format se décide au tirage, pas au chargement | Tout levier de format |
| [0006](0006-un-refus-dit-tout-d-un-coup.md) | Un refus dit tout d'un coup, et une seule fois | Toute nouvelle erreur |
| [0007](0007-publier-un-type-est-definitif.md) | Publier un type est définitif | Tout nouveau type, toute dépendance |

Pour écrire un thème plutôt que du code : [`../writing-a-theme.md`](../writing-a-theme.md).

## Ce qui n'est délibérément pas ici

Ces ADR remplacent `docs/slugger-spec.md`, supprimée : 63 % de ses 353 lignes paraphrasaient le
code et ne pouvaient que le suivre. Le contrat exact — les vingt flags, le schéma JSON, ce que
`--init` persiste, ce que dit chaque message — est dit par le code et pinné par les tests, qui
préviennent en rouge plutôt qu'en markdown périmé. Git garde le reste.

N'ont pas mérité un ADR, faute de contraindre quoi que ce soit :

- **L'internement des strings au chargement** — une optimisation invisible, sans effet sur une
  décision future.
- **La bascule REPL → oneshot** derrière un pipe — un comportement sensé, jamais débattu.
- **Les deux trains de version `lib-v*` / `cli-v*`** — du process, déjà dans `CLAUDE.md`.
- **Les deux moteurs de mutation** (Stryker et KillMutants côte à côte) — une expérience en
  cours, pas une décision arrêtée. Elle en deviendra une, ou disparaîtra.
