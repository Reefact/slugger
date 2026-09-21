# DEC0019 | Ligne de commande déclarée une seule fois, lue et rendue par Spectre

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-21 | Accepté | | |

## Contexte

`CommandLineParser` était écrit à la main : 254 lignes, un `switch` sur vingt-quatre flags, et à
côté une propriété `KnownFlags` énumérant ces mêmes vingt-quatre flags — deux déclarations des
mêmes options, que rien ne réconciliait. `KnownFlags` ne servait qu'au refus, où une distance
d'édition nommait le flag connu le plus proche quand il l'était à moins de trois éditions.

**`slugger` n'avait pas de `--help`.** Mesuré : aucune occurrence de `--help` dans le dépôt avant
ce changement, ni de `--version`. Les vingt-quatre options n'étaient décrites nulle part que le
code, et `docs/slugger-spec.md`, qui les paraphrasait, a été supprimée.

DEC0014 a donné au CLI un rapport d'analyse à dessiner. Il est aujourd'hui rendu en lignes de
texte assemblées à la main, comme `--list-themes` et comme les refus.

DEC0006 rapporte en une seule exécution toutes les raisons d'un refus, « d'un thème ou d'une ligne
de commande », et nomme lui-même son exception : un JSON illisible ne permet aucun constat
supplémentaire.

DEC0007 réduit la surface publique et la liste des dépendances **du moteur** ; `TextCopy` est déjà
laissé au seul CLI, au motif que le presse-papiers n'a de sens que là.

Spectre.Console 0.55.0 est distribué en deux paquets, `Spectre.Console` et `Spectre.Console.Cli`,
le second dépendant du premier. Mesuré sur cette version :

- une option que Spectre ne connaît pas est **ignorée** : elle part dans les arguments restants
  (`context.Remaining`) et rien ne la regarde, donc `slugger --nope` produisait un slug et sortait
  0. `UseStrictParsing()` la refuse à la place, mais en levant à ce jeton-là : le reste de la
  ligne n'est alors jamais lu, et `--nope --casing SHOUT --count abc` ne rapporte qu'une plainte
  au lieu de quatre ;
- Spectre convertit et valide chaque option au moment où il la lie, et s'arrête à la première qui
  échoue ;
- une option laissée sans valeur est refusée en nommant l'option — sauf sous `UseStrictParsing()`,
  où elle avale le nom interne de la commande par défaut : `slugger --theme` allait alors chercher
  un thème appelé `__default_command` ;
- un mot seul est lu comme un nom de commande, et aucune commande ne porte ce nom : `slugger
  docker` lève, quel que soit le mode de lecture ;
- un `FlagValue<bool>` ne distingue pas `--mimic-style` de `--mimic-style false`, le flag nu
  valant le défaut du type ;
- sortie redirigée, sans largeur donnée, l'aide entière se réduit à une ellipse.

## Décision

Dans ce contexte, nous décidons de confier à Spectre la lecture et le rendu de la ligne de
commande, les vingt-quatre options étant déclarées une seule fois sur un type dont Spectre tire à
la fois la liaison des arguments et `--help`.

## Justification

**La déclaration unique est le gain.** Le `switch` et `KnownFlags` disaient deux fois la même
chose sans que rien ne l'exige, et une aide écrite à la main l'aurait dite une troisième — au
moment exact où la spécification venait d'être supprimée pour cette raison-là. Une option porte
désormais son nom, son gabarit de valeur et sa phrase d'aide sur les mêmes trois lignes : l'aide
ne peut plus décrire un flag retiré ni omettre un flag ajouté, parce qu'elle n'est pas écrite.

**Le rendu est la raison de fond.** DEC0014 a créé un rapport à dessiner, et le dessiner à la main
est ce qui coûtera. Prendre la même bibliothèque pour le parseur et pour le rendu, c'est n'en
prendre qu'une : `Spectre.Console.Cli` amène `Spectre.Console` de toute façon.

**Séparer la liaison de la conversion est ce qui tient DEC0006.** Spectre convertit à la liaison
et s'arrête au premier échec ; une ligne portant trois valeurs fautives n'en signalerait qu'une.
Les options sont donc liées en chaînes, et `CommandLineReader` les convertit ensuite en une passe
qui accumule. Le coût est visible — chaque propriété est un `string?` — et c'est le prix de trois
plaintes en une exécution.

**La même raison décide de ne pas activer `UseStrictParsing()`.** Il y a deux façons de refuser
une option inconnue, et elles ne rapportent pas pareil : la lecture stricte lève au premier jeton
qu'elle ne place pas, la lecture permissive lie toute la ligne et dépose le reste dans
`context.Remaining`. Ce que le mode permissif ne fait pas de lui-même, c'est regarder ce qu'il a
déposé — d'où `slugger --nope` qui produisait un slug. `CommandLineReader` le regarde, et chaque
jeton restant devient une plainte parmi les autres. Refuser n'est donc pas ce que la lecture
stricte apporte : c'est une ligne de code dans le lecteur, et la lecture permissive est ce qui
permet de la refuser **sans perdre les trois plaintes suivantes**.

**La dépendance est prise du bon côté de la frontière.** DEC0007 vise le moteur, que Spectre ne
touche pas : `Slugger` reste sur `FirstClassErrors` seul. Un outil en ligne de commande empaquette
ce dont il a besoin, comme il empaquette déjà `TextCopy`.

## Alternatives envisagées

### Alternative 1 — Statu quo, et écrire `--help` à la main

