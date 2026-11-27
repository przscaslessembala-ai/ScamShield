# ScamShield

Antivirus intelligent pour Windows : il surveille le PC en temps réel, repère les fichiers dangereux, **explique pourquoi** ils le sont grâce à l'IA, puis réagit automatiquement.

![CI](https://github.com/ORGANISATION/DEPOT/actions/workflows/ci.yml/badge.svg)

## Le trajet d'un fichier

1. **Détection** : l'Agent repère un nouveau fichier (téléchargement, clé USB).
2. **Analyse** : l'Engine calcule le hash, examine la structure et applique les règles.
3. **Score** : l'IA donne un score de risque de 0 à 1 avec les raisons en français.
4. **Décision** : sain, suspect (avertissement) ou dangereux (quarantaine).
5. **Alerte** : l'interface informe l'utilisateur.

## Modules

| Projet | Rôle | Responsable |
| --- | --- | --- |
| `ScamShield.Core` | Modèles et interfaces partagés, intégration | Prisca |
| `ScamShield.Agent` | Service Windows, surveillance, quarantaine | Jean |
| `ScamShield.UI` | Interface WPF | Joseph |
| `ScamShield.Engine` | Hash, analyse des exécutables, règles | Andy |
| `ScamShield.AI` | Score de risque et explications | Gabrielle |
| `ScamShield.Fakes` | Implémentations factices pour développer en parallèle | Prisca |

## Démarrage rapide

Prérequis : [Git](https://git-scm.com) et le SDK [.NET 10](https://dotnet.microsoft.com/download).

```powershell
git clone https://github.com/ORGANISATION/DEPOT.git
cd DEPOT
dotnet restore
dotnet build
dotnet test
```

## Structure du dépôt

```text
src/
  ScamShield.Core/    contrats partagés (modèles, interfaces)
  ScamShield.Fakes/   implémentations factices
tests/
  ScamShield.Core.Tests/
.github/
  workflows/ci.yml    compilation et tests à chaque push
```

## Contribuer

Lisez [CONTRIBUTING.md](CONTRIBUTING.md) : branches, messages de commit, pull requests et règles sur les contrats de `ScamShield.Core`.

## Sécurité des tests

N'utilisez **jamais de vrais malwares**. Le fichier de test [EICAR](https://www.eicar.org) et les jeux de données de caractéristiques suffisent pour tout tester et pour la démo.

## Limites connues

Projet pédagogique : il ne remplace pas un antivirus du commerce. Le modèle IA peut se tromper, et ses limites sont documentées dans sa fiche.
