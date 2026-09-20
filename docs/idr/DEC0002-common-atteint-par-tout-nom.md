# DEC0002 | Adoption de « common » comme socle atteint par tout nom

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-19 | Accepté | Corrige la règle écrite, démentie par les fichiers livrés ; `519c4f6`, `777fff6` | |

## Contexte

La règle initialement écrite disait deux choses : qu'un nom sans catégorie n'atteint aucun
adjectif, et que `common` est une convention de nommage sans statut dans le code.

Les fichiers livrés la démentent. Les 236 noms de `docker` et 103 de ceux de `heroku` ne
déclarent aucune catégorie, et aucun des deux fichiers n'écrit `common` sur un nom. Sous la
règle littérale, chacun de ces noms résout un pool vide et les deux thèmes sont refusés au
chargement — alors que le même document affirmait qu'ils passent les trois règles de taille par
eux-mêmes, en citant 236 noms contre 187 adjectifs.

Le thème `slugger` le montre depuis l'autre bord : ses cinq catégories comptent 45 adjectifs
chacune et `common` en compte 60, quand le seuil par nom est de 100.

`heroku` classe ses noms par capacité physique pour piloter ses participes, alors que ses
adjectifs tiennent dans le seul `common` : ses catégories n'existent donc que du côté
`participles`.

Un commit antérieur (`ac3531d`) avait supprimé les catégories vides écrites sur 339 noms, au
motif qu'écrire l'évidence sur chaque nom coûtait 41 % de la taille de `docker.json`.

## Décision

Dans ce contexte, nous décidons que tout nom atteint la catégorie `common` en plus de celles
qu'il déclare.

## Justification

C'est la seule lecture sous laquelle les chiffres annoncés pour `docker` et `heroku` sont
vrais : 236 noms contre 187 adjectifs suppose que chaque nom atteint les 187, donc qu'il
atteint `common` sans le déclarer.

C'est aussi la seule sous laquelle `slugger` se charge : aucune de ses catégories n'atteignant
100 adjectifs, seule l'addition avec `common` franchit le seuil.

Accepter qu'une catégorie déclarée uniquement dans `participles` satisfasse la règle de
cohérence découle du même constat : c'est la structure réelle de `heroku`.

La décision ne coûte rien à l'auteur puisqu'elle lui évite d'écrire `common` sur chaque nom,
dans la continuité de la suppression des catégories vides.

## Alternatives envisagées

### Alternative 1 — Conserver la lecture littérale

- **Description :** un nom sans catégorie n'atteint aucun adjectif ; `common` n'a aucun statut.
- **Pourquoi écartée :** refuse au chargement `docker` et `heroku` tels qu'ils sont livrés.

### Alternative 2 — `common` comme repli pour les seuls noms sans catégorie

- **Description :** un nom qui ne déclare rien atteint `common` ; un nom qui déclare des
  catégories n'atteint que les siennes.
- **Pourquoi écartée :** `slugger` ne se chargerait pas — un de ses noms catégorisés
  n'atteindrait que 45 adjectifs, sous le seuil de 100.

### Alternative 3 — Écrire `common` explicitement sur chaque nom

- **Description :** garder la règle littérale et ajouter `common` aux `categories` de tous les
  noms des fichiers livrés.
- **Pourquoi écartée :** répète l'évidence sur plusieurs centaines de lignes, dans le sens
  inverse de la suppression des catégories vides déjà actée.

## Conséquences

### Positives

- Aucune catégorie n'a à franchir seule le seuil de 100 adjectifs : c'est l'addition avec
  `common` qui compte, ce qui rend un thème catégorisé écrivable.
- Un thème n'a jamais à écrire `common` sur un nom.
- Une catégorie déclarée uniquement dans `participles` devient valide, ce qui autorise la
  structure de `heroku`.

### Négatives

- C'est le seul écart au principe « un nom de catégorie est une chaîne opaque pour le code » :
  `common` signifie quelque chose, les autres non.
- Un auteur doit connaître ce statut particulier pour dimensionner ses catégories.

### Risques

- `common` peut devenir un fourre-tout : un auteur qui y déverse tout son vocabulaire annule le
  filtrage de DEC0001 tout en passant la validation.

### Actions de suivi

- Pinner la lecture contre les fichiers réellement livrés plutôt que contre une fixture — fait
  dans `ThemeResolverTests`, précisément pour qu'une correction de ce genre se remarque.
- Énoncer le statut de `common` au premier plan du guide d'auteur — fait dans
  [`../writing-a-theme.md`](../writing-a-theme.md).
