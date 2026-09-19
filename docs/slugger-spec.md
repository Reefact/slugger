# Spécification — slugger

2026-09-18 · @Someone

## Vue d'ensemble

slugger est un CLI .NET qui génère des slugs `adjectif-nom` (façon Docker ou Claude Code), à partir de thèmes JSON fournis par l'utilisateur, avec deux besoins que les générateurs existants ne couvrent pas :

- Restreindre quels adjectifs peuvent accompagner quel nom, à l'intérieur d'un même thème (via des catégories).
- Permettre plusieurs thèmes nommés, sélectionnables en ligne de commande.

Une seule dépendance NuGet pour le moteur : `FirstClassErrors` (Apache-2.0, sans dépendance propre), qui porte `Outcome` et le modèle d'erreur — voir Rapport de chargement. Tout le reste est sur la BCL (`System.Text.Json` inclus dans le SDK) — voir Architecture pour le découpage bibliothèque/CLI.

## Architecture : bibliothèque + CLI

`slugger` n'est pas un bloc monolithique : le moteur de génération est une bibliothèque autonome, `Slugger`, que le CLI consomme comme n'importe quel autre projet .NET pourrait le faire — sans jamais installer ni invoquer le CLI.

**`Slugger`** (bibliothèque, une seule dépendance : `FirstClassErrors`) porte tout ce qui touche à la génération elle-même :

- Le modèle `Theme` (`adjectives`/`participles`/`nouns`/`defaults`/`allowSmall`) et sa sérialisation JSON
- Les 3 thèmes embarqués (`slugger`, `heroku`, `docker`) en ressources — c'est elle qui les porte, pas le CLI (voir Fourniture des thèmes)
- L'algorithme de résolution (pools, catégories, `pool(noun)`/`partPool(noun)`)
- La génération (`segmentMode`, token — longueur, hex, collé, probabiliste —, normalisation, séparateur/casse)
- La validation (3 règles de Taille minimale d'un thème, avec override `allowSmall`)
- Le tirage pondéré multi-thème

**`Slugger.Cli`** (l'exécutable `slugger`) référence `Slugger` et n'ajoute que l'orchestration propre à une interface en ligne de commande : parsing des flags, boucle REPL/oneshot, résolution de `--theme-dir`, `--register`/`--unregister` (opérations fichier), persistance `--init`, et `--clipboard` — qui reste la seule dépendance externe du projet (`TextCopy`), mais scopée au CLI : `Slugger` n'en a besoin pour rien, sa seule dépendance étant `FirstClassErrors`.

Surface publique minimale de `Slugger`, pour un usage direct sans CLI. Le paquet expose le namespace `Slugger` (la façade) et `Slugger.Domain` ; `Slugger.Application` et `Slugger.Infrastructure` sont `internal` — ils disent comment le moteur est construit, pas ce qu'il offre, et rien au dehors ne dépend de leur stabilité :

```csharp
var theme = Theme.LoadEmbedded("docker");   // ou Theme.LoadFromFile(path), Theme.LoadFromJson(json)
var slug = SlugGenerator.Generate(theme, new GenerationOptions());
```

`GenerationOptions` porte les mêmes leviers que les flags CLI (`sep`, `casing`, `segmentMode`, `tokenLength`, `tokenHex`, `tokenGlued`, `tokenChance`, une graine optionnelle) — le CLI ne fait que les remplir depuis les arguments de la ligne de commande plutôt que d'avoir sa propre logique de génération. Un consommateur de `Slugger` peut aussi fournir son propre `Theme` construit en mémoire, sans passer par un fichier JSON du tout.

## Format d'un thème (fichier JSON)

Un thème = un fichier JSON = un namespace complet et étanche (voir Résolution et isolation des thèmes). La duplication de mots dans le texte JSON est acceptée et normale ; elle n'a aucun coût mémoire (voir Modèle mémoire).

```json
{
  "adjectives": {
    "categorie_a": ["mot1", "mot2"],
    "categorie_b": ["mot3"],
    "common": ["mot4"]
  },
  "participles": {
    "categorie_a": ["orbiting", "dreaming"],
    "common": ["floating"]
  },
  "nouns": [
    { "value": "nom1", "categories": ["categorie_a"] },
    { "value": "nom2", "categories": ["categorie_a", "categorie_b"] },
    { "value": "nom3" }
  ],
  "defaults": {
    "sep": "_",
    "casing": "kebab",
    "tokenLength": 4,
    "tokenHex": false
  },
  "allowSmall": false
}
```

- `participles` (optionnel) : même structure que `adjectives` (catégories + `common`). Sa présence ajoute un segment participe présent au slug — voir Segment participe présent.

* `allowSmall` (optionnel, défaut `false`) : à `true`, outrepasse le refus de chargement pour un thème sous le seuil de 100 adjectifs / 100 noms — voir Taille minimale d'un thème.

- `defaults` (optionnel) : les préférences de formatage propres au style du thème (séparateur, casse, suffixe). Ignoré sauf si `--mimic-style` est actif — voir Style hérité.

* `adjectives` : dictionnaire `nom de catégorie → liste d'adjectifs`. Les noms de catégories sont libres (chaînes arbitraires définies par l'auteur du fichier). `common` est la seule catégorie au statut particulier : **tout nom l'atteint**, en plus de celles qu'il déclare (voir Algorithme de résolution). Un thème n'a donc jamais à l'écrire sur un nom.
* `nouns` : liste d'objets `{ value, categories }`. `categories` est optionnel : l'omettre équivaut à `[]`, et c'est la forme normale pour un nom sans catégorie propre — `docker.json` n'est ainsi qu'une liste de `{ "value": ... }`. Un nom peut appartenir à zéro, une ou plusieurs catégories.
* Un même mot (adjectif ou nom) peut apparaître dans plusieurs catégories, ou être recopié tel quel dans plusieurs thèmes, sans contrainte.

## Algorithme de résolution

Pour un nom donné, le pool d'adjectifs disponibles est l'ensemble des adjectifs dont au moins une catégorie est commune avec les catégories du nom :

```
pool(noun) = { adj | adj.categories ∩ (noun.categories ∪ {common}) ≠ ∅ }
```

- `common` est un socle partagé, pas un repli réservé aux noms qui ne déclarent rien : un nom sans catégorie atteint `common`, un nom qui en déclare atteint les siennes **et** `common`. Le thème `slugger` en dépend directement — ses six catégories font 45 adjectifs chacune et `common` 60, donc aucune n'atteint seule le seuil de 100 : c'est l'addition avec `common` qui le franchit. C'est le seul écart au principe « une catégorie est une chaîne libre sans signification pour le code ».
- Le tirage se fait toujours entièrement à l'intérieur d'un seul thème (voir Résolution et isolation des thèmes) : jamais de croisement entre catégories de fichiers différents, même si elles portent le même nom.
- Étapes de génération : 1) choisir un thème, 2) tirer un nom au hasard dans ce thème, 3) calculer son pool d'adjectifs, 4) tirer un adjectif au hasard dans ce pool, 5) formater le slug.
- Validation au chargement : chaque catégorie référencée dans `nouns[].categories` doit exister comme clé dans `adjectives` **ou** dans `participles` du même fichier, sinon erreur explicite nommant la catégorie manquante et les catégories connues. Les deux suffisent l'une comme l'autre : `heroku` classe ses noms par capacité physique (`eau`, `mobile`, `lumineux`...) pour piloter ses participes, alors que ses adjectifs tiennent dans le seul `common` — ces catégories n'existent donc que côté `participles`, et le thème est valide.

