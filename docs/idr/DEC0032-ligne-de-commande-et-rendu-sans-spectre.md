# DEC0032 | Ligne de commande lue et rendue par slugger lui-même, pour une commande compilée en natif

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-11-11 | Accepté | Remplace DEC0019 pour la lecture et le rendu ; garde sa déclaration unique | |

## Contexte

`slugger` n'est distribué que comme outil .NET : l'installer demande le SDK .NET 10. Des agents
qui nomment leurs sessions de revue avec slugger tournent dans des environnements sans .NET, et y
installer le SDK à chaque revue n'est pas envisageable. Un exécutable autonome, téléchargé avec la
release, est la réponse naturelle.

Mesuré sur `cli-v1.0.0`, Linux x64, en tirant un slug du thème `code-review` :

- **compilé en natif (`PublishAot`)**, le binaire se construit mais refuse toute ligne de commande :
  *« Could not get settings type for command of type 'SluggerCommand' »*. `Spectre.Console.Cli`
  découvre ses options par réflexion, et le compilateur le dit lui-même (`IL3050`, `IL3053`) ;
- **élagué (`PublishTrimmed`)**, même refus, pour la même raison ;
- **autonome compressé**, sans élagage, il marche : 40 Mo et 650 ms par tirage ;
- `XdgConfigStore` lit et écrit les défauts de `--init` par réflexion (`JsonSerializer` sans
  contexte généré), ce que la compilation native signale aussi.

DEC0019 a confié à Spectre la lecture et le rendu de la ligne de commande. Ce qu'il en reste à
l'usage, mesuré dans `src/Slugger.Cli` :

- **la lecture** se borne à collecter du texte : DEC0019 a déjà retiré à Spectre la conversion, que
  `CommandLineReader` fait en une passe qui accumule (DEC0006). Spectre découpe la ligne, et
  `SluggerApp.Spelled` la réécrivait d'abord pour les deux formes que son découpage lisait mal,
  `--sep -` et `--sep=` ;
- **le rendu** se borne à des lignes : un verdict en gras et en couleur, une ligne atténuée pour les
  raisons comptées plutôt que nommées, et l'aide. Aucun tableau, aucun panneau ; l'action de suivi
  de DEC0019 qui voulait porter le rapport de DEC0014 sur le rendu de Spectre n'a pas eu lieu ;
- **deux concessions** de DEC0019 sont encore là : un mot seul est lu comme un nom de commande et
  arrête la lecture, et une option sans valeur aussi, avec le message de Spectre ;
- Spectre enrichit sa console depuis l'environnement, et son enrichisseur GitHub Actions rallumait
  les couleurs que les tests avaient éteintes (consigné dans `CLAUDE.md`).

Un prototype sans `Spectre.Console.Cli`, en gardant `Spectre.Console`, compile en natif sans aucun
avertissement : 5,9 Mo. Sans les deux paquets : 4,8 Mo et 176 ms par tirage. `Spectre.Console`
seul pèse environ 1,5 Mo dans un binaire natif (programme minimal : 1,3 Mo sans, 2,8 Mo avec).

## Décision

Dans ce contexte, nous décidons que slugger lit et dessine lui-même sa ligne de commande, sans
Spectre, en gardant la déclaration unique de DEC0019 : les options restent déclarées une fois sur
`SluggerSettings`, d'où le découpage et `--help` sont tous deux tirés.

## Justification

**La compilation native est le but, et Spectre est ce qui l'empêche.** Le binaire natif est le
seul qui se télécharge en quelques secondes et tire un slug sans rien installer : 4,8 Mo contre
40, 176 ms contre 650. Les deux voies qui gardaient Spectre ont été mesurées, et refusent toute
ligne de commande.

**Ce que Spectre faisait encore tient en peu de code.** DEC0019 avait déjà réduit sa part à
« collecter le texte » : le découpeur l'écrit en un peu plus de 300 lignes, ses petits types
compris, l'aide en une centaine et le terminal en autant, documentation XML comprise. Le
gain de DEC0019 n'est pas perdu, parce qu'il venait de la déclaration et non de Spectre : un
attribut `CommandOption` propre à slugger remplace celui de Spectre sur les mêmes propriétés, et
`HelpRenderer` dessine l'aide depuis lui. L'aide ne peut toujours pas décrire un flag retiré.

**Le rendu est des lignes, pas un rapport à mettre en page.** Le gros de ce qui justifiait
`Spectre.Console` dans DEC0019 — dessiner le rapport de DEC0014 — ne s'est pas fait. Ce qui reste
est un choix d'encre par ligne, qu'une énumération et une table de codes d'échappement disent.
Garder `Spectre.Console` coûterait 1,1 Mo et l'enrichissement de sa console par l'environnement,
qui a déjà rendu un test faux sur le seul runner.

**Les deux concessions de DEC0019 tombent.** Un mot seul et une option sans valeur deviennent des
plaintes parmi les autres, dans la voix de slugger : `docker --casing SHOUT --count abc` en
rapporte trois au lieu d'une. C'est ce que DEC0006 promettait de toute ligne de commande.

