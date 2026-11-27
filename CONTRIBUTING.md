# Guide de contribution

## Branches

- `principal` est protégée : aucune modification directe, tout passe par une pull request (PR).
- Chaque module a sa branche longue (`fonctionnalité/agent`, `fonctionnalité/moteur`...). Pour un travail précis, créez une branche courte à partir de la branche de votre module, par exemple `feature/agent-quarantine`.
- Une branche courte vit quelques jours au maximum, puis elle est fusionnée et supprimée.
- Mettez régulièrement votre branche à jour depuis `principal`.

## Messages de commit

Format : `type(portée): résumé à l'impératif`, en français, 72 caractères au maximum.

| Type | Usage |
| --- | --- |
| `feat` | nouvelle fonctionnalité |
| `fix` | correction de bug |
| `test` | ajout ou modification de tests |
| `docs` | documentation |
| `refactor` | réécriture sans changement de comportement |
| `build` | solution, projets, dépendances |
| `ci` | GitHub Actions |
| `chore` | tâche de maintenance |

Exemples : `feat(agent): surveille le dossier Téléchargements`, `fix(engine): corrige le calcul de l'entropie`.

Un commit = une idée. Le code compile et les tests passent à chaque commit.

## Pull requests

1. La CI doit être verte.
2. Remplissez le modèle de PR.
3. Au moins une relecture est requise ; Prisca relit en priorité tout ce qui touche à `ScamShield.Core`.
4. Fusion par « Rebase and merge » ou « Create a merge commit » pour conserver l'historique des commits.

## Contrats de `ScamShield.Core`

Les modèles et interfaces de `ScamShield.Core` sont utilisés par tous les modules.

- Toute modification passe par une PR **annoncée à l'équipe**.
- Préférez ajouter plutôt que modifier ou supprimer.
- Un changement du vecteur de caractéristiques incrémente `FeatureVector.CurrentSchemaVersion`.

## Critères de qualité (« terminé » signifie)

- Le code compile sans avertissement.
- Les nouveaux comportements sont couverts par des tests.
- Les messages destinés à l'utilisateur sont en français simple.
- Aucun vrai malware, aucun secret, aucune donnée personnelle dans le dépôt.
