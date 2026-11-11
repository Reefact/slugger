# DEC0033 | L'identifiant `Slugger` à la commande, `Slugger.Core` à la bibliothèque

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-11-11 | Accepté | Effectif à la première release `cli` sous l'identifiant `Slugger`, que nul push n'a encore prouvée | |

## Contexte

Deux paquets sont publiés sur nuget.org. Relevé le 2026-11-11 par l'API de recherche :

| Identifiant | Type | Versions | Téléchargements |
| --- | --- | --- | --- |
| `Slugger` | `Dependency` (la bibliothèque) | `1.0.0-preview.1` seulement | 1 138 |
| `Slugger.Cli` | `DotnetTool` (la commande) | `1.0.0`, `1.1.0` | 128 |
| `Slugger.Core` | — | libre | — |

Une recherche « slugger » sur nuget.org rend `Slugger`, puis `FastSlugger` et `LocationSlugger.net`,
puis seulement `Slugger.Cli`, en quatrième position. Qui tape le nom de l'outil tombe sur la
bibliothèque. Or le produit que l'on cherche par son nom est la commande `slugger`, et c'est elle que
la documentation met en avant.

La bibliothèque n'a jamais publié de version stable : personne ne dépend d'une `1.x` de `Slugger`.

Ce qu'on sait de la réaffectation d'un identifiant à un autre type de paquet, recherché avant de
décider :

- **Le serveur.** Dans le code source de nuget.org (`NuGet/NuGetGallery`), le type de paquet est une
  propriété de chaque version (`Package.PackageTypes`), remplie depuis le nuspec de cette version au
  dépôt. La validation à l'upload ne contrôle qu'un type, `McpServer`, qui exige `DotnetTool` ; aucune
  comparaison avec le type des versions déjà publiées n'y figure. La documentation n'en dit rien non
  plus.
- **Un précédent.** `Microsoft.EntityFrameworkCore.Tools.DotNet` sert, sous un même identifiant, une
  `0.0.1-alpha` sans type (donc `Dependency`, délistée) et des versions `DotnetCliTool`. Précédent
  faible : ancien, probablement une réservation d'identifiant, et pas le type `DotnetTool`.
- **Les clients.** Ils choisissent la version la plus haute d'un identifiant, puis refusent son type
  s'il ne convient pas (`NU1212`) — mesuré par d'autres, dotnet/sdk#32092 : `dotnet tool install`
  a pris une version qui n'était pas un outil, parce qu'elle était la plus haute.

Rien n'a été mesuré par un push : c'est la seule incertitude, et elle ne coûte rien si elle tombe
du mauvais côté — un push refusé ne publie rien.

## Décision

Dans ce contexte, nous décidons de publier la commande sous l'identifiant `Slugger` et la
bibliothèque sous `Slugger.Core`, sans renommer ni les projets, ni les assemblys, ni les namespaces.

## Justification

L'identifiant court est celui qu'on tape ; il revient au paquet qu'on cherche par son nom, la
commande. `.Core` est la convention de l'écosystème pour « le moteur derrière l'outil ».

Le moment est le moins cher qu'il sera jamais : la bibliothèque n'a qu'une préversion, et chaque
release stable reculerait ce moment en le renchérissant.

L'identifiant est ce que voit nuget.org, et rien d'autre ne le suit. Renommer le namespace `Slugger`
en `Slugger.Core` casserait chaque `using` de chaque consommateur pour un gain nul en recherche ;
renommer les projets toucherait la solution, les tests, la CI et les commandes documentées, pour un
gain nul aussi. Le nom de la commande, `ToolCommandName`, est déjà `slugger` et ne change pas.

## Alternatives envisagées

### Alternative 1 — Garder `Slugger.Cli` et soigner la page de `Slugger`

- **Description :** laisser les identifiants, et ouvrir la page NuGet de la bibliothèque par un
  renvoi vers la commande.
- **Pourquoi écartée :** soigne le symptôme sur une seule page, alors que la recherche, les badges et
  `dnx` continuent de nommer la commande par un identifiant long. Elle reste le repli si nuget.org
  refuse le push.

### Alternative 2 — `Slugger.Tool` pour la commande

- **Description :** suivre la convention `.Tool` (`Cake.Tool`, `GitVersion.Tool`).
- **Pourquoi écartée :** remplace un suffixe par un autre ; la recherche du nom nu tombe toujours sur
  la bibliothèque.

### Alternative 3 — Renommer aussi le namespace en `Slugger.Core`

- **Description :** aligner le namespace sur l'identifiant du paquet.
- **Pourquoi écartée :** cassant pour tout consommateur, sans effet sur ce qui motivait la décision.

## Conséquences

### Positives

- `dotnet tool install -g Slugger` et `dnx Slugger` installent et lancent la commande. Mesuré sur
  des paquets construits localement : `Slugger` est un `DotnetTool` qui s'installe et tire,
  `Slugger.Core` un `Dependency` qu'un projet référence et appelle par `using Slugger;`.
- Une recherche du nom sur nuget.org rend la commande en tête.

### Négatives

- `dotnet add package Slugger` échoue désormais en `NU1212` (mesuré sur le paquet construit
  localement) : la version la plus haute de `Slugger` est un outil. C'est voulu ; la dépréciation de `Slugger 1.0.0-preview.1` désigne
  `Slugger.Core` à qui la rencontre.
- Une installation de `Slugger.Cli` ne se met plus à jour : `dotnet tool update` reste sur `1.1.0`.
  Il faut désinstaller `Slugger.Cli` et installer `Slugger`, et dans un manifeste d'outils changer
  l'entrée `slugger.cli` en `slugger`.
- Les deux identifiants abandonnés restent sur nuget.org pour toujours : un identifiant se déliste,
  il ne se supprime pas.

### Risques

- **nuget.org peut refuser le premier push.** Aucune règle trouvée ne l'interdit, aucun push ne l'a
  prouvé. S'il est refusé, rien n'est publié : la commande reste `Slugger.Cli`, et ce document gagne
  une ligne de statut qui le dit.
- **Une bibliothèque republiée sous `Slugger` casserait l'outil.** Les clients choisissent la
  version avant le type : une version de bibliothèque au-dessus de celles de l'outil transformerait
  chaque `dotnet tool install Slugger` en `NU1212`. Aucune version de l'outil ne doit non plus être
  inférieure ou égale à `1.0.0-preview.1`.

### Actions de suivi

- Publier `lib-v1.0.0-preview.2`, première `Slugger.Core`, puis déprécier `Slugger 1.0.0-preview.1`
  avec `Slugger.Core` comme paquet de remplacement.
- Publier `cli-v1.2.0`, premier `Slugger` outil, puis déprécier toutes les versions de `Slugger.Cli`
  avec `Slugger` comme paquet de remplacement.
- Passer ensuite `dotnet-tools.json` de `slugger.cli` à `slugger`, ici et dans
  `Reefact/conventional-comments-toolkit`, dont les skills de revue nomment `Slugger.Cli`.
