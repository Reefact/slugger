# DEC0020 | Tirage de l'absence de participe comme un participe de plus, dans un mode dédié

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-21 | Accepté | | |

## Contexte

`both` est le mode par défaut, celui qu'un thème obtient en ne déclarant aucun bloc `defaults`.
Il met un participe devant le nom à **tous** les tirages : quatre des six thèmes livrés sont
écrits pour lui, et aucun ne produit jamais un slug à deux segments. `either` en met déjà un sur
cinq ou sur dix, mais il remplace alors l'adjectif au lieu de s'y ajouter.

Mesuré, participes atteints par nom et part qu'ils représentent dans le vocabulaire du nom :

| thème | mode déclaré | participes par nom | part des participes |
| --- | --- | --- | --- |
| docker | `either` | 76 partout | 28,9 % |
| heroku | `either` | 20 → 69 | 10,1 % → 27,9 % |
| slugger | aucun, donc `both` | 40 → 60 | 27,6 % → 32,3 % |
| fairy-tales | `both` | 30 → 155 | 18,2 % → 46,4 % |
| mineralogy | aucun, donc `both` | 29 → 67 | 17,1 % → 26,9 % |
| retro-computing | `both` | 24 → 64 | 17,6 % → 36,8 % |

DEC0015 a tranché la question voisine : le mot unique d'`either` est tiré dans les deux pools
réunis, proportionnellement à leur taille, parce que « *either* nomme un choix entre deux
vocabulaires, pas un partage du résultat en deux moitiés ». Un tirage à pile ou face y avait été
écarté pour cette raison.

DEC0016 aligne les planchers par nom sur le mode déclaré, et DEC0017 retire du pool de participes
ce que l'adjectif déjà tiré refuse, **avant** le tirage — ce qui garde le nombre de tirages fixe
et le participe uniforme sur ce qui lui reste.

DEC0012 avait consigné la perte qu'il causait : « un nom qui n'atteignait aucun participe se
dégradait silencieusement vers l'adjectif seul ; il fait désormais refuser le thème ».

Le tirage passe par `IRandomSource`, dont la version scriptée des tests vérifie chaque valeur
contre la borne demandée : changer la borne d'un tirage invalide les scripts écrits contre elle.

## Décision

Dans ce contexte, nous décidons d'ajouter un mode de segment `threeOrTwo`, dont le participe est
tiré dans un pool comptant un candidat de plus que le thème n'en déclare : l'absence de
participe.

## Justification

Le geste est celui de DEC0015, une dimension plus loin : un seul tirage uniforme sur les
candidats, dont la proportion est décidée par ce que le thème déclare et non par un réglage.
L'absence est un candidat comme les autres — un nom qui atteint 25 participes tire sur 26.

Il n'y a donc **aucune option à ajouter** : rien dans `ThemeDefaults`, rien dans la chaîne de
DEC0004, rien sur la ligne de commande que le mode lui-même. C'est ce qui sépare ce mécanisme
d'une probabilité déclarée, qui aurait demandé une clé, un défaut, une place dans la chaîne et
une valeur à justifier.

**Il améliore la variété au lieu de la coûter**, ce qu'une probabilité ne fait pas. La
distribution reste parfaitement uniforme sur `|pool| × (|partPool| + 1)` slugs, et les slugs à
deux segments sont des slugs que `both` ne produit pas du tout. Mesuré sur le nombre effectif de
slugs — 1/Σp², celui que lit une collision, et non le compte brut :

| thème | `both` | `threeOrTwo` |
| --- | --- | --- |
| docker | 3 354 032 | **+1,3 %** |
| heroku | 1 034 881 | **+4,2 %** |
| slugger | 1 096 101 | **+2,2 %** |
| fairy-tales | 3 838 898 | **+1,6 %** |
| mineralogy | 3 033 880 | **+2,5 %** |
| retro-computing | 807 277 | **+3,1 %** |

Une probabilité déclarée aurait fait l'inverse : en concentrant 20 à 33 % de la masse sur les
seuls slugs à deux segments, elle rend chacun d'eux dix fois plus probable que n'importe quel
slug à trois mots, et fait tomber ce même nombre effectif à 40-55 % de celui de `both` — mesuré,
à un poids de dix.

**La fréquence suit le nom.** Plus un nom est pauvre en participes, plus il s'en passe souvent :
c'est l'affinité entre un nom et les participes, lue sur une donnée que le thème porte déjà,
plutôt que déclarée nom par nom. `except` (DEC0011) continue d'agir dessus dans le même sens.

Les planchers de `both` s'appliquent tels quels, parce que l'absence prend une part des tirages
et jamais une part du pool : un pool maigre est aussi répétitif ici que là, simplement atteint
moins souvent.

## Alternatives envisagées

### Alternative 1 — Un pourcentage `participleChance` sous `both`

