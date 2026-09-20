# ADR 0002 — `common` est un socle partagé, pas un repli

Statut : **accepté**, corrige la lecture d'origine · 2026-09-20

## Contexte

La spécification initiale disait deux choses : qu'une catégorie est une chaîne libre sans
signification pour le code, et qu'un nom sans catégorie n'atteint aucun adjectif. Passer les
règles réelles sur les fichiers livrés a montré que ce ne pouvait pas être ce qui était voulu.

Les 236 noms de `docker` et 103 de ceux de `heroku` ne déclarent aucune catégorie, et aucun des
deux fichiers n'écrit `common` sur un nom. La lecture littérale donne donc à chacun d'eux un pool
vide et refuse les deux thèmes — alors que la même spécification affirmait qu'ils passent les
trois règles par eux-mêmes.

## Décision

**Tout nom atteint `common`, en plus de ce qu'il déclare.** Un nom sans catégorie atteint
`common` ; un nom qui en déclare atteint les siennes **et** `common`.

## Conséquences

- **Aucune catégorie n'a à franchir seule le seuil de 100.** Le thème `slugger` en dépend
  directement : cinq catégories de 45 adjectifs chacune et `common` à 60 — seule l'addition
  passe (mesuré sur le fichier livré).
- **C'est le seul écart** au principe « une catégorie est une chaîne opaque pour le code ».
  Un nom de catégorie reste libre ; `common` est le seul qui signifie quelque chose.
- **Une catégorie déclarée uniquement dans `participles` est valide.** C'est ainsi que `heroku`
  classe ses noms par capacité physique pour piloter ses participes alors que ses adjectifs
  tiennent dans le seul `common`.
- **Pinné contre le fichier réel** par `ThemeResolverTests`, précisément pour qu'une correction
  de ce genre se remarque au lieu de dériver en silence.

Commits : `519c4f6`, `777fff6`.
