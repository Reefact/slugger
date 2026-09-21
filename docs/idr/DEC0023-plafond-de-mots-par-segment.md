# DEC0023 | Plafond de mots par segment, tenu en retirant des valeurs avant le tirage

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-21 | Accepté | | |

## Contexte

**Les thèmes embarqués n'ont aucune valeur composée.** Mesuré : `docker` a 0 nom composé sur 236,
`heroku` 0 sur 216. Leur forme — `focused_turing`, `mystic-wind` — tient à ce que chaque segment
est un mot, et rien dans le code ne l'impose. `slugger` en a 180 sur 231, et `code-review`, le
quatrième thème du dépôt, 171 sur 351.

**Une frontière de segment est invisible dès qu'une valeur est composée.** DEC0008 réduit tout
caractère non alphanumérique en frontière de mot, et `SlugFormatter` remplace ces frontières par
le séparateur. `awful` + `snake cased` + `property name` s'écrit donc
`awful-snake-cased-property-name` : cinq morceaux pour trois segments, et rien ne dit où chacun
commence. Mesuré sur 1000 tirages de `code-review` : **57 % des slugs portent au moins une valeur
composée**, et 8 % font cinq morceaux ou plus.

`--word-sep` répond déjà à la lisibilité — `awful-snake_cased-property_name` — mais il ne réduit
rien : le slug reste aussi long, et la valeur composée reste tirée.

DEC0018 a posé le geste : un plafond retire des mots du vivier **avant** le tirage, la surface
réduite est validée par les règles ordinaires, et le refus nomme ce qui manque. DEC0005 range les
leviers de format à l'exécution. DEC0004 fixe la chaîne de priorité unique.

Mesuré sur `code-review` avec un plafond d'un mot : il resterait 180 noms, 525 adjectifs et 259
participes, et le thème serait refusé pour trois catégories sous le seuil des 40 000
(`smellActions` 18 304, `smellTraits` 18 304, `testActions` 30 156). Avec un plafond de deux, un
seul mot du fichier s'en va.

## Décision

Dans ce contexte, nous décidons qu'un nombre maximal de mots par segment est obtenu en retirant
des valeurs avant le tirage, déclaré par le thème comme un trait de style et par l'exécution
comme un plafond.

## Justification

C'est DEC0018 appliqué à une autre unité, et rien de plus : la même couture, le même contrat —
retirer une valeur entière plutôt que la raccourcir. `speculative generality` quitte le vivier
sous un plafond d'un mot ; il n'en sort jamais `speculative`. Le nombre de tirages reste fixe et
le tirage reste uniforme sur ce qui reste.

**Le plafond compte les mots d'un segment, jamais ceux du slug.** Un nom de trois mots ne dépense
pas le budget de l'adjectif : il est simplement écarté. Compter le slug ferait dépendre la
disponibilité d'un adjectif de la longueur du nom tiré, ce qu'aucune autre règle ne fait.

**Le nom est mesuré pour lui-même**, ce qu'un budget n'a jamais eu à faire. Sous DEC0018, un nom
trop long ne laisse de place à aucun mot devant lui, donc ses viviers se vident et il tombe de
lui-même. Un plafond de mots ne marche pas ainsi — un nom de deux mots atteint tous les adjectifs
d'un mot — et c'est précisément le cas qui compte, puisqu'un nom composé est le plus souvent la
partie longue.

**Le trait appartient au thème** parce que la largeur d'un segment fait partie de son allure, au
même titre que son séparateur : `docker` et `heroku` sont reconnaissables à leurs segments d'un
mot. Il va dans `defaults`, avec `segmentMode` et `sep`, et non à la racine comme `maxLength` —
ce n'est pas une contrainte de sûreté qui échouerait ouvert en multi-thème, c'est un choix
esthétique, et un choix esthétique qui s'éteint quand un deuxième thème entre en portée est
exactement ce que `defaults` veut dire.

**Le plafond appartient à l'exécution** parce qu'elle seule sait où le slug est posé. Et comme la
surface réduite est un thème comme un autre, elle est validée par les règles ordinaires : un
plafond qui vide une catégorie est refusé, en la nommant, plutôt que de produire en silence une
variété moindre.

## Alternatives envisagées

### Alternative 1 — Statu quo, retirer les valeurs composées à la main du thème