## Normalisation des valeurs

Appliquée à chaque `value` chargée depuis le JSON (adjectif ou nom), dans cet ordre :

1. `trim()` — espaces en début/fin retirés
2. Collapse des espaces multiples consécutifs en un seul
3. Passage en minuscule
4. Chaque espace restant remplacé par le séparateur (`--sep`)

Exemple : `" John     Doe             "` → `"john-doe"` (avec `--sep -`).

Les accents et caractères spéciaux sont conservés tels quels, sans translittération ni rejet : ce qui est écrit dans le JSON est voulu (`" René     Dupont "` → `"rené-dupont"`).

Une valeur composée (plusieurs mots) est traitée comme un seul token logique par le générateur — l'étape 4 n'est qu'un détail de formatage final, elle n'introduit pas d'ambiguïté sur la frontière adjectif/nom puisque cette frontière est connue par construction, pas reparsée depuis le slug final.

## Séparateur et formatage du slug final

Un seul séparateur configurable (`--sep`, défaut `-`) sert à deux choses :

- Joindre l'adjectif et le nom tirés.
- Remplacer les espaces internes d'une valeur composée (étape 4 de la normalisation).

Exemple avec `--sep -` : adjectif `gorgeous` + nom `"John Doe"` → `gorgeous-john-doe`.

Ce choix assume qu'aucun besoin de reparser un slug généré pour en extraire l'adjectif et le nom d'origine n'existe : la frontière entre les deux n'est pas récupérable depuis le texte final seul si le nom contient lui-même le séparateur.

## Résolution et isolation des thèmes

