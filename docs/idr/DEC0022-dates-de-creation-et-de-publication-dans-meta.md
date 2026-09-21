# DEC0022 | Dates de création et de publication dans `meta`

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-21 | Accepté | | |

## Contexte

DEC0021 a ouvert `meta` avec cinq clés — `title`, `description`, `version`, `author`, `source` —
toutes descriptives, jamais consultées par la génération. `git log` répond « quand » pour qui a
le dépôt sous la main ; un thème copié ailleurs, ou partagé comme simple fichier, perd cette
réponse en même temps que l'historique.

`version` existe déjà dans `meta` sans jamais être comparée ni interprétée (DEC0021) : un
précédent direct pour une valeur qui documente sans jamais peser sur le chargement.

## Décision

Dans ce contexte, nous décidons d'ajouter deux clés à `meta` : `createdAt`, la première écriture
du thème, et `publishedAt`, la date à laquelle sa `version` actuelle a été publiée. Les deux sont
optionnelles, lues comme une chaîne quelconque, jamais parsées en date ni comparées.

## Justification

Rester une chaîne libre plutôt qu'un format imposé suit exactement le choix déjà fait pour
`version` par DEC0021 : aucune des deux clés ne pèse sur le chargement, donc leur exactitude
n'a de conséquence que pour qui les lit, pas pour l'outil. Imposer ISO 8601 protégerait contre
une faute de frappe qu'aucun mécanisme ne peut de toute façon exploiter.

Séparer `createdAt` de `publishedAt` reconnaît que les deux répondent à des questions
différentes : l'un date le thème, l'autre sa version actuelle. Un thème dont le contenu n'a pas
changé depuis sa création aura les deux identiques ; un thème mis à jour verra `publishedAt`
avancer seul, ce qu'une clé unique ne pourrait pas distinguer.

## Alternatives envisagées

### Alternative 1 — Une seule clé `date`

- **Description :** une clé générique, sans préciser de quoi elle date.
- **Pourquoi écartée :** ambiguë dès qu'un thème est mis à jour - la date change-t-elle avec le
  contenu, ou reste-t-elle celle de la création ? Deux clés nommées évitent la question.

### Alternative 2 — Un format de date imposé et validé

- **Description :** exiger `AAAA-MM-JJ` et refuser le chargement sinon.
- **Pourquoi écartée :** même raisonnement que DEC0021 pour `version` - une validation qui ne
  protège rien que l'affichage n'a pas sa place dans le chargement.

## Conséquences

### Positives

- Un thème copié hors du dépôt garde de quoi répondre « depuis quand » sans son historique git.
- Le même thème peut distinguer son âge de celui de sa version actuelle.

### Négatives

- Deux clés de plus, aucune vérifiée au-delà de son type : rien n'empêche `publishedAt`
  de précéder `createdAt` dans un fichier mal renseigné, et rien ne le signalera.

### Actions de suivi

- Documenter les deux clés là où `meta` est déjà documenté - fait dans
  [`../writing-a-theme.md`](../writing-a-theme.md).