- **Description :** éditer le fichier pour n'y laisser que des valeurs d'un mot.
- **Pourquoi écartée :** mesuré sur `code-review`, cela emporte `refactoringActions` et
  `namingActions` en entier, et ne laisse qu'un nom sur dix-neuf aux odeurs. L'auteur perdrait le
  vocabulaire de son domaine pour satisfaire une contrainte qui appartient à la destination.

### Alternative 2 — Rendre la frontière visible au lieu de réduire

- **Description :** `wordSep` dans `defaults`, soit `awful-snake_cased-property_name`.
- **Pourquoi écartée :** répond à la lisibilité et à elle seule. Le slug reste aussi long, et
  qui veut des segments d'un mot ne l'obtient pas. Les deux leviers sont complémentaires, pas
  concurrents.

### Alternative 3 — Un booléen plutôt qu'un nombre

- **Description :** `--single-word`, ou `"singleWordSegments": true`.
- **Pourquoi écartée :** un nombre dit la même chose à 1 et dit en plus ce que 2 veut dire.
  Mesuré : sur `code-review`, 2 ne retire qu'un mot et 1 en retire 342 — les deux réglages ne
  répondent pas à la même question, et un booléen n'en pose qu'une.

### Alternative 4 — Compter les mots du slug entier

- **Description :** `--max-words 3` pour un slug de trois mots au plus.
- **Pourquoi écartée :** un nom de trois mots mangerait alors tout le budget et il ne resterait
  ni adjectif ni participe. La disponibilité d'un mot dépendrait de la valeur tirée à côté, ce
  qu'aucune autre règle du moteur ne fait — et le nom de l'option le laisserait croire dans les
  deux sens, ce qui est pire que l'un ou l'autre.

### Alternative 5 — Découper une valeur composée au lieu de l'écarter

- **Description :** garder `speculative generality` en n'écrivant que `speculative`.
- **Pourquoi écartée :** c'est la troncature que DEC0018 a déjà refusée, sur une autre unité :
  elle mutile la valeur, et deux valeurs distinctes peuvent devenir la même.

## Conséquences

### Positives

- `--max-segment-words 1` donne des slugs à trois morceaux pour trois segments, où la frontière
  se lit sans convention : `plodding-recorded-fallback`.
- La surface réduite est un thème comme un autre — validée, mesurée par `--analyze`, et ses
  planchers levés par `--allow-small-theme`.
- Le refus est utile plutôt que binaire : il dit quelles catégories tombent et de combien.
- `docker` et `heroku` peuvent désormais écrire dans leur fichier ce que leur vocabulaire tient
  déjà, et un nom composé ajouté un jour deviendrait un refus au chargement.
- La question « y a-t-il une réduction ? » devient explicite (`ThemeResolver.Narrows`), là où
  elle se lisait `_budget is null` à cinq endroits.
- Un thème qui déclare le plafond est **mesuré dessus au chargement**, comme DEC0016 mesure ses
  planchers sur le `segmentMode` déclaré. Déclarer 1 sans le vocabulaire qui va avec est un refus
  au chargement, pas une surprise au premier tirage.

### Négatives

- Une cinquième raison pour qu'une valeur disparaisse d'un tirage, après les catégories,
  `except`, les paires et le budget.
- Sur un thème dont le vocabulaire est majoritairement composé, le seul réglage utile refuse. La
  correction n'est pas dans l'option, elle est dans le vocabulaire — l'option le dit, elle ne le
  répare pas.
- Un plafond de deux est un quasi no-op sur les thèmes mesurés : la marche utile est entre 1 et
  2, et il n'y a rien au-dessus.

### Risques

- Un thème qui déclare `maxSegmentWords` dans ses `defaults` perd ce trait dès qu'un deuxième
  thème est en portée, comme tout `defaults`. C'est voulu, mais ça surprendra.
- Un plafond bas réduit la variété avant de refuser quoi que ce soit : la surface maigrit en
  silence tant qu'elle reste au-dessus des planchers.

### Actions de suivi

- Documenter `maxSegmentWords` et `--max-segment-words` dans `docs/writing-a-theme.md`.
- `code-review` ne passe pas sous un plafond d'un mot : son vocabulaire d'odeurs et de tests
  demande des noms d'un mot avant que la clé puisse entrer dans ses `defaults`.
