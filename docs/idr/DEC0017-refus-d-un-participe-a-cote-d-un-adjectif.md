# DEC0017 | Refus d'un participe à côté d'un adjectif donné, retiré du pool avant le tirage

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-20 | Accepté | | |

## Contexte

Sous `both`, deux mots précèdent le nom : un adjectif, puis un participe, tirés dans cet ordre.
C'est le seul mode où deux mots se retrouvent côte à côte (DEC0016).

Un thème dispose déjà de deux filtres, et aucun ne porte sur un couple : les catégories
restreignent ce qu'un nom atteint (DEC0001), et `except` retire des mots nom par nom (DEC0011).
Un auteur qui ne veut pas d'une association donnée ne peut donc que retirer un mot du thème
entier, ou l'interdire à un nom.

DEC0013 a rencontré le cas voisin — le même mot tiré des deux côtés — et l'a absorbé : le mot est
écrit une fois. Son compte rendu consigne pourquoi il n'a pas re-tiré : « re-tirer jusqu'à ce
qu'ils diffèrent aurait été non borné sur un pool d'un mot, et aurait rendu un tirage scripté
imprévisible ». La suite s'appuie effectivement sur une source de tirage scriptée, qui échoue
quand la valeur ne correspond pas à la borne demandée.

DEC0016 impose sous `both` un plancher de 20 participes par nom, mesuré sur `partPool(noun)`.

DEC0011 lit ses exclusions à travers la même normalisation que les listes de mots, au motif
qu'une exclusion qui raterait sur la casse échouerait ouvert — et qu'une liste de sécurité qui
échoue ouvert est pire que pas de liste du tout.

Le rapport d'analyse (DEC0014) affiche le pire cas de chaque plancher, nom nommé.

## Décision

Dans ce contexte, nous décidons qu'un adjectif peut refuser nommément des participes, retirés du
pool avant que le participe ne soit tiré.

## Justification

Retirer avant le tirage est le geste que le thème fait déjà deux fois : une catégorie restreint
le pool, une exclusion le réduit. La paire est le même geste une dimension plus loin, l'adjectif
déjà tiré servant de clé au lieu du nom. Le format gagne une capacité, pas un mécanisme.

L'ordre du tirage rend ce retrait possible sans rien réorganiser : l'adjectif est connu quand le
participe est tiré. Le nombre de tirages reste donc fixe et le participe reste uniforme sur ce
qui lui est laissé — deux propriétés que le re-tirage écarté par DEC0013 aurait perdues toutes
les deux.

Le plancher de DEC0016 devait suivre, sous peine de mentir : un nom qui atteint 40 participes et
un adjectif qui en refuse 35 se lisent comme confortables et ne le sont pas. Le mesurer sur le
pire couple (nom, adjectif) est la lecture de DEC0003 — valider sur le pool réellement résolu —
appliquée au pool que le tirage voit vraiment.

Normaliser les deux moitiés de la paire comme le reste des mots suit le motif de DEC0011 : une
paire qui raterait sur la casse échouerait ouvert, et l'auteur croirait protégée une association
que tous les tirages atteignent encore.

## Alternatives envisagées

### Alternative 1 — Statu quo, laisser l'auteur choisir ses mots

- **Description :** ne rien ajouter au format, à charge pour l'auteur d'écarter du thème les mots
  qui produisent de mauvais couples.
- **Pourquoi écartée :** ne permet pas d'éviter un couple sans retirer un mot du thème entier,
  alors que le mot est bon partout ailleurs.

### Alternative 2 — Re-tirer le participe jusqu'à obtenir un couple acceptable

- **Description :** détecter le couple refusé après le tirage et recommencer.
- **Pourquoi écartée :** DEC0013 a déjà écarté ce raisonnement — non borné sur un pool d'un mot,
  et le nombre de tirages devient variable, ce dont la suite scriptée dépend.

### Alternative 3 — Absorber le couple comme une collision, en écrivant l'adjectif seul

- **Description :** garder les deux tirages et n'écrire que l'adjectif quand le couple est refusé,
  exactement comme DEC0013 traite un mot tiré deux fois.
- **Pourquoi écartée :** produit des slugs à deux segments à un taux que rien ne montre, et
  d'autant plus souvent que l'auteur ajoute des paires.

### Alternative 4 — Des groupes d'incompatibilité plutôt que des paires

- **Description :** déclarer des ensembles — « chaud », « froid » — et interdire leurs
  croisements.
- **Pourquoi écartée :** beaucoup plus à valider et à expliquer pour un besoin que les paires
  couvrent ; à reprendre si les paires deviennent nombreuses.

## Conséquences

### Positives

- Le nombre de tirages reste fixe et le participe reste uniforme sur ce qui lui est laissé.
- Une paire est déclarée une fois pour tout le thème, là où `except` se répète nom par nom.
- Le plancher sous `both` se mesure sur le pire couple, donc il ne surestime plus la variété.
- Une paire écrite à l'envers est refusée en le disant, plutôt que de ne jamais se déclencher.
- Une paire qu'aucun nom ne peut réunir est signalée sans rien refuser, comme une catégorie que
  personne ne porte.

### Négatives

- **Troisième endroit où un mot peut disparaître d'un tirage** — les catégories, `except`, la
  paire. Un auteur qui cherche pourquoi un mot ne sort jamais a trois pistes au lieu de deux.
- Le plancher se mesure sur les couples (nom, adjectif nommé dans une paire) et non plus sur le
  nom seul, ce qui fait dépendre un refus d'une section que l'auteur ne regardait pas.
- Une paire n'a d'effet que sous `both` : déclarée sous un autre mode, elle est inerte.
- Quand une paire vide le pool d'un nom, le deuxième tirage n'est pas fait — le nombre de tirages
  dépend donc de l'adjectif sorti, ce qui n'était vrai jusqu'ici que d'un nom sans participes.

### Risques

- Un auteur qui ajoute des paires au fil de l'eau peut faire refuser un thème accepté, loin de la
  ligne qu'il vient d'éditer.
- Le participe n'est plus uniforme sur `partPool(noun)` mais sur ce que l'adjectif lui laisse :
  un participe refusé par beaucoup d'adjectifs devient rare sans qu'aucun compte le dise.

### Actions de suivi

- Documenter la section dans `docs/writing-a-theme.md`, à côté de `except`.
- Reprendre l'alternative 4 — les groupes — le jour où un thème livré dépassera la dizaine de
  paires.
