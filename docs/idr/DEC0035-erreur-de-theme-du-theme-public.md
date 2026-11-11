# DEC0035 | Une erreur de thème nommée d'après le thème public, `SluggerThemeError`, à la place de `ThemeErrors`

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-11-11 | Accepté | Deuxième point de l'issue #63, avant `lib-v1.0.0` | |

## Contexte

`ThemeErrors` était le catalogue public de toutes les raisons de refuser un thème : une classe
statique au pluriel, rangée dans `Slugger.Domain.Validation`, dont une vingtaine de factories
publiques renvoyaient un `DomainError` générique, levé en `DomainException` générique. Elle exposait
24 codes dans `ThemeErrors.Codes` et 8 clés de contexte.

Tous les autres catalogues du moteur suivent la règle écrite dans `CLAUDE.md` : chaque concept
possède ses erreurs. `WordError` parle pour `Word`, `TermError` pour `Term`, `ThemeNameError` pour
`ThemeName` : une classe au singulier, dérivée de `Error`, rangée à côté du type dont elle porte le
nom et levant sa propre exception. `ThemeErrors` était le dernier à ne pas la suivre, et l'étape 4 de
`refactoring-in-progress.md` prévoyait de le transformer en `ThemeError`.

Depuis DEC0034, deux classes portent le thème : `Theme`, l'entité interne du refactoring, et
`SluggerTheme`, le type public que la bibliothèque remet au consommateur.

DEC0029 avait gardé les factories publiques et renvoyé leur sort à #63. Le seul usage documenté —
`ThemeErrors.Rejected`, pour échouer au démarrage comme un chargement — est couvert depuis DEC0031
par `SlugSource.PrepareResult`.

Trois codes, `THEME_ALREADY_REGISTERED`, `THEME_NOT_A_FILE` et `THEME_NOT_SELECTABLE`, ne sont levés
que par le répertoire de thèmes de la commande, qu'aucune méthode publique n'atteint.

## Décision

Dans ce contexte, nous décidons que tout refus de thème est un `SluggerThemeError`, dérivé de
`Error` et rangé dans `Slugger.Domain` à côté de `SluggerTheme`, qui lève un
`SluggerThemeException`, dont les codes et les clés de contexte sont publics et les factories
internes, et dont `Codes` ne publie que ce qu'une méthode publique peut renvoyer.

## Justification

**L'erreur porte le nom de la classe dont elle parle.** C'est la règle des autres catalogues.
Le consommateur ne tient jamais un `Theme` : un `ThemeError` le renverrait à un type qu'il ne voit
pas. Ses refus sont ceux du `SluggerTheme` que `Themes` lui remet.

**Le type dit de quoi parle l'erreur.** `catch (SluggerThemeException)` attrape un refus de thème et
rien d'autre, et `outcome.Error is SluggerThemeError` le reconnaît sans comparer de code. Avec un
`DomainException` générique, seul le code distinguait un refus de thème de toute autre erreur du
moteur.

**Un seul catalogue pour le thème.** L'entité `Theme` lèvera les mêmes `SluggerThemeError` une fois
branchée. Deux catalogues auraient déclaré deux fois les mêmes codes, `THEME_NO_NOUN` le premier —
le piège que l'étape 4 signalait.

**`Codes` et non `Code`.** `Error.Code` est déjà la propriété d'instance qui porte le code de
l'erreur. Une classe imbriquée `Code` la masque (`CS0108`) et rend `erreur.Code` illisible sur une
variable typée `SluggerThemeError` (`CS0572`, vérifié). Le pluriel est aussi la convention .NET d'une
classe de constantes : `StatusCodes`, `HttpMethods`, `MediaTypeNames`.

**Un consommateur lit une erreur, il n'en fabrique pas.** Le type, les codes et les clés de contexte
lui suffisent. Une factory publique fige sa signature : enrichir le message de `PoolTooSmall` d'un
paramètre deviendrait une version majeure. Fermer maintenant laisse ouvrir plus tard, en version
mineure, si un besoin réel apparaît — l'asymétrie de DEC0007.

**Un code public est un code qu'on peut recevoir.** Les trois codes du répertoire de thèmes de la
commande restent internes : publiés, ils promettraient une réponse qu'aucune méthode publique ne donne.

## Alternatives envisagées

### Alternative 1 — Garder `ThemeErrors` tel quel

- **Description :** figer le catalogue statique, ses `DomainError` et son `DomainException`.
- **Pourquoi écartée :** garde une exception permanente à la règle de nommage, et un type d'erreur
  qui ne dit pas de quoi il parle.

### Alternative 2 — `ThemeError`, comme le prévoyait l'étape 4

- **Description :** nommer le catalogue d'après le thème en général.
- **Pourquoi écartée :** `Theme` est l'entité interne. Le nom rattacherait l'erreur publique à un type
  que le consommateur ne voit pas, et laisserait l'entité sans nom pour la sienne.

### Alternative 3 — Un `SluggerThemeErrorCode` hors de la classe d'erreur

- **Description :** sortir les codes dans un type à eux, pour écrire `SluggerThemeErrorCode.PoolTooSmall`
  au singulier.
- **Pourquoi écartée :** un type public de plus, et une exception à la forme de tous les autres
  catalogues, pour un pluriel que la convention .NET et la collision avec `Error.Code` justifient.

### Alternative 4 — Un type d'erreur par concept touché

- **Description :** séparer ce qui porte sur le fichier (`THEME_MALFORMED_JSON`), la recherche
  (`THEME_NOT_FOUND`) ou les options de génération (`THEME_NOTHING_FITS_THE_LIMIT`).
- **Pourquoi écartée :** pour un consommateur, ce sont toutes des raisons de ne pas obtenir un thème
  utilisable, réunies dans un même `THEME_REJECTED` (DEC0006). Les éclater rendrait ce rapport
  hétérogène sans rien lui apprendre de plus.

## Conséquences

### Positives

- Un refus de thème s'attrape et se reconnaît par son type.
- Plus aucun catalogue ne fait exception à la règle de nommage, et l'étape 4 du refactoring est faite.
- La liste des codes publics est exactement ce que la bibliothèque peut renvoyer.

### Négatives

- La documentation XML qui décrit chaque situation — la règle et ce qui la casse d'habitude — vit sur
  les factories, désormais internes : un consommateur la lit dans `library.md`, pas dans son éditeur.
- `DomainError` et `DomainException` ne servent plus qu'à la commande, par `CliErrors`, tout en restant
  publics.

### Risques

- Aucun identifié.

### Actions de suivi

- À l'acceptation, ajouter une ligne « Nuancé par DEC0035 » au statut de DEC0029 : les factories des
  catalogues, qu'elle gardait publiques, sont internes.
- Décider avec le reste de #63 si `DomainError` et `DomainException` restent publics.
