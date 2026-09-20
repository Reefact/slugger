# DEC0015 | Tirage du mot unique de « either » dans les deux pools réunis, pondéré par leur taille

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-20 | Accepté | | |

## Contexte

`segmentMode: either` place **un seul** mot devant le nom, adjectif ou participe. C'est la forme
que reproduisent `docker` et `heroku`, qui le déclarent tous deux ; `slugger` ne déclare pas de
bloc `defaults` et vaut donc `both`.

Le choix entre les deux vocabulaires se faisait par un tirage à pile ou face : une valeur sous
deux, participe sur zéro. La taille des pools n'y entrait pas.

Mesuré sur les thèmes livrés, par nom :

| thème | adjectifs | participes | part des participes, à pile ou face | pondérée par les pools |
| --- | --- | --- | --- | --- |
| docker | 187 | 76 | 50 % | 28,9 % |
| heroku, nom le plus pauvre | 178 | 20 | 50 % | 10,1 % |
| heroku, moyenne sur ses 216 noms | | | 50 % | 14,9 % |

`heroku` déclare 20 participes dans `common`, et 103 de ses 216 noms n'atteignent que `common` :
la moitié du thème voyait donc un mot sur deux tiré d'un dixième de son vocabulaire.

DEC0003 impose un plancher de 100 adjectifs par nom et DEC0012 un plancher de 20 participes par
nom ; aucun des deux ne lit le `segmentMode`.

Le tirage passe par `IRandomSource`, dont les tests fournissent une version scriptée qui vérifie
chaque valeur contre la borne demandée : changer la borne du tirage de choix change ces scripts.

## Décision

Dans ce contexte, nous décidons que « either » tire son mot unique dans les deux pools réunis,
proportionnellement à leur taille.

## Justification

« Either » nomme un choix entre deux vocabulaires, pas un partage du résultat en deux moitiés :
réunis, les deux pools forment le vocabulaire des mots qui peuvent précéder le nom, et chacun
d'eux mérite la même chance quelle que soit la section qui l'a déclaré.

Le pile ou face donnait à 20 participes le même poids qu'à 178 adjectifs — mesuré sur `heroku`,
sur la moitié de ses noms. Un thème qui étoffe ses adjectifs sans toucher à ses participes voyait
donc la part de ces derniers ne pas bouger d'un pouce, ce que rien dans le mot « either » ne
laisse attendre.

La taille des pools est déjà connue au moment du tirage : la pondération ne demande aucune
donnée que le thème n'ait déjà déclarée, et aucune option de plus dans la chaîne de DEC0004.

## Alternatives envisagées

### Alternative 1 — Statu quo, le tirage à pile ou face

- **Description :** conserver un tirage sous deux, indépendant de la taille des pools.
- **Pourquoi écartée :** donne à 20 participes le même poids qu'à 178 adjectifs, mesuré sur la
  moitié des noms de `heroku`.

## Conséquences

### Positives

- Un mot a la même chance d'être tiré quelle que soit la section qui le déclare.
- La part des participes suit désormais ce que le thème déclare : 28,9 % pour `docker`, 14,9 %
  en moyenne pour `heroku` au lieu de 50 % pour les deux.
- Les deux pools réunis forment un pool unique, ce qui rend calculable un plancher sur leur
  somme (DEC0016).

### Négatives

- Un thème qui comptait sur le pile ou face pour faire ressortir une petite section de participes
  ne l'obtient plus.
- La borne du tirage de choix n'est plus deux mais la somme des deux pools, ce qui invalide les
  scripts de tirage écrits contre l'ancienne borne.

### Risques

- Un thème dont les participes sont volontairement rares les verra presque disparaître, sans que
  rien ne le signale : la rareté voulue et la pauvreté involontaire se ressemblent de l'extérieur.

### Actions de suivi

- Mettre à jour `docs/writing-a-theme.md`, qui décrivait le choix comme indépendant des pools.
