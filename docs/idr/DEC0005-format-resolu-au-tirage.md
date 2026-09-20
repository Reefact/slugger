# DEC0005 | Résolution du format au tirage plutôt qu'au chargement

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-19 | Accepté | Écart assumé avec la spécification d'origine ; `89448e3` | |

## Contexte

La normalisation d'une valeur de thème compte quatre étapes : retirer les espaces de bordure,
réduire les espaces multiples, passer en minuscule, puis remplacer les espaces restants par le
séparateur. La spécification d'origine appliquait les quatre à la lecture du JSON.

Le séparateur n'est pas connu à ce moment-là. Il vient de la chaîne de priorité (DEC0004), donc
de la ligne de commande, du thème tiré ou de la config — et en multi-thème avec `--mimic-style`,
chaque thème tiré applique le sien, si bien qu'il change d'un tirage à l'autre.

Les valeurs composées sont courantes dans les thèmes livrés : `slugger.json` en compte 53
(`Oracle Park`, `Babe Ruth`).

## Décision

Dans ce contexte, nous décidons d'appliquer au formatage, et non au chargement, le remplacement
des espaces internes d'une valeur composée.

## Justification

Appliquer l'étape au chargement fige un séparateur pour toute l'exécution, ce qui donne un
résultat faux dès qu'une exécution multi-thème mime le style de chaque thème tiré.

Le découpage retenu donne le même résultat qu'au chargement pour un thème unique et le résultat
correct pour plusieurs, donc il ne coûte rien au cas simple.

Il place au même endroit tout ce qui dépend des options de format, ce qui rend un nouveau levier
local : `--word-sep` (`96a83e7`), qui donne aux mots d'une valeur composée un séparateur distinct
de celui des segments, n'a touché ni le chargement ni le modèle de thème.

## Alternatives envisagées

### Alternative 1 — Les quatre étapes au chargement

- **Description :** appliquer la normalisation complète à la lecture du JSON, comme la
  spécification d'origine le prévoyait.
- **Pourquoi écartée :** choisit un séparateur unique pour toute l'exécution, ce qui est faux en
  multi-thème mimé.

### Alternative 2 — Recharger le thème à chaque tirage

- **Description :** garder les quatre étapes au chargement et relire le fichier quand le
  séparateur change.
- **Pourquoi écartée :** paie une relecture par tirage pour obtenir ce qu'un remplacement au
  formatage donne directement.

## Conséquences

### Positives

- Le multi-thème mimé produit des slugs corrects, chaque tirage appliquant le style de son
  thème.
- Un levier de format nouveau est local au formateur et à la chaîne d'options.
- Une valeur reste en mémoire sous sa forme lisible, indépendante de tout séparateur.

### Négatives

- La normalisation est coupée en deux endroits, et cette coupure doit être connue de qui touche
  à l'un ou à l'autre.
- L'étape 4 est payée à chaque tirage plutôt qu'une fois au chargement.

### Risques

- La coupure n'étant pas visible depuis `WordNormalizer` seul, un développeur peut la croire
  incomplète et y réintégrer l'étape 4, ce qui rétablirait silencieusement le défaut en
  multi-thème.

### Actions de suivi

- Expliquer la coupure à l'endroit où elle surprend, c'est-à-dire dans `WordNormalizer` lui-même
  — fait, et pinné par `WordNormalizerTests` et `SlugFormatterTests`.
