# DEC0021 | Un bloc `meta` descriptif, jamais consulté par la génération

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-21 | Accepté | | |

## Contexte

Un thème publié circule au-delà de son fichier : copié entre équipes, partagé sur un dépôt,
repris par quelqu'un qui n'a jamais lu le JSON d'origine. Rien dans le fichier ne dit alors qui
l'a écrit, d'où il vient, ni quelle version on a sous la main — l'information vit, quand elle
existe, dans un message de commit ou une page à côté, jamais dans le thème lui-même.

`Theme.Name` vient du nom de fichier, jamais d'un champ JSON (`Theme.cs`, et redit dans
`writing-a-theme.md`) : un thème renommé sur disque change d'identité sans que son contenu ait
bougé, et c'est délibéré — deux copies du même fichier sous deux noms sont deux thèmes distincts
pour le catalogue.

`JsonThemeSerializer` ignore déjà silencieusement toute clé racine qu'il ne connaît pas : ajouter
un bloc que d'anciens fichiers ne déclarent pas ne casse rien qui existe.

## Décision

Dans ce contexte, nous décidons d'ajouter un bloc `meta` optionnel — `title`, `description`,
`version`, `author`, `source`, chaque clé optionnelle et lue comme une chaîne quelconque, jamais
comparée ni interprétée. `title` est un intitulé d'affichage, pas une identité : le nom de fichier
reste la seule clé de recherche, de registre et de catalogue.

## Justification

Séparer `title` de l'identité du fichier évite deux sources de vérité pour la même chose : un
fichier renommé sans que son `title` suive, ou l'inverse, ne créerait une confusion que si le
champ pouvait se substituer au nom de fichier — en le cantonnant à l'affichage, la question ne se
pose jamais.

Ne rien comparer ni interpréter dans `version` — pas de contrainte de format, pas de blocage de
chargement selon sa valeur — garde le bloc entièrement descriptif : un thème dont le `meta` est
mal renseigné se charge et génère exactement comme si le bloc était absent. Une version qui
déciderait un jour de la compatibilité serait une décision différente, avec ses propres
conséquences sur le chargement, et n'a pas sa place ici.

Lire chaque champ comme une chaîne quelconque, sans passer par `WordNormalizer`, suit le
précédent déjà posé par DEC0008 : la normalisation existe pour des mots destinés au tirage, pas
pour de la prose qu'aucun tirage ne consulte.

## Alternatives envisagées

### Alternative 1 — Un champ `name` qui devient l'identité du thème

- **Description :** remplacer le nom de fichier par un champ interne comme clé de recherche.
- **Pourquoi écartée :** rouvre une règle déjà actée dans `Theme.cs` et suivie par tout le
  catalogue (`--theme`, `--register`, `--unregister`), pour un gain purement cosmétique que
  `meta.title` couvre déjà.

### Alternative 2 — Valider `version` comme un semver

- **Description :** exiger un format `X.Y.Z` et refuser le chargement sinon.
- **Pourquoi écartée :** ajoute une règle de validation à un champ qui ne sert à rien d'autre
  qu'à s'afficher ; rien dans le générateur ne compare deux versions, donc la contrainte
  protégerait contre une erreur qui ne peut avoir aucune conséquence.

## Conséquences

### Positives

- Un thème peut se documenter lui-même, sans dépendre d'un fichier ou d'un message à côté.
- Aucun thème existant n'a besoin d'être modifié : `meta` absent se comporte exactement comme
  avant.

### Négatives

- Cinq clés de plus dans le schéma, aucune vérifiée au-delà de son type — un auteur peut y écrire
  n'importe quoi sans que rien ne le lui signale.

### Risques

- `meta.title` invite à l'afficher partout où le nom de fichier l'est aujourd'hui ; un futur
  changement qui le ferait *remplacer* l'affichage du nom de fichier plutôt que s'y ajouter
  rapprocherait ce champ d'une identité qu'il n'est pas censé être.

### Actions de suivi

- Documenter le bloc là où un auteur de thème le rencontre — fait dans
  [`../writing-a-theme.md`](../writing-a-theme.md).
