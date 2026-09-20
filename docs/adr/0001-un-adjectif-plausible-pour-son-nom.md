# ADR 0001 — Un adjectif doit être plausible pour son nom

Statut : **accepté**, fondateur · 2026-09-20

## Contexte

Docker et Heroku tirent leur adjectif dans une liste plate : n'importe quel adjectif peut
accompagner n'importe quel nom. Cela produit `thundering-moon` et `weeping-server` aussi
volontiers que `focused-turing`. Pour un identifiant jetable, ça n'a pas d'importance.

C'est pourtant le premier des deux manques que `slugger` a été écrit pour combler : un slug est
lu par un humain, et un adjectif qui ne peut physiquement pas s'appliquer à son nom se remarque.

## Décision

Un adjectif n'est tirable pour un nom que s'ils partagent au moins une catégorie :

```
pool(noun) = { adj | adj.categories ∩ (noun.categories ∪ {common}) ≠ ∅ }
```

La catégorie est le mécanisme, pas une décoration.

## Conséquences

- **Les participes sont classés par capacité physique, jamais par domaine.** `mobile` (se
  déplace), `sonore` (fait du bruit), `lumineux`, `vivant`, `chaleur`, `eau`. `moon` est
  `[lumineux, mobile]` et n'a pas `sonore` : `thundering-moon` n'est pas un tirage possible,
  `waning-moon` l'est. C'est la même décision appliquée une seconde fois — un classement par
  thème (« astronomie », « météo ») n'aurait rien filtré.
- **Le tirage reste toujours à l'intérieur d'un seul thème.** Croiser les catégories de deux
  fichiers viderait le pool de son sens même quand elles portent le même nom. Une exécution
  multi-thème choisit donc d'abord un thème — tirage pondéré par effectif, équivalent à une
  liste plate concaténée mais en `O(D)` de mémoire plutôt qu'en `O(N)` — puis tire à
  l'intérieur.
- **La validation doit juger le pool résolu**, pas la taille des listes brutes, sinon elle
  accepte un thème dont un nom est coincé dans une petite catégorie → ADR 0003.
- **`common` reste accessible à tout nom**, ce qui autorise occasionnellement une métaphore plus
  lâche (`waning-cake`). Compromis assumé : `common` doit rester non vide pour tout nom → ADR 0002.
