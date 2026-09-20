# DEC0012 | Instauration d'un plancher de participes par nom, dans les thèmes qui en déclarent

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-20 | Accepté | Réalise et corrige un risque consigné par DEC0003, qui reste en vigueur | |
| 2026-09-20 | Remplacé par DEC0016 | Le plancher de vingt participes ne vaut plus que pour le mode « both » | |

## Contexte

DEC0003 impose trois règles calculées sur le pool résolu, dont un plancher de 100 adjectifs par
nom. Aucune ne porte sur les participes, et DEC0003 consigne lui-même le risque : « un thème peut
passer les règles avec un pool de participes très pauvre ».

Le `segmentMode` par défaut est `both` : un participe figure donc dans le slug au même titre
qu'un adjectif.

La troisième règle de DEC0003 compte les combinaisons **par catégorie**, en sommant sur ses noms.
Une catégorie de 200 noms disposant chacun de 178 adjectifs et 20 participes totalise largement
plus de 40 000 : la somme masque une pauvreté qui est par nom. C'est exactement le défaut que le
plancher par nom corrigeait déjà pour les adjectifs.

Mesuré sur les thèmes livrés, participes atteints par nom :

| thème | noms | minimum | médiane | maximum |
| --- | --- | --- | --- | --- |
| docker | 236 | 76 | 76 | 76 |
| heroku | 216 | **20** | 29 | 69 |
| slugger | 231 | 40 | 45 | 60 |

`heroku` déclare 20 participes dans `common`, et 103 de ses 216 noms n'atteignent que `common`.
Son minimum de 20 concerne donc la moitié du thème, pas un cas isolé.

Mesuré également : un plancher à 100 refuserait les trois thèmes, un plancher à 30 en refuserait
deux, un plancher à 50 en refuserait deux.

Un thème peut ne déclarer aucun participe ; cette possibilité est acquise et le format à deux
segments reste le comportement d'origine.

## Décision

Dans ce contexte, nous décidons qu'un thème déclarant des participes doit en offrir au moins
vingt à chacun de ses noms.

## Justification

Le participe étant tiré dans le slug par défaut, la garantie qui vaut pour l'adjectif vaut pour
lui : un nom qui n'atteint qu'une poignée de participes produit des slugs dont un tiers se répète.

La règle par catégorie ne couvre pas ce cas, sa somme masquant une pauvreté par nom — le même
constat qui avait justifié le plancher par nom sur les adjectifs.

La conditionner à la présence de participes préserve la validité d'un thème qui n'en déclare
aucun, sans exception ni cas particulier dans la règle.

Le seuil de vingt est celui que les trois thèmes livrés franchissent sans modification, ce qui
sépare l'instauration de la règle du travail de fond sur les thèmes.

## Alternatives envisagées

### Alternative 1 — Ne rien ajouter

- **Description :** laisser la règle par catégorie couvrir le sujet.
- **Pourquoi écartée :** sa somme masque la pauvreté par nom, qui est précisément ce qui se voit
  à l'usage.

### Alternative 2 — Un plancher symétrique de celui des adjectifs, à 100

- **Description :** exiger autant de participes que d'adjectifs par nom.
- **Pourquoi écartée :** refuserait les trois thèmes livrés.

### Alternative 3 — Un plancher à 30 ou 50

- **Description :** un seuil offrant de la marge plutôt que collé au minimum actuel.
- **Pourquoi écartée :** refuse deux thèmes livrés, et mêle l'instauration de la règle à un
  travail d'étoffement qui n'était pas le sujet.

### Alternative 4 — Une recommandation dans le guide plutôt qu'une règle bloquante

- **Description :** conseiller un nombre de participes sans refuser le thème.
- **Pourquoi écartée :** un conseil ne protège pas un catalogue, alors que c'est là que la
  pauvreté d'un thème se remarque.

## Conséquences

### Positives

- Un thème accepté garantit un troisième segment varié, nom par nom et non en moyenne.
- Le refus nomme le nom fautif et son compte, comme les autres règles de taille.
- Un thème sans participes reste valide, sans exception dans la règle.
- Les trois thèmes livrés passent sans être modifiés.

### Négatives

- **Le seuil est exactement le minimum actuel de `heroku`, donc sans marge** : toute réduction de
  ses participes `common` fera refuser le thème.
- Un nom qui n'atteignait aucun participe se dégradait silencieusement vers l'adjectif seul ; il
  fait désormais refuser le thème.
- Une fixture de test qui se disait valide avec quatre participes ne l'était plus, et a dû être
  relevée.

### Risques

- Le seuil, choisi pour ne refuser aucun thème livré, peut s'avérer trop bas pour garantir la
  variété qu'il vise ; vingt mots au milieu d'un slug restent peu.
- Une exclusion (DEC0011) peut faire passer un nom sous le plancher, ce qui est voulu, mais
  déplace la cause du refus loin de la ligne qui l'a provoqué.

### Actions de suivi

- Étoffer les participes de `heroku` et de `slugger`, puis remonter le seuil — c'est un cliquet,
  au même titre que celui des avertissements et celui de la mutation.
- Ne jamais l'abaisser pour rendre vert un chargement rouge.
