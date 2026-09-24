# Vocabulaire

Un même mot en désignait deux : « mot » nomme tantôt ce qu'on tire d'un thème, tantôt l'unité
littérale qui le compose. Cette page fixe un mot par niveau, et un seul.

> Un **slug** est un **nom** précédé de zéro à deux **épithètes** et suivi d'un **jeton**
> optionnel. Chacun de ces rôles est un **terme**, tiré du vocabulaire d'un **thème** ; un terme
> compte un ou plusieurs **mots**. Rendu, le slug se découpe en **segments**.

## Les mots

### slug

La chaîne produite par un tirage. C'est l'unité livrée : celle qui devient un nom de conteneur,
une branche git, un sous-domaine.

### terme

Le nom générique d'un **nom**, d'un **adjectif** ou d'un **participe** — ce qui est tiré du
vocabulaire d'un thème. C'est l'unité du tirage : on tire un terme entier, jamais une partie de
terme.

Un terme compte un ou plusieurs mots. `cobaltite` en compte un, `sharp faced` deux, et l'un comme
l'autre est **un seul terme**.

### mot

Un mot au sens littéral. Dans un fichier de thème, les mots d'un terme sont séparés par une
espace : `"value": "rock crystal"` déclare un terme de deux mots.

### nom

Le terme central. Toujours présent, toujours dernier.

### épithète

Un terme qui qualifie le nom : un adjectif ou un participe. Un slug en porte zéro, une ou deux.

Le mot est pris à la grammaire, où l'épithète est la **fonction** d'un terme attaché directement
à un nom. C'est ce que l'adjectif et le participe ont en commun ici, et c'est bien leur rôle qu'on
nomme — pas leur place. *Préfixe* aurait nommé la place et rien d'autre.

### jeton

Les caractères de fin, tirés au hasard et non du vocabulaire. Ni terme, ni mot : il ne vient pas
du thème.

### thème

Le vocabulaire dans lequel les termes sont tirés.

### segment

Ce qui, dans le slug **rendu**, se tient entre deux caractères séparateurs. Le premier segment est
suivi d'un séparateur, le dernier précédé d'un séparateur.

Le segment est une propriété du **rendu**, pas du slug : pour un même tirage, son nombre change
avec les options. On dit « ce slug rendu en camel a un segment », jamais « ce slug a un segment ».

## Les trois comptes, sur un exemple

L'adjectif `sharp faced` et le nom `cobaltite`, tirés de `mineralogy`. Le tirage ne change pas ;
seul le rendu change.

| rendu | segments | termes | mots |
| --- | --- | --- | --- |
| `sharpfaced-cobaltite` (`--word-sep ""`) | 2 | 2 | 3 |
| `sharp-faced-cobaltite` (par défaut) | 3 | 2 | 3 |
| `sharp_faced-cobaltite` (`--word-sep _`) | 3 | 2 | 3 |
| `sharpFacedCobaltite` (`--casing camel`) | 1 | 2 | 3 |

Les deux colonnes de droite sont des faits du tirage : elles ne bougent pas. Celle de gauche est
un fait du rendu, et elle prend trois valeurs pour un même slug.

C'est la raison d'être de la distinction : **terme et mot appartiennent au tirage, segment au
rendu.** Ce qui raisonne avant le tirage — un plafond, un plancher, une promesse de longueur —
raisonne donc en termes et en mots, jamais en segments.

Et c'est aussi pourquoi le segment ne permet pas de remonter aux termes : `sharp-faced-cobaltite`
s'écrit pareil qu'on ait tiré `sharp faced` + `cobaltite`, `sharp` + `faced cobaltite`, ou un
terme unique. Le rendu perd la structure ; `--word-sep` sert à la rendre lisible à nouveau.

## Ce qui n'appartient pas à ce vocabulaire

Le destinataire du slug a le sien, et il ne faut pas le lui emprunter :

| | son unité |
| --- | --- |
| DNS (RFC 1035) | **label** — un slug entier *est* un label, d'où les 63 octets promis par `docker` |
| URI (RFC 3986) | **segment** de chemin, délimité par `/` |
| ref git | **component**, délimité par `/` |

Le *segment* de RFC 3986 est bien l'ancêtre du nôtre, mais il se définit par son délimiteur et
rien d'autre. Le nôtre décrit un rendu, jamais un tirage.

## Où le code ne suit pas encore

Cette page est la référence ; le code la précède et ne l'a pas attendue. Trois noms disent
aujourd'hui « mot » pour un terme, ou « segment » pour un terme :

- `MaxLength.TwoWords` / `ThreeWords` comptent des **termes**
- `--max-segment-words` plafonne les **mots d'un terme**
- `SegmentMode` choisit quelles **épithètes** précèdent le nom

Les corriger touche une option publique et des clés JSON publiées : c'est une décision, donc un
DEC, et elle n'est pas prise ici.
