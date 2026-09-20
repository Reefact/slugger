# Écrire un thème

Un thème est un fichier JSON. Aucun code, aucune recompilation : tu le déposes dans
`~/.slugger/themes/` (ou n'importe quel dossier passé à `--theme-dir`) et `slugger --theme
<nom-du-fichier>` s'en sert.

Le thème est identifié par **son nom de fichier**, jamais par un champ interne. `porno.json`
devient `--theme porno`. Un fichier qui porte le nom d'un thème embarqué (`slugger`, `heroku`,
`docker`) le masque — c'est autorisé, mais annoncé.

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

Sous `either`, un seul mot précède le nom et il est tiré dans les deux sections réunies,
proportionnellement à leur taille : 178 adjectifs et 20 participes, c'est un pool de 198 dont le
participe sort une fois sur dix. C'est pourquoi ce sont les deux ensemble qui doivent faire 100,
et non chacun de leur côté.

Sous `both`, le participe est un mot **de plus** à côté de l'adjectif, dans le slug autant que
lui — un nom qui n'en atteint que trois répète son mot du milieu sans fin. D'où la règle 3, dont
le seuil est bas et le restera le temps que les thèmes livrés soient étoffés.

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
| `segmentMode` | `adjective`, `participle`, `either` ou `both` (défaut) — décide aussi des planchers, voir ci-dessus |
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

---

Pourquoi ces choix : [`idr/`](idr/).