**La conversion des défauts sauvegardés passe à un contexte JSON généré.** C'est la seule réflexion
du moteur que la compilation native signale ; le fichier écrit est identique octet pour octet
(mesuré dans les deux sens).

## Alternatives envisagées

### Alternative 1 — Garder Spectre et publier un exécutable autonome

- **Description :** `PublishSingleFile` autonome et compressé, sans élagage ni compilation native.
- **Pourquoi écartée :** 40 Mo à télécharger et 650 ms par tirage, pour un outil qui tire un nom de
  trois mots. Mesuré, il marche ; il reste là si la compilation native venait à se casser.

### Alternative 2 — Ne retirer que `Spectre.Console.Cli`

- **Description :** le découpeur maison, mais le rendu laissé à `Spectre.Console`, qui compile en
  natif sans avertissement.
- **Pourquoi écartée :** 1,1 Mo de plus pour colorer des lignes, et la détection d'environnement de
  Spectre, qui a déjà fait mentir un test. Mesuré : le binaire marche et l'aide est identique.

### Alternative 3 — `System.CommandLine`

- **Description :** la bibliothèque de Microsoft, qui découpe et génère une aide.
- **Pourquoi écartée :** non mesurée ici. Elle ferait une dépendance pour ce que deux cents lignes
  font, avec ses propres règles de découpage — celles que `Spelled` a dû contourner chez Spectre
  sont le précédent.

### Alternative 4 — Élaguer en gardant Spectre, avec des annotations de conservation

- **Description :** `PublishTrimmed` en indiquant au compilateur quels types Spectre lit par
  réflexion.
- **Pourquoi écartée :** l'élagage simple refuse la ligne comme la compilation native (mesuré), et
  annoter les types d'une bibliothèque tierce, c'est suivre ses internes à chaque version.

## Conséquences

### Positives

- `slugger` se compile en natif sans un avertissement : un binaire de 4,8 Mo, sans SDK ni runtime,
  qui tire en 176 ms.
- Sur 41 lignes de commande comparées à la version Spectre, tirages à graine fixe compris, 32
  donnent la même sortie et le même code de retour, espaces de fin de ligne exceptés. Les 9 autres
  sont les écarts voulus : le message d'une option sans valeur (5), le commit dans `--version` (2),
  `--theme -` et un mot seul (ci-dessous). `--help` est identique ligne pour ligne ; les défauts de
  `--init` sont écrits et relus à l'identique.
- Un mot seul et une option sans valeur sont rapportés avec les autres plaintes (DEC0006).
- `--sep -` et `--sep=` se lisent sans réécriture : un tiret seul et un nombre négatif sont des
  valeurs, et rien après le signe égal est une valeur vide. `SluggerApp.Spelled` disparaît.
- `PortRegistrar`, l'enregistreur de types que Spectre exigeait, disparaît.
- `Slugger.Cli` ne dépend plus que de `TextCopy`.

### Négatives

- **Ça coûte du code.** Mesuré sur `src/` : 695 lignes ajoutées, 485 retirées, soit 210 de plus,
  documentation XML comprise. Le découpage et la détection des couleurs sont désormais à slugger.
- **L'aide perd ses couleurs fines** : seuls les titres `USAGE`, `EXAMPLES`, `OPTIONS` sont colorés.
- **L'aide est pliée à 80 colonnes** même dans une fenêtre plus large, comme elle l'était déjà
  redirigée.
- **Sous Windows, les couleurs ne s'affichent que dans Windows Terminal**, qui pose `WT_SESSION`.
  L'ancienne console n'interprète les codes d'échappement qu'une fois qu'on le lui demande, et elle
  reste en texte brut plutôt que d'être réglée par un appel natif. Lu dans la documentation, non
  mesuré : aucun Windows interactif n'était disponible.
- **Dans un terminal, un long rapport n'est plus replié au mot près** : c'est le terminal qui coupe
  la ligne.
- Deux messages changent : une option sans valeur dit *« "--count" was given no value. »* au lieu du
  message de Spectre, et `--theme -` cherche un thème nommé `-` au lieu de répondre *« Option does
  not have a name. »*, une erreur de Spectre.

### Risques

- Le découpage est désormais celui de slugger : un cas que Spectre traitait sans qu'aucun test ne le
  dise pourrait changer sans bruit. Les cas mesurés sur la version Spectre sont pinnés dans
  `CommandLineParserTests` ; les autres ne le sont pas.
- La détection des couleurs est simple : sortie non redirigée, `NO_COLOR` absent, Windows Terminal
  sous Windows. Un terminal qui ne sait pas les dessiner et ne le dit pas affichera les codes.

### Actions de suivi

- Publier le binaire natif avec chaque release `cli-v*`, et le compiler à chaque pull request pour
  que la compilation native ne casse pas en silence — fait dans `release.yml` et `ci.yml`.
- Si l'ancienne console Windows compte, y activer les codes d'échappement par `SetConsoleMode`.
- Réévaluer DEC0019 et ses mesures de mutation, qui décrivaient le CLI tel qu'il n'est plus.
