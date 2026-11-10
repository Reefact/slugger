# DEC0028 | Un mot déclaré dans les deux sections n'est jamais suivi de lui-même, retiré avant le tirage

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-11-10 | Accepté | Remplace la partie « n'en écrire qu'une occurrence » de DEC0013 ; la tolérance et le signalement restent | |

## Contexte

DEC0013 accepte qu'un mot soit déclaré à la fois adjectif et participe, le signale à
l'enregistrement, et absorbe le tirage qui le rencontre deux fois : le slug ne l'écrit qu'une
fois. Elle écarte le re-tirage, non borné sur un pool d'un seul mot et dont la consommation de la
source aléatoire dépendrait du résultat.

DEC0013 mesurait qu'aucun thème n'était concerné. Ce n'est plus vrai : dans `themes/`, `flowers`
déclare 18 mots dans les deux sections, `cocktails` 5 et `synthesizers` 3. Le golden master de
`flowers` contient un tirage absorbé, `scrambling_wisteria`, là où le thème promettait trois
segments.

Le calcul du slug le plus long — celui de `--analyze` et celui qui vérifie une promesse
`maxLength` (DEC0018) — combine un adjectif et le participe le plus large qu'il laisse au nom,
sans écarter le mot lui-même. Avec `mellow` seul dans les deux sections, il mesure
`mellow-mellow-moon`, 18 caractères, qu'aucun tirage ne produit ; une promesse `threeWords` de 15
est refusée au chargement alors que tout slug tiré en fait 11.

DEC0017 a introduit le refus d'un participe à côté d'un adjectif, retiré du pool avant le tirage.
Le resolver porte ces refus, et tout ce qui lit le pool après un adjectif — le générateur, les
planchers, l'analyse, la promesse — les voit sans les recopier.

## Décision

Dans ce contexte, nous décidons qu'un mot déclaré dans les deux sections refuse de se suivre
lui-même, comme une incompatibilité que le thème n'a pas besoin d'écrire, de sorte que le
participe est tiré parmi les autres et qu'un nom qui n'en a pas d'autre garde l'adjectif seul.

## Justification

Retirer le mot avant le tirage donne exactement la distribution d'un re-tirage jusqu'à obtenir un
autre mot — chaque autre participe a la même chance — en un seul tirage. Les deux objections que
DEC0013 opposait au re-tirage tombent : le tirage est borné, et la source aléatoire est consommée
de la même façon quel que soit le résultat.

Le re-tirage ne serait borné qu'en pratique, et seulement là où les planchers tiennent : sous
`--allow-small-theme` ou sous une limite de longueur (DEC0018), le pool après un adjectif peut se
réduire à ce seul mot, et la boucle ne finirait pas. Le retrait laisse alors un pool vide, et la
dégradation déjà pratiquée — l'adjectif seul — s'applique sans cas particulier.

Porter la règle dans le resolver, à côté des refus de DEC0017, la donne d'un coup à tout ce qui
lit ce pool. Le générateur et le calcul du slug le plus long ne peuvent plus diverger sur ce cas :
c'est précisément une règle recopiée dans un seul des deux qui a produit `mellow-mellow-moon`.

Un slug ne perd plus un segment par collision : le thème qui tire trois mots en tire trois chaque
fois qu'il le peut, ce que la négative principale de DEC0013 regrettait.

Les planchers comptent ce retrait, comme ils comptent ceux de DEC0017 : un nom qui atteint
exactement vingt participes n'en atteint que dix-neuf après ce mot, et le thème est refusé.
C'est ce que le plancher promet — vingt participes après chaque adjectif tirable — et c'est le
moment où l'auteur peut encore en ajouter un.

## Alternatives envisagées

### Alternative 1 — Garder l'écriture unique de DEC0013 et corriger seulement la mesure

- **Description :** dans le calcul du slug le plus long, mesurer chaque adjectif à côté du
  participe le plus large différent de lui, en laissant le générateur absorber la collision.
- **Pourquoi écartée :** recopie dans la mesure une règle que le générateur applique ailleurs,
  ce qui est la cause du défaut corrigé ici ; et garde un slug à deux segments là où le thème en
  promettait trois.

### Alternative 2 — Écrire le mot une fois, aussi dans la mesure

- **Description :** mesurer `mellow-moon` quand le participe le plus large est l'adjectif.
- **Pourquoi écartée :** sous-estime : avec `mellow` et `lit`, le générateur produit
  `mellow-lit-moon`, plus long, que la mesure ne regarderait jamais. Une promesse trop courte
  serait acceptée.

### Alternative 3 — Re-tirer tant que le participe vaut l'adjectif

- **Description :** relancer le tirage du participe, en moyenne un peu plus d'une fois sur un
  pool de vingt.
- **Pourquoi écartée :** même distribution que le retrait, mais non bornée là où le pool se
  réduit au mot seul (`--allow-small-theme`, limite de longueur), et la règle resterait dans le
  générateur, hors de portée de la mesure et des planchers.

### Alternative 4 — Refuser au chargement un thème où le mot est le seul participe possible

- **Description :** une règle de validation dédiée.
- **Pourquoi écartée :** les planchers couvrent déjà le cas d'un thème validé normalement, et une
  limite de longueur arrive à l'exécution, après tout chargement ; le retrait couvre les deux sans
  erreur nouvelle.

## Conséquences

### Positives

- Un slug n'écrit jamais deux fois le même mot, et ne perd plus de segment par collision.
- Le slug le plus long rapporté par `--analyze`, et celui qu'une promesse `maxLength` mesure, est
  toujours un slug que le générateur peut produire.
- Aucune boucle de tirage, quel que soit le thème ou la limite.

### Négatives

- Les slugs tirés changent pour les thèmes concernés, à graine égale : le golden master de
  `flowers` a bougé de deux lignes.
- Un thème au seuil exact du plancher de participes peut être refusé alors qu'il était accepté.
  Aucun des thèmes de `themes/` ni des thèmes embarqués ne l'est.
- Le refus pour plancher manqué porte le code `IncompatibilityStarvesTheNoun` même sans aucune
  entrée `incompatible` ; son message le dit, mais le code ne distingue pas les deux causes.

### Risques

- Un auteur qui déclare un mot des deux côtés par erreur voit son pool réduit d'un mot derrière
  cet adjectif sans le chercher ; la remarque à l'enregistrement le dit.

### Actions de suivi

- Dire la règle là où l'auteur la rencontre — fait dans
  [`../writing-a-theme.md`](../writing-a-theme.md).
- Inscrire le remplacement partiel dans le statut de DEC0013 — fait.
