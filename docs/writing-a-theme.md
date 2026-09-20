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

## Ce qui arrive à tes valeurs

Chaque `value` est nettoyée au chargement : espaces de début et de fin retirés, espaces
multiples réduits à un seul, passage en minuscule. **Les accents et caractères spéciaux sont
conservés tels quels** — `" René     Dupont "` devient `rené dupont`, jamais `rene-dupont`.

Les espaces internes survivent jusqu'au formatage, où `--sep` les remplace — ou `--word-sep`
si tu veux qu'ils deviennent autre chose :

| | `gorgeous` + `"John Doe"` |
| --- | --- |
| par défaut | `gorgeous-john-doe` |
| `--word-sep _` | `gorgeous-john_doe` |
| `--word-sep ''` | `gorgeous-johndoe` |

Une valeur en plusieurs mots est donc parfaitement normale — `"Oracle Park"`, `"Babe Ruth"`.

## Les trois règles de taille

Elles portent sur le pool **réellement résolu**, pas sur la taille des listes : un fichier de
500 adjectifs dont 480 tiennent dans une catégorie laisse les autres noms avec une douzaine de
choix, et un comptage global ne le verrait pas.

1. Au moins **100 noms** distincts.
2. Chaque nom doit atteindre au moins **100 adjectifs** (les siens + `common`).
3. Chaque catégorie doit totaliser au moins **40 000 combinaisons**, où
   `combos(noun) = |pool(noun)| × max(1, |partPool(noun)|)`.

Le seuil de 40 000 est le point où un thème a besoin d'un suffixe pour éviter les collisions :
Docker (108 × 236 = 25 488) et Heroku (91 × 95 = 8 645) sont tous deux en dessous, et tous deux
en ajoutent un. Les trois thèmes livrés les passent sans aide.

**Si tu es en dessous et que tu l'assumes**, deux façons de lever les trois seuils :

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
| `segmentMode` | `adjective`, `participle`, `either` ou `both` (défaut) |
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
slugger --register ./mon-theme.json    # valide puis copie dans --theme-dir
slugger --unregister mon-theme         # supprime le fichier
slugger --list-themes                  # ce qui est disponible
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

Pourquoi ces choix : [`adr/`](adr/).
