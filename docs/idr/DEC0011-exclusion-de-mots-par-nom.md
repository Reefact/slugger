# DEC0011 | Refus d'un mot par le nom lui-même, en plus du filtrage par catégories

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-20 | Accepté | Complète DEC0001, qui reste en vigueur | |

## Contexte

DEC0001 restreint les adjectifs tirables pour un nom à ceux qui partagent une catégorie avec lui.
Le critère est la plausibilité : `thundering-moon` est écarté parce qu'une lune ne tonne pas.

Une autre famille de tirages est indésirable sans être implausible. Docker en porte le cas connu :
son générateur contient un refus écrit en dur de `boring_wozniak`, commenté « Steve Wozniak is not
boring ». L'adjectif décrit parfaitement une personne ; c'est l'association qui blesse.

Docker n'a pas d'autre recours parce qu'il tire dans une liste plate. Les catégories en offrent
un : il suffirait de ne pas mettre `boring` dans une catégorie que les personnes atteignent. Mais
cela retire le mot à *toutes* les personnes du thème, alors que le besoin exprimé est de le
garder disponible et d'excepter un nom.

Le résidu ne forme pas de classe : un nom qui veut dire autre chose dans une autre langue, une
expression accidentelle, un adjectif anodin qui tombe mal sur ce nom précis.

Docker n'a pas anticipé `boring_wozniak`, il l'a découvert après publication.

Le mot `boring` est à la fois un adjectif et un participe présent ; un thème peut légitimement le
déclarer dans les deux sections.

DEC0003 calcule le plancher des 100 adjectifs sur le pool réellement résolu de chaque nom.

DEC0006 refuse un thème en nommant le sujet de chaque plainte, et le validateur refuse déjà un
nom qui référence une catégorie non déclarée.

## Décision

Dans ce contexte, nous décidons qu'un nom peut déclarer des mots qui lui sont refusés, soustraits
de ses pools après résolution de ses catégories.

## Justification

Porter l'exception sur le nom répond au fait que le résidu ne forme pas de classe : ce qui est
vrai de Wozniak et d'aucune autre personne appartient à l'entrée de Wozniak.

La soustraction après résolution laisse le mot disponible partout ailleurs, ce qui est la
différence exacte entre cette décision et le simple déplacement du mot dans une autre catégorie.

Soustraire des deux pools découle de ce qui rend un mot mal venu : l'association, pas la fonction
grammaticale. Un mot présent dans les deux sections serait autrement à exclure deux fois, et
l'oubli de la seconde serait silencieux.

Refuser une exclusion qui ne correspond à aucun mot du thème est ce qui empêche la protection
d'échouer en s'ouvrant : une faute de frappe laisserait un nom qui paraît protégé sans l'être.
Le validateur applique déjà ce raisonnement aux catégories non déclarées, et le rapport groupé
l'accueille sans rien inventer.

Le plancher de DEC0003 borne l'exclusion sans règle supplémentaire, puisqu'il est calculé après
soustraction : un auteur qui vide un nom fait refuser son thème.

Le fait que le besoin se découvre après publication plaide pour une ligne à ajouter dans une
entrée existante plutôt que pour une réorganisation des catégories.

## Alternatives envisagées

### Alternative 1 — Déplacer le mot dans une catégorie que le nom n'atteint pas

- **Description :** ranger `boring` hors des catégories que les personnes déclarent, comme le
  mécanisme de DEC0001 le permet déjà.
- **Pourquoi écartée :** retire le mot à toutes les personnes du thème, alors que le besoin est de
  le garder disponible et d'excepter un nom.

### Alternative 2 — Une liste globale de paires interdites

- **Description :** un tableau au premier niveau du fichier énumérant les couples mot/nom refusés.
- **Pourquoi écartée :** vit à côté du calcul des pools au lieu d'en faire partie, donc n'hérite
  pas du plancher qui borne l'exclusion, et éloigne le fait du nom qu'il concerne.

### Alternative 3 — Deux listes séparées, adjectifs et participes

- **Description :** `exceptAdjectives` et `exceptParticiples`, chacune visant sa section.
- **Pourquoi écartée :** oblige à écrire deux fois un mot présent dans les deux sections, et
  l'oubli de la seconde ne se voit pas.

## Conséquences

### Positives

- Un thème exprime lui-même ce que Docker code en dur, sans que le moteur connaisse un seul nom.
- Le mot reste disponible pour tous les autres noms.
- Une faute de frappe dans une liste de sûreté devient un refus au chargement.
- L'exclusion est bornée par le plancher existant, sans règle nouvelle.
- Une exclusion s'ajoute en une ligne, ce que réclame un besoin qui se découvre après coup.

### Négatives

- Écarter un mot de toutes les personnes d'un thème demande de le répéter sur chaque nom, là où
  une catégorie le ferait d'un trait.
- Le modèle public `Noun` gagne un membre, donc la surface publiée grandit (DEC0007).
- Un auteur doit savoir que l'exclusion vise le mot et non la section.

### Risques

- La liste peut donner un faux sentiment de couverture : elle ne protège que des paires déjà
  identifiées, et rien ne signale celles qui restent.
- Les participes n'ayant pas de plancher propre, une exclusion peut vider le pool de participes
  d'un nom sans qu'aucune règle ne le voie ; le nom retombe alors silencieusement sur l'adjectif
  seul.

### Actions de suivi

- Énoncer la règle et le piège de la faute de frappe là où l'auteur les rencontre — fait dans
  [`../writing-a-theme.md`](../writing-a-theme.md).
- Prévoir, pour un catalogue public, le chemin par lequel un signalement arrive — l'outil offre
  désormais le champ, pas la modération.
