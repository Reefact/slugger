# DEC0038 | La mesure d'un thème publiée : `SluggerTheme.Analyze*` et `ThemeAnalysis`

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-11-11 | Accepté | Règle l'issue #76, avant `lib-v1.0.0` | |

## Contexte

`slugger --analyze` mesure un thème : la marge sur chaque plancher, les noms déclarés deux fois, les
catégories que personne ne porte, l'écart d'exposition entre les adjectifs, l'espace combinatoire.
DEC0014 a séparé les nombres de la prose : le moteur calcule un modèle, la commande l'écrit en
markdown. Ce modèle était `internal`, et DEC0014 prévoyait de le publier « le jour où un catalogue
écrit en .NET en a besoin, pas avant ».

Ce consommateur existe : le site `slugger.online`, en Blazor WebAssembly, dont la spécification
(`docs/design/specification.md`, §7.6 et §10.2) rend la mesure de chaque thème livré et celle du
thème qu'un visiteur ouvre — « elle fonctionne sur un thème refusé, ce qui est le cas qui compte » — et
s'interdit d'en écrire une seconde version (§10.4).

Le modèle interne était fait de records positionnels, dont un de vingt paramètres. Publié tel quel, un
record positionnel expose son constructeur et sa déconstruction : lui ajouter une mesure changerait les
deux, ce que DEC0037 interdit en `1.x`.

Un `SluggerTheme` n'existe que pour un thème accepté (DEC0034) ; mesurer un thème refusé ne peut donc
pas partir d'un `SluggerTheme`.

DEC0036 a rangé le chargement sur `SluggerTheme`, sous des noms qui disent d'où vient le thème :
`LoadEmbedded`, `LoadFromFile`, `LoadFromJson`. Un consommateur qui embarque son thème comme ressource
devait lire le flux en texte lui-même.

## Décision

Dans ce contexte, nous décidons de publier la mesure d'un thème dans `Slugger`, sous des classes à
propriétés en lecture seule que seule la bibliothèque construit — `ThemeAnalysis`, `ThemeMeasurements`,
`ThemeMargin`, `ThemeRule`, `WordExposure` —, obtenue de `SluggerTheme.AnalyzeEmbedded`,
`AnalyzeFromFile`, `AnalyzeFromJson` et `AnalyzeFromStream`, qui ne lèvent jamais pour le thème ; et
d'ajouter `LoadFromStream` et `LoadFromStreamOrThrow`.

## Justification

**Le modèle est celui des pages qui l'affichent.** La spécification du site demande les marges, les
doublons, les catégories que personne ne porte, l'écart d'exposition et l'espace combinatoire : c'est
ce qui est publié. Les autres mesures du rapport de la commande — mots en plusieurs morceaux, nombre de
segments — restent internes, et s'ajouteront en version mineure le jour où un consommateur les
demandera.

**Des classes, pour pouvoir grandir.** Une propriété ajoutée à une classe à constructeur interne ne
casse personne ; une mesure ajoutée à un record positionnel public casserait son constructeur et sa
déconstruction.

**Une ligne par règle.** `Margins` reprend le tableau du rapport : chaque `ThemeMargin` donne la règle,
le pire cas, ce qu'il concerne, la limite et la marge. Une règle de plus est une valeur de plus de
`ThemeRule`, pas une propriété de plus ; une règle qui ne s'applique pas au thème n'a pas de ligne.
Chaque longueur promise dans `maxLength` a la sienne — `CharactersInTwoWords`, `CharactersInThreeWords` —
parce qu'un chargement vérifie chacune, quel que soit le mode que le thème tire : un refus pour un slug
à deux mots trop long trouve ainsi sa ligne dans les marges. Ce sont des plafonds : leur marge se compte
sous la limite. Le rapport de la commande affiche les mêmes lignes.

**L'analyse part de la source, comme le chargement.** Un thème refusé n'a pas de `SluggerTheme` ; la
mesurer exige de partir du JSON. Les noms suivent ceux du chargement, un par source, parce qu'un chemin
et du JSON sont tous deux des `string` et qu'une surcharge ne saurait les distinguer.

**Pas de forme `OrThrow`.** Une analyse ne peut pas échouer pour le thème : un refus est un résultat, que
`Refusals` porte. La règle de DEC0036 nomme ce qui peut échouer ; une méthode qui ne le peut pas garde
son nom simple et rend son résultat.

**Le même compte partout.** L'analyse compte les combinaisons comme `SlugGenerator.Combinations`, nom
par nom ; le rapport de la commande, écrit à partir des mêmes faits, en hérite sans changer un chiffre
sur les thèmes du dépôt.

**Un flux est une source comme les autres.** `LoadFromStream` et `AnalyzeFromStream` lisent le flux
jusqu'au bout et le laissent ouvert : il appartient à qui l'a ouvert.

## Alternatives envisagées

### Alternative 1 — Publier le modèle interne tel quel

- **Description :** rendre publics les records de `Domain.Analysis`.
- **Pourquoi écartée :** gèle pour toute la `1.x` un modèle de vingt champs pensé pour un rapport
  markdown, dont le constructeur et la déconstruction deviendraient des promesses.

### Alternative 2 — Publier la mesure après la `1.0`

- **Description :** sortir `lib-v1.0.0` sans elle, et l'ajouter en version mineure.
- **Pourquoi écartée :** possible, puisque l'ajout est mineur, mais le consommateur qui la demande existe
  déjà et ses besoins sont écrits ; rien ne gagnait à attendre.

### Alternative 3 — `theme.Analyze()` sur un thème chargé

- **Description :** une méthode d'instance de `SluggerTheme`.
- **Pourquoi écartée :** ne mesure que des thèmes acceptés, ce qui manque le cas qui compte, et doublerait
  `AnalyzeEmbedded` pour les thèmes livrés.

### Alternative 4 — Des surcharges `AnalyzeFrom(...)` et `LoadFrom(...)`

- **Description :** un seul nom, la source choisie par le type de l'argument.
- **Pourquoi écartée :** un chemin et du JSON sont deux `string` ; une surcharge par `FileInfo` laisse
  compiler `LoadFrom("themes/jazz.json", "jazz")`, lu comme du JSON et refusé à l'exécution par
  `THEME_MALFORMED_JSON` — l'erreur que des noms explicites rendent impossible.

## Conséquences

### Positives

- Un consommateur .NET affiche la mesure d'un thème, accepté ou non, avec les chiffres de la commande.
- Un thème embarqué comme ressource se charge sans lecture intermédiaire.
- La surface publique gagne sept membres et cinq types, tous ajoutés sans rien casser.

### Négatives

- La surface publique passe de 20 à 25 types.
- Une page de thème ne peut toujours pas lire ses catégories ni ses mots : c'est l'issue #114.

### Risques

- Aucun identifié.

### Actions de suivi

- À l'acceptation, ajouter une ligne « Nuancé par DEC0038 » au statut de DEC0014 : le modèle qu'elle
  gardait interne est publié, sous une autre forme.
