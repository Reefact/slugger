# DEC0009 | Pliage des accents décidé par l'exécution, jamais par le thème

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-20 | Accepté | Complète DEC0008, qui reste en vigueur | |

## Contexte

DEC0008 conserve toute lettre, accentuée comprise : `François Sagat` produit
`françois-sagat` et `risqué` reste `risqué`. Un thème écrit donc ses mots tels qu'ils
s'écrivent.

Le thème ne sait pas où ses slugs vont servir. Un même fichier peut alimenter un nom de branche,
une étiquette DNS, un identifiant de conteneur ou une URL, et certaines de ces destinations
n'acceptent pas un caractère hors ASCII.

Mesuré sur un thème personnalisé réel de 887 valeurs : deux lettres accentuées seulement, `ç`
dans un nom et `é` dans un adjectif, mais elles suffisent à rendre les slugs concernés
inutilisables là où l'ASCII est imposé.

La décomposition Unicode sépare une lettre latine accentuée de sa marque diacritique. Elle ne le
fait pas pour `ß`, `ø` ni `œ`, qui n'ont pas de décomposition canonique, ni pour les alphabets
non latins.

Mesuré : le hangul se décompose en jamo, dont aucun n'est une marque diacritique — `한글` passe
de deux points de code à six si le résultat n'est pas recomposé.

DEC0004 établit une chaîne de priorité unique, sans traitement particulier par option, et DEC0005
place au tirage tout ce qui dépend d'une option.

## Décision

Dans ce contexte, nous décidons d'offrir une option qui plie au formatage la marque diacritique
de toute lettre qui en porte une.

## Justification

Placer le levier du côté de l'exécution répond au fait que la contrainte vient de la destination
du slug, que le thème ne connaît pas : le thème garde son orthographe, l'exécution impose la
sienne.

Le formatage est le bon moment parce que le pliage dépend d'une option, et qu'une option varie
d'une exécution à l'autre alors qu'un thème n'est écrit qu'une fois — c'est exactement le
critère de DEC0005.

L'option rejoint la chaîne de DEC0004 sans exception, ce qui la rend persistable par `--init`
comme n'importe quelle autre.

Le défaut reste le non-pliage, donc aucun slug existant ne change et DEC0008 n'est pas touché.

Recomposer après le pliage est nécessaire et pas seulement cosmétique : sans cela, un alphabet
syllabique serait rendu décomposé, c'est-à-dire découpé, par une option qui ne promet que de
retirer des diacritiques.

Nommer l'option pour ce qu'elle fait plutôt que pour un résultat ASCII est la seule formulation
honnête, puisque ce qui ne se décompose pas n'est pas plié.

## Alternatives envisagées

### Alternative 1 — Ne rien faire

- **Description :** laisser l'auteur du thème écrire ses valeurs sans accents s'il veut des slugs
  ASCII.
- **Pourquoi écartée :** oblige à dégrader le thème pour satisfaire une seule destination, alors
  que les autres usages du même fichier n'ont rien demandé.

### Alternative 2 — Plier par défaut, pour tous

- **Description :** retirer systématiquement les diacritiques, comme le font la plupart des
  générateurs de slugs.
- **Pourquoi écartée :** renverse la décision de conserver les accents, alors que le besoin
  vient d'une destination particulière et non de l'outil.

### Alternative 3 — Une liste blanche de caractères déclarée par le thème

- **Description :** le thème déclare les caractères qu'il autorise dans un slug, tout autre étant
  remplacé ou refusé au chargement.
- **Pourquoi écartée :** place la décision chez l'auteur du thème alors que la contrainte
  appartient au consommateur, et coûte deux réglages et une validation là où un pliage
  optionnel suffit. Reste possible plus tard sans rien invalider.

## Conséquences

### Positives

- Un même thème sert une destination qui accepte les accents et une qui les refuse, sans être
  modifié.
- Aucun slug existant ne change : le défaut ne plie pas.
- L'option se sauvegarde par `--init` comme les autres, donc une machine dont tous les usages
  exigent l'ASCII le règle une fois.
- Le pliage précède la casse, donc `camel` en bénéficie aussi.

### Négatives

- L'option ne garantit pas un slug ASCII, ce qui demande d'être dit plutôt que supposé.
- `ß`, `ø`, `œ` et les alphabets non latins ne sont pas pliés, alors qu'un utilisateur peut
  attendre l'inverse de la part d'une option d'apparence globale.
- Le pliage est payé à chaque tirage plutôt qu'une fois au chargement.

### Risques

- Un utilisateur peut croire l'option suffisante pour une destination strictement ASCII et n'en
  découvrir la limite qu'en production, sur un mot non latin.
- Deux valeurs distinctes d'un thème peuvent se plier vers le même slug — `côte` et `cote` — sans
  qu'aucune règle de taille, calculée avant, ne le voie.

### Actions de suivi

- Dire la limite là où l'auteur la rencontre — fait dans
  [`../writing-a-theme.md`](../writing-a-theme.md).
- Pinner la recomposition par un cas syllabique, faute de quoi rien ne la protège — fait dans
  `SlugFormatterTests`.
