# DEC0024 | Un plafond de mots explicitement absent, qui outrepasse celui du thème

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-22 | Accepté | | |

## Contexte

DEC0023 range `maxSegmentWords` dans les `defaults` d'un thème, au même titre que `sep` ou
`segmentMode` : c'est un trait de style, mesuré au chargement, et la chaîne de priorité de DEC0004
le fait céder devant un argument explicite de la ligne de commande.

Sauf que DEC0004 le dit lui-même, dans ses propres conséquences négatives : *« Une option dite par
un thème ne peut pas être "dé-dite" depuis la ligne de commande autrement qu'en donnant une autre
valeur : la chaîne n'a pas de niveau "revenir au défaut". »* Pour la plupart des options, ce n'est
pas gênant — dire une autre valeur suffit (`--sep` en prend un autre, `--casing` un autre mot).
Mais `maxSegmentWords` n'a que deux états dans le fichier : un nombre, ou son absence. Sur la
ligne de commande, `--max-segment-words <N>` ne peut dire que « ce nombre-ci », jamais « aucun » —
`int?` confond « ce niveau ne dit rien » et « ce niveau dit explicitement rien » dans le même
`null`.

C'est le thème `quantum-physics`, en s'ajoutant `"maxSegmentWords": 1` pour porter la forme de
Docker, qui a rendu le trou concret : il porte 195 noms, dont 88 en plusieurs mots
(`black hole`, `bell pair`, `wave-particle duality`…), écrits pour la forme adjectif-participe-nom
à trois mots et non pour trois segments d'un mot chacun. Une fois `maxSegmentWords: 1` dans ses
`defaults`, plus rien sur la ligne de commande ne pouvait redonner accès à ce vocabulaire pour un
tirage ponctuel, sinon en répétant `--theme quantum-physics,docker` pour profiter de l'extinction
des `defaults` en multi-thème (DEC0023) — un contournement qui change aussi tout le reste du
tirage, pas seulement le plafond.

