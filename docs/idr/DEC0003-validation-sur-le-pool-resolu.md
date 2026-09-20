# DEC0003 | Validation de la taille d'un thème sur le pool résolu

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-19 | Accepté | Règle non dispensable ajoutée par `2c9c273` | |

## Contexte

Un thème peut être gros sur le papier et pauvre à l'usage. Un fichier de 500 adjectifs dont 480
tiennent dans une seule catégorie laisse les noms des autres catégories avec une douzaine de
choix ; un comptage des listes brutes ne le voit pas.

La restriction par catégorie (DEC0001) rend cet écart possible : le pool réel d'un nom se
calcule, il n'est pas lisible dans le fichier.

Les précédents du domaine sont mesurables. Docker totalise 108 × 236 = 25 488 combinaisons,
Heroku 91 × 95 = 8 645, et tous deux ajoutent un suffixe numérique — Docker en cas de collision,
Heroku systématiquement.

Les trois thèmes livrés passent les seuils sans aide : `slugger` (231 noms), `heroku`
(216 noms, 178 adjectifs) et `docker` (236 noms, 187 adjectifs).

Un thème ne déclarant aucun nom faisait planter le chargement au lieu de le refuser
(`2c9c273`).

## Décision

Dans ce contexte, nous décidons de calculer les trois règles de taille minimale sur le pool
réellement résolu de chaque nom et de chaque catégorie.

## Justification

Seul le pool résolu voit le cas que la décision existe pour attraper : un nom coincé dans une
petite catégorie, invisible d'un comptage global.

Le seuil de 40 000 combinaisons par catégorie n'est pas arbitraire mais calé sur les deux
précédents mesurés : Docker et Heroku sont tous deux en dessous, et tous deux compensent par un
suffixe. C'est donc le point où un thème a besoin d'un token, et le refus dit à l'auteur ce que
ses prédécesseurs ont fait à sa place.

Nommer le nom ou la catégorie fautive est nécessaire pour que le refus soit actionnable : sur un
pool calculé, un chiffre seul ne dit pas où corriger.

Un thème sans aucun nom ne relève pas du même arbitrage qu'un thème réduit : il ne peut rien
produire, donc la règle qui le refuse ne peut pas être levée sans rendre l'outil inutilisable.

## Alternatives envisagées

### Alternative 1 — Comptage global des listes

- **Description :** compter les entrées de `adjectives` et de `nouns` et comparer aux seuils.
- **Pourquoi écartée :** accepte un fichier dont un nom n'atteint qu'une poignée d'adjectifs,
  c'est-à-dire exactement le défaut introduit par la restriction par catégorie.

### Alternative 2 — Aucune validation de taille

- **Description :** charger ce que l'auteur fournit et le laisser juger de la taille.
- **Pourquoi écartée :** un thème sous-dimensionné produit des collisions silencieuses, que
  l'auteur ne découvre qu'à l'usage.

### Alternative 3 — Seuils levables sans exception

- **Description :** `allowSmall` et `--allow-small-theme` lèvent toutes les règles, y compris
  celle qui exige au moins un nom.
- **Pourquoi écartée :** il n'y a pas d'arbitrage à assumer sur un fichier qui ne peut produire
  aucun slug.

## Conséquences

### Positives

- Un thème accepté est utilisable par construction, nom par nom, pas seulement en moyenne.
- Le refus est actionnable : il nomme le nom ou la catégorie et donne son total.
- Les trois thèmes livrés démontrent que les seuils sont atteignables sans dérogation.

### Négatives

- La validation coûte un calcul de pool par nom et par catégorie, au lieu d'un comptage.
- Le ticket d'entrée pour un thème est élevé : 100 noms et 100 adjectifs accessibles à chacun.
- Deux mécanismes de dérogation doivent coexister — le champ dans le fichier et le flag — parce
  qu'ils ne répondent pas à la même situation.

### Risques

- Les seuils peuvent s'avérer trop hauts pour un thème de niche légitime, et pousser ses auteurs
  à déclarer `allowSmall` par réflexe plutôt que par arbitrage.
- Les participes n'entrant pas dans le minimum, un thème peut passer les règles avec un pool de
  participes très pauvre.

### Actions de suivi

- Donner à l'auteur la formule et la conduite à tenir sous les seuils — fait dans
  [`../writing-a-theme.md`](../writing-a-theme.md).
- Exécuter les règles réelles sur les fichiers réels plutôt que sur des fixtures — fait dans
  `EmbeddedThemeCatalogTests`.
