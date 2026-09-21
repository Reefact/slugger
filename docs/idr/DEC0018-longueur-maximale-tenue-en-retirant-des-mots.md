# DEC0018 | Longueur maximale d'un slug tenue en retirant des mots avant le tirage

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-21 | Accepté | | |

## Contexte

**Le générateur de Docker ne teste aucune longueur.** Son code, lu à la source, tire un adjectif
et un patronyme, les joint par un souligné, et son unique `goto` sert à éviter `boring_wozniak`.
Ses deux listes — 108 adjectifs, 236 noms — plafonnent l'une et l'autre à **13 caractères**, ce
qui borne son pire slug à 28. La contrainte n'est pas dans le code, elle est dans le vocabulaire.

**Heroku valide à la création de l'app**, pas au tirage : `^[a-z][a-z0-9-]{1,28}[a-z0-9]$`, soit
3 à 30 caractères. Son générateur n'est pas public ; l'exemple de sa documentation,
`mystic-wind-83`, fait 14 caractères.

**Un label DNS est limité à 63 octets** (RFC 1035), un nom complet à 253. Mesuré : un label de 64
caractères est refusé par le résolveur avant tout accès réseau, et `HOST_NAME_MAX` vaut 64 sous
Linux.

**Le démon Docker accepte un nom de conteneur de n'importe quelle longueur** : sa validation porte
sur `^[a-zA-Z0-9][a-zA-Z0-9_.-]+$` et sur rien d'autre. Sa documentation précise par ailleurs que
les conteneurs d'un même réseau défini par l'utilisateur se résolvent **par leur nom via DNS**.
Un nom trop long est donc accepté, et ce qui casse casse plus tard, ailleurs.

Mesuré sur les thèmes livrés, sous leurs propres `defaults` : `docker` produit au pire 28
caractères, `heroku` 27.

DEC0013 a écarté le re-tirage, au motif qu'il est non borné sur un pool d'un mot et rend le nombre
de tirages variable, ce dont la suite scriptée dépend. DEC0017 a créé la couture
`partPool(noun, adjective)`, où l'adjectif déjà tiré retire des participes avant que le participe
ne soit tiré. DEC0016 fait suivre les planchers au mode de segment déclaré.

Les `defaults` d'un thème sont éteints dès que plusieurs thèmes sont en portée, et
`--allow-small-theme` lève les règles de taille.

## Décision

Dans ce contexte, nous décidons qu'une longueur maximale de slug est obtenue en retirant des mots
avant le tirage, déclarée par le thème comme une promesse et par l'exécution comme un plafond.

## Justification

Retirer avant de tirer est le geste que le thème fait déjà trois fois — une catégorie restreint le
pool, `except` le réduit par nom, une paire le réduit par adjectif. La longueur le réduit par
budget, sur la couture que DEC0017 a posée et sans en ouvrir une nouvelle. Le nombre de tirages
reste fixe, le tirage reste uniforme sur ce qui reste, et aucun mot n'est coupé.

La promesse appartient au thème parce que **c'est son vocabulaire qui la tient** : Docker et
Heroku n'ont pas de garde-fou, ils ont des listes courtes. Écrire la promesse dans le fichier est
ce qui transforme le mot de quinze caractères ajouté un jour en build rouge devant son auteur,
plutôt qu'en slug refusé par un registre six mois plus tard.

Le plafond appartient à l'exécution parce qu'elle seule connaît sa destination : le même thème
sert à nommer un conteneur, un Service Kubernetes ou une branche Git, dont les limites n'ont rien
à voir. Et comme la surface réduite est un thème comme un autre, elle est validée par les règles
ordinaires : un plafond qui affame les noms est refusé, en nommant lequel, au lieu de produire en
silence des slugs plus courts que prévu.

La promesse au niveau racine plutôt que dans `defaults` suit du fait que ceux-ci s'éteignent en
multi-thème : une contrainte de sûreté qui disparaît quand on ajoute un thème échoue ouvert, ce
que DEC0011 et DEC0017 refusent déjà chacun de leur côté.

