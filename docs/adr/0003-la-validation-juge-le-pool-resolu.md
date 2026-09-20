# ADR 0003 — La validation juge le pool résolu, jamais des comptages bruts

Statut : **accepté** · 2026-09-20

## Contexte

Un thème peut être gros sur le papier et inutilisable à l'usage. Un fichier de 500 adjectifs
dont 480 tiennent dans une seule catégorie laisse les noms des autres catégories avec une
douzaine de choix. Un comptage global ne voit rien.

## Décision

Trois règles, toutes calculées sur le pool réellement résolu (ADR 0001) :

1. Moins de 100 noms distincts dans le fichier → refus.
2. Un nom dont `|pool(noun)| < 100` → refus, **en nommant ce nom** et la taille de son pool.
3. Une catégorie dont `combos(C) < 40 000` → refus, **en nommant la catégorie** et son total.

Le seuil de 40 000 n'est pas arbitraire : Docker (108 × 236 = 25 488) et Heroku (91 × 95 =
8 645) sont tous deux en dessous, et tous deux compensent effectivement par un suffixe. C'est
le point où un thème a besoin d'un token.

## Conséquences

- **Un refus nomme son sujet.** Un chiffre seul ne dit pas à un auteur quoi corriger.
- **`allowSmall` est un arbitrage assumé, pas un contournement.** Le champ dans le fichier vaut
  pour toujours, `--allow-small-theme` pour une exécution. Les deux lèvent les trois seuils.
- **Une règle n'est jamais dispensable : un thème doit déclarer au moins un nom.** La différence
  entre un thème réduit et un fichier qui ne peut rien produire (`2c9c273`, qui plantait avant
  de refuser).
- **Les participes n'entrent pas dans le minimum.** Un nom franchit la barre des 100 adjectifs
  qu'il ait ou non des participes associés.
- Toute règle future se calcule de la même façon, sur le pool résolu.
