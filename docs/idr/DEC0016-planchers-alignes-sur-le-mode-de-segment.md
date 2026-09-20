# DEC0016 | Alignement des planchers de taille par nom sur le mode de segment déclaré par le thème

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-20 | Accepté | Remplace DEC0012, dont le plancher ne vaut plus que pour « both » | |

## Contexte

DEC0003 impose un plancher de 100 adjectifs par nom, calculé sur le pool résolu. DEC0012 y a
ajouté un plancher de 20 participes par nom, dans tout thème qui en déclare. Aucun des deux ne
lit le `segmentMode` du thème.

Un thème déclare pourtant le mode pour lequel il est écrit, dans `defaults.segmentMode` :
`adjective`, `participle`, `either` ou `both`, ce dernier par défaut en l'absence du bloc. Ce
que le mode tire devant le nom diffère à chaque valeur : un adjectif seul, un participe seul, un
mot pris dans les deux pools réunis depuis DEC0015, ou un de chaque.

Mesuré sur les thèmes livrés :

| thème | mode déclaré | adjectifs, nom le plus pauvre | participes, nom le plus pauvre | somme |
| --- | --- | --- | --- | --- |
| docker | `either` | 187 | 76 | 263 |
| heroku | `either` | 178 | 20 | 198 |
| slugger | aucun, donc `both` | 105 | 40 | — |

DEC0012 consigne que son seuil de 20 était exactement le minimum de `heroku` et le laissait donc
sans aucune marge.

La troisième règle de DEC0003 compte les combinaisons par catégorie, `|pool(noun)| × max(1,
|partPool(noun)|)` sommé sur les noms de la catégorie, avec un seuil de 40 000. Mesuré sur
`heroku`, sa catégorie la plus pauvre — `chaleur` — totalise 50 374 combinaisons ainsi comptées,
et 1 529 si l'on compte ce que « either » produit réellement.

`--segment` est une option d'exécution : elle passe au-dessus du `segmentMode` du thème dans la
chaîne de DEC0004, et permet donc d'atteindre depuis n'importe quel thème les combinaisons que
son mode déclaré ne produit pas.

Un thème peut ne déclarer aucun participe ; le générateur se replie alors silencieusement sur
l'adjectif seul, quel que soit le mode demandé, et un mode qui réclame des participes absents est
déjà refusé pour cette raison.

## Décision

Dans ce contexte, nous décidons que le plancher de taille par nom porte sur le pool que le mode
de segment déclaré par le thème tire effectivement.

## Justification

Le plancher existe pour garantir la variété de ce qui est **tiré** — c'est le raisonnement de
DEC0003, qui l'a précisément placé sur le pool résolu plutôt que sur la taille des listes. Le
pool résolu d'un nom dépend de ses catégories ; ce que ce pool devient dans un slug dépend du
mode. Lire le mode prolonge la règle au lieu d'en ajouter une.

Sous « either », le mot devant le nom est tiré des deux pools réunis (DEC0015) : c'est donc leur
somme qui doit être riche, et aucun des deux séparément. `heroku` passe ainsi d'une marge nulle,
consignée par DEC0012, à 198 mots pour un plancher de 100, sans qu'un seul mot lui soit ajouté —
l'absence de marge mesurait une règle appliquée à un pool que le mode ne tire jamais seul.

Sous « adjective », aucun participe n'est jamais tiré : refuser le thème pour la pauvreté de sa
section `participles` le refuserait pour des mots dont il ne se sert pas. Sous « participle »,
c'est le participe qui est le mot devant le nom, et il mérite alors le plancher entier plutôt
que celui, bien plus bas, que DEC0012 avait calibré pour un deuxième mot.

Sous « both », les deux planchers existants continuent de valoir tels quels : c'est le seul mode
où un participe est un mot **de plus** à côté d'un adjectif, donc le seul où la lecture de
DEC0012 reste la bonne.

La règle par catégorie ne suit pas le mode parce qu'elle pose une autre question : non pas si le
mot devant un nom varie assez, mais si une branche du thème vaut la peine d'être portée. Cette
richesse-là est celle du vocabulaire disponible, que `--segment both` atteint depuis n'importe
quel thème.

## Alternatives envisagées

### Alternative 1 — Statu quo, des planchers aveugles au mode

- **Description :** conserver 100 adjectifs et 20 participes par nom dans tout thème qui déclare
  des participes, quel que soit son `segmentMode`.
- **Pourquoi écartée :** applique à `heroku`, écrit pour « either », un plancher sur un pool que
  ce mode ne tire jamais seul ; DEC0012 consigne lui-même la conséquence, un seuil sans marge.

### Alternative 2 — Aligner aussi le plancher par catégorie sur le mode

- **Description :** compter les combinaisons d'une catégorie comme le mode déclaré les produit,
  en additionnant les pools sous « either » au lieu de les multiplier.
- **Pourquoi écartée :** le seuil de 40 000 est calibré sur un produit ; appliqué à une somme il
  ferait tomber `chaleur` de 50 374 à 1 529 et refuserait `heroku`, alors que `--segment both`
  atteint bien les 50 374.

## Conséquences

### Positives

- `heroku` passe d'une marge nulle à 98 mots au-dessus du plancher qui le concerne, sans ajout.
- Un thème écrit pour l'adjectif seul n'est plus refusé pour des participes qu'il ne tire jamais.
- Un thème écrit pour le participe seul est enfin tenu au plancher entier sur ce qu'il tire.
- Le rapport de DEC0014 n'affiche que les planchers qui s'appliquent, une marge contre un seuil
  qui ne vaut pas étant pire qu'aucune marge.
- Le refus sous « either » nomme les deux moitiés du compte, donc laquelle étoffer.

### Négatives

- Le `segmentMode` déclaré décide désormais si un fichier charge : les mêmes mots acceptés sous
  « either » sont refusés sous « participle ».
- Le plancher de 20 participes de DEC0012 ne protège plus un thème déclarant « either » : les 20
  participes par nom de `heroku` ne sont plus bornés que par la somme.
- Une erreur de plus au catalogue, pour le pool combiné.

### Risques

- `--segment both` sur un thème écrit pour « either » tire dans deux pools dont aucun n'a été
  planchéié séparément : l'exécution obtient moins de variété que le plancher ne le laisse croire.
- Changer `defaults.segmentMode` sur un thème accepté peut le faire refuser, loin de la ligne
  éditée.

### Actions de suivi

- Mettre à jour `docs/writing-a-theme.md`, dont les quatre règles de taille énoncent le plancher
  des participes sans condition de mode.
- Revoir si la règle par catégorie doit suivre le mode, le jour où un thème « either » passera
  ses 40 000 combinaisons sans les produire.
- L'action de suivi de DEC0012 reste ouverte : étoffer les participes de `heroku` et de
  `slugger`, puis remonter le seuil — qui n'est plus que celui de « both ».
