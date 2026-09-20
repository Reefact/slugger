# DEC0013 | Tolérance d'un mot déclaré dans les deux sections, signalée et absorbée

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-20 | Accepté | | |

## Contexte

`adjectives` et `participles` sont deux sections indépendantes, et rien n'interdit qu'un mot
figure dans les deux. Plusieurs le méritent : `charming`, `boring` et `dancing` sont des
adjectifs autant que des participes présents.

`segmentMode` vaut `both` par défaut, et le générateur tire alors dans les deux pools sans les
comparer. Un mot présent des deux côtés peut donc sortir deux fois : `boring-boring-turing`.

Mesuré sur les thèmes livrés et sur un thème personnalisé de 887 valeurs : aucun mot n'est
déclaré dans les deux sections. Le cas est possible, pas constaté.

Le générateur pratique déjà une dégradation silencieuse : un nom qui n'atteint aucun participe
produit un slug à deux segments plutôt qu'une erreur.

`--register` applique la même validation qu'un chargement runtime, et dispose déjà d'un
avertissement qui n'empêche rien : un thème dont le nom masque un thème embarqué est accepté,
mais jamais en silence.

Le tirage passe par `IRandomSource`, et les tests s'appuient sur une source scriptée qui échoue
si la séquence consommée ne correspond pas à celle qui est écrite.

## Décision

Dans ce contexte, nous décidons d'accepter qu'un mot soit déclaré dans les deux sections, de le
signaler à l'enregistrement et de n'en écrire qu'une occurrence lorsqu'un tirage le rencontre
deux fois.

## Justification

Refuser serait faux sur le fond : la double appartenance est grammaticalement correcte, et un
thème ne doit pas avoir à mentir sur sa langue pour être accepté.

Refuser serait aussi disproportionné au vu de la mesure : aucun thème existant n'est concerné,
donc la règle ne protégerait personne aujourd'hui tout en interdisant un usage légitime demain.

Écrire le mot une fois réutilise la dégradation déjà pratiquée pour un nom sans participe, donc
n'introduit aucune notion nouvelle dans le générateur.

Effectuer les deux tirages même lorsqu'ils se rejoignent garde la consommation de la source
aléatoire identique dans les deux cas : un nouveau tirage jusqu'à obtenir un mot différent
serait sans borne sur un pool d'un seul élément, et rendrait imprévisible une séquence scriptée.

Signaler à l'enregistrement plutôt qu'au chargement place l'information au moment où un second
regard est bon marché, et où l'auteur s'apprête à publier — tandis qu'un avertissement à chaque
exécution pèserait sur qui n'a rien à corriger.

L'avertissement de masquage établit déjà ce canal, donc le signalement n'invente pas un
mécanisme pour lui seul.

## Alternatives envisagées

### Alternative 1 — Refuser au chargement un thème déclarant un mot des deux côtés

- **Description :** une règle de validation, appliquée partout comme les autres.
- **Pourquoi écartée :** refuse une construction grammaticalement juste, et casserait des thèmes
  personnalisés qui fonctionnent aujourd'hui chez leurs auteurs.

### Alternative 2 — Retirer un mot jusqu'à en obtenir un différent

- **Description :** relancer le tirage du participe tant qu'il vaut l'adjectif.
- **Pourquoi écartée :** sans borne sur un pool d'un seul élément, et modifie la consommation de
  la source aléatoire selon le résultat, ce qui rend une séquence scriptée imprévisible.

### Alternative 3 — Une ligne dans le guide, sans code

- **Description :** demander à l'auteur de ne pas déclarer le même mot deux fois.
- **Pourquoi écartée :** laisse le slug doublé sortir chez qui n'a pas lu le guide, et un
  catalogue est précisément l'endroit où cela se verrait.

## Conséquences

### Positives

- Un thème peut nommer ses mots comme la langue les nomme.
- Un slug ne répète jamais un mot, quel que soit le thème.
- L'auteur est averti au moment utile, sans que rien ne soit refusé.
- Le canal de remarques ouvert ici accueillera les contrôles de catalogue suivants sans
  mécanisme nouveau.

### Négatives

- Un tirage qui se rejoint produit deux segments là où le thème en promettait trois, donc un
  slug plus court et un espace combinatoire plus petit que le calcul ne le laisse croire.
- Les règles de taille comptent ces mots dans les deux pools, donc elles surestiment légèrement
  ce que le thème produit réellement.
- Une remarque est une phrase et non des faits portés par une erreur, à la différence de ce que
  DEC0006 impose aux refus ; c'est le choix déjà fait pour l'avertissement de masquage.

### Risques

- L'avertissement n'étant émis qu'à l'enregistrement, un thème installé autrement — copié à la
  main dans le dossier — ne le verra jamais.

### Actions de suivi

- Dire la règle et son effet là où l'auteur la rencontre — fait dans
  [`../writing-a-theme.md`](../writing-a-theme.md).
- Vérifier que les thèmes livrés n'ont rien à se reprocher — fait dans
  `EmbeddedThemeCatalogTests`, qui assure qu'aucun ne produit de remarque.
