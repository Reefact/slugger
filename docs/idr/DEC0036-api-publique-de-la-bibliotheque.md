# DEC0036 | Une API publique resserrée : `SluggerTheme`, `SlugGenerator`, deux namespaces, `OrThrow` pour ce qui lève

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-11-11 | Accepté | Troisième point de l'issue #63, avant `lib-v1.0.0` | |

## Contexte

Après DEC0034 et DEC0035, la bibliothèque publiait 29 types répartis sur six namespaces —
`Slugger`, `Slugger.Domain`, `Slugger.Domain.Generation`, `Slugger.Domain.Resolution`,
`Slugger.Domain.Validation`, `Slugger.Domain.Normalization`, plus `Slugger.Domain.Errors`. Un usage
complet en importait cinq. `Domain`, `Generation`, `Validation` sont le vocabulaire des couches
internes : ils disaient au consommateur comment le moteur est construit.

Obtenir un slug demandait quatre concepts — la façade `Themes`, le thème, les options, la source :

```csharp
SluggerTheme      docker = Themes.LoadEmbedded("docker");
GenerationOptions style  = GenerationOptions.Default.WithDefaultsOf(docker) with { MaxLength = 30 };
SlugSource        source = SlugSource.Prepare(docker, style);
string            slug   = source.Next();
```

Le style du thème ne s'appliquait pas tout seul : oublier `WithDefaultsOf` donnait des slugs qui ne
ressemblaient pas au thème. Deux façons de tirer coexistaient : `SlugGenerator.Generate`, qui
réduisait le thème à chaque appel et ne validait rien, et `SlugSource`, qui faisait les deux une
fois. DEC0031 avait laissé le sort de la première à #63.

Depuis DEC0034, un consommateur ne voit plus les mots d'un thème. `SlugFormatter`, `SlugBudget`,
`WordNormalizer` et `WeightedThemePicker` travaillaient sur des mots qu'il n'a plus.
`ThemeValidator` n'exposait plus que quatre constantes. Depuis DEC0035, plus aucune méthode publique
ne renvoyait de `DomainError` ni ne levait de `DomainException`.

Enfin, l'API publique nommait ses méthodes à l'inverse du reste du code : `Themes.LoadEmbedded`
levait et `LoadEmbeddedResult` rapportait, là où `Word.From` rapporte et `Word.FromOrThrow` lève, comme
`ICatalog.Get` et `GetOrThrow`, et `Outcome<T>.GetResultOrThrow`.

## Décision

Dans ce contexte, nous décidons que la bibliothèque publie dans `Slugger` un thème qui se charge
lui-même, `SluggerTheme`, et un générateur préparé qui tire dans le style du thème, `SlugGenerator`,
seule façon de tirer, avec le modèle d'erreur dans `Slugger.Errors` ; que tout le reste devient
interne ; et que partout, le nom simple d'une méthode qui peut échouer renvoie un `Outcome`, et
qu'un suffixe `OrThrow` nomme celle qui lève.

```csharp
SluggerTheme  docker    = SluggerTheme.LoadEmbeddedOrThrow("docker");
SlugGenerator generator = SlugGenerator.ForOrThrow(docker, style => style with { MaxLength = 30 });
string        slug      = generator.Next();
long          distinct  = generator.Combinations;
```

## Justification

**Une seule façon de tirer, et c'est la sûre.** `SlugGenerator.For` réduit le thème une fois, le
valide, puis tire en microsecondes ; des options qui l'appauvrissent sous ses planchers sont refusées
à la construction, avec toutes les raisons. L'ancien `Generate` laissait passer ces options jusqu'au
tirage, et payait la réduction à chaque appel. Le cas « un seul slug » reste une ligne.

**Le style du thème s'applique sans qu'on y pense.** L'ajustement reçoit le style du thème et le
modifie ; l'ignorer est un geste explicite (`_ => GenerationOptions.Default with { ... }`). C'est le
contraire de l'ancien piège, où le style n'existait que si l'on y pensait.

**Plusieurs thèmes, chacun dans son style.** En bibliothèque, l'appelant choisit ses thèmes : chacun
garde son style, et le même ajustement s'applique à tous. La commande, elle, éteint les styles d'un
tirage mélangé (DEC0004) parce que l'utilisateur y mélange des thèmes avec `*` sans les avoir choisis
un à un ; elle garde ce comportement par son point d'entrée interne.

**Le nombre de slugs se lit sur le générateur.** `Combinations` dépend des options, et c'est le
générateur qui les connaît : la question « combien de slugs distincts ? » se pose là où la réponse
a un sens, sans type de plus.

