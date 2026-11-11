# DEC0031 | Une source de slugs préparée une fois, réduite et validée comme la commande le fait

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-11-11 | Accepté | Règle les issues #59 et #60 | |
| 2026-11-11 | Nuancé par DEC0034 | `ThemeDocument` est `internal` : seul un test construit encore un thème qu'aucun chargement n'a vérifié | |
| 2026-11-11 | Nuancé par DEC0036 | `SlugSource` devient `SlugGenerator`, seule façon de tirer ; l'ancien `SlugGenerator.Generate` est interne | |

## Contexte

Une limite de longueur (DEC0018) ou de mots par terme (DEC0023) réduit un thème aux mots qui
tiennent. La réduction coûte : mesurée en Release, environ 15 ms et 24 Mo pour le style `docker`
sous 63 caractères, 63 ms et 78 Mo pour `slugger` en ASCII sous 63 caractères.

La commande réduit chaque thème une fois par lot, dans `GenerateSlugsUseCase`, puis le valide
lorsque la réduction ou le mode tiré change ce qui est tiré (DEC0016, DEC0018) : un thème que les
options appauvrissent sous un plancher est refusé, avec les raisons. Tout cela était `internal`.

La bibliothèque n'offrait que `SlugGenerator.Generate(theme, options)`, qui refait la réduction à
chaque appel (#59) et ne valide rien (#60) : `docker` sous 12 caractères tirait sans un mot, là où
la commande le refuse pour 373 raisons. La documentation proposait un contournement — construire
soi-même un `ThemeResolver` et l'adresser à `ThemeValidator.Validate` — qui validait sans rien
garder pour tirer.

Un `ThemeResolver` remplissait ses caches de pools sans synchronisation, et ne pouvait donc servir
qu'un thread à la fois.

## Décision

Dans ce contexte, nous décidons de publier `SlugSource`, qui réduit un ou plusieurs thèmes une fois
pour les options données, les valide selon la règle de la commande et tire ensuite autant de slugs
qu'on lui en demande, et de faire passer la commande par elle.

## Justification

Garder la surface réduite entre deux tirages est la seule façon de ne payer la réduction qu'une
fois : mesuré, `Next` coûte de 0,5 µs à 56 µs selon les options, là où `Generate` refaisait 15 à
63 ms à chaque appel.

Valider au moment de préparer, et non à chaque tirage, met le refus là où l'appelant peut encore
réagir — au démarrage de son application — et au prix d'une seule validation.

Appliquer la règle de la commande — valider quand une limite réduit le thème ou qu'un autre mode le
tire, ne rien revalider sinon — donne à la bibliothèque et à la commande la même réponse pour les
mêmes options. Ne pas revalider un thème tiré tel qu'il a été chargé évite de payer deux fois la
même validation, et de refuser un thème chargé avec ses planchers levés par une source à qui on ne
l'a pas demandé.

Ce raccourci ne vaut que pour un thème qu'un chargement a effectivement accepté. Le constructeur
public de `ThemeDocument` ne vérifie rien, si bien qu'un thème construit en mémoire n'a jamais été
tenu aux règles : le chargement marque le document qu'il accepte, et une source valide tout thème qui
ne porte pas cette marque, même tiré tel quel. Un thème qui ne déclare aucun nom ne passe donc plus la préparation
pour ne lever `THEME_NO_NOUN` qu'au premier tirage.

Que la commande passe par `SlugSource` en fait une seule implémentation : les deux ne peuvent plus
diverger. Elle garde la formulation de son refus pour un thème tiré parmi d'autres, par un point
d'entrée interne qui lui laisse construire l'erreur.

Accepter plusieurs thèmes, chacun avec ses options, couvre ce que fait `--mimic-style` et remplace la
boucle que la documentation faisait écrire à la main. La source copie la liste reçue avant de s'en
servir : une liste que l'appelant modifie ensuite ne change rien à ce qu'elle tire. Le tirage pondéré par le nombre de noms est
celui de `WeightedThemePicker`, et un thème seul ne consomme aucun tirage pour être choisi : une
source tire exactement ce que `Generate` et la commande tiraient, graine pour graine.

Remplacer les caches du `ThemeResolver` par des `ConcurrentDictionary` rend une source partageable
entre threads sans précalculer tous les pools, ce qui aurait alourdi le démarrage d'un `--count 1`.
Deux threads qui demandent le même pool au même moment peuvent le calculer tous deux ; ils obtiennent
la même réponse.

## Alternatives envisagées

### Alternative 1 — Mettre en cache la surface réduite dans `SlugGenerator.Generate`

- **Description :** garder, par couple thème-options, le `ThemeResolver` réduit à l'intérieur du
  générateur.
- **Pourquoi écartée :** un état caché, sans borne ni durée de vie que l'appelant contrôle, et qui
  ne dit toujours rien du thème refusé avant le premier tirage.

### Alternative 2 — Valider dans `SlugSource.Next` ou dans `Generate`

- **Description :** refuser au tirage un thème que les options appauvrissent.
- **Pourquoi écartée :** le refus arrive au milieu du travail plutôt qu'au démarrage, et une
  validation coûte des dizaines de millisecondes qu'un tirage ne peut pas payer.

### Alternative 3 — Rendre `Generate(ThemeResolver, …)` public

- **Description :** publier la surcharge que la commande utilisait déjà.
- **Pourquoi écartée :** laisse à l'appelant la construction du `ThemeResolver`, le choix de
  valider ou non, et la pondération entre plusieurs thèmes — exactement ce que la commande faisait
  pour lui.

### Alternative 4 — Précalculer tous les pools à la préparation

- **Description :** rendre la source sûre entre threads en calculant tous les pools d'avance, puis
  en ne faisant plus que les lire.
- **Pourquoi écartée :** fait payer à chaque lancement de la commande le calcul des pools de tous
  les noms, même pour un seul slug ; des caches concurrents donnent la même sûreté sans ce coût.

## Conséquences

### Positives

- Une limite se paie une fois : chaque slug suivant coûte quelques microsecondes.
- Des options qui appauvrissent un thème sous ses planchers sont refusées à la préparation, avec les
  raisons que la commande donne.
- Une source préparée au démarrage sert tous les threads.
- La bibliothèque et la commande préparent un thème par le même code.

### Négatives

- `SlugGenerator.Generate` reste sans validation et réduit à chaque appel : deux façons de tirer, dont
  la documentation doit dire laquelle choisir.
- Un type public de plus, dans une bibliothèque en préversion dont l'API n'est pas encore stabilisée
  (#63).

### Risques

- Sous `Ascii` et une limite de longueur, `Next` filtre encore à chaque tirage les participes qui
  tiennent derrière l'adjectif tiré : environ 50 µs et 100 Ko par slug, bien moins que la réduction
  mais plus qu'un tirage sans limite.

### Actions de suivi

- Dire laquelle des deux façons de tirer choisir — fait dans `library.md`, à la place du contrôle au
  démarrage écrit à la main.
- Décider du sort de `SlugGenerator.Generate` avec le reste de l'API avant la version stable (#63).