Un thème est identifié par son nom de fichier, pas par un champ interne au JSON : `--theme porno` charge `<theme-dir>/porno.json`. L'unicité est garantie par le système de fichiers, sans validation applicative supplémentaire.

- Un thème = un namespace complètement étanche. Deux fichiers peuvent avoir une catégorie du même nom (ex: `feminin` dans deux fichiers différents) sans aucun rapport entre elles.
- Le tirage nom+adjectif se fait toujours à l'intérieur d'un seul thème chargé, jamais en croisant les catégories de plusieurs fichiers.
- `--theme-dir <path>` permet de pointer vers un dossier de thèmes autre que celui par défaut (`~/.slugger/themes/`).
- `--list-themes` liste les thèmes disponibles dans `--theme-dir`.
- Sélection quand plusieurs thèmes sont explicitement en scope (via `--theme <plusieurs>`) : tirage pondéré par effectif, sans concaténer les listes de noms. Construire un tableau des effectifs cumulés par thème (`[nA, nA+nB, nA+nB+nC, ...]`), tirer un nombre aléatoire dans `[0, total)`, localiser l'intervalle par recherche dichotomique pour choisir le thème — proportionnellement à sa taille — puis tirer un nom uniformément dans la liste déjà en mémoire de ce thème. Mathématiquement équivalent à une liste plate concaténée (`P = 1/(nA+nB+...)` par nom dans les deux cas), mais en `O(D)` de mémoire additionnelle (D = nombre de thèmes) plutôt qu'en `O(N)` (N = nombre total de noms). Le pool d'adjectifs du nom tiré reste résolu uniquement dans son thème source.
- Fourniture des thèmes : un seul format (JSON), deux origines. Un thème nommé `slugger` est embarqué en ressource dans le binaire (fonctionne dès l'installation, sans setup). Tout autre thème est un fichier `.json` dans `--theme-dir`, même schéma, ajouté/édité librement sans recompiler.
- Résolution de `--theme <name>` : cherche d'abord `<name>.json` dans `--theme-dir` (le custom prend le pas), sinon dans les ressources embarquées — un fichier `slugger.json` dans `--theme-dir` surcharge donc le thème embarqué du même nom.
- `slugger` participe au tirage pondéré exactement comme les autres thèmes dès qu'il est nommé explicitement dans un `--theme` à plusieurs valeurs, sans traitement spécial — mais par défaut (sans `--theme`), c'est le seul actif : rien à exclure puisqu'il n'y a personne d'autre à écarter.
- `docker` et `heroku` sont embarqués en ressources dans le binaire, au même titre que `slugger`. Les deux suivent le même principe éditorial : garder les noms officiels de la source, réinventer et étoffer adjectifs et participes en veillant à ce qu'aucun adjectif ne prenne une forme en *-ing* (frontière nette entre les deux segments, contrairement aux originaux qui mélangent les deux — ex. *billowing*/*falling* chez Haikunator, *admiring*/*charming* chez Docker). `docker.json` : 236 noms de famille de scientifiques officiels inchangés (source `moby/moby/pkg/namesgenerator`, Apache 2.0), adjectifs entièrement réinventés (une seule catégorie `common`, comme l'original), participes composés des 16 mots officiels qui étaient en réalité des participes déguisés en adjectifs (*admiring*, *recursing*, *trusting*...) plus des ajouts originaux — teneur DevOps/CS (*compiling*, *debugging*, *shipping*, *refactoring*...). `heroku.json` : noms nature repris de la source Haikunator (MIT) et complétés d'un lot de noms originaux dans le même registre pour dépasser la centaine ; adjectifs entièrement réinventés, en une seule catégorie `common` (un adjectif générique va avec n'importe quel nom, fidèle à l'esprit Haikunator) ; participes également réinventés, répartis en catégories fonctionnelles (voir Segment participe présent). Mots extraits ou inspirés des sources officielles au moment de l'implémentation, pas recopiés de mémoire ; licence/attribution mentionnée dans le fichier ou un README à côté.

  Sous leurs chiffres officiels bruts, Docker (236 noms, 108 adjectifs, 25 488 combinaisons) et Heroku (95 noms, 91 adjectifs, 8 645 combinaisons) sont sous le seuil de validation minimale de `slugger` (voir Taille minimale d'un thème). `docker.json` et `heroku.json`, en gardant les noms officiels et en étoffant adjectifs et participes, dépassent les trois seuils par eux-mêmes — `allowSmall` n'est nécessaire pour aucun des deux. Ce ne sont pas des reproductions contraintes par un vocabulaire historique limité, mais des thèmes à part entière qui gardent seulement le style (format, séparateur, casse, token) de l'original.

  Livré : `docker.json` garde 236 noms de scientifiques officiels inchangés (le fichier source contient, en dehors du tableau, des chaînes entre guillemets à l'intérieur des commentaires bibliographiques — surnoms comme `"Bill"` Gates ou `"Steve"` Shirley, titres d'ouvrages, citations —, exclues du compte), avec 187 adjectifs entièrement réinventés et 76 participes (les 16 mots officiels réellement participiaux, réintégrés à leur bonne place, plus 60 ajouts à teneur DevOps/CS). Passe les trois règles par lui-même (236 noms ≥ 100, 187 adjectifs ≥ 100, 3 354 032 combinaisons résolues ≥ 40 000, contre 25 488 pour l'original). `defaults` : `sep: "_"`, `casing: "snake"`, `segmentMode: "either"` (un seul mot avant le nom, comme l'original), `tokenLength: 1`, `tokenHex: false` (un seul chiffre décimal, comme le vrai suffixe de retry de Docker), `tokenGlued: true` (collé sans séparateur — `focused_turing3`, pas `focused_turing_3`), `tokenChance: 1` (1 % des générations, pour simuler la rareté d'une collision sans en implémenter la détection réelle). `slugger --theme docker` (thème unique, defaults auto-appliqués) : `fearless_haibt`, `admiring_gauss` — un seul mot avant le nom, conformément à `segmentMode: either`. `slugger --theme docker --segment both` (override explicite) : `humorous_computing_hodgkin`, `agile_thinking_chandrasekhar`.
- Comportement par défaut, sans `--theme` : seul le thème `slugger` est utilisé, jamais de tirage pondéré entre plusieurs thèmes sans demande explicite. Pour élargir : `--theme <un seul>` (thème unique, auto-mimic — voir Style hérité) ou `--theme <plusieurs>` (mode multi-thème, tirage pondéré ci-dessus).

## Modèle mémoire

Le fichier JSON reste optimisé pour la lisibilité humaine : duplication de strings autorisée et normale (voir Format d'un thème). L'optimisation mémoire se fait uniquement au chargement, de façon invisible pour l'auteur du thème.

- Chaque string lue depuis le JSON passe par un pool d'internement partagé (`Dictionary<string,string>` avec `GetOrAdd`, ou `string.Intern`).
- Le pool est unique pour tout le run du programme, instancié une fois au point d'entrée et transmis à chaque chargement de fichier — jamais recréé par fichier, sinon la dédup ne fonctionnerait qu'à l'intérieur d'un seul fichier.
- Résultat : un même mot (`"gorgeous"`) apparaissant dans plusieurs catégories, plusieurs noms ou plusieurs thèmes différents ne correspond qu'à une seule instance en mémoire.
- Cette dédup mémoire est indépendante de la logique de résolution : elle ne crée aucun lien logique entre deux catégories de même nom dans des fichiers différents (voir Résolution et isolation des thèmes).

## Interface CLI

Nom d'outil : `slugger`.

| Option | Rôle |
| --- | --- |
| `--theme <name>[, <name>...]` | Thème(s) autorisé(s) — répétable et/ou liste séparée par virgules |
| `--theme-dir <path>` | Dossier de thèmes, si différent du dossier par défaut |
| `--sep <char>` | Séparateur (défaut `-`) |
| `--casing <kebab\|snake\|camel>` | Format de sortie |
| `--token-length <n>` | Ajoute un suffixe de `n` caractères au slug (0 = aucun) |
| `--token-hex` | Le suffixe (`--token-length`) est en hexadécimal plutôt que numérique |
| `--token-chance <0-100>` | % de chance que le token apparaisse quand tokenLength > 0 (défaut 100) |
| `--token-glued` | Colle le token au segment précédent sans séparateur (défaut : séparé) |
| `--segment <adjective\|participle\|either\|both>` | Override explicite du `segmentMode` du thème (voir Segment participe présent) |
| `--count <n>` | Nombre de slugs à générer (mode oneshot uniquement) |
| `--seed <n>` | Graine aléatoire pour un résultat reproductible |
| `--list-themes` | Liste les thèmes disponibles |
| `--oneshot` | Génère un slug puis quitte, au lieu du mode REPL |
| `--clipboard` | Copie le résultat généré dans le presse-papiers (désactivé par défaut) |
| `--mimic-style` | Flag à 3 états (absent/true/false) contrôlant l'application des `defaults` du thème actif, voir Style hérité |
| `--allow-small-theme` | Override ponctuel du refus sous les seuils de Taille minimae d'un thème |
| `--init` | Sauvegarde les options de cette commande comme config par défaut, ne génère rien |
| `--register <path>` | Valide le thème puis le copie dans --theme-dir ; ne génère rien |
| `--unregister <name>` | Supprime un thème custom de --theme-dir ; ne génère rien |

Exemples :

```bash
slugger --theme porno --sep '-'
slugger --count 5 --seed 42
slugger --list-themes
```

Support de plusieurs thèmes : résolu, voir Filtrage des thèmes ci-dessous.

## Filtrage des thèmes

Deux cas d'usage :

- `--theme porno` — un seul thème autorisé
- `--theme porno,animaux` (ou `--theme porno --theme animaux`, les deux formes cumulables) — plusieurs thèmes autorisés

## Enregistrement de thèmes personnalisés (--register / --unregister)

`--register <path>` charge le fichier situé à `<path>` et lui applique exactement la même validation qu'un chargement normal au runtime : structure JSON conforme au schéma (voir Format d'un thème), cohérence des catégories (chaque catégorie référencée dans `nouns[].categories` doit exister comme clé dans `adjectives` ou dans `participles`, sinon erreur explicite nommant la catégorie manquante — voir Algorithme de résolution), et les 3 règles de Taille minimale d'un thème. `--allow-small-theme` fonctionne aussi ici, exactement comme à l'usage normal, pour outrepasser ponctuellement un thème volontairement réduit sans éditer son JSON.

- Fichier invalide → erreur explicite (même format que les erreurs de chargement runtime, pas un message différent), rien n'est copié.
- Fichier valide → copié vers `<theme-dir>/<nom-du-fichier>.json`, où `<nom-du-fichier>` est le nom du fichier source sans son extension — un thème reste identifié par son nom de fichier, jamais par un champ interne (voir Résolution et isolation des thèmes), `--register` ne fait pas exception.
- Un thème du même nom existe déjà dans `--theme-dir` → refus explicite ("un thème `<name>` existe déjà dans `--theme-dir` ; `--unregister <name>` d'abord pour le remplacer"), pour ne jamais écraser un fichier existant par accident.
- Le nom coïncide avec un thème embarqué (`slugger`, `heroku`, `docker`) → autorisé, c'est la règle de surcharge déjà prévue (un fichier du même nom dans `--theme-dir` prend le pas sur l'embarqué), mais un avertissement est affiché pour que ce ne soit jamais silencieux.

`--unregister <name>` supprime `<theme-dir>/<name>.json`.

- Aucun fichier de ce nom dans `--theme-dir` → erreur explicite.
- Le nom correspond à un thème embarqué sans fichier custom du même nom → refus ("`docker` est embarqué dans le binaire, rien à désinscrire — omets-le simplement de `--theme` pour ne pas l'utiliser") : on ne peut pas désinscrire ce qui n'est pas un fichier.

Ni l'un ni l'autre ne génère de slug, comme `--init` et `--list-themes` : action exécutée, confirmation affichée, puis le programme quitte.

## Mode d'exécution : REPL vs oneshot

Par défaut, le CLI reste ouvert en mode REPL : chaque appui sur Entrée génère `--count` slugs d'un coup (1 par défaut), puis la boucle continue ; `Ctrl+C` quitte.

`--oneshot` génère un slug (ou `--count` slugs) puis quitte immédiatement — sans boucle interactive.

Détection automatique : si l'entrée standard n'est pas un terminal interactif (`Console.IsInputRedirected` vrai — cas d'un pipe, d'un script, ou d'un runner CI/CD), le CLI bascule automatiquement en mode oneshot même sans `--oneshot` explicite, pour éviter tout blocage sur une boucle `ReadLine()` qui n'aura jamais d'entrée.

## Persistance de configuration (--init)

`--init` ne génère aucun slug : il persiste toutes les autres options passées sur la même ligne de commande dans un fichier de config (`~/.config/slugger/config.json`, convention XDG), pour qu'elles deviennent les valeurs par défaut des exécutions futures.

Toute option de ce document est concernée de la même façon, sans traitement spécial par option : `--theme`, `--sep`, `--casing`, `--oneshot`, `--clipboard`, etc.

Priorité de résolution à l'exécution : argument explicite sur la ligne de commande > config sauvegardée par `--init` > valeur par défaut du programme.

## Presse-papiers (--clipboard)

`--clipboard` copie automatiquement le slug généré dans le presse-papiers système. Désactivé par défaut : c'est un effet de bord qu'on n'attend pas forcément d'un CLI, et en mode REPL chaque Entrée écraserait silencieusement le contenu précédent du presse-papiers.

Configurable via `--init` comme toute autre option.

Dépendance technique : la BCL .NET n'a pas d'accès cross-platform (Windows/macOS/Linux) au presse-papiers. Utilisation de la librairie NuGet `TextCopy`, seule dépendance externe propre au projet `Slugger.Cli` (voir Architecture) — `Slugger` ne la tire pas, puisque `--clipboard` n'a aucun sens hors d'un contexte CLI.

## Style hérité (--mimic-style)

`--mimic-style` fait reprendre à `slugger` les préférences de formatage propres au thème actif, définies dans son bloc `defaults` (voir Format d'un thème), au lieu des valeurs par défaut du programme.

Quatre arguments ont leur place dans `defaults`, parce qu'ils font partie de l'identité visuelle historique du style imité, pas d'une préférence de session :

```
| Argument | Exemple |
| --- | --- |
| `sep` | Docker utilise `_`, Heroku/slugger utilisent `-` |
| `casing` | CoHérent avec le séparateur historique du style |
| `tokenLength` | Heroku/Haikunator ajoute un nombre à 4 chiffres (`wispy-dust-1337`) |
| `tokenHex` | Docker ajoute un suffixe hex seulement en cas de collision |
| `tokenChance` | Docker n'ajoute son chiffre qu'en cas de collision, donc rarement : simulé à 1% |
| `tokenGlued` | Docker colle son chiffre de collision sans séparateur (focused\_turing3) |

Ne font PAS partie de `defaults`, car ce sont des préférences de session sans rapport avec le thème : `--count`, `--seed`, `--theme`/`--theme-dir`, `--oneshot`, `--clipboard`.

Ces quatre arguments (`--sep`, `--casing`, `--token-length`, `--token-hex`) sont gérés nativement par `slugger`, indépendamment de `--mimic-style` — ce sont des options CLI de base, voir Interface CLI.

Le déclencheur de l'application des `defaults` hérités n'est pas `--mimic-style` en soi, mais le nombre de thèmes actifs.

- **Un seul `--theme <name>` explicite** (choix univoque) → les `defaults` du thème (`sep`, `casing`, `tokenLength`, `tokenHex`, `tokenGlued`, `tokenChance`, `segmentMode`) s'appliquent automatiquement, sans flag. `slugger --theme heroku` seul reproduit le style de l'original — un seul mot avant le nom (`segmentMode: either`) — sans qu'aucun flag ne soit nécessaire, par exemple `weathered-iceberg-7789` (adjectif tiré) ou `rising-horizon-8096` (participe tiré).
- **Plusieurs thèmes en scope** (via `--theme <plusieurs>`) → les défauts globaux de `slugger` s'appliquent pour garder un format cohérent entre les tirages, sauf si `--mimic-style` est explicitement passé — chaque génération applique alors les `defaults` du thème réellement tiré, quitte à varier de tirage en tirage.

`--mimic-style` est en réalité un flag à 3 états, pas un simple booléen de présence : absent (comportement automatique ci-dessus) ; `--mimic-style` seul ou `--mimic-style true` (force l'application des `defaults`, même en mode multi-thème) ; `--mimic-style false` (force la **non**-application des `defaults`, même avec un seul thème sélectionné — retour aux valeurs par défaut de `slugger`). Un argument CLI explicite (`--sep`, `--casing`...) reste toujours prioritaire par-dessus, quel que soit l'état de `--mimic-style`, permettant par exemple `slugger --theme heroku --mimic-style false --sep =` : ignore les `defaults` de `heroku.json`, puis force `=` comme séparateur.

`segmentMode` (voir Segment participe présent) suit exactement cette même règle plutôt qu'une règle à part : il rejoint le bloc `defaults`.

`tokenChance` suit exactement la même règle : il rejoint lui aussi le bloc `defaults`.

Priorité de résolution : argument CLI explicite > `defaults` du thème (thème unique, ou `--mimic-style` actif en mode multi-thème) > config sauvegardée par `--init` > valeur par défaut du programme.

## Rapport de chargement

Un thème n'est jamais refusé une raison à la fois. Le parsing collecte toutes les sections malformées avant d'abandonner, la validation passe toutes les règles sur tous les noms et toutes les catégories, et **les deux étapes rapportent ensemble** : un fichier qui a quatre problèmes de forme et vingt échecs de règle en signale vingt-quatre en une seule exécution, pas quatre puis vingt.

Deux exceptions délibérées. Un JSON malformé est terminal — rien ne peut être lu d'un document qui n'a pas parsé. Et lorsqu'une section que les règles elles-mêmes lisent est malformée, les règles sont sautées pour elle : `"nouns" doit être un tableau` dit déjà tout, et `0 nom, au moins 100 requis` par-dessus serait du bruit, pas une seconde trouvaille.

Le chargement renvoie un `Outcome<Theme>` (`FirstClassErrors`) dont l'erreur porte chaque raison en `InnerErrors`. Une factory par situation — et non une mise en forme au point d'appel — est ce qui garantit qu'un thème refusé par `--register` et le même thème refusé au runtime se lisent identiquement : la formulation est écrite une fois, là où l'erreur est levée. Les formes `LoadEmbedded`/`LoadFromFile`/`LoadFromJson` restent disponibles et lèvent une `DomainException` qui transporte le même rapport complet.

## Taille minimale d'un thème

Validation en deux temps, sur le pool réellement résolu — pas sur un simple comptage de listes brutes :

1. Nombre total de noms distincts dans le fichier < 100 → refus.
2. Pour chaque nom, calculer `pool(noun)` (l'union des adjectifs de `common` et des catégories du nom, comme défini dans Algorithme de résolution) ; si `|pool(noun)| < 100` pour au moins un nom → refus, en nommant précisément ce nom et la taille de son pool.
3. Pour chaque catégorie C utilisée par au moins un nom, calculer `combos(C) = Σ combos(noun)` pour tous les noms où C figure dans `categories` (un nom dans plusieurs catégories contribue à chacune — pas une partition, juste une vérification de couverture par branche) ; si `combos(C) < 40 000` → refus, en nommant la catégorie et son total.

Cette deuxième règle garantit ce que le simple comptage global ne pouvait pas garantir : même un fichier avec des centaines d'adjectifs au total est refusé si un nom particulier est coincé dans une petite catégorie.

Ce seuil porte exclusivement sur `pool(noun)` (les adjectifs) : les participes (voir Segment participe présent) n'entrent pas dans cette validation minimale — un nom doit passer la barre des 100 adjectifs qu'il ait ou non des participes associés.

La troisième règle évite qu'une branche entière (une catégorie) soit pauvre en combinaisons même si chaque nom individuel dépasse le seuil de 100 adjectifs — utile quand une catégorie a peu de noms au total, ou des pools d'adjectifs proches du minimum. Elle utilise le même seuil (40 000) que la recommandation du guide de création, mais en refus bloquant plutôt qu'en simple conseil.

Deux façons d'outrepasser ce refus, pour un auteur qui assume une taille réduite :

- Champ `"allowSmall": true` dans le JSON du thème lui-même — déclaré une fois par son auteur, vaut pour toutes les exécutions futures.
- Flag CLI `--allow-small-theme` pour un override ponctuel (tester un thème en cours de rédaction sans éditer le fichier).

## Guide de création d'un thème

Recommandation, non imposée par la validation : pour chaque nom, `combos(noun) = |pool(noun)| × max(1, |partPool(noun)|)` — le `max(1, ...)` évite d'annuler le compte quand le nom n'a pas de participe associé (voir Segment participe présent). Le total du thème est `total_combinaisons = Σ combos(noun)` sur tous les noms. Si ce total est inférieur à 40 000, activer un token par défaut dans `defaults` (`tokenLength`, éventuellement `tokenHex`) pour limiter le risque de collision.

Ce seuil est coHérent avec les précédents observés : Docker (108 adjectifs × 236 noms = 25 488 combinaisons) et Heroku/Haikunator (91 × 95 = 8 645) sont tous deux sous ce seuil, et les deux compensent effectivement par un suffixe — Docker en ajoute un uniquement en cas de collision détectée, Heroku en ajoute un systématiquement par défaut.

## Segment participe présent (optionnel)

Inspiré du format à 3 mots des fichiers de plan de Claude Code (`dreamy-orbiting-quokka.md`). Un thème peut déclarer un champ `participles` optionnel, avec exactement la même structure que `adjectives` (catégories + `common`).

Résolution, en extension de l'algorithme existant :

```
partPool(noun) = union des participles[c] pour c dans noun.categories ∪ {common}
```

Le segment entre l'éventuel préfixe et le nom est piloté par `defaults.segmentMode`, un champ à 4 valeurs plutôt qu'un booléen de visibilité :

- `adjective` — un adjectif uniquement, tiré dans `pool(noun)` ; les participes, même déclarés, ne sont jamais utilisés.
- `participle` — un participe uniquement, tiré dans `partPool(noun)`.
- `either` — à chaque génération, un tirage aléatoire choisit entre un adjectif seul ou un participe seul (jamais les deux) : c'est ce mode qui reproduit la structure à un seul mot avant le nom des générateurs historiques comme Heroku, tout en piochant dans un vocabulaire adjectif+participe combiné.
- `both` — un adjectif ET un participe, comme le format à 3 segments d'origine de `slugger`.

Dégradation silencieuse, sans erreur, quand `partPool(noun)` est vide pour le nom tiré (thème sans `participles`, ou catégorie sans participe associé) : `both` retombe sur l'adjectif seul (comportement historique inchangé) ; `participle` et `either` retombent aussi sur l'adjectif seul pour cette génération précise, plutôt que d'échouer. En revanche, si le thème ne déclare `participles` nulle part et que ses `defaults` demandent `participle` ou `either`, c'est une erreur de chargement (thème incohérent), pas une dégradation à la volée. Absence de `segmentMode` dans les `defaults` d'un thème ⇒ `both` implicite (se réduit de lui-même à l'adjectif seul si aucun participe n'est déclaré, comme pour `docker.json`).

Format généralisé :

```
<adjectif>[sep](<participe>[sep])?<nom>([sep]<token>)?
```

L'apparition même du token, une fois `tokenLength > 0`, est probabiliste plutôt que systématique : `defaults.tokenChance` (0 à 100, pourcentage) tranche la question à chaque génération. `0` équivaut à ne jamais en avoir (comme `tokenLength: 0`), `100` (valeur implicite si absent, pour ne rien changer au comportement de `slugger`/`heroku`) équivaut à toujours en avoir. Une valeur intermédiaire simule un token rare — utile pour un thème comme `docker.json` qui veut représenter un token conditionné par une collision sans implémenter de vraie détection de collision. Si le tirage ne produit pas de token pour cette génération, le segment est simplement absent, `tokenGlued` n'a alors rien à coller.

Flag CLI `--segment <adjective|participle|either|both>` : override explicite de `segmentMode`, avec la même priorité que `--sep`/`--casing`/`--token-length` (argument CLI > `defaults` du thème > config `--init` > défaut programme, qui est `both`).

Les participes sont classés par capacité physique plutôt que par domaine, pour que seul un participe physiquement plausible pour un nom donné lui soit accessible : `mobile` (se déplace : vent, rivière, oiseau, nuage — 27 participes), `sonore` (fait du bruit : tonnerre, rivière, oiseau — 9 participes), `lumineux` (émet/reflète la lumière : étoile, lune, feu, luciole — 12 participes), `vivant` (croît, respire, se fane : fleur, arbre, oiseau — 9 participes), `chaleur` (feu, lave, désert — 8 participes), `eau` (rivière, lac, pluie, glacier — 13 participes), plus `common` (20 participes universels et neutres — *fading*, *lingering*, *settling*, *crumbling*... — qui servent aussi de pool par défaut pour les noms sans capacité assignée, comme les objets abstraits ou géologiques inertes). Exemples : `moon` = `[lumineux, mobile]`, sans `sonore` — `thundering-moon` n'est pas un tirage possible, `waning-moon` (via `common`) l'est. `willow` = `[vivant]` — `weeping-willow`, un idiome anglais existant, est un tirage possible. `river` = `[eau, mobile, sonore]` — `thundering-river`, `humming-river`, `swirling-river` sonnent tous justes. Le pool `common` reste accessible à tout nom quelles que soient ses capacités, ce qui autorise occasionnellement une métaphore plus lâche (`waning-cake`, `throbbing-feather`) sur les noms sans capacité assignée — un compromis délibéré : `common` doit rester non vide pour tout nom. Validation : 216 noms ≥ 100, 178 adjectifs ≥ 100, 1 234 252 combinaisons résolues ≥ 40 000 — `allowSmall` non nécessaire. `slugger --theme heroku --segment both` : `warped-humming-river`, `hushed-brightening-twilight`, `rusted-settling-sediment`.

Pour la validation (Taille minimale d'un thème) et le guide de création, `combos(noun)` compte le participe qu'il soit utilisé par défaut ou non — `segmentMode` ne change qu'un comportement de génération, pas l'espace combinatoire réel du thème.

## Identité du thème slugger

Le thème embarqué `slugger` (voir Fourniture des thèmes) est construit autour du baseball — cohérent avec le nom de l'outil lui-même (un « slugger » est un frappeur puissant au baseball), plutôt qu'un thème plaqué sans rapport.

Aucune restriction d'époque : le vocabulaire couvre l'histoire du baseball dans son ensemble, jusqu'à aujourd'hui (2026), sans se limiter à une période particulière (ex: pas de restriction à une « Golden Age »). Choix aligné sur celui de Docker, dont le vocabulaire scientifique couvre l'Antiquité jusqu'à des figures contemporaines sans contrainte temporelle.

Utilise le segment participe présent optionnel (voir Segment participe présent) en plus du format à 2 segments de base.