**Le thème se charge lui-même.** `SluggerTheme.LoadEmbedded` est l'idiome .NET d'une fabrique
statique sur le type qu'elle fabrique (`JsonDocument.Parse`, `TimeZoneInfo.FindSystemTimeZoneById`) :
un nom à connaître au lieu de deux. Ce n'était interdit que parce que le type public vivait dans
`Slugger.Domain`, que le JSON et le système de fichiers ne doivent pas atteindre ; hors du domaine,
la contrainte tombe.

**Deux namespaces, sans le vocabulaire des couches.** `using Slugger;` suffit au cas courant ;
`Slugger.Errors` ne sert qu'à lire un refus en détail. Les couches `Domain`, `Application`,
`Infrastructure` restent ce qu'elles sont, internes, et un test vérifie que la surface publique ne
nomme ni un cas d'usage ni un type d'infrastructure.

**Ce que le consommateur ne peut pas nourrir n'a pas à être public.** Les outils qui mettent en forme
ou mesurent des mots, le choix pondéré d'un thème, `ThemeCombinatorics` et `ThemeValidator` deviennent
internes ; les quatre planchers restent publics dans `ThemeFloors`, parce qu'un auteur de thème a
besoin de les connaître pour garder sa marge. `DomainError` et `DomainException` deviennent internes :
plus rien de public ne les produit. Ouvrir plus tard est une version mineure, fermer une majeure
(DEC0007).

**`OrThrow` est la même méthode, qui lève.** Les deux formes prennent les mêmes paramètres,
`allowSmall` compris : l'ancienne API le refusait à la forme qui lève, pour qu'on ne lève pas les
planchers sans lire le rapport, mais un appelant qui passe `allowSmall: true` a déjà décidé de ne pas
les lire. Les options effectives d'un générateur (`OptionsFor`) restent internes : personne n'en a
exprimé le besoin, et les ouvrir plus tard ne casse rien.

**Un nom qui dit s'il lève.** Le nom simple rapporte, `OrThrow` lève : la convention du reste du code,
appliquée à l'API publique. Un appelant lit à l'appel s'il doit attraper une exception.

## Alternatives envisagées

### Alternative 1 — Garder `SlugGenerator.Generate` à côté du générateur préparé

- **Description :** deux façons de tirer, la documentation disant laquelle choisir.
- **Pourquoi écartée :** l'une des deux ne valide rien, et la documentation devient le seul garde-fou.

### Alternative 2 — `Generate` comme raccourci de la préparation

- **Description :** garder `Generate(theme, options)`, implémenté par une préparation suivie d'un tirage.
- **Pourquoi écartée :** valide, mais paie la préparation à chaque slug, ce qui est le piège de coût que
  `SlugSource` corrigeait ; et deux noms pour une seule chose.

### Alternative 3 — Garder la façade sous un autre nom, `SluggerThemes`

- **Description :** renommer `Themes` pour l'accorder à `SluggerTheme`.
- **Pourquoi écartée :** deux noms presque identiques côte à côte, là où une fabrique sur le type en
  demande un seul.

### Alternative 4 — Laisser les types publics dans les namespaces des couches

- **Description :** garder `Slugger.Domain` et ses sous-namespaces.
- **Pourquoi écartée :** cinq `using` pour un usage complet, et le vocabulaire de l'architecture interne
  dans le code de chaque consommateur.

### Alternative 5 — Garder la convention `*Result` / nom simple qui lève

- **Description :** ne pas toucher aux noms des chargements.
- **Pourquoi écartée :** l'API publique ferait l'inverse du reste du code, et l'appel le plus court
  serait celui qui lève.

## Conséquences

### Positives

- Un usage complet tient en `using Slugger;` et trois appels.
- Il n'existe plus qu'une façon de tirer, et elle ne laisse jamais passer des options qui affament un
  thème.
- La surface publique passe de 29 types à 20, sur deux namespaces.
- Un nom dit, partout, s'il lève.

### Négatives

- La bibliothèque et la commande traitent différemment les styles d'un tirage mélangé.
- Un consommateur ne peut plus mettre en forme ses propres mots avec les règles de casse du moteur ;
  ce serait un autre produit, et il se rouvrira en version mineure si le besoin apparaît.
- Tirer un seul slug d'un thème sous une limite paie la préparation, quelques dizaines de
  millisecondes, là où l'ancien `Generate` les payait aussi mais sans rien vérifier.

### Risques

- Aucun identifié.

### Actions de suivi

- Écrire la règle de nommage `OrThrow` dans `CLAUDE.md`, une fois pour tout le code.
- Le cas d'usage interne qui a remplacé `SlugGenerator.Generate` porte le nom `SlugDrawer` : le
  refactoring du vocabulaire peut le reprendre sans toucher la surface publique.
