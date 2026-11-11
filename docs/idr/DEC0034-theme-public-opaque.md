# DEC0034 | Un thème public opaque, `SluggerTheme`, à la place de `ThemeDocument`

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-11-11 | Accepté | Premier point de l'issue #63, avant `lib-v1.0.0` | |

## Contexte

`ThemeDocument` était le type que tout consommateur manipule : chaque `Themes.Load*` le renvoyait,
chaque `SlugGenerator.Generate` le prenait, et 31 signatures publiques le nommaient. Il était
entièrement transparent : un constructeur public qui prenait des dictionnaires de chaînes sans rien
valider, et des propriétés publiques — `Nouns`, `Adjectives`, `Participles`, `Incompatible` — qui
exposaient les mots tels que le fichier les écrit.

`docs/refactoring-in-progress.md` prévoit de remplacer ce modèle par l'entité `Theme` et ses objets
valeur, puis de déplacer `ThemeDocument` dans `Infrastructure` (étape 3). Le moteur tourne encore
sur des chaînes : les étapes 1 à 3 restent à faire. Le document le dit lui-même : faite après une
version stable de la bibliothèque, l'étape 3 est une version majeure, puisque `Infrastructure` est
`internal` et que `ThemeDocument` disparaîtrait de la surface publique.

Une version `1.0.0` aurait donc figé l'une de deux choses : la représentation en chaînes des mots,
ou la promesse d'une `2.0.0` dès la fin du refactoring.

Le nom `Theme` est pris par l'entité interne. Personne en dehors de la commande ne lisait les mots :
la commande les lit par `InternalsVisibleTo`, et la documentation de la bibliothèque n'utilisait que
`Name`.

## Décision

Dans ce contexte, nous décidons que la bibliothèque remet un thème sous la forme d'une classe
scellée `SluggerTheme`, qu'elle seule construit, qui expose ce que le thème dit de lui-même —
`Name`, `Metadata`, `Defaults`, `MaxLength` — et jamais ses mots, et que `ThemeDocument` devient
`internal`.

## Justification

**Une poignée opaque découple ce que le consommateur nomme de ce que le moteur tire.**
`SluggerTheme` enveloppe aujourd'hui un `ThemeDocument` ; il enveloppera l'entité `Theme` une fois le
refactoring fini. L'étape 3 devient un détail d'implémentation au lieu d'une version majeure.

**Rien de ce qui n'est pas publié ne peut être cassé.** C'est l'asymétrie de DEC0007 : ouvrir un
membre plus tard est une version mineure, en fermer un est une version majeure. Les mots restent
fermés jusqu'à ce qu'un consommateur en ait besoin, et ce jour-là ils s'ouvrent sous une forme
pensée pour lui plutôt que sous la forme du fichier.

**Seule la bibliothèque en construit, donc un `SluggerTheme` est toujours un thème accepté.** Le
constructeur est `internal` : un `SluggerTheme` vient d'un chargement, qui l'a tenu à toutes les
règles. Un thème se construit en mémoire en passant son JSON à `Themes.LoadFromJson`, qui le valide
comme un fichier.

**Un nom à lui.** `SluggerTheme` ne se confond ni avec `ThemeDocument`, la forme du fichier, ni avec
`Theme`, l'entité. Le préfixe évite aussi la collision avec les `Theme` des bibliothèques d'interface
qu'une application importe souvent à côté.

**Ce qui ne servait qu'à lire le contenu se ferme avec lui.** `ThemeResolver` et `NounEntry` ne
s'obtenaient qu'à partir des mots ; `ThemeValidator.Validate` et `Remarks` ne servaient qu'à
vérifier un document construit à la main, que plus personne ne peut construire. Ils deviennent
`internal`. Ce qu'un consommateur en tirait reste possible : `SlugSource.PrepareResult` valide un
thème sous des options, et `new ThemeCombinatorics(theme, options)` compte ce que des options laissent
d'un thème, ce qui demandait jusqu'ici de construire un `ThemeResolver`.

## Alternatives envisagées

### Alternative 1 — Finir le refactoring avant la version stable

- **Description :** mener les étapes 1 à 4 de `refactoring-in-progress.md`, puis publier l'entité
  `Theme` comme type public.
- **Pourquoi écartée :** recule la version stable d'autant, alors que la surface publique de `Theme`
  n'est pas encore conçue — l'étape 1 dit justement de la déduire des besoins du résolveur.

### Alternative 2 — Figer `ThemeDocument` tel quel

- **Description :** renoncer à l'étape 3 et promettre `ThemeDocument` pour toujours.
- **Pourquoi écartée :** promet pour toujours la représentation en chaînes, le constructeur qui ne
  valide rien et chacun des accesseurs, et fait cohabiter deux modèles du thème sans fin.

### Alternative 3 — Une interface `ISluggerTheme`

- **Description :** renvoyer une interface publique, implémentée en interne par `ThemeDocument`
  puis par `Theme`.
- **Pourquoi écartée :** une interface publique s'implémente hors de la bibliothèque, ce qui force le
  moteur soit à refuser à l'exécution ce qu'il ne connaît pas, soit à exposer les mots pour tirer
  dedans, et gèle l'interface : lui ajouter un membre casse ceux qui l'implémentent. Un membre
  `internal` empêche l'implémentation extérieure (vérifié : `CS0535`), mais l'interface n'offre
  alors plus rien de plus qu'une classe scellée, par un mécanisme moins connu.

## Conséquences

### Positives

- Le refactoring peut se terminer en versions mineures, sans casser aucun consommateur.
- Un `SluggerTheme` en main a toujours été accepté par un chargement.
- La surface publique perd trois types : `ThemeDocument`, `ThemeResolver`, `NounEntry`.

### Négatives

- Un consommateur ne peut plus lire les mots d'un thème, ni en construire un sans passer par du JSON.
- Les 31 signatures qui nommaient `ThemeDocument` changent : c'est une rupture pour qui a écrit contre
  `1.0.0-preview.1`, acceptable tant que la bibliothèque est en préversion.
- Un consommateur ne peut pas substituer un faux `SluggerTheme` dans ses propres tests ; il charge un
  thème embarqué et fixe la graine.

### Risques

- Le site `slugger.online` pourrait avoir besoin de lire les mots d'un thème pour ses pages. Cette
  lecture s'ajoutera alors en version mineure, sous une forme conçue pour elle (#76).

### Actions de suivi

- À l'acceptation, ajouter une ligne « Nuancé par DEC0034 » au statut de DEC0031 : le constructeur
  public de `ThemeDocument` dont elle parle n'existe plus, et le document non validé n'est plus
  possible que dans les tests.
- Mettre à jour `refactoring-in-progress.md` : l'étape 3 ne change plus la surface publique.