`--mimic-style` avait déjà ce problème, pour une question différente (« le style du thème
s'applique-t-il ? ») et l'avait déjà résolu : trois états plutôt qu'un booléen — absent, forcé,
explicitement désactivé (DEC0004).

## Décision

Dans ce contexte, nous décidons que `--max-segment-words` accepte, en plus d'un nombre, le mot
`none` : un plafond explicitement absent, qui l'emporte sur celui des `defaults` du thème tiré
exactement comme un nombre explicite l'aurait fait.

## Justification

**C'est `--mimic-style` appliqué à un autre levier, et rien de plus.** Le même trou - un `null`
qui doit dire deux choses à la fois - reçoit la même réponse : une valeur qui peut dire
« explicitement rien » plutôt que seulement « quelque chose » ou « rien dit ». Le type qui la
porte, `SegmentWordsCap`, est un nullable de plus autour d'un nullable : l'extérieur reste ce que
chaque couche de la chaîne connaît déjà - absent, une couche ne parle pas - et l'intérieur
distingue enfin `None` (cette couche demande explicitement l'absence de plafond) de `Of(n)` (cette
couche demande ce plafond-ci).

**La fusion vers `GenerationOptions` ne peut plus être un simple `??`.** `layer.MaxSegmentWords ??
options.MaxSegmentWords` lirait `None` comme un silence et laisserait la couche du dessous
répondre à sa place - exactement le bogue que la chaîne devait éviter. La bonne lecture filtre sur
la couche elle-même (`layer.MaxSegmentWords is { } cap ? cap.Words : options.MaxSegmentWords`),
comme `AppliesTheStyleOf` le fait déjà pour `MimicStyle` plutôt que de le comparer à un booléen.

**`none` et non `0` ou `-1`.** Un plafond de zéro ne retirerait pas une contrainte, il viderait le
thème - `ArgumentOutOfRangeException.ThrowIfLessThan(maxSegmentWords ?? 1, 1)` dans
`ThemeResolver` le refuse déjà. Un entier sentinelle (`-1`, `int.MaxValue`) porterait le sens dans
la tête de qui lit le code, jamais dans le type, et un thème qui écrirait ce nombre par accident
matérialiserait le même bogue sans qu'aucune règle ne le voie venir. `none` est un mot qui ne
prétend pas être un compte, comme `--word-sep ""` n'a jamais prétendu être un séparateur.

**Le reste de `ThemeResolver` n'a rien à savoir de tout ça.** `int? maxSegmentWords` y reste
exactement ce qu'il était : nul pour aucun plafond, un nombre sinon. La distinction à trois états
n'existe que le temps de fusionner les couches de la chaîne dans
`Slugger.Application.Options` ; une fois `GenerationOptions` construit, il n'y a plus de couches à
départager, donc plus besoin du type qui les départage.

## Alternatives envisagées

### Alternative 1 — Un plafond de zéro veut dire « aucun »

- **Description :** réutiliser `int?` tel quel, et prêter au chiffre 0 le sens spécial
  « explicitement aucun plafond ».
- **Pourquoi écartée :** `0` a déjà un sens - un plafond qui ne laisse rien passer - et
  `ThemeResolver` le refuse pour cette raison. Lui en donner un second au même endroit est
  exactement l'ambiguïté que ce DEC ferme pour `null`, ouverte à nouveau pour `0`.

### Alternative 2 — Un flag séparé, `--no-segment-word-cap`

- **Description :** un booléen à côté de `--max-segment-words`, plutôt qu'une valeur de plus pour
  la même option.
- **Pourquoi écartée :** deux options pour un seul levier posent la question de leur désaccord
  (`--max-segment-words 2 --no-segment-word-cap` veut dire quoi ?) sans qu'aucune n'y réponde par
  construction. Un mot de plus sur l'option existante ne peut pas se contredire lui-même.

### Alternative 3 — Ne rien faire, contourner avec `--mimic-style false`

- **Description :** `--mimic-style false` retire déjà tous les `defaults` du thème tiré, plafond
  compris.
- **Pourquoi écartée :** ça répond à la mauvaise question. Le style d'un thème - son séparateur,
  sa casse, son mode de segment - reste ce qu'on veut la plupart du temps ; seul son plafond de
  mots gêne un tirage ponctuel. `--mimic-style false` jetterait tout le reste avec.

## Conséquences

### Positives

- `quantum-physics` peut écrire `"maxSegmentWords": 1` dans ses `defaults` - la forme de Docker
  par défaut - sans perdre l'accès à ses 88 noms composés : `--max-segment-words none` les rend
  pour un tirage qui le demande.
- Le trou que DEC0004 documentait déjà comme une conséquence négative est refermé pour le seul
  levier où il mordait vraiment.
- `SegmentWordsCap` ne fuit jamais hors de `Slugger.Application.Options` : `GenerationOptions`,
  `ThemeResolver`, `SlugGenerator` gardent le `int?` qu'ils avaient.

### Négatives

- Une option de plus qui n'est pas un nombre pur : `--max-segment-words` accepte maintenant un mot
  réservé, comme `--mimic-style` le fait déjà pour `true`/`false`.
- Le mot est propre à cette option - `--max-length none` reste refusé, puisque rien ne l'a jamais
  demandé pour elle, et l'unifier maintenant serait une décision distincte, prise sans thème pour
  la réclamer.

### Risques

- `none` en minuscules et rien d'autre : `None`, `NONE` restent refusés comme n'importe quel mot
  hors de l'ensemble attendu (`Choice<TChoice>` fait déjà ce choix pour `--casing` et `--segment`,
  celui-ci le reprend pour la même raison).

### Actions de suivi

- Documenter `--max-segment-words none` dans `docs/writing-a-theme.md`, à côté de
  `maxSegmentWords`.
- `quantum-physics.json` déclare `"maxSegmentWords": 1` dans ses `defaults` : premier thème du
  dépôt à s'en servir, et premier cas réel qui aurait buté sur le trou que ce DEC ferme.
