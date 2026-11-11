# DEC0037 | Le contrat de compatibilité de la `1.x` : signatures et codes stables, planchers fixes

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-11-11 | Accepté | Quatrième point de l'issue #63, avant `lib-v1.0.0` | |

## Contexte

Une version `1.0.0` de la bibliothèque promet ce que SemVer promet : une version mineure ne casse pas
ce qui marchait. Rien n'écrivait jusqu'ici ce que « marcher » couvre pour slugger.

Deux règles étaient déjà en vigueur, écrites dans `library.md` : les messages d'erreur peuvent être
reformulés à tout moment, et une graine ne reproduit une suite de slugs que pour une même version de
la bibliothèque et du thème.

Les planchers de validation — 100 noms, 100 mots devant chaque nom, 20 participes derrière chaque
adjectif, 40 000 combinaisons par catégorie, publiés dans `ThemeFloors` (DEC0036) — étaient présentés
comme faits pour monter : la documentation XML de `ThemeFloors` et `library.md` annonçaient que le
plancher des participes s'élèverait avec les thèmes livrés, et conseillaient de garder une marge.
Aucune IDR ne l'avait décidé. Un plancher relevé en version mineure fait refuser au chargement un
thème personnel accepté la veille, sans que son auteur ait changé une ligne.

Les planchers sont des `public const` : le compilateur recopie leur valeur dans le code de qui les lit.
Une valeur appelée à changer d'une version à l'autre y serait restée figée chez le consommateur jusqu'à
sa prochaine compilation.

Les codes d'erreur (`THEME_*`) sont ce sur quoi la documentation demande de brancher.

## Décision

Dans ce contexte, nous décidons que chaque version `1.x` garde les signatures publiques et les codes
d'erreur de celle qui la précède — en ajoutant, jamais en retirant ni en renommant —, que les
planchers ne bougent pas, que les messages peuvent être reformulés, et qu'une graine ne reproduit ses
slugs que pour une même version de la bibliothèque et du thème.

## Justification

**Les planchers ne sont pas là pour bouger.** Ils disent ce qu'est un thème utilisable, comme ses clés
disent ce qu'est un fichier de thème. Un auteur qui les atteint a écrit un thème valide, et une mise à
jour mineure ne doit pas lui retirer ce statut. Ils restent des constantes : une valeur qui ne change
jamais peut être recopiée chez le consommateur sans risque.

**Un code se branche, un message se lit.** Le code d'un consommateur compare des codes ; renommer
`THEME_POOL_TOO_SMALL` laisserait sa condition fausse à jamais, sans erreur pour le dire. En ajouter
un ne casse rien : il ne le connaît pas encore. Un message, lui, est fait pour un humain, et peut
s'améliorer à chaque version.

**Une graine ne fige pas les thèmes.** Les thèmes livrés grossissent d'une version à l'autre ; une même
graine tire alors dans un autre vocabulaire. Le promettre interdirait d'enrichir un thème en version
mineure.

**Les signatures suivent SemVer.** C'est la promesse d'une `1.0.0`, et la seule raison d'en publier une.

## Alternatives envisagées

### Alternative 1 — Laisser les planchers monter en version mineure, en l'annonçant

- **Description :** écrire dans le contrat que les planchers peuvent monter, jamais en patch, jamais à
  la baisse, toujours annoncés, avec `allowSmall` pour échappatoire.
- **Pourquoi écartée :** une mise à jour mineure pourrait refuser un thème valide ; c'est exactement ce
  qu'une version mineure promet de ne pas faire.

### Alternative 2 — Relever les planchers seulement en version majeure, en prévoyant de le faire

- **Description :** geler les planchers pendant la `1.x`, avec l'intention de les relever en `2.0`.
- **Pourquoi écartée :** les planchers ne sont pas une étape vers d'autres valeurs. Un changement reste
  possible, par une nouvelle IDR et une version majeure, mais il n'est pas prévu.

### Alternative 3 — Ne rien promettre des codes d'erreur

- **Description :** traiter les codes comme les messages.
- **Pourquoi écartée :** la documentation demande de brancher sur les codes plutôt que sur les messages ;
  sans promesse, il ne resterait rien de stable sur quoi brancher.

## Conséquences

### Positives

- Une mise à jour mineure ne refuse pas un thème parce qu'un plancher a bougé.
- Un consommateur sait sur quoi brancher et ce qui peut changer sous lui.
- `ThemeFloors` reste un ensemble de constantes, sans le piège de leur recopie.

### Négatives

- Le plancher des participes, délibérément bas, le reste pendant toute la `1.x`.

### Risques

- Une nouvelle règle de cohérence — une contradiction dans un fichier qu'aucune règle ne relève
  aujourd'hui — refuserait elle aussi un thème accepté la veille. Ce contrat ne la traite pas ; elle
  demandera sa propre décision le jour où elle se présentera.

### Actions de suivi

- Épingler les quatre valeurs des planchers et la liste des codes publics par des tests, pour qu'un
  retrait ou un changement fasse échouer le build.
