# DEC0014 | Mesure d'un thème par une commande dédiée, rendue par le CLI

## Statut

| Date | Statut | Note | Compte rendu lié |
| --- | --- | --- | --- |
| 2026-09-20 | Accepté | | |

## Contexte

La validation répond par oui ou par non. `--register` dit accepté ou refusé, jamais de combien.

Mesuré sur un thème personnalisé de 490 noms, au cours de deux allers-retours avec son auteur :
il atteignait 102 adjectifs par nom pour un plancher de 100, et 8 participes pour un plancher
de 20. Le premier chiffre — deux mots de marge — n'apparaissait nulle part ; il a fallu le
calculer à la main pour le découvrir.

D'autres faits sur un thème n'ont aucune règle qui les regarde. Le tirage d'un nom indexe la
liste (`nouns[random.Next(nouns.Count)]`) alors que la règle de taille compte les valeurs
distinctes après normalisation : un nom écrit deux fois est donc tiré deux fois plus souvent
sans que rien ne le signale. La validation vérifie qu'un nom ne cite pas de catégorie inconnue,
jamais l'inverse : une catégorie que personne ne porte n'est jamais atteinte, en silence.

`ThemeParseResult` rend le thème construit à partir de ce qui a parsé, même avec des erreurs de
forme, et `ThemeValidator.Validate` accepte de lever les règles de taille.

DEC0006 établit qu'une erreur porte des faits et que la phrase est écrite par le CLI.

DEC0007 veut qu'un type nouveau soit `internal` par défaut.

## Décision

Dans ce contexte, nous décidons d'ajouter une commande `--analyze` qui mesure un fichier de
thème et écrit le rapport à côté de lui.

## Justification

Mesurer répond à la question que la validation ne pose pas : un thème deux mots au-dessus d'un
plancher se lit comme sain et casse à l'édition suivante.

Charger en levant les règles de taille est ce qui rend le rapport utile au moment où on le veut :
un thème s'analyse précisément quand il ne passe pas, et les refus reviennent tout de même,
du validateur exécuté contre les vrais planchers.

Composer le validateur plutôt que le réimplémenter garde une seule réponse à « ce thème est-il
bon », qui ne peut donc pas diverger d'une seconde.

La librairie ne rend que des nombres et le CLI écrit le markdown, ce qui est exactement le
partage de DEC0006 : les faits d'un côté, la prose de l'autre. Un autre rendu — HTML, JSON, une
page web — ne demande alors que de réécrire la partie qui écrit.

Écrire à côté du fichier mesuré plutôt que dans le dossier des thèmes découle de ce que le
fichier analysé n'est pas forcément enregistré, et ne le sera peut-être jamais.

Remplacer un rapport existant sans rien demander se distingue du refus d'écraser un thème :
un rapport est calculé, pas écrit à la main, donc il n'y a rien de l'auteur à perdre.

## Alternatives envisagées

### Alternative 1 — Enrichir le rapport de `--register`

- **Description :** afficher les marges et les mesures à l'enregistrement, sans commande nouvelle.
- **Pourquoi écartée :** un rapport complet à chaque enregistrement est du bruit pour qui n'a
  rien à corriger, et n'aide pas avant que le thème soit prêt à être enregistré.

### Alternative 2 — Écrire le rapport sur la sortie standard

- **Description :** afficher plutôt qu'écrire un fichier, et laisser l'appelant rediriger.
- **Pourquoi écartée :** le rapport accompagne le thème — les deux fichiers voyagent ensemble
  vers un catalogue —, et un rapport long défile hors de l'écran.

### Alternative 3 — Rendre le markdown dans la librairie

- **Description :** que `Slugger` produise directement le texte du rapport.
- **Pourquoi écartée :** contredit DEC0006, et enferme tout autre consommateur dans un format
  qu'il devrait analyser pour en extraire des nombres.

## Conséquences

### Positives

- Un auteur voit ses marges, et pas seulement un verdict.
- Le rapport fonctionne sur un thème refusé, donc au moment où il sert.
- Trois faits qu'aucune règle ne regardait sont désormais rapportés : un nom en double, une
  catégorie que personne ne porte, l'écart d'exposition entre les mots.
- Un rendu différent ne demande que de réécrire ce qui écrit, pas ce qui compte.
- Le canal servira aux contrôles de catalogue suivants sans mécanisme nouveau.

### Négatives

- La commande écrit un fichier sans le demander, là où toutes les autres n'écrivent que dans le
  dossier des thèmes ou sur la sortie.
- `IThemeStore` gagne une écriture à chemin arbitraire, symétrique de la lecture qui existait.
- Le modèle de rapport est un type de plus, `internal` pour l'instant : un catalogue écrit en
  .NET ne peut pas encore l'utiliser sans qu'on le publie.

### Risques

- Le rapport mesure sans juger, et son silence peut se lire comme une approbation : rien n'y
  dira qu'un adjectif est un nom mal employé ni qu'un participe prête une intention à une
  pierre. Ces défauts-là se lisent, ils ne se calculent pas.
- La longueur du plus long slug est une borne supérieure et non un tirage, donc elle annonce
  parfois plus long que ce qui sortira jamais.

### Actions de suivi

- Dire à l'auteur de commencer par là — fait dans [`../writing-a-theme.md`](../writing-a-theme.md).
- Publier le modèle le jour où un catalogue écrit en .NET en a besoin, pas avant.
