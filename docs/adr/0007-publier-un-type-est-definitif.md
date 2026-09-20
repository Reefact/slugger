# ADR 0007 — Publier un type est définitif, donc on publie le moins possible

Statut : **accepté** · 2026-09-20

## Contexte

Le découpage en couches voulait quatre assemblys. Mais entre assemblys, chaque couche doit être
`public` pour que la suivante l'utilise : quatre assemblys auraient publié toute la construction
interne du moteur, et un paquet publie pour toujours.

## Décision

**Un seul assembly, trois namespaces.** `Slugger.Domain` et la façade `Themes` sont publics ;
`Slugger.Application` et `Slugger.Infrastructure` sont `internal` — ils disent comment le moteur
est construit, pas ce qu'il offre. Le CLI et les tests les atteignent par `InternalsVisibleTo`.

Dix-huit types publics.

## Conséquences

- **Un nouveau type est `internal` par défaut.** Publier plus tard est facile ; dé-publier est
  un breaking change.
- **Le layering devient une convention, donc un test remplace le compilateur.**
  `NamespaceDependencyTests` lit les signatures de types et échoue si `Domain` nomme quoi que ce
  soit de `Application` ou `Infrastructure`. Il ne lit pas les corps de méthodes : une
  vérification bon marché qui reste vraie vaut mieux qu'une exhaustive que personne ne maintient.
- **La liste de dépendances du moteur est une whitelist courte** — `FirstClassErrors` seul — car
  une dépendance prise par `Slugger` atteint tous ceux qui le référencent, comme une ligne du
  nuspec publié. `TextCopy` est au CLI seul : un presse-papiers n'a rien à faire dans un moteur
  de slugs, et `ClipboardDependencyTests` échoue s'il fuit vers l'intérieur.
- **La séparation `Slugger` / `Slugger.Cli` est gardée** parce que c'est une vraie frontière de
  packaging, et que c'est elle qui décide où une dépendance a le droit de s'asseoir.
