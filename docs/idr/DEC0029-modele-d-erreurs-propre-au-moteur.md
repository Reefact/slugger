# DEC0029 | Un modèle d'erreurs propre au moteur, à la place de FirstClassErrors

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-11-10 | Accepté | | |
| 2026-11-11 | Nuancé par DEC0035 | Les factories des catalogues sont internes ; `ThemeErrors` devient `SluggerThemeError` | |

## Contexte

`Slugger` dépend de `FirstClassErrors` en `0.1.0-preview.1`, la seule version publiée sur
nuget.org (vérifié le 2026-11-10). NuGet refuse de produire un paquet stable qui dépend d'une
préversion (NU5104) : `lib-v1.0.0` échoue au pack, ce qui a été mesuré. Tant que `FirstClassErrors`
reste en préversion, le moteur y reste aussi.

Le mainteneur de `FirstClassErrors` ne souhaite pas le publier en version stable.

Les types de `FirstClassErrors` font partie de la surface publique du moteur :

- `Themes.LoadEmbeddedResult`, `LoadFromFileResult` et `LoadFromJsonResult` renvoient un
  `Outcome<ThemeDocument>`, et leurs variantes sans `Result` lèvent une `DomainException` ;
- `ThemeErrors` renvoie des `DomainError` et expose des `ErrorContextKey<T>` et des `ErrorCode` ;
- `ThemeValidator.Validate` renvoie une liste de `DomainError` ;
- les types de vocabulaire, que le moteur n'utilise pas encore (`Word`, `Category`, `ThemeName`…),
  renvoient des `Outcome<T>`, et leurs erreurs dérivent de `Error`.

Le moteur et le CLI n'en utilisent qu'une partie, relevée dans le code :

- `Outcome<T>` et `Outcome` : `Success`, `Failure`, `IsSuccess`, `IsFailure`, `Error`,
  `GetResultOrThrow` ;
- `Error` et `DomainError` : un code, trois messages, des erreurs internes et un contexte typé ;
- `ErrorCode`, `ErrorContextKey<T>`, `DiagnosableException` et `DomainException` ;
- `PrimaryPortError`, dans le seul CLI, pour regrouper les plaintes sur une ligne de commande.

Rien n'utilise les enchaînements (`Then`, `To`, `Recover`, `Finally` et leurs formes asynchrones),
l'identifiant ni l'horodatage d'une occurrence, la transience, ni les erreurs d'infrastructure.

La couche de documentation de `FirstClassErrors` (`DescribeError`, `[DocumentedBy]`,
`[ProvidesErrorsFor]`) décrit dix factories. Aucun test, aucun générateur ne la lit dans ce dépôt :
`[ProvidesErrorsFor]` ne sert qu'à un test d'architecture qui vérifie qu'une erreur s'appelle
`<Concept>Error`.

DEC0007 limite la surface publique du moteur à `Slugger.Domain` et à la façade `Themes`.

## Décision

Dans ce contexte, nous décidons de remplacer `FirstClassErrors` par un modèle d'erreurs propre au
moteur, dans `Slugger.Domain.Errors`, qui reprend le nom et la forme des seuls types utilisés et
dont les primitives de construction restent internes.

## Justification

**C'est le seul chemin vers une version stable** qui ne passe pas par une version stable de
`FirstClassErrors`, et c'est elle que le mainteneur écarte.

**Garder les noms et la forme limite la casse à un `using`.** Un consommateur qui lit
`outcome.Error.Code`, parcourt `InnerErrors` ou attrape une `DomainException` remplace
`using FirstClassErrors;` par `using Slugger.Domain.Errors;` et ne change rien d'autre. Le rapport
groupé de DEC0006, une erreur qui porte toutes les raisons comme erreurs internes, garde exactement
sa forme.

**Un consommateur ne fabrique pas d'erreur arbitraire.** Les primitives de construction -
`DomainError.Create`, `ErrorCode.Create`, le builder de contexte, `Outcome<T>.Success` et `Failure`,
les constructeurs - restent internes au moteur, que le CLI et les tests atteignent par
`InternalsVisibleTo`. C'est l'asymétrie de DEC0007 : ouvrir plus tard ne casse personne, refermer
casse tout le monde.

Les factories des catalogues restent publiques, comme avant : `ThemeErrors.Rejected(...)`,
`WordError.Empty(...)` et les autres. Chacune décrit une situation que le moteur connaît, et
`docs/library.md` se sert de `ThemeErrors.Rejected` pour échouer au démarrage comme le ferait un
chargement. Les fermer est une décision sur la surface publique, que l'issue #63 doit prendre avec
le reste.

