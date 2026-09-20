# ADR 0006 — Un refus dit tout d'un coup, et il le dit une seule fois

Statut : **accepté** · 2026-09-20

## Contexte

Un auteur de thème qui corrige une plainte par exécution est un auteur qui lance `slugger` vingt
fois. Et un même fichier refusé par `--register` puis au runtime, avec deux formulations
différentes, donne l'impression de deux problèmes.

## Décision

**Tout d'un coup.** Le parsing collecte toutes les sections malformées avant d'abandonner, la
validation passe toutes les règles sur tous les noms et toutes les catégories, et les deux
étapes rapportent **ensemble** : quatre problèmes de forme et vingt échecs de règle reviennent
en vingt-quatre raisons, en une exécution.

Deux exceptions délibérées :

- **Un JSON malformé est terminal** — rien ne peut être lu d'un document qui n'a pas parsé.
- **Quand une section que les règles lisent est malformée, les règles sont sautées pour elle.**
  `"nouns" doit être un tableau` dit déjà tout ; `0 nom, au moins 100 requis` par-dessus serait
  du bruit, pas une seconde trouvaille.

**Une seule fois.** Une factory par situation, jamais une mise en forme au point d'appel.

## Conséquences

- Le chargement renvoie un `Outcome<Theme>` dont l'erreur porte chaque raison en `InnerErrors`.
  Les formes `LoadEmbedded`/`LoadFromFile`/`LoadFromJson` lèvent une exception qui transporte le
  même rapport complet, jamais la première plainte seule.
- **Une nouvelle plainte est une factory** dans `ThemeErrors` ou `CliErrors`, portant les faits
  dans son contexte — pas une phrase assemblée par l'appelant (`3cc7780`).
- Un seul renderer côté CLI transforme les faits en prose, donc il n'existe qu'une formulation,
  et `--register` et un chargement runtime se lisent identiquement.
- **La ligne de commande suit la même forme** : le parser la parcourt en entier avant de
  refuser, et rend un `PrimaryPortError` portant chaque plainte.
