# Thèmes du dépôt

Les thèmes que `slugger` **transporte sans les embarquer**. Trois thèmes sont compilés dans la
DLL — `slugger`, `heroku`, `docker` — et se demandent par `--theme`. Ceux d'ici sont des fichiers
ordinaires : on les analyse, on les enregistre, on les copie.

```bash
slugger --analyze  themes/mineralogy.json    # mesure, et écrit le rapport à côté
slugger --register themes/mineralogy.json    # valide puis installe dans --theme-dir
slugger --theme mineralogy --count 3         # une fois enregistré
```

## Ce que le dépôt garantit

`RepositoryThemeTests` charge **chaque `.json` de ce répertoire** avec les règles ordinaires —
pas d'`--allow-small-theme`, pas de passe-droit. Un thème refusé est donc une build rouge, pas une
découverte pour qui le télécharge.

Les planchers sont un cliquet : le jour où l'un d'eux monte, ces thèmes montent avec, ou ils
sortent. C'est voulu — un catalogue qui traîne des fichiers que l'outil refuserait ne vaut rien.

## Le rapport n'est pas dans git

`--analyze` écrit `<thème>-analysis.md` à côté du fichier, et ces rapports sont ignorés par git :
ils ne peuvent que suivre le thème et les règles, jamais les précéder. Régénère-les, ne les lis
pas depuis l'historique.

## En ajouter un

[`../docs/writing-a-theme.md`](../docs/writing-a-theme.md) dit tout : les catégories, `common`,
les participes, `except`, `incompatible`, les quatre règles de taille, et comment lire un refus.

**Commence par `--analyze`.** `--register` répond accepté ou refusé ; l'analyse répond *de
combien*, et c'est elle qui te dit quelle marge te reste avant le prochain cliquet.
