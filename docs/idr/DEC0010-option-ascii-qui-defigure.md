# DEC0010 | Adoption d'une option ASCII qui défigure plutôt que de renoncer

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-20 | Accepté | Complète DEC0009, qui reste en vigueur | |

## Contexte

DEC0009 plie la marque diacritique d'une lettre qui en porte une. Mesuré : `François Sagat`
devient `francois-sagat`.

Ce pliage repose sur la décomposition Unicode, qui ne couvre pas tout. Mesuré : `Søren Straße`
ressort inchangé sous `--fold-accents`, parce que ni `ø` ni `ß` n'ont de décomposition
canonique ; un mot en hangul ou en cyrillique ressort inchangé lui aussi.

L'option nomme donc un mécanisme, et un mécanisme peut échouer sans le dire : une exécution qui
demande `--fold-accents` pour satisfaire une destination strictement ASCII peut en obtenir un
slug qui ne l'est pas.

Certaines destinations n'acceptent rien d'autre que l'ASCII — une étiquette DNS, certains noms
de conteneurs, certains systèmes de fichiers — et n'offrent pas de dégradation partielle.

## Décision

Dans ce contexte, nous décidons d'offrir une option qui garantit un slug ASCII en remplaçant par
une frontière de mot tout caractère qui ne l'est pas après pliage.

## Justification

Promettre le résultat plutôt que le mécanisme est la seule façon qu'une exécution obtienne la
garantie dont sa destination a besoin, puisqu'un pliage seul ne peut pas la donner.

Défigurer est préférable à échouer dans ce cadre précis : la destination refuserait de toute
façon le mot intact, donc un mot abîmé mais utilisable vaut mieux qu'un slug rejeté plus loin.

Remplacer par une frontière plutôt que supprimer garde la disparition visible dans le texte —
`stra e` montre qu'il manque quelque chose là où `strae` le cache — ce qui aide qui cherche
pourquoi son slug a changé.

Passer le résultat par la forme canonique évite de dupliquer la réduction des suites et le
rognage des extrémités déjà écrits pour DEC0008.

Faire que l'option implique le pliage évite d'avoir à les combiner, donc une exécution n'a
jamais à connaître les deux.

Écarter un segment devenu vide plutôt que le joindre est ce qui sépare un slug défiguré d'un
slug troué.

## Alternatives envisagées

### Alternative 1 — Se contenter du pliage de DEC0009

- **Description :** laisser `--fold-accents` seul, et accepter qu'il ne garantisse rien.
- **Pourquoi écartée :** ne répond pas au cas qui a motivé la demande, celui d'une destination
  qui n'accepte que l'ASCII.

### Alternative 2 — Supprimer le caractère non ASCII au lieu de le remplacer

- **Description :** `straße` donnerait `strae` plutôt que `stra e`.
- **Pourquoi écartée :** produit un mot plausible mais faux, là où une frontière montre qu'il
  manque quelque chose.

### Alternative 3 — Translittérer les alphabets non latins

- **Description :** transcrire le cyrillique, le grec ou le hangul vers l'alphabet latin.
- **Pourquoi écartée :** demande une table par alphabet et des choix de transcription qui
  n'appartiennent pas à un générateur de slugs.

## Conséquences

### Positives

- Une exécution peut garantir un slug ASCII, ce qu'aucune option ne permettait.
- L'option implique le pliage, donc elle se suffit à elle-même.
- Un segment qui ne survit pas est retiré, sans laisser de séparateur en trop.
- Elle rejoint la chaîne de DEC0004 comme les autres et se sauvegarde par `--init`.

### Négatives

- Un mot peut ressortir méconnaissable : `Søren Straße` devient `s-ren-stra-e`.
- Un thème écrit dans un alphabet non latin perd ses noms entièrement.
- **Un slug peut être vide** quand aucun segment ne survit, ce qui est mesuré et pinné mais
  reste un résultat inutilisable pour l'appelant.
- Deux options voisines coexistent, et il faut savoir laquelle répond à quelle question.

### Risques

- Une exécution qui active l'option sur un thème mal connu peut produire des slugs vides ou
  méconnaissables sans que rien ne la prévienne, la combinaison n'étant vérifiable ni au
  chargement du thème ni à la lecture de la ligne de commande.
- Deux valeurs distinctes peuvent se réduire au même slug, sans qu'aucune règle de taille,
  calculée avant, ne le voie.

### Actions de suivi

- Pinner le cas où rien ne survit, pour qu'il soit un résultat connu et non une surprise — fait
  dans `SlugFormatterTests`.
- Montrer les trois modes côte à côte là où un auteur les rencontre — fait dans
  [`../writing-a-theme.md`](../writing-a-theme.md).
- Décider, si le cas se présente, s'il faut avertir plutôt que rendre un slug vide.