- **Description :** une clé 0–100, sœur de `tokenChance`, disant à quelle fréquence le participe
  apparaît ; `0` vaudrait `adjective` et `100` le `both` d'aujourd'hui.
- **Pourquoi écartée :** met un jugement éditorial dans une molette d'exécution — à 30 %,
  `thundering-moon` est écarté sept fois sur dix par chance et tiré les trois autres — et coûte
  près de la moitié de la variété effective, là où le candidat supplémentaire en ajoute.

### Alternative 2 — Un compte de participes déclaré par nom

- **Description :** `"participles": 0 | 1 | 2` sur le nom, `0` rendant à DEC0012 ce qu'il avait
  pris et `2` mettant deux participes devant les noms qui s'y prêtent.
- **Pourquoi écartée :** les deux moitiés n'ont pas la même taille. `0` est un énoncé sur le
  pool et ne coûterait presque rien ; `2` est un énoncé sur le compte, et ajoute une quatrième
  forme de slug — une clé `fourWords` dans `maxLength`, deux participes qui peuvent se
  télescoper (DEC0013) et qui doivent respecter les paires de DEC0017 contre l'adjectif **et**
  l'un contre l'autre. Et l'affinité que `0` déclare est déjà portée par la taille du pool. À
  reprendre le jour où un auteur aura besoin qu'un nom refuse des participes que ses catégories
  n'écartent pas.

### Alternative 3 — Donner un poids à l'absence

- **Description :** tirer sur `|partPool| + w` plutôt que sur `|partPool| + 1`, pour obtenir la
  fréquence voulue sans quitter le mécanisme.
- **Pourquoi écartée :** c'est la probabilité déclarée sous un autre nom, avec son coût mesuré —
  à `w = 10`, le nombre effectif de slugs tombe à 40-55 % de celui de `both`. Le poids de un est
  le seul qui garde la distribution uniforme.

### Alternative 4 — Modifier `both` plutôt qu'ajouter un mode

- **Description :** faire de l'absence un candidat du pool sous `both` lui-même, sans cinquième
  valeur.
- **Pourquoi écartée :** change la borne du second tirage, donc invalide tous les tirages
  scriptés écrits contre elle ; et un thème voulant un participe à chaque slug n'aurait plus
  aucun moyen de le dire.

## Conséquences

### Positives

- Aucune option : le mode est la seule chose à écrire, dans `defaults.segmentMode` ou derrière
  `--segment`.
- La variété augmente au lieu de diminuer, et le tirage reste uniforme.
- La fréquence suit ce que chaque nom atteint, sans rien déclarer par nom.
- Sous plafond (DEC0018), `SlugBudget.DrawsTwoWords` reste faux : un adjectif qui ne laisse pas
  la place d'un participe est **gardé** au lieu d'être écarté, son pool de participes revenant
  vide et l'absence étant alors tout ce qui reste à tirer. Le thème conserve des mots que `both`
  doit jeter.
- Les planchers, les paires de DEC0017 et le rapport d'analyse suivent ceux de `both` par un
  seul prédicat, `PutsAParticipleBesideAnAdjective`.

### Négatives

- **Une cinquième valeur dans six `switch`**, dont plusieurs ont un bras par défaut qui aurait
  répondu comme pour un autre mode. Deux d'entre eux sont justes par chance — `DrawsTwoWords` et
  le `max(1, ...)` du comptage — et portent désormais un commentaire disant que l'omission est
  voulue.
- Mesuré bout en bout sur 5 000 tirages, l'absence sort entre **1,6 %** (docker) et **4,1 %**
  (heroku) : de quoi varier un catalogue, pas de quoi servir de « participes coupés ».
  `--segment adjective` est là pour ça.
- Un nom de mode en plusieurs mots là où les quatre autres en font un : cinq endroits rendaient
  le nom par `ToLowerInvariant()` et devaient cesser, sous peine d'offrir « threeortwo ».
- Un thème déclarant `threeOrTwo` produit les deux formes de slug, donc son `maxLength` devrait
  promettre sur les deux clés ; une clé absente ne promet rien et n'est pas vérifiée.

### Risques

- L'absence ne se voit pas sur un petit échantillon : un auteur qui tire une douzaine de slugs
  lit du `both` et conclut que le mode ne fait rien.
- La fréquence n'est pas déclarable. Un thème qui voudrait un slug court sur trois ne peut que
  maigrir ses participes — ce qui lui coûte par ailleurs la variété que ce mode lui rend.

### Actions de suivi

- Documenter le mode dans [`../writing-a-theme.md`](../writing-a-theme.md), à côté de
  `segmentMode` et du tableau des planchers.
- Reprendre l'alternative 2 — le compte par nom — le jour où un auteur aura besoin qu'un nom
  refuse les participes que ses catégories n'écartent pas.
- Re-mesurer les deux moteurs de mutation : les figures de `CLAUDE.md` précèdent ce mode comme
  elles précèdent DEC0019.
