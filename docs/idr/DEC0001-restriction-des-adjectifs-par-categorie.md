# DEC0001 | Restriction des adjectifs tirables par catégorie partagée avec le nom

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-19 | Accepté | Décision fondatrice, portée par le code depuis `0a7ae9e` | |

## Contexte

Les générateurs de slugs existants tirent leur adjectif dans une liste plate : n'importe quel
adjectif peut accompagner n'importe quel nom. Docker dispose de 108 adjectifs pour 236 noms,
Heroku de 91 pour 95.

Un slug est lu par un humain — nom de conteneur, de branche, de fichier de plan. Un adjectif
qui ne peut physiquement pas s'appliquer à son nom se remarque : sur une liste plate,
`thundering-moon` et `weeping-server` sont des tirages aussi probables que `focused-turing`.

`slugger` a été écrit pour couvrir deux besoins que ces générateurs ne couvrent pas, dont le
premier est de restreindre quels adjectifs peuvent accompagner quel nom à l'intérieur d'un
même thème.

Les thèmes livrés classent leurs mots de deux façons différentes : `slugger` par domaine
(joueurs, équipement, stade, technique, récompenses), `heroku` par capacité physique du nom
(`mobile`, `sonore`, `lumineux`, `vivant`, `chaleur`, `eau`).

## Décision

Dans ce contexte, nous décidons de ne rendre un adjectif tirable pour un nom que si tous deux
partagent au moins une catégorie.

## Justification

La liste plate est exactement le manque à l'origine de l'outil : couvrir ce besoin suppose un
mécanisme de restriction, et la catégorie partagée est le plus simple qui le fasse.

Le classement par capacité physique plutôt que par domaine filtre réellement, là où un
classement thématique ne filtre rien : `moon` déclaré `[lumineux, mobile]` sans `sonore` rend
`thundering-moon` intirable, alors qu'une catégorie « astronomie » l'aurait laissé passer.
`willow` déclaré `[vivant]` rend `weeping-willow` — un idiome anglais réel — possible.

Le mécanisme n'impose rien à l'auteur : les noms de catégories sont des chaînes libres, donc un
thème qui ne veut pas filtrer déclare tout dans une seule catégorie et retrouve le comportement
d'une liste plate.

## Alternatives envisagées

### Alternative 1 — Liste plate, comme Docker et Heroku

- **Description :** tout adjectif du thème accessible à tout nom, sans catégories.
- **Pourquoi écartée :** c'est précisément le manque que l'outil existe pour combler.

### Alternative 2 — Catégories par domaine thématique plutôt que par capacité

- **Description :** classer les mots par sujet (« astronomie », « météo », « réseau ») plutôt
  que par ce que le nom peut physiquement faire.
- **Pourquoi écartée :** un classement thématique ne filtre pas les associations impossibles —
  `thundering` et `moon` appartiennent tous deux au registre céleste.

## Conséquences

### Positives

- Un thème peut garantir qu'aucun tirage n'est absurde, sans énumérer les paires interdites.
- Le mécanisme est optionnel de fait : un thème sans catégories reste valide et se comporte
  comme une liste plate.
- Le tirage restant confiné à un seul thème, deux fichiers peuvent utiliser le même nom de
  catégorie sans aucun lien entre eux.

### Négatives

- Écrire un thème coûte plus cher : il faut classer les mots, pas seulement les lister.
- Le pool d'un nom n'est plus lisible dans le fichier ; il se calcule.
- Une exécution multi-thème doit choisir un thème avant de tirer, au lieu de concaténer les
  listes — tirage pondéré par effectif.

### Risques

- Un auteur qui découpe trop finement ses catégories bloque ses noms sous le seuil d'adjectifs
  accessibles ; DEC0003 le refuse au chargement plutôt que de le laisser passer.
- `common` restant accessible à tout nom, une métaphore lâche (`waning-cake`) reste tirable sur
  un nom sans capacité assignée.

### Actions de suivi

- Expliquer le classement par capacité à qui écrit un thème — fait dans
  [`../writing-a-theme.md`](../writing-a-theme.md).