## Alternatives envisagées

### Alternative 1 — Statu quo, ne rien ajouter

- **Description :** laisser l'auteur d'un thème veiller lui-même à la longueur, comme Docker et
  Heroku le font.
- **Pourquoi écartée :** le démon Docker accepte un nom trop long, et la casse arrive ensuite à la
  résolution DNS — une panne différée qui ressemble à un problème de réseau.

### Alternative 2 — Tronquer le slug à la longueur demandée

- **Description :** couper la chaîne produite au nombre de caractères voulu.
- **Pourquoi écartée :** mutile un mot, et deux slugs distincts peuvent devenir le même sans que
  rien ne le détecte.

### Alternative 3 — Re-tirer jusqu'à obtenir un slug qui tienne

- **Description :** mesurer après le tirage et recommencer quand ça dépasse.
- **Pourquoi écartée :** DEC0013 a déjà écarté ce raisonnement, non borné et à nombre de tirages
  variable.

### Alternative 4 — Laisser tomber un segment quand le slug dépasse

- **Description :** écrire l'adjectif seul, comme pour un nom qui n'atteint aucun participe.
- **Pourquoi écartée :** un thème `both` produirait des slugs à deux segments à un taux que rien
  ne montre, et d'autant plus souvent que la limite est basse.

### Alternative 5 — Ne déclarer la limite que sur la ligne de commande

- **Description :** pas de `maxLength` dans le thème, seulement `--max-length`.
- **Pourquoi écartée :** c'est le vocabulaire du thème qui tient la limite, donc c'est au thème de
  la déclarer — sans quoi rien ne signale le mot trop long au moment où il est ajouté.

### Alternative 6 — Déclarer `maxLength` dans `defaults`

- **Description :** la ranger avec les autres préférences du thème.
- **Pourquoi écartée :** les `defaults` sont éteints dès qu'un deuxième thème est en portée, et la
  promesse disparaîtrait alors en silence.

## Conséquences

### Positives

- `docker` déclare 63 et `heroku` 30 ; mesuré, ils tiennent avec 35 et 3 caractères de marge.
- Un mot trop long fait échouer la build, devant son auteur.
- La surface réduite par `--max-length` est un thème comme un autre : validée par les règles
  ordinaires, mesurée par `--analyze`, et ses planchers levés par `--allow-small-theme`.
- Aucun slug tronqué, aucun mot coupé, aucun segment perdu en silence.
- `--analyze --max-length N` répond à « est-ce que ce thème tient sous N » sans rien générer.
- Le refus distingue ses deux causes : une paire qui affame un nom et un plafond qui l'affame ne
  se corrigent pas de la même façon.

### Négatives

- **`heroku` n'a que trois caractères de marge** : un mot de plus de quatorze fera échouer la
  build. C'est le prix d'une promesse serrée, et c'est voulu.
- Une quatrième raison pour qu'un mot disparaisse d'un tirage, après les catégories, `except` et
  les paires.
- La promesse est mesurée sous les `defaults` du thème : un `--token-length` plus grand la dépasse
  sans que rien ne le signale, et c'est alors `--max-length` qu'il faut passer.
- Réduire la surface coûte un formatage par mot et par nom, payé une fois par exécution — mesuré
  sans effet perceptible, mais réel.

### Risques

- Un thème qui grandit peut franchir sa propre promesse loin de la ligne éditée, et le refus
  nomme le slug plutôt que le mot ajouté.
- Un plafond bas réduit la surface sans que l'utilisateur s'en rende compte tant qu'il reste
  au-dessus des planchers : la variété baisse avant que quoi que ce soit ne refuse.

### Actions de suivi

- Documenter `maxLength` et `--max-length` dans `docs/writing-a-theme.md`.
- Étoffer le vocabulaire de `heroku` demanderait de revoir sa promesse à 30 ; les deux vont
  ensemble.
