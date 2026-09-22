# Écrire un thème

Un thème est un fichier JSON. Aucun code, aucune recompilation : tu le déposes dans
`~/.slugger/themes/` (ou n'importe quel dossier passé à `--theme-dir`) et `slugger --theme
<nom-du-fichier>` s'en sert.

Le thème est identifié par **son nom de fichier**, jamais par un champ interne. `porno.json`
devient `--theme porno`. Un fichier qui porte le nom d'un thème embarqué (`slugger`, `heroku`,
`docker`) le masque — c'est autorisé, mais annoncé.

Cette page dit ce qu'un fichier de thème peut contenir. Pour la ligne de commande qui le lit,
`slugger --help` liste les options, leurs valeurs et quelques exemples.

## Le plus petit fichier qui fonctionne

```json
{
  "adjectives": { "common": ["vieux", "neuf", "..."] },
  "nouns": [{ "value": "saule" }, { "value": "rivière" }]
}
```

Il ne passera pas la validation — il faut 100 noms et 100 adjectifs accessibles à chacun, voir
plus bas — mais c'est toute la structure obligatoire. Tout le reste est optionnel.

## Le fichier complet

```json
{
  "adjectives": {
    "vegetal": ["noueux", "touffu"],
    "aquatique": ["limpide"],
    "common": ["vieux", "neuf"]
  },
  "participles": {
    "aquatique": ["ruisselant"],
    "common": ["persistant"]
  },
  "nouns": [
    { "value": "saule", "categories": ["vegetal"] },
    { "value": "rivière", "categories": ["vegetal", "aquatique"] },
    { "value": "caillou" }
  ],
  "defaults": { "sep": "_", "casing": "snake", "tokenLength": 4 },
  "allowSmall": false
}
```

Écris-le pour être lu. Répéter un mot dans plusieurs catégories, ou d'un thème à l'autre, ne
coûte rien : les chaînes sont dédupliquées en mémoire au chargement.

## Les catégories : quel adjectif pour quel nom

C'est le cœur du fichier, et la raison d'être de `slugger`. Un adjectif n'est tirable pour un
nom que s'ils partagent une catégorie — sinon tu obtiens `tonnant-lune` aussi volontiers que
`vieux-saule`.

**`common` est un socle, pas un repli.** Tout nom l'atteint, *en plus* de ce qu'il déclare :

| Le nom déclare | Il atteint |
| --- | --- |
| *(rien)* | `common` |
| `["vegetal"]` | `vegetal` + `common` |
| `["vegetal", "aquatique"]` | `vegetal` + `aquatique` + `common` |

Tu n'as donc jamais à écrire `common` sur un nom, et **aucune catégorie n'a à atteindre seule
les 100 adjectifs** : c'est l'addition avec `common` qui compte. Le thème `slugger` en vit —
cinq catégories de 45 et `common` à 60.

À part `common`, un nom de catégorie est une chaîne libre, sans aucune signification pour le
code. Deux thèmes peuvent avoir une catégorie `vegetal` sans le moindre rapport : le tirage ne
sort jamais d'un seul fichier.

Une seule règle de cohérence : **toute catégorie citée par un nom doit exister** comme clé dans
`adjectives` **ou** dans `participles`. L'une des deux suffit.

## Refuser un mot pour un nom précis

Les catégories écartent un adjectif d'un nom qu'il ne peut pas décrire. Elles n'écartent pas un
adjectif d'un nom qu'il décrit très bien et insulte quand même — Docker embarque un refus en dur
de `boring_wozniak` pour exactement ça.

`except` le dit dans le thème, nom par nom :

```json
{ "value": "Wozniak", "categories": ["personne"], "except": ["boring", "dull"] }
```

`boring` reste disponible pour tous les autres noms ; il n'atteint simplement jamais celui-là.
La soustraction s'applique **aux adjectifs comme aux participes** : ce qui rend un mot mal venu à
côté d'un nom, c'est le mot, pas sa fonction grammaticale — et `boring` est aussi un participe
présent.

Deux choses à savoir :

- **Un mot que le thème ne déclare nulle part est refusé au chargement**, pas ignoré. Une liste
  de sûreté qui laisse passer une faute de frappe est pire que pas de liste : `boaring` donnerait
  un nom qui *paraît* protégé et ne l'est pas.
- **Tu ne peux pas trop exclure sans t'en apercevoir.** Le plancher des 100 adjectifs se calcule
  après soustraction, donc un nom vidé par ses exclusions fait refuser le thème, en le nommant.

## Refuser un participe à côté d'un adjectif

`except` écarte un mot d'un nom. Il ne dit rien du couple que forment les **deux** mots placés
devant le nom en mode `both` : `frozen` est un bon adjectif, `burning` un bon participe, et
`frozen-burning-forge` n'a aucun sens.

`incompatible` déclare ces couples une fois pour tout le thème :

```json
"incompatible": {
  "frozen": ["burning", "blazing", "melting"],
  "silent": ["roaring", "screaming"]
}
```

La clé est un adjectif, les valeurs des participes. Slugger tire l'adjectif **d'abord**, puis le
participe dans ce qui reste — le couple refusé n'existe donc jamais, il n'est pas rattrapé après
coup.

Quatre choses à savoir :

- **Le sens compte.** `frozen` refuse `burning` ; `burning`, s'il est aussi déclaré comme
  adjectif, ne refuse rien. Écris l'autre sens si tu le veux aussi.
- **Une paire à l'envers est refusée au chargement**, et le message te le dit : si ta clé est
  déclarée dans `participles` et pas dans `adjectives`, c'est presque toujours ça.
- **Le plancher des participes se mesure après soustraction**, pour le pire couple. Un nom qui
  atteint 40 participes dont un adjectif en refuse 35 en a 5 pour ce tirage-là : le thème est
  refusé, en nommant le nom **et** l'adjectif.
- **Ça ne sert que sous `both`.** Les trois autres modes ne placent qu'un mot devant le nom, donc
  deux mots ne s'y rencontrent jamais. Une paire déclarée quand même est signalée, sans rien
  refuser — tout comme une paire qu'aucun nom ne peut réunir.

C'est le troisième endroit où un mot peut disparaître d'un tirage, après les catégories et
`except`. Si un mot ne sort jamais, `--analyze` est ce qui te dira lequel des trois.

## Promettre une longueur

Un slug finit quelque part, et cet endroit a des règles. **63 caractères** est celle qui compte :
c'est la limite d'un label DNS, donc celle d'un bucket S3, d'un Service Kubernetes, d'un
sous-domaine. **30** si la cible est une app Heroku ou un projet GCP.

Ni Docker ni Heroku ne coupent quoi que ce soit : ils tiennent parce que leur vocabulaire est
court. Les deux listes de Docker plafonnent à 13 caractères, ce qui borne son pire slug à 28.
`maxLength` est cette discipline écrite dans le fichier :

```json
"maxLength": {
  "twoWords": 63,
  "threeWords": 120
}
```

- **`twoWords`** : un seul mot devant le nom — `segmentMode` `adjective`, `participle` ou `either`.
- **`threeWords`** : deux mots devant le nom — `segmentMode` `both`.
- `segmentMode: threeOrTwo` produit les deux formes, donc promets sur les deux clés.
- Une clé absente ne promet rien. Ce n'est pas la même chose que promettre l'infini.

La clé est à la **racine** du fichier, à côté d'`allowSmall`, pas dans `defaults` : un `defaults`
s'éteint dès qu'un deuxième thème est en portée, et une promesse qui disparaît quand on ajoute un
thème n'en est pas une.

**Ce que ça te coûte :** le jour où tu ajoutes un mot qui fait dépasser, le thème est **refusé au
chargement**, en te montrant le slug fautif. C'est exactement l'intérêt — le problème arrive
devant toi plutôt que devant le registre qui refuse ton image six mois plus tard.

`docker.json` promet 63 et `heroku.json` 30. Mesuré : ils tiennent avec 35 et **3** caractères de
marge. Trois. Un mot de plus de 14 caractères dans `heroku.json` fait échouer la build.

### Et à l'exécution

```bash
slugger --theme mineralogy --max-length 63
```

`--max-length` ne tronque rien non plus : il **retire du tirage** les mots qui ne tiennent pas,
puis valide ce qu'il reste comme n'importe quel thème. Si la surface réduite ne tient plus ses
planchers, l'exécution est refusée en disant lequel :

```console
$ slugger --theme mineralogy --segment both --max-length 40
Theme "mineralogy" was refused for 125 reasons:

  - Under 40 characters, "rammelsbergite" reaches 1 participle behind "visually arresting", but a
    theme drawing "both" needs at least 20 per noun for every adjective it can draw - raise the
    limit, shorten the words, or draw one word instead of two.
```

La dernière suggestion est la bonne ici : `--segment either --max-length 40` passe, parce qu'un
seul mot devant le nom laisse deux fois plus de place.

**`--analyze` connaît l'option**, ce qui répond à la question sans rien générer :

```bash
slugger --analyze mon-theme.json --max-length 40 --segment either
```

## Promettre des segments courts

Une valeur composée passe au formatage en plusieurs morceaux — DEC0008 fait de chaque caractère
non alphanumérique une frontière de mot, et `--sep` la remplace. `awful` + `snake cased` +
`property name` s'écrit donc :

```console
awful-snake-cased-property-name
```

Cinq morceaux pour trois segments, et rien ne dit où chacun commence. Les thèmes embarqués
n'ont pas ce souci : `docker` et `heroku` n'ont **aucun** nom composé, c'est ce qui donne à
`focused_turing` sa forme.

`maxSegmentWords` écrit cette discipline dans le fichier :

```json
"defaults": { "maxSegmentWords": 1 }
```

Et `--max-segment-words` la demande à l'exécution, sur n'importe quel thème :

```bash
slugger --theme mon-theme --max-segment-words 1
```

Comme `--max-length`, **il retire, il ne coupe jamais** : `speculative generality` quitte le
tirage sous un plafond d'un mot, il n'en sort pas `speculative`. Le compte porte sur **un
segment**, jamais sur le slug : un nom de trois mots ne mange pas la place de l'adjectif, il est
simplement écarté.

Le nom est mesuré comme les autres, et c'est là que ça se voit le plus — c'est presque toujours
lui la partie longue.

Ce qui reste est validé comme n'importe quel thème, donc un plafond qui vide une catégorie est
refusé en la nommant :

```console
$ slugger --theme code-review --max-segment-words 1
Theme "code-review" was refused for 3 reasons:

  - Category "smellActions" totals 18,304 combinations, but every category needs at least 40,000.
```

C'est l'intérêt : la réponse n'est pas « non », elle est « de combien ». `--analyze` connaît
l'option, et `--allow-small-theme` lève les planchers le temps d'un essai.

Ce plafond va dans `defaults` et non à la racine, contrairement à `maxLength` : c'est un choix
d'allure, pas une promesse de sûreté. Il s'éteint donc quand un deuxième thème entre en portée,
comme tout ce qui est dans `defaults`.

### Lever le plafond d'un thème pour un tirage

`defaults` s'applique **sans qu'on le demande**, pas seulement quand rien d'autre ne parle : un
thème qui écrit `"maxSegmentWords": 1` tire en un mot par segment à chaque fois, y compris quand
la ligne de commande ne dit rien sur le sujet — `--analyze` le mesure sous ce plafond de la même
façon (DEC0023). Un nom composé écrit pour ce thème n'y disparaît donc pas seulement le temps d'un
essai : il en sort tout le temps, sauf à demander explicitement l'inverse.

`--max-segment-words none` est ce contraire explicite :

```bash
slugger --theme quantum-physics --max-segment-words none
```

`quantum-physics` promet la forme de Docker par défaut (`maxSegmentWords: 1`), et porte quand même
88 noms de plusieurs mots — `black hole`, `bell pair` — écrits pour ce vocabulaire-là plutôt que
pour trois segments d'un mot chacun. `none` les rend le temps d'un tirage, sans toucher au reste
du style du thème : le séparateur, la casse, le mode de segment restent les siens. C'est la
différence avec `--mimic-style false`, qui jetterait tout ça avec le plafond (DEC0024).

## Les participes (optionnel)

`participles` a exactement la structure d'`adjectives` et ajoute un troisième segment :
`vieux-ruisselant-rivière`. Sa présence suffit, aucun flag n'est nécessaire.

**Classe-les par capacité physique, pas par domaine.** C'est ce qui les rend plausibles. `heroku`
utilise `mobile` (se déplace), `sonore` (fait du bruit), `lumineux`, `vivant`, `chaleur`, `eau` :

- `moon` est `[lumineux, mobile]` et n'a pas `sonore` — donc `thundering-moon` n'est jamais tiré,
  `waning-moon` (via `common`) l'est.
- `willow` est `[vivant]` — `weeping-willow`, un idiome anglais réel, est un tirage possible.
- `river` est `[eau, mobile, sonore]` — `thundering-river`, `humming-river` et `swirling-river`
  sonnent tous justes.

Un classement par thème (« astronomie », « météo ») n'aurait rien filtré.

Si un nom tiré n'a aucun participe accessible, le slug retombe sur l'adjectif seul, sans erreur.
En revanche, des `defaults` qui réclament un participe dans un thème qui n'en déclare aucun sont
une erreur de chargement.

**Le même mot peut figurer dans les deux sections** — `charming` et `boring` sont des adjectifs
*et* des participes présents. C'est légal, et `--register` te le signale sans rien refuser :

```console
$ slugger --register ./cuisine.json
theme "cuisine" registered.
warning: "boring", "charming" declared as both an adjective and a participle;
         a draw that lands on the same word twice writes it once.
```

Si le tirage tombe effectivement deux fois sur le même mot, le slug l'écrit **une seule fois** —
`charming-lune` plutôt que `charming-charming-lune`. C'est la même dégradation que pour un nom
sans participe accessible. Mais tu perds un segment sur ces tirages-là, d'où l'avertissement.

## Ce qui arrive à tes valeurs

Écris tes valeurs comme on les écrit vraiment — `"Jack O'Neil"`, `"Jean-Luc Picard"`,
`"Smith & Wesson"`, `"St. Louis"`. Elles sont nettoyées au chargement : passage en minuscule, et
**tout ce qui n'est ni une lettre ni un chiffre devient une frontière de mot**, les frontières
consécutives n'en faisant qu'une et celles des extrémités disparaissant.

| Écrit dans le JSON | En mémoire après chargement |
| --- | --- |
| `"Jack O'Neil"` | `jack o neil` |
| `"Jean-Luc Picard"` | `jean luc picard` |
| `"Smith & Wesson"` | `smith wesson` |
| `"St. Louis"` | `st louis` |
| `"Yahoo!"` | `yahoo` |
| `"Apollo 11"` | `apollo 11` |

**Les accents sont conservés tels quels** — `" René     Dupont "` devient `rené dupont`, jamais
`rene dupont` : une lettre accentuée est une lettre. Il en va de même de tout alphabet.

Écris donc tes mots comme ils s'écrivent. Si le slug doit ensuite vivre quelque part qui ne
supporte pas ton alphabet, c'est à l'exécution de le dire, pas au thème : `--fold-accents` plie
`é` en `e` et `ç` en `c` au moment de former le slug.

```console
$ slugger --theme cuisine
flottante-crème-brûlée

$ slugger --theme cuisine --fold-accents
flottante-creme-brulee
```

Ne plie que ce qui se décompose, c'est-à-dire les accents de l'alphabet latin. `ß`, `ø` et `œ`
n'ont pas de décomposition, et aucun alphabet non latin non plus : ils passent tels quels. C'est
un pliage, jamais une garantie que le slug soit devenu ASCII.

Quand la destination l'exige vraiment, `--ascii` promet le résultat au lieu du mécanisme — et
défigure ce qu'il ne sait pas plier :

| Valeur | *(rien)* | `--fold-accents` | `--ascii` |
| --- | --- | --- | --- |
| `François Sagat` | `françois-sagat` | `francois-sagat` | `francois-sagat` |
| `Søren Straße` | `søren-straße` | `søren-straße` | `sren-strae` |
| `한글 서울` | `한글-서울` | `한글-서울` | *(le segment disparaît)* |

`--ascii` implique le pliage, les deux ne servent donc jamais ensemble. Un segment qui ne
survit pas est retiré du slug plutôt que joint à vide — mais **si aucun segment ne survit, le
slug est vide**. C'est le prix assumé de l'option, à ne prendre que là où rien d'autre ne passe.

Une valeur qui ne contient aucune lettre ni chiffre est refusée, puisqu'il n'en resterait rien à
tirer.

Les frontières survivent ensuite jusqu'au formatage, où `--sep` les remplace — ou `--word-sep`
si tu veux qu'elles deviennent autre chose :

| | `gorgeous` + `"John Doe"` | `gorgeous` + `"Jack O'Neil"` |
| --- | --- | --- |
| par défaut | `gorgeous-john-doe` | `gorgeous-jack-o-neil` |
| `--word-sep _` | `gorgeous-john_doe` | `gorgeous-jack_o_neil` |
| `--word-sep ''` | `gorgeous-johndoe` | `gorgeous-jackoneil` |

Une valeur en plusieurs mots est donc parfaitement normale — `"Oracle Park"`, `"Babe Ruth"`.

## Les quatre règles de taille

Elles portent sur le pool **réellement résolu**, pas sur la taille des listes : un fichier de
500 adjectifs dont 480 tiennent dans une catégorie laisse les autres noms avec une douzaine de
choix, et un comptage global ne le verrait pas.

1. Au moins **100 noms** distincts.
2. Chaque nom doit atteindre au moins **100 mots à mettre devant lui**.
3. **En mode `both` uniquement**, chaque nom doit atteindre **20 participes** de plus.
4. Chaque catégorie doit totaliser au moins **40 000 combinaisons**, où
   `combos(noun) = |pool(noun)| × max(1, |partPool(noun)|)`.

« Les mots à mettre devant lui » dépend du `segmentMode` que tu déclares, parce que c'est lui
qui décide de ce qui est tiré :

| `segmentMode` | les 100 mots de la règle 2 sont | règle 3 |
| --- | --- | --- |
| `adjective` | ses adjectifs seuls | — |
| `participle` | ses participes seuls | — |
| `either` | ses adjectifs **+** ses participes, qui ne font qu'un pool | — |
| `both` (défaut) | ses adjectifs | 20 participes en plus |
| `threeOrTwo` | ses adjectifs | 20 participes en plus, comme `both` |

Sous `either`, un seul mot précède le nom et il est tiré dans les deux sections réunies,
proportionnellement à leur taille : 178 adjectifs et 20 participes, c'est un pool de 198 dont le
participe sort une fois sur dix. C'est pourquoi ce sont les deux ensemble qui doivent faire 100,
et non chacun de leur côté.

Sous `both`, le participe est un mot **de plus** à côté de l'adjectif, dans le slug autant que
lui — un nom qui n'en atteint que trois répète son mot du milieu sans fin. D'où la règle 3, dont
le seuil est bas et le restera le temps que les thèmes livrés soient étoffés.

Sous `threeOrTwo`, le participe est tiré dans un pool comptant **un candidat de plus** que tu
n'en déclares, et ce candidat est l'absence de participe : un nom qui atteint 25 participes tire
sur 26, et la vingt-sixième issue écrit un slug à deux segments. Les planchers sont ceux de
`both`, parce que l'absence prend une part des tirages et jamais une part du pool.

Sous `adjective`, une section `participles` maigre ne fait rien refuser : elle n'est jamais
tirée. Le rapport de `--analyze` te la montrera quand même, avec un tiret à la place du plancher.

Rien n'empêche ensuite un `--segment both` sur un thème écrit pour `either` : la ligne de
commande passe au-dessus de ton `segmentMode`, et tire alors dans deux pools qu'aucune règle
n'a mesurés séparément.

Le seuil de 40 000 est le point où un thème a besoin d'un suffixe pour éviter les collisions :
Docker (108 × 236 = 25 488) et Heroku (91 × 95 = 8 645) sont tous deux en dessous, et tous deux
en ajoutent un. Les trois thèmes livrés les passent sans aide.

**Si tu es en dessous et que tu l'assumes**, deux façons de lever les quatre seuils :

- `"allowSmall": true` dans le fichier — déclaré une fois par son auteur, vaut pour toujours.
- `--allow-small-theme` sur la ligne de commande — ponctuel, pour tester un thème en cours
  d'écriture sans éditer le JSON.

Une seule règle ne se lève jamais : **un thème doit déclarer au moins un nom.** `allowSmall`
permet d'assumer un thème réduit, pas un fichier qui ne peut rien produire.

## `defaults` : le style du thème

Ce bloc porte l'identité visuelle du style que le thème imite, pas une préférence de session :

| Clé | Ce qu'elle décide |
| --- | --- |
| `sep` | Le séparateur entre segments — Docker écrit `_`, Heroku et slugger `-` |
| `wordSep` | Ce qui joint les mots d'une valeur composée ; `""` les colle |
| `casing` | `kebab`, `snake` ou `camel` |
| `foldAccents` | Plie les accents (`é` → `e`) ; rarement l'affaire d'un thème, voir ci-dessus |
| `ascii` | Force un slug ASCII, quitte à défigurer ; rarement l'affaire d'un thème non plus |
| `segmentMode` | `adjective`, `participle`, `either`, `both` (défaut) ou `threeOrTwo` — décide aussi des planchers, voir ci-dessus |
| `maxSegmentWords` | Combien de mots un segment peut porter — `1` pour la forme de Docker, un mot par segment |
| `tokenLength` | Longueur du suffixe, `0` pour aucun |
| `tokenHex` | Suffixe en hexadécimal plutôt qu'en décimal |
| `tokenChance` | % de chance que le suffixe apparaisse (défaut 100) |
| `tokenGlued` | Colle le suffixe au segment précédent — `focused_turing3` |

N'y mets **pas** ce qui relève de la session : `--count`, `--seed`, `--oneshot`, `--clipboard`.

Ces `defaults` passent **au-dessus** de la config sauvegardée par `--init` de l'utilisateur :
demander `--theme docker` demande son format autant que son vocabulaire. C'est pourquoi il ne
faut **pas** y recopier les valeurs par défaut du programme — un bloc qui ne dit rien de neuf ne
ferait que neutraliser la config de qui utilise ton thème. `slugger.json` n'a pas de bloc
`defaults` du tout, pour cette raison exacte.

## `meta` : décrire le thème

Un bloc entièrement optionnel, pour qui distribue ou reprend un thème plutôt que pour le
moteur — rien ici n'influence un tirage :

```json
"meta": {
  "title": "Docker",
  "description": "Le style des noms de conteneurs Docker",
  "version": "1.0.0",
  "author": "Sylvain Aurat",
  "source": "https://github.com/reefact/slugger-themes/blob/main/docker.json",
  "createdAt": "2024-01-15",
  "publishedAt": "2024-02-01"
}
```

| Clé | Ce que c'est |
| --- | --- |
| `title` | Un nom d'affichage lisible, à côté du nom de fichier — jamais un identifiant. `docker.json` reste identifié par `docker`, `title` n'est là que pour l'humain |
| `description` | Ce qu'est le thème, ou à quoi il sert |
| `version` | Libre — jamais comparée ni imposée par slugger |
| `author` | Qui l'a écrit |
| `source` | Où retrouver l'original — l'URL du **fichier**, pas celle du dépôt : qui lit ce champ tient déjà une copie et cherche d'où elle sort. Un thème compilé dans l'outil n'a pas de copie à retracer et omet la clé |
| `createdAt` | Quand le thème a été écrit pour la première fois |
| `publishedAt` | Quand cette `version` a été publiée |

`createdAt` et `publishedAt` sont des chaînes libres, comme `version` : rien ne les interprète
comme une date, rien ne les compare. Une copie qui a quitté son dépôt n'a plus d'historique git
pour porter cette information ailleurs.

### Faire évoluer un thème déjà publié

Rien ne vérifie `version`, `createdAt` ou `publishedAt` au chargement — la discipline est donc
entièrement à la charge de qui modifie le fichier :

- **Seul le thème modifié avance.** Changer `mineralogy.json` bouge sa `version` et son
  `publishedAt` ; les huit autres thèmes du dépôt n'ont aucune raison de changer avec lui.
- **`createdAt` ne bouge jamais** après la première publication — il date le thème, pas sa
  dernière modification.
- **`publishedAt` avance à chaque `version`.** Un thème qui n'a pas changé de contenu n'a pas de
  raison d'avancer sa `version`, et donc pas son `publishedAt` non plus.

Les neuf thèmes livrés partagent aujourd'hui la même `version` et les mêmes deux dates parce
qu'ils sont sortis ensemble, en 1.0.0 — une coïncidence de cette première publication groupée,
pas une règle à maintenir : le prochain thème à changer partira seul.

Toutes les clés sont optionnelles, y compris `meta` lui-même : un thème qui n'en dit rien se
charge exactement comme avant. Chaque valeur présente doit être une chaîne, sous peine d'un
refus au chargement comme n'importe quelle autre section malformée.

```console
$ slugger --theme-info docker
theme "docker"
  title: Docker
  description: Docker's own style of container names - an adjective, sometimes a participle, and a scientist's surname
  version: 1.0.0
  author: Reefact
  createdAt: 2026-09-21
  publishedAt: 2026-09-21
```

Pas de `source` ici : `docker` est compilé dans la DLL, donc il n'existe aucune copie
détachée à faire remonter jusqu'au fichier. Une clé absente ne s'affiche pas — elle ne
s'affiche pas vide.

Un thème qui ne déclare pas `meta` le dit tout aussi simplement :

```console
$ slugger --theme-info mon-theme
theme "mon-theme"
  (no metadata declared)
```

`--theme-info <nom>` l'affiche sans rien mesurer — à la différence de `--analyze`, qui charge le
fichier pour en valider la taille. Elle prend un **nom** de thème, comme `--theme`, jamais un
chemin de fichier.

## Installer et retirer un thème

```bash
slugger --analyze ./mon-theme.json     # mesure et écrit mon-theme-analysis.md à côté
slugger --register ./mon-theme.json    # valide puis copie dans --theme-dir
slugger --unregister mon-theme         # supprime le fichier
slugger --list-themes                  # ce qui est disponible
```

**Commence par `--analyze`.** `--register` répond accepté ou refusé ; l'analyse répond *de
combien*. Elle écrit un `.md` à côté de ton fichier avec tes marges sur chaque plancher, les
noms déclarés deux fois, les catégories que personne ne porte, l'écart entre ton adjectif le
plus rare et le plus commun, la longueur du plus long slug possible, et le nombre de
combinaisons.

Elle fonctionne **aussi sur un thème refusé** — c'est même là qu'elle sert : savoir qu'un nom
atteint 8 participes plutôt que 19 te dit quoi corriger, là où le refus dit seulement qu'il en
manque.

```
| Rule                      | Worst case       | Floor  | Margin  |
| Adjectives per noun       | 102 (anglesite)  | 100    | +2      |   ← deux mots de marge
| Participles per noun      | 8 (realgar)      | 20     | -12     |
```

`--register` applique exactement la validation d'un chargement normal — rien n'est copié si le
fichier est refusé. Un thème du même nom déjà présent est un refus, pas un écrasement :
`--unregister` d'abord.

## Lire un refus

Un thème n'est jamais refusé une raison à la fois. Le parsing et la validation rapportent
ensemble, donc une exécution te dit tout ce que le fichier demande :

```console
$ slugger --register ./demo.json
Theme "demo" was refused for 9 reasons:

  - nouns[2]: no non-empty "value".
  - "defaults.sep" must be a single character.
  - "defaults.casing" must be one of kebab, snake, camel.
  - "riviere" references category "aquatique", which the theme does not declare (it declares common, vegetal).
  - 2 nouns, but a theme needs at least 100.
  - "saule" reaches 3 adjectives, but every noun needs at least 100.
  - "riviere" reaches 2 adjectives, but every noun needs at least 100.
  - Category "aquatique" totals 2 combinations, but every category needs at least 40,000.
  - Category "vegetal" totals 3 combinations, but every category needs at least 40,000.
```

Chaque refus nomme son sujet — le nom, la catégorie, la clé — parce qu'un chiffre seul ne dit
pas quoi corriger.

Deux choses ne sont volontairement pas rapportées. Un JSON malformé est terminal : rien ne peut
être lu d'un document qui n'a pas parsé. Et quand une section que les règles lisent est
elle-même malformée, les règles sont sautées pour elle — `"nouns" doit être un tableau` dit déjà
tout.

## Valider le sens d'un thème

`--analyze` et `--register` valident une structure : la taille des pools, les longueurs, les
catégories référencées qui existent bien. Rien là-dedans ne sait qu'une orchidée n'est pas
fragile, ou qu'un mot poli en anglais sonne mal à côté de tel nom. Cette partie-là ne se
mesure pas au chargement, elle se lit dans ce que le thème produit réellement.

Le protocole a trois phases, dans cet ordre, et pas dans un autre : tant que les catégories
elles-mêmes bougent encore, une comparaison automatique n'a rien de stable à comparer.

Les exemples qui suivent viennent tous de `jazz.json`, qui est dans le dépôt : chacun est un
défaut que ce protocole a réellement trouvé, dans cet ordre-là. Le fichier porte aujourd'hui la
correction — c'est elle qui s'y lit, pas le défaut.

### Les sept familles

Tu ne cherches pas « des problèmes ». Une consigne ouverte produit de la vision en tunnel : tu
trouves une famille, et les passes suivantes ne cherchent plus qu'elle. Tu cherches **ces
sept-là**, nommément, et tu reprends la liste à chaque passe.

| | Ce que c'est | Vu sur `jazz` |
| --- | --- | --- |
| 1 | **Fuite de catégorie** — un pool trop large laisse un mot atteindre un nom d'une autre famille | `reed-lined` sur une contrebasse, `gut-strung` sur un saxophone : une seule catégorie `instrument` pour cinq familles d'instruments |
| 2 | **Impossibilité physique** — le mot décrit une propriété que le nom n'a pas | `pentatonic` sur des balais de batterie (aucune hauteur), `felt-hammered` sur un orgue Hammond (aucun marteau), `droning` sur un banjo (aucune tenue) |
| 3 | **Affirmation vérifiable, mauvais sujet** — le mot n'est pas une couleur, c'est un fait | `self-taught` sur Coleman Hawkins, qui a étudié à Washburn College ; `twelve-bar` sur Epistrophy, qui fait 32 mesures |
| 4 | **Anachronisme** — le mot et le nom existent, mais pas au même moment | `bebop-fueled` sur Louis Armstrong, qui a rejeté le bebop publiquement ; `avant-garde` sur Wes Montgomery |
| 5 | **Auto-référence** — l'adjectif répète le nom | `flatted` sur « flatted fifth », `muted` sur « mute », `blue` sur « Blue Monk » |
| 6 | **Le couple adjectif–participe** — chacun juste isolément, faux ensemble | `metronomic-drifting`, `hushed-hollering`, `staccato-sustaining`, `breathless-breathing`, `swung-swinging` |
| 7 | **Faux registre** — grammatical, possible, mais personne ne le dirait | `walking` (une ligne de basse) sur un trille ou un bec |

Les familles 1, 2 et 4 se corrigent par `categories` et `except` ; la 3 aussi, mais elle se
**trouve** autrement (voir la phase 2) ; la 5 par `except` ; la 6 est la raison d'être
d'`incompatible` ; la 7 est la seule qui demande de lire à voix haute.

La 7 est aussi la seule où l'on se trompe **dans l'autre sens**, en corrigeant ce qui allait :
l'argot d'un domaine n'est pas la langue générale. Sur `jazz`, `wailing` appliqué à un batteur a
d'abord été noté comme faux — on gémit avec une voix ou un souffle, pas avec des fûts — avant
vérification : *« the band was really wailing »* veut dire jouer fort et bien, pour n'importe
quel instrument. Devant une tournure qui sonne étrange dans un domaine que tu connais mal,
vérifie l'usage avant de restreindre un pool. Un faux positif ici coûte un mot au thème et ne
corrige rien.

### Phase 1 — un slug à la fois, jusqu'à une passe blanche

Génère environ **10 fois le nombre de noms du thème**, dans son mode par défaut. Pour un thème
de 213 noms comme `flowers`, ça fait 2 000 à 2 500 slugs.

**Lis-les un par un.** Pas groupés par nom, pas en diagonale : un slug, un verdict, le suivant.
Grouper par nom fait lire le nom et survoler les deux mots devant — c'est exactement ce qui
laisse passer les familles 3, 5 et 6, qui ne sont visibles que dans le triplet entier. Un
échantillon de 2 000 slugs se lit par blocs de cent ou deux cents ; ce n'est pas rapide, et
c'est la partie du protocole qui trouve le plus.

Corrige après chaque passe, régénère, recommence. Trois règles tiennent cette boucle :

- **Recommence par la liste, pas par la dernière trouvaille.** La passe qui suit une découverte
  est la plus mauvaise de toutes : tu y cherches ce que tu viens de trouver. Reprends les sept
  familles dans l'ordre.
- **Une passe blanche, ou rien.** La phase se termine sur une passe qui ne trouve **rien** — pas
  sur une passe dont tu as corrigé les trouvailles. Tant que tu n'as pas lu un échantillon
  entier sans rien noter, tu ne converges pas, tu t'arrêtes.
- **Note le rendement de chaque passe.** Une suite qui descend dit que tu converges ; une suite
  plate dit que tu relis la même chose. Sur `jazz` : 38, 2, 29, 1, 3, 3 défauts. Le 29 arrive
  en troisième position parce que c'est là qu'une famille entière — le couple adjectif-participe
  — a été regardée pour la première fois.

C'est cette boucle qui façonne la taxonomie : quelles catégories existent, à quel grain. Pas
l'inverse. Une catégorie ne se crée pas parce qu'elle serait jolie, mais parce qu'un mot
atteignait un nom qu'il ne pouvait pas décrire.

### Phase 2 — la table de vérité, sur tous les axes

Construis une table **indépendante du fichier** : pour chaque nom, quels traits sont réellement
vrais, d'après ce que tu sais du sujet, jamais d'après ce que le JSON déclare déjà. Compare
ensuite à `categories`. Un **faux positif** — un tag qui autorise un mot qui ne devrait pas
s'appliquer — est le bug grave, et se corrige dès que tu es confiant. Un **faux négatif** — un
trait réel non déclaré — ne fait que réduire un pool : note-le, ne le force pas si le cas est
incertain.

Deux exigences, et ce sont elles qui font le rendement de cette phase :

**Couvre chaque catégorie de noms, pas celle qui a déjà donné.** Sur `jazz`, la table a d'abord
été faite pour les 48 instruments — et déclarée finie. Les 67 musiciens n'ont été audités qu'à
la passe suivante, et c'est là qu'est sorti `scatting` appliqué à Count Basie, pianiste, parce
que le pool vocal atteignait tout le monde.

**Passe chaque mot de chaque pool au test du fait.** La question est : *est-ce qu'on peut me
contredire avec une source ?* « tormented » non, c'est une lecture, et un lecteur qui n'est pas
d'accord n'a pas raison. « self-taught » oui — il y a une biographie. Tout mot qui passe ce test
est une **affirmation**, et une affirmation doit être vraie de **chaque** nom qu'elle atteint.
Elle s'audite donc nom par nom, exhaustivement, jamais par échantillon : un mot faux sur trois
noms sur cent ne sortira pas d'un tirage, et sortira devant le premier lecteur qui connaît le
sujet. C'est ce test, appliqué tard, qui a trouvé les deux derniers défauts de `jazz`.

Cette passe couvre les 100+ noms, y compris ceux qu'un tirage n'aurait pas fait sortir souvent.
C'est ce qui la rend complémentaire des revues par échantillon plutôt que redondante.

### Phase 3 — une vérification que la correction ne peut pas satisfaire

**Ne vérifie jamais une correction en cherchant le motif que tu viens de corriger.** Ça ne
prouve que la correction, et ça ne trouve rien d'autre par construction. Tu as passé la phase 1
à découvrir que tu ne sais pas d'avance ce que tu cherches : ne reviens pas à une vérification
qui le suppose.

Redécompose plutôt chaque slug en **adjectif + participe + nom**, par plus longue correspondance
sur les listes du fichier lui-même, et vérifie chaque partie contre le `except` de son nom et
contre `incompatible`. Le filtre est indépendant de ce que tu viens de corriger : il vaut pour
tout ce que le fichier déclare, y compris ce que tu déclareras plus tard. Un `--count 10000`
passé dedans se lit en une seconde, et couvre ce qu'aucune relecture ne couvre.

Garde-le exact : un préfixe n'est pas un mot. Sur `jazz`, une première version a signalé
`blue-note-returning-blue-train` comme violant l'exclusion de `blue` — l'adjectif tiré était
`blue-note`, et sur *Blue Train*, seul album de Coltrane en leader chez **Blue Note**, il tombe
même juste.

Puis relis un dernier échantillon frais, à la main, sur **chaque mode que le thème promet
explicitement** : son mode par défaut, `--max-segment-words none` si `maxLength` est promis sans
restriction, `--segment either` si le thème garde un sens ainsi. Compte **25 à 30 fois le nombre
de noms par mode** — sur `flowers`, autour de 10 000 slugs cumulés. Son rôle est de confirmer que
rien ne s'est cassé. Si elle trouve quand même du fond, la taxonomie n'était pas stable : retour
en phase 1.

### Ce qui fait rater le protocole

Les quatre façons de croire qu'on l'a suivi, toutes vues sur `jazz` :

- **Déclarer la convergence sans passe blanche.** Une passe qui trouve et corrige n'est pas une
  passe qui converge. Tu t'arrêtes sur du blanc, pas sur du corrigé.
- **Rétrécir le champ à ce qui a déjà donné.** Après les instruments, les passes suivantes n'ont
  plus lu que des instruments — les musiciens, les titres et les termes techniques sont restés
  intacts trois passes de plus.
- **Prendre un `grep` pour une vérification.** Chercher le motif corrigé, le trouver absent, et
  appeler ça une phase 3.
- **Traiter une affirmation comme une couleur.** Le pool entier se lit comme de la poésie, donc
  le mot vérifiable qui s'y cache se lit comme de la poésie aussi — jusqu'au lecteur qui connaît
  le sujet.

Aucun de ces échantillons ni la table de vérité de la phase 2 ne sont des fichiers du dépôt —
jetables comme le `.md` que `--analyze` écrit à côté du thème. Seul le thème corrigé reste.

---

Pourquoi ces choix : [`idr/`](idr/).