- **Description :** garder le parseur maison et lui ajouter une aide rédigée.
- **Pourquoi écartée :** l'aide serait une troisième déclaration des vingt-quatre options, à côté
  du `switch` et de `KnownFlags`, que rien ne réconcilierait — la dérive que ce changement
  supprime, et celle pour laquelle la spécification a été supprimée.

### Alternative 2 — `System.CommandLine`

- **Description :** la bibliothèque de ligne de commande de Microsoft, qui lie les arguments et
  génère une aide.
- **Pourquoi écartée :** elle ne dessine rien. Le rapport de DEC0014 resterait à écrire à la main
  ou à prendre ailleurs, soit deux bibliothèques là où une suffit.

### Alternative 3 — Spectre pour le rendu seulement

- **Description :** ne prendre `Spectre.Console` que pour dessiner, et garder le parseur maison.
- **Pourquoi écartée :** garde les deux déclarations et l'absence d'aide, pour la moitié du gain
  et la même dépendance.

### Alternative 4 — Laisser Spectre convertir les valeurs

- **Description :** déclarer `int Count`, `char Separator`, `Casing Casing`, et laisser Spectre
  lier des types plutôt que des chaînes.
- **Pourquoi écartée :** il s'arrête à la première valeur invalide, ce que DEC0006 refuse
  explicitement pour une ligne de commande.

### Alternative 5 — Découper en sous-commandes Spectre

- **Description :** `slugger generate`, `slugger analyze`, `slugger register`, la forme que
  Spectre encourage.
- **Pourquoi écartée :** change la ligne de commande publique — `slugger --analyze ./t.json`
  deviendrait `slugger analyze ./t.json` — pour un confort d'implémentation.

## Conséquences

### Positives

- `--help` existe, tiré de la déclaration : il liste les vingt-quatre options, leurs gabarits de
  valeur et quatre exemples.
- `--version` existe, tirée de la version informationnelle de l'assembly.
- Une option inconnue est un refus, avec un code de sortie de 1, **et elle est rapportée avec les
  autres** : `--nope --casing SHOUT --count abc --token-chance 500` donne quatre plaintes en une
  exécution, comme le parseur écrit à la main. C'est ce qui décide de lire la ligne en mode
  permissif et de refuser ce que Spectre n'a pas su placer, plutôt que de le lui faire lever.
- Le rendu de Spectre est disponible pour la suite, sans nouvelle dépendance.
- L'application est construite par `SluggerApp.Build`, donc un test conduit la vraie ligne de
  commande sous les vraies règles : ce que Spectre lui-même refuse est couvert.
- Ajouter une option est une propriété, non plus un `case`, une entrée de liste et une ligne
  d'aide.

### Négatives

- **Ça coûte du code.** Mesuré sur `src/` : 632 lignes ajoutées, 340 retirées, soit 292 de plus.
  L'aide, la version et le refus strict n'existaient pas ; le parseur, lui, existait.
- **La suggestion « vouliez-vous dire » disparaît.** Le parseur maison nommait le flag connu le
  plus proche ; Spectre nomme l'option inconnue et laisse `--help` répondre.
- **Deux jetons arrivent encore seuls** : un mot seul, que Spectre lit comme un nom de commande,
  et une option laissée sans valeur. Dans les deux cas il lève avant d'avoir lu la suite. C'est
  la forme de l'exception que DEC0006 nomme déjà pour un JSON illisible, et c'est tout ce qui
  reste de la concession — une option inconnue, elle, est rapportée avec les autres.
- Le message de ces deux-là est celui de Spectre, pas celui de slugger : *« Option 'theme' is
  defined but no value has been provided. »* Il est juste et il nomme l'option ; il n'a pas la
  voix des autres refus.
- Chaque option est un `string?` et sa conversion est écrite à la main : le typage que Spectre
  offre n'est pas pris, et une option ajoutée demande sa ligne de conversion.
- Deux paquets de plus pour le CLI, et un enregistreur de types — `PortRegistrar` — là où la
  racine de composition suffisait.
- Trois comportements de Spectre sont contournés, chacun pinné par un test : `FlagValue<string>`
  pour distinguer un flag nu d'un `false` explicite, une largeur de 80 imposée quand la sortie est
  redirigée, et la culture épinglée sur l'invariant — Spectre traduit le cadre de l'aide
  (`USAGE`, `EXAMPLES`) selon la machine, et tout ce qu'il y a dedans est écrit en anglais, donc
  une machine réglée en français affichait `UTILISATION` au-dessus de descriptions anglaises
  (mesuré).
- Un jeton écrit après le `--` qui termine les options est rendu **deux fois** par Spectre, dans
  `Parsed` et dans `Raw` ; sans dédoublonnage, `slugger -- --nope` s'en plaint deux fois (mesuré).

### Risques

- Lire les arguments restants repose sur le fait que Spectre y range ce qu'il n'a pas su placer.
  Une version qui rangerait ailleurs rendrait une option inconnue à nouveau silencieuse, et c'est
  le test qui le dirait.
- `PortRegistrar` construit un type inconnu depuis son premier constructeur. Cela suffit pour une
  poignée de ports et ne suffirait pas pour un graphe.
- Spectre.Console 0.55.0 n'a pas atteint sa 1.0.

### Actions de suivi

- Porter les refus et le verdict du rapport de DEC0014 sur le rendu de Spectre, qui est la moitié
  encore non employée de cette décision. `--list-themes` en est exclu : un nom par ligne existe
  pour être redirigé, et un tableau se lirait mieux et se redirigerait moins bien.
- Mentionner `--help` dans le `README.md` et dans `docs/writing-a-theme.md`, qui décrivent des
  options sans dire où les lire toutes.
