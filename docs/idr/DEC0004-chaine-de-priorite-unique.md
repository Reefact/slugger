# DEC0004 | Adoption d'une chaîne de priorité unique pour toutes les options

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-19 | Accepté | Chaîne inverse implémentée en `7888e35` puis revertée en `4f0f9be` | |

## Contexte

Une option de formatage peut être dite à quatre endroits : sur la ligne de commande, dans les
`defaults` du thème tiré, dans la config persistée par `--init`, ou nulle part. L'outil en
compte vingt aujourd'hui.

L'ordre a été effectivement contesté pendant la construction. La chaîne plaçant la config
sauvegardée au-dessus des `defaults` du thème a été implémentée (`7888e35`) puis revertée
(`4f0f9be`) ; un commit antérieur (`8955fa5`) avait déjà corrigé une option sauvegardée qui
outrepassait le style du thème tiré.

Le test de mutation a mesuré l'absence de garantie sur ce point : 45 des 48 mutants
d'`OptionResolver` survivaient, tous sur la chaîne de `??` qui décide du vainqueur. Les tests
écrits depuis sont passés de 20 à 39 mutants tués.

Aucun thème livré ne déclare de `defaults` identiques aux valeurs par défaut du programme ;
`slugger.json` n'a pas de bloc `defaults` du tout.

`--mimic-style` est un flag à trois états : absent, forcé, ou explicitement désactivé.

## Décision

Dans ce contexte, nous décidons de résoudre toute option par la chaîne unique suivante :
argument de la ligne de commande, puis `defaults` du thème mimiqué, puis config `--init`, puis
valeur par défaut du programme.

## Justification

Placer les `defaults` du thème au-dessus de la config sauvegardée est ce qui fait que
`--theme docker` continue de reproduire Docker : demander un thème demande son format autant
que son vocabulaire, et l'inverse a été essayé puis annulé pour cette raison.

L'utilisateur qui veut imposer son format malgré les thèmes n'est pas bloqué pour autant, parce
que le flag qui les retire de la chaîne est une option comme les autres et se sauvegarde donc
lui aussi : `slugger --init --casing camel --mimic-style false`.

Cette place des `defaults` est aussi ce qui explique qu'aucun thème livré ne recopie les valeurs
par défaut du programme — un bloc qui ne dit rien de neuf ne ferait que neutraliser la config de
l'utilisateur.

L'absence de traitement particulier par option est ce qui rend la chaîne tenable dans le temps :
chaque nouvelle option prend sa place sans rouvrir la question, comme l'a fait `--word-sep`
(`96a83e7`) au prix d'une ligne par couche.

## Alternatives envisagées

### Alternative 1 — Config `--init` au-dessus des `defaults` du thème

- **Description :** une préférence sauvegardée par l'utilisateur l'emporte sur le style du thème
  tiré. Implémentée dans `7888e35`.
- **Pourquoi écartée :** `--theme docker` cesse de reproduire Docker dès qu'une config existe,
  ce qui vide `--mimic-style` de son sens. Revertée dans `4f0f9be`.

### Alternative 2 — Priorité décidée au cas par cas selon l'option

- **Description :** chaque option place elle-même les couches dans l'ordre qui lui convient.
- **Pourquoi écartée :** chaque nouvelle option rouvre la question, et rien ne permet plus à
  l'utilisateur de prévoir ce qui l'emporte.

## Conséquences

### Positives

- Un seul ordre à connaître pour vingt options, et pour toutes celles à venir.
- Ajouter une option est mécanique : une ligne par couche, sans arbitrage.
- `--theme <nom>` seul reproduit le style du thème, sans qu'aucun flag soit nécessaire.
- `--mimic-style` étant sur la chaîne, l'utilisateur peut sauvegarder le fait de ne pas mimer.

### Négatives

- Une option dite par un thème ne peut pas être « dé-dite » depuis la ligne de commande autrement
  qu'en donnant une autre valeur : la chaîne n'a pas de niveau « revenir au défaut ».
- Un auteur de thème doit savoir qu'un bloc `defaults` redondant neutralise la config de ses
  utilisateurs.

### Risques

- Un utilisateur dont la config semble ignorée peut conclure à un bug plutôt qu'à la place des
  `defaults` du thème, tant qu'il ne connaît pas `--mimic-style false`.

### Actions de suivi

- Pinner la chaîne là où un utilisateur la rencontre, sur le terminal, et non sur le résolveur —
  fait dans `OptionPrecedenceTests`.
- Énoncer la place des `defaults` dans le guide d'auteur de thème, puisqu'elle le concerne —
  fait dans [`../writing-a-theme.md`](../writing-a-theme.md).
