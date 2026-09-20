# DEC0007 | Réduction de la surface publique à un seul assembly

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-19 | Accepté | Fusion des quatre couches par `c34af8c`, passage en `internal` par `a458af1` | |

## Contexte

Le découpage initial de la solution prévoyait un assembly par couche, soit quatre.

Entre deux assemblys, une couche doit être `public` pour que la suivante l'utilise. Quatre
assemblys publient donc l'intégralité de la construction interne du moteur, y compris ce qui ne
constitue pas une offre pour un consommateur.

Le moteur est publié sur NuGet : dé-publier un type est un changement cassant pour tous ceux qui
le référencent, et une dépendance prise par `Slugger` apparaît comme une ligne du nuspec chez
chacun d'eux.

`--clipboard` a besoin de `TextCopy`, faute d'accès au presse-papiers cross-platform dans la
BCL. Le presse-papiers n'a de sens que dans un contexte de ligne de commande.

Le moteur compte aujourd'hui dix-huit types publics.

## Décision

Dans ce contexte, nous décidons de livrer le moteur en un seul assembly dont seuls
`Slugger.Domain` et la façade `Themes` sont publics.

## Justification

Fusionner les assemblys est ce qui rend `internal` possible : c'est la frontière d'assembly,
et elle seule, qui forçait à publier les couches de support.

L'asymétrie entre publier et dé-publier commande de publier peu : publier un type plus tard ne
casse personne, le retirer casse tout le monde. Le même raisonnement tient pour les dépendances,
d'où une liste réduite à `FirstClassErrors` pour le moteur et `TextCopy` laissé au seul CLI.

Le coût de la fusion est faible parce que le découpage en couches subsiste en namespaces : ce
qui disparaît est le graphe de paquets, pas l'organisation du code.

La séparation entre `Slugger` et `Slugger.Cli` est conservée parce qu'elle, contrairement aux
trois autres, correspond à une vraie frontière de publication et décide donc où une dépendance
a le droit de s'installer.

## Alternatives envisagées

### Alternative 1 — Quatre assemblys, un par couche

- **Description :** conserver le découpage initial, chaque couche livrée séparément.
- **Pourquoi écartée :** oblige à publier toute la construction interne du moteur, pour un
  graphe de paquets dont un projet de cette taille n'a pas l'usage.

### Alternative 2 — Un seul assembly, tout public

- **Description :** fusionner les couches sans marquer les couches de support `internal`.
- **Pourquoi écartée :** publie autant que quatre assemblys tout en perdant l'isolation vérifiée
  par le compilateur, c'est-à-dire le pire des deux.

## Conséquences

### Positives

- Dix-huit types publics au lieu de la construction entière, et un nouveau type est `internal`
  par défaut.
- Une dépendance du moteur est un acte conscient, puisqu'elle atteint tous les consommateurs.
- Un consommateur prend une seule référence pour tout ce qui lui est promis.

### Négatives

- L'isolation entre couches n'est plus garantie par le compilateur, et doit l'être autrement.
- Les tests doivent passer par `InternalsVisibleTo` pour atteindre les couches de support.

### Risques

- Un consommateur ayant besoin d'un type resté `internal` n'a aucun recours dans le paquet
  publié.
- Une vérification par test ne lit pas les corps de méthodes : une dépendance de couche
  introduite à l'intérieur d'une méthode passerait inaperçue.

### Actions de suivi

- Remplacer le compilateur par un test sur les signatures de types — fait dans
  `NamespaceDependencyTests`, qui échoue si `Domain` nomme quoi que ce soit d'`Application` ou
  d'`Infrastructure`.
- Garder `TextCopy` hors du moteur par un test dédié — fait dans `ClipboardDependencyTests`.
- Vérifier que les catalogues d'analyseurs n'atteignent aucun consommateur — fait dans
  `PackageWeightTests` et par la cible MSBuild `AnalyzersStayPrivate`.