**`Slugger.Domain.Errors` respecte DEC0007** sans le modifier : la surface publique reste
`Slugger.Domain` et la façade. Et le domaine nomme ses erreurs sans dépendre d'une couche
extérieure.

**Ce que rien n'utilise n'est pas réécrit.** Les enchaînements, l'horodatage, la transience et la
distinction entre erreur de domaine et erreur de port pourront être ajoutés le jour où un appelant
en aura besoin, sans casser personne. D'ici là, la plainte groupée du CLI est une `DomainError`
comme celles qu'elle regroupe.

**La couche de documentation est abandonnée, pas son contenu.** Ce que décrivaient les dix
factories passe dans leur commentaire XML, que lisent à la fois un lecteur du code et un
consommateur dans son éditeur. Le test de nommage se lit sur la dérivation de `Error`, qui dit la
même chose que l'attribut.

**Un message vide est refusé à la construction**, là où `FirstClassErrors` le remplace par une
sentinelle pour qu'une erreur fabriquée sur un chemin d'échec ne lève jamais. Ici, chaque message
est un littéral écrit dans une factory : un message vide est une faute de programmation, et
l'exception la montre dès le premier test qui passe par la factory.

## Alternatives envisagées

### Alternative 1 — Publier FirstClassErrors en version stable

- **Description :** une `0.1.0` sans suffixe suffit à lever NU5104 sans toucher au moteur.
- **Pourquoi écartée :** le mainteneur ne le souhaite pas. Et une `1.0.0` du moteur figerait alors
  des types publics dont la version `0.x` ne promet aucune stabilité.

### Alternative 2 — Rester en préversion jusqu'à une version stable de FirstClassErrors

- **Description :** ne rien changer et attendre.
- **Pourquoi écartée :** la `1.0.0` du moteur dépendrait d'une décision que personne ne prévoit de
  prendre.

### Alternative 3 — Copier les sources de FirstClassErrors dans le moteur

- **Description :** reprendre les cinquante et un fichiers tels quels.
- **Pourquoi écartée :** les signatures publiques nomment ces types, qui ne pourraient donc pas
  devenir internes. Le moteur publierait et maintiendrait des enchaînements asynchrones, la
  transience et un générateur de documentation dont il n'a pas l'usage.

### Alternative 4 — Des signatures sans type d'erreur dédié

- **Description :** des exceptions seules, ou des `TryLoad` qui renvoient une liste de messages.
- **Pourquoi écartée :** un consommateur perd les codes stables sur lesquels `docs/library.md` lui
  dit de brancher, et le CLI le contexte typé qu'il lit pour ses indices. C'est une refonte de
  l'API, et non le retrait d'une dépendance.

## Conséquences

### Positives

- Le moteur ne dépend plus que de `Value` : NU5104 ne bloque plus `lib-v1.0.0`.
- La surface publique perd les primitives qui fabriquent une erreur quelconque : hors du moteur, on
  ne construit plus que les situations que décrit un catalogue.
- `IsFailure` et `IsSuccess` disent au compilateur quand `Error` est renseigné. Un consommateur n'a
  plus besoin d'un motif pour lire `outcome.Error.Code` après `if (outcome.IsFailure)`.

### Négatives

- Changement cassant pour un consommateur de la préversion : le namespace change, et ce qui
  n'est pas repris (`InstanceId`, `OccurredAt`, `Then`, `Recover`, `Finally`) disparaît.
- La description structurée de chaque erreur (titre, règle, causes probables) devient de la
  prose dans un commentaire XML, sans forme imposée.
- Le modèle d'erreurs devient du code à maintenir et à tester dans ce dépôt.

### Risques

- Le modèle grossit au fil des besoins jusqu'à redevenir `FirstClassErrors`, en moins éprouvé.
- Ces types, et les factories publiques des catalogues, entrent dans la surface qu'une `1.0.0`
  promet : le tri de la surface publique, ouvert par l'issue #63, doit les compter.

### Actions de suivi

- Retirer `FirstClassErrors` de `Directory.Packages.props` et de la liste blanche de
  `NamespaceDependencyTests`.
- Lire la règle de nommage `<Concept>Error` sur la dérivation de `Error` dans
  `ValueObjectRulesTests`.
- Vérifier par un pack en `1.0.0` que NU5104 a disparu.
- Mettre à jour `docs/library.md`, `docs/architecture.md`, `docs/releasing.md`, `CONTRIBUTING.md`,
  `CLAUDE.md`, la page du paquet et `release.yml`.
- À l'acceptation, ajouter une ligne « Nuancé par DEC0029 » au statut de DEC0006 et de DEC0007.
