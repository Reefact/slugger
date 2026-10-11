# DEC0027 | Une clé inconnue dans un thème est signalée, jamais refusée

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-10-11 | Accepté | Issue #29 | |

## Contexte

`JsonThemeSerializer` lit les clés qu'il connaît avec `TryGetProperty` et ne regarde jamais les
autres. Une faute de frappe retire donc en silence ce que la clé voulait dire : `"excpet"` laisse
un nom sans ses exclusions, `"maxlength"` un thème sans sa promesse de longueur, `"participels"` un
thème sans aucun participe. Pour reproduire le problème, il suffit d'un thème valide portant
`"excpet"` sur un nom et `"maxlength": { "twoWords": 5 }` à la racine : il tire des slugs et sort
en 0, sans le moindre avertissement, alors qu'aucun slug ne tient en cinq caractères.

Le guide refuse pourtant un mot d'`except` qui n'existe nulle part, au motif qu'*« une liste de
sécurité qui laisse passer une faute de frappe est pire que pas de liste »* (DEC0011) ; la même
faute, lorsqu'elle porte sur le nom de la clé plutôt que sur sa valeur, passait inaperçue.

DEC0021 s'appuie à l'inverse sur ce silence : *« `JsonThemeSerializer` ignore déjà silencieusement
toute clé racine qu'il ne connaît pas : ajouter un bloc que d'anciens fichiers ne déclarent pas ne
casse rien qui existe. »* C'est ce qui a permis d'ajouter `meta`, puis `createdAt` et `publishedAt`
(DEC0022), sans qu'une version antérieure refuse un fichier écrit pour une version postérieure.

`XdgConfigStore` traite déjà le même problème pour le fichier de configuration : une clé inconnue y
est une remarque, avec la clé probablement visée quand elle est évidente, et le fichier est lu
quand même.

## Décision

Dans ce contexte, nous décidons qu'une clé inconnue dans un thème, à chaque niveau dont les clés
sont fixes, est signalée par une remarque de `--register` et de `--analyze` qui nomme la clé,
l'endroit où elle se trouve et la clé connue la plus proche, et que le thème se charge comme avant.

## Justification

**Signaler sans refuser garde ce que DEC0021 protège.** Un fichier écrit pour une version qui
ajoute une clé doit rester lisible par une version antérieure ; un refus le casserait, une
remarque non. L'auteur, lui, apprend sa faute au moment où il enregistre ou analyse son thème,
c'est-à-dire au moment où il l'écrit.

**C'est la politique déjà en place pour la configuration.** Deux fichiers JSON que l'utilisateur
édite à la main, deux réponses différentes à la même faute de frappe, seraient une incohérence de
plus à expliquer.

**Proposer la clé visée vaut mieux que la nommer seule.** La plupart des fautes réelles sont à une
ou deux lettres de la bonne clé (`excpet`, `participels`, `casng`, `categorie`) ou n'en diffèrent
que par la casse (`maxlength`) ; `KeySpelling` propose la clé connue à au plus deux lettres
ajoutées, retirées, changées ou interverties, une par tranche de trois lettres de la clé, et rien
quand aucune n'est assez proche, pour ne jamais suggérer un mot que personne ne visait.

**Regroupée par clé.** Une faute recopiée sur cinq cents noms est une seule faute : la remarque
nomme les trois premiers emplacements et compte les autres.

**Seuls les niveaux aux clés fixes sont regardés** : la racine, un nom, `defaults`, `maxLength`,
`meta`. Sous `adjectives`, `participles` et `incompatible`, les clés sont des noms de catégorie ou
d'adjectif, que l'auteur choisit librement.

## Alternatives envisagées

### Alternative 1 — Refuser une clé inconnue

- **Description :** une erreur de forme par clé inconnue, comme pour une valeur du mauvais type.
- **Pourquoi écartée :** elle casse la compatibilité sur laquelle DEC0021 et DEC0022 se sont
  appuyés : le prochain bloc ajouté au format ferait refuser par toutes les versions antérieures
  les fichiers qui l'utilisent.

### Alternative 2 — Refuser seulement une clé presque identique à une clé connue

- **Description :** une clé à une lettre de `except` est presque sûrement une faute ; la refuser,
  et laisser passer les autres.
- **Pourquoi écartée :** une clé future peut être proche d'une clé existante ; la règle refuserait
  alors un fichier valide pour la version qui l'a écrit, et la frontière entre « proche » et
  « nouveau » serait un réglage, pas un fait.

## Conséquences

### Positives

- Une faute de frappe dans une clé ne retire plus rien en silence au moment où l'auteur enregistre
  ou analyse son thème.
- Le thème se charge exactement comme avant : aucun thème accepté aujourd'hui ne devient refusé.

### Négatives

- Un tirage (`--theme`) n'affiche pas les remarques, comme pour les autres remarques ; un thème
  copié à la main dans un dossier sans passer par `--register` garde sa faute en silence.
- La liste des clés connues est écrite une seconde fois dans `JsonThemeSerializer`, à côté des
  lectures ; une clé ajoutée au format doit l'être aux deux endroits, faute de quoi elle sera
  signalée à tort.

### Risques

- Une suggestion fausse sur une clé courte : le nombre de lettres tolérées croît avec la longueur
  de la clé connue pour le limiter.

### Actions de suivi

- Remplacer *« Accepted without a word »* par *« Accepted with a warning »* pour les clés inconnues
  dans `docs/theme-reference.md`.
- `RepositoryThemeTests` vérifie qu'aucun thème de `themes/` ne porte de clé inconnue.
