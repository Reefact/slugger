# ADR 0005 — Le format se décide au tirage, jamais au chargement

Statut : **accepté**, écart assumé avec la spécification d'origine · 2026-09-20

## Contexte

La normalisation d'une valeur compte quatre étapes : `trim`, collapse des espaces multiples,
minuscule, puis remplacement des espaces restants par le séparateur. La spécification d'origine
appliquait les quatre au chargement du JSON.

La quatrième ne peut pas y être. Le séparateur n'est connu qu'au moment de générer, et en
multi-thème `--mimic-style` il change d'un tirage à l'autre puisque chaque thème tiré applique
le sien. Le figer au chargement choisit un séparateur pour toute l'exécution.

## Décision

`WordNormalizer` fait les étapes 1 à 3 au chargement. `SlugFormatter` fait l'étape 4 au
formatage. Une valeur chargée garde ses espaces internes simples : `"John  Doe"` est en mémoire
`"john doe"`, et devient un slug au dernier moment.

## Conséquences

- Même résultat pour un thème unique, résultat **correct** pour plusieurs.
- **Un levier de format va dans `GenerationOptions`, jamais dans la valeur chargée.**
  `--word-sep` (`96a83e7`) en est la preuve : donner aux mots d'une valeur composée un
  séparateur distinct de celui des segments a touché le formateur et la chaîne d'options, et pas
  une ligne du chargement.
- La frontière adjectif/nom n'est pas récupérable depuis le texte final tant qu'un seul
  caractère tient les deux rôles. Ce n'est pas gênant pour le générateur, qui la connaît par
  construction et ne reparse jamais un slug — `--word-sep` existe pour qui veut la garder
  lisible malgré tout.
