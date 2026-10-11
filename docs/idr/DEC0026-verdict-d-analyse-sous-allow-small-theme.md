# DEC0026 | Le verdict de `--analyze` suit `--allow-small-theme`, les marges restent sur les vrais planchers

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-10-11 | Accepté | Issue #39 ; nuance la justification de DEC0014 | |

## Contexte

DEC0014 charge le fichier analysé en levant les règles de taille, pour pouvoir le mesurer au moment
même où il ne passe pas, et fait revenir les refus *« du validateur exécuté contre les vrais
planchers »*. `AnalyzeThemeUseCase` ne lisait donc jamais `--allow-small-theme`, et `ThemeAnalyzer`
appelait `ThemeValidator.Validate(resolver)` sans lui.

Deux commandes se contredisaient alors sur le même fichier et la même option :

```console
$ slugger --analyze ./small.json --allow-small-theme
Theme "small" would be refused for 2 reasons: ...
$ slugger --register ./small.json --allow-small-theme
Theme "small" registered.
```

Le champ `"allowSmall": true` d'un thème, lui, était déjà honoré par le verdict, puisque
`ThemeValidator.Validate` le lit lui-même : l'option de la ligne de commande était la seule des deux
façons de lever les planchers que l'analyse ignorait. Et un thème qui ne passait que grâce à ce champ
était annoncé *« accepted as it is »* au-dessus de marges négatives.

## Décision

Dans ce contexte, nous décidons que le verdict de `--analyze` lève les planchers de taille quand
`--allow-small-theme` le demande, comme `--register` le fait, et dit qu'ils ont été levés ; les
marges, elles, restent mesurées contre les vrais planchers.

## Justification

**Une seule réponse à « ce thème est-il accepté sous ces options ».** DEC0014 compose le validateur
pour ne pas avoir deux réponses qui divergent ; elles divergeaient pourtant, entre `--analyze` et
`--register`, sur le seul point où l'une lisait une option que l'autre ignorait. Passer l'option au
validateur remet les deux commandes d'accord.

**Ce que DEC0014 voulait garder est gardé.** Son argument porte sur les marges : un thème deux mots
au-dessus d'un plancher se lit comme sain, et le rapport doit dire de combien. Les mesures ne
dépendent pas de l'option ; seul le verdict change, et le rapport dit pourquoi il accepte un thème
dont les marges sont négatives.

**« Accepté tel quel » devient vrai.** `ThemeAnalysis.FloorsWaived` distingue un thème accepté sur
ses mérites d'un thème accepté parce que les planchers ont été levés, par l'option ou par son propre
`allowSmall` ; le terminal et le rapport disent *« accepted with the size floors waived »* dans le
second cas, au lieu de *« accepted as it is »*.

## Alternatives envisagées

### Alternative 1 — Refuser la combinaison `--analyze --allow-small-theme`

- **Description :** une erreur de ligne de commande qui dit que l'analyse mesure toujours contre les
  vrais planchers.
- **Pourquoi écartée :** l'auteur qui écrit un petit thème volontairement, et qui l'enregistre avec
  cette option, ne pourrait plus demander à l'analyse le verdict qui vaut pour lui ; et le champ
  `allowSmall` du thème resterait honoré, ce qui garde l'incohérence entre les deux façons de lever
  les planchers.

### Alternative 2 — Laisser tel quel et le documenter

- **Description :** c'était l'état documenté (*« It ignores `--allow-small-theme` »*).
- **Pourquoi écartée :** une documentation qui explique pourquoi deux commandes se contredisent ne
  les empêche pas de se contredire.

## Conséquences

### Positives

- `--analyze` et `--register` rendent le même verdict sous les mêmes options.
- Un thème accepté seulement parce que ses planchers sont levés n'est plus annoncé « tel quel ».

### Négatives

- Un rapport produit avec l'option ne dit pas, à lui seul, que le thème serait refusé sans elle ;
  la phrase du verdict et les marges en gras sont ce qui le montre.

### Risques

- Aucun identifié : les mesures sont inchangées, et la règle aligne l'analyse sur un comportement
  existant de `--register`.

### Actions de suivi

- Retirer de `docs/theme-reference.md` la ligne *« It ignores `--allow-small-theme` »* et décrire le
  nouveau verdict.
