# ADR 0004 — Une seule chaîne de priorité, la même pour toute option

Statut : **accepté**, après débat et revert · 2026-09-20

## Contexte

Une option peut être dite à quatre endroits : sur la ligne de commande, dans les `defaults` du
thème tiré, dans la config sauvegardée par `--init`, ou nulle part. L'ordre devait être tranché
une fois pour toutes les options, sinon chaque nouvelle option rouvrait la question.

## Décision

```
argument CLI  >  defaults du thème mimiqué  >  config --init  >  défaut programme
```

Aucun traitement spécial par option.

## Conséquences

- **Les `defaults` d'un thème passent au-dessus de la config sauvegardée**, et c'est la partie
  portante : demander `--theme docker` demande son format autant que son vocabulaire. Pour
  imposer le sien malgré les thèmes, on sauvegarde le flag qui les retire de la chaîne —
  `slugger --init --casing camel --mimic-style false`.
- **Donc aucun thème livré ne déclare de `defaults` identiques aux valeurs par défaut du
  programme.** Un bloc qui ne dit rien de neuf ne ferait que neutraliser la config de
  l'utilisateur. C'est pourquoi `slugger.json` n'a pas de bloc `defaults` du tout.
- **Ce qui arme les `defaults` d'un thème est le nombre de thèmes actifs, pas le flag.** Un seul
  `--theme heroku` reproduit déjà son style sans qu'aucun flag soit nécessaire.
- **`--mimic-style` est lui-même une option sur la chaîne**, donc `--init` peut le sauvegarder.
- **Une nouvelle option prend sa place sans cas particulier.** `--word-sep` (`96a83e7`) a coûté
  une ligne par couche.
- **Ça s'est joué.** La chaîne a été inversée (`7888e35`) puis revertée (`4f0f9be`) ;
  `8955fa5` avait corrigé une option sauvegardée qui outrepassait le style du thème tiré.
  `OptionPrecedenceTests` la pinne là où un utilisateur la rencontre — sur le terminal — et non
  sur le résolveur.
