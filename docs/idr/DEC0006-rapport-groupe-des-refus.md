# DEC0006 | Rapport groupé de toutes les raisons d'un refus

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-19 | Accepté | `ce4d5a8` pour le rapport groupé, `3cc7780` pour les faits portés par l'erreur | |

## Contexte

Un fichier de thème peut cumuler des erreurs de forme et des échecs de règle. Mesuré sur un
fichier de démonstration : neuf raisons, allant d'une valeur vide à deux catégories sous le
seuil de combinaisons.

Le même fichier peut être refusé à deux moments distincts et par deux chemins de code
différents : à l'enregistrement par `--register`, et au chargement à l'usage.

Le moteur porte `FirstClassErrors`, dont le type `Outcome<T>` transporte une erreur pouvant
elle-même contenir des erreurs internes.

Un document JSON qui ne parse pas ne livre rien dont les règles pourraient juger. Et lorsqu'une
section que les règles lisent est malformée, les règles appliquées à cette section ne peuvent
produire que des constats dérivés de l'erreur de forme.

## Décision

Dans ce contexte, nous décidons de rapporter en une seule exécution toutes les raisons pour
lesquelles un thème ou une ligne de commande est refusé.

## Justification

Un refus par exécution oblige l'auteur à relancer autant de fois qu'il y a de problèmes, alors
que la validation les connaît déjà tous : le coût est supporté par l'auteur sans rien apporter.

`Outcome<T>` et ses erreurs internes portent nativement cette forme, donc le choix ne demande
aucune structure propre au projet.

Les deux exceptions ne contredisent pas la règle mais la servent : un JSON illisible ne permet
aucun constat supplémentaire, et empiler sur `"nouns" doit être un tableau` un
`0 nom, au moins 100 requis` ajoute du bruit, pas une seconde trouvaille.

Écrire la formulation une seule fois, là où l'erreur est levée plutôt qu'au point d'appel, est
ce qui garantit qu'un même fichier refusé par `--register` et au runtime se lit à l'identique —
sans quoi l'utilisateur croit à deux problèmes distincts.

## Alternatives envisagées

### Alternative 1 — Échec à la première erreur

- **Description :** interrompre le chargement dès le premier constat et le signaler seul.
- **Pourquoi écartée :** impose à l'auteur autant d'exécutions que son fichier a de problèmes,
  alors qu'ils sont déjà connus.

### Alternative 2 — Mise en forme du message au point d'appel

- **Description :** laisser l'erreur porter un code et composer la phrase là où elle est
  affichée.
- **Pourquoi écartée :** `--register` et le chargement runtime divergeraient, et la formulation
  serait à maintenir en deux endroits.

## Conséquences

### Positives

- Une exécution dit à l'auteur tout ce que son fichier réclame.
- `--register` et un chargement à l'usage produisent le même texte, parce qu'il n'en existe
  qu'un.
- Les erreurs portant leurs faits, un consommateur de la bibliothèque peut les exploiter sans
  analyser des phrases.
- La ligne de commande suit la même forme : le parser la parcourt entièrement avant de refuser.

### Négatives

- La validation ne peut pas s'arrêter tôt : elle passe toutes les règles même quand le fichier
  est manifestement à refaire.
- Chaque nouvelle plainte demande une factory dédiée plutôt qu'une chaîne écrite sur place.

### Risques

- Un rapport long — vingt-quatre raisons sur un fichier très abîmé — peut noyer la plus
  importante d'entre elles.

### Actions de suivi

- Pinner la prose des refus, et pas seulement leurs codes — fait ; les tests écrits depuis ont
  fait passer les mutants non détectés de 36 à 18 sur `ThemeErrors` et de 22 à 9 sur
  `CliErrors`.
