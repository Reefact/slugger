# DEC0008 | Réduction au chargement de tout caractère non alphanumérique en frontière de mot

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-20 | Accepté | Remplace la conservation des caractères spéciaux telle qu'elle était écrite | |

## Contexte

La normalisation conservait les caractères spéciaux tels qu'écrits, au même titre que les
accents. Mesuré sur un thème contenant des noms réels, avant décision :

| Valeur | Slug produit |
| --- | --- |
| `Jack O'Neil` | `vieux-jack-o'neil` |
| `Smith & Wesson` | `vieux-smith-&-wesson` |
| `St. Louis` | `vieux-st.-louis` |
| `Jean-Luc Picard`, avec `--sep _` | `vieux_jean-luc_picard` |

Un slug sert de nom de conteneur, de branche ou de segment d'URL. Le dernier cas mélange deux
joints différents dans un même slug : le tiret interne à la valeur subsiste alors que le
séparateur est `_`, ce qui est l'ambiguïté que `--word-sep` avait été ajouté pour lever.

Les trois thèmes livrés totalisent 1632 valeurs et ne contiennent aucun caractère non
alphanumérique : le défaut ne concerne donc que les thèmes personnalisés.

La conservation des accents est une décision distincte, testée
(`Preserves_accents_instead_of_transliterating_them`), et le classement Unicode range les
lettres accentuées parmi les lettres.

La normalisation réduit déjà les suites d'espaces à une seule et retire ceux des extrémités.

Le contrôle qui refuse un nom sans valeur porte sur la valeur brute, avant normalisation.

## Décision

Dans ce contexte, nous décidons de réduire au chargement tout caractère qui n'est ni une lettre
ni un chiffre à une frontière de mot.

## Justification

La réduction rend impossible qu'un caractère inutilisable dans un nom de conteneur, de branche
ou d'URL atteigne le slug, ce que la conservation ne garantissait pas.

Elle supprime du même coup le mélange de joints : le tiret d'une valeur devenant une frontière
comme une autre, il reçoit le séparateur courant au lieu de subsister.

Définir la frontière comme « ni lettre ni chiffre » au sens Unicode préserve la décision sur les
accents sans traitement particulier, puisqu'une lettre accentuée est une lettre.

Placer la réduction au chargement est cohérent avec DEC0005 : elle ne demande de connaître aucun
séparateur, donc elle appartient aux étapes qui précèdent le tirage. Elle laisse aussi les
étapes existantes faire le reste — une suite de frontières se réduit comme une suite d'espaces,
et une frontière d'extrémité se retire comme un espace d'extrémité.

Refuser une valeur qui ne contient ni lettre ni chiffre découle de la décision elle-même :
elle ne laisserait rien à tirer, alors que le contrôle existant la laisse passer.

## Alternatives envisagées

### Alternative 1 — Ne rien changer

- **Description :** conserver les caractères spéciaux tels qu'écrits, comme les accents, et
  laisser à l'auteur du thème le soin d'écrire des valeurs propres.
- **Pourquoi écartée :** laisse `&` et `.` casser un slug là où il est utilisé, et laisse deux
  joints différents cohabiter dans un même slug.

### Alternative 2 — Réduire au formatage plutôt qu'au chargement

- **Description :** conserver la valeur telle qu'écrite et remplacer les caractères spéciaux au
  moment de former le slug, par le séparateur courant.
- **Pourquoi écartée :** oblige le formateur à réduire les suites et à rogner les extrémités,
  alors que le chargement le fait déjà pour les espaces ; et la réduction ne dépend d'aucune
  option, donc rien ne justifie de l'y placer.

### Alternative 3 — Un levier dédié aux caractères spéciaux

- **Description :** une option supplémentaire, distincte de `--word-sep`, remplaçant les seuls
  caractères spéciaux — permettant par exemple d'élider une apostrophe (`jack-oneil`) tout en
  gardant les espaces séparés.
- **Pourquoi écartée :** `--word-sep` couvre déjà le besoin de surcharger `--sep` pour les
  joints internes, et un troisième séparateur serait ajouté le jour même que le deuxième. La
  distinction espace/apostrophe reste possible plus tard sans rien invalider.

## Conséquences

### Positives

- Un slug ne peut plus contenir qu'une lettre, un chiffre et les séparateurs choisis.
- Un seul joint par slug : le tiret d'une valeur suit désormais `--sep` comme le reste.
- L'auteur écrit ses valeurs comme on les écrit — `"Jack O'Neil"` plutôt que `"Jack ONeil"`.
- `--word-sep` gouverne uniformément tous les joints internes, sans option supplémentaire.
- Aucun thème livré n'est affecté, les 1632 valeurs étant déjà alphanumériques.

### Négatives

- Un thème personnalisé existant qui s'appuyait sur un caractère conservé voit ses slugs changer.
- L'apostrophe est traitée comme une frontière alors qu'elle marque une élision : `O'Neil`
  devient deux mots, ce qui donne `jack-o-neil` et non `jack-oneil`.
- Deux valeurs distinctes peuvent se réduire au même mot — `"St. Louis"` et `"St Louis"` — sans
  que le compte de noms distincts, calculé avant, s'en aperçoive.

### Risques

- Un auteur peut lire la réduction comme une translittération et s'attendre à ce que les accents
  tombent aussi, alors qu'ils sont conservés.
- Une écriture dont la ponctuation porte du sens — une unité, une formule — perdrait ce sens
  sans avertissement.

### Actions de suivi

- Énoncer la règle et ses exemples dans le guide d'auteur — fait dans
  [`../writing-a-theme.md`](../writing-a-theme.md).
- Refuser une valeur qui ne contient ni lettre ni chiffre, en la citant — fait dans le
  sérialiseur, pour les noms comme pour les adjectifs et participes.
- Décider plus tard s'il faut distinguer l'élision de la frontière de mot, si le besoin se
  manifeste à l'usage.
