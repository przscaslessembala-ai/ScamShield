# Guide pas à pas pour Prisca — ScamShield.Core et intégration

Ce guide vous mène de dépôt vide à **Core v0.1.0**, que Jean, Joseph, Andy et Gabrielle peuvent utiliser dès le premier jour. Chaque tâche liste les fichiers à ajouter et les commits à faire (un commit = une idée).

> **À lire d'abord.** Le code de ce dossier n'a pas pu être compilé là où je l'ai écrit (pas de SDK .NET). Votre toute première vérification est donc la tâche 0. Si `dotnet build` affiche une erreur, copiez-la-moi et je corrige.

## Vue d'ensemble

| # | Tâche | Commits | Durée |
|---|-------|---------|-------|
| 0 | Préparer l'environnement et vérifier le code | 0 | 30 min |
| 1 | Socle du dépôt (réglages communs) | 3 | 30 min |
| 2 | Solution et projet Core | 2 | 20 min |
| 3 | Modèles du noyau | 5 | 1 h |
| 4 | Interfaces (contrats) | 2 | 30 min |
| 5 | Implémentations factices (Fakes) | 2 | 45 min |
| 6 | Tests unitaires | 4 | 1 h |
| 7 | Intégration continue (CI) | 1 | 20 min |
| 8 | Documentation et modèle de PR | 3 | 30 min |
| 9 | Protéger `principal` et livrer Core v0.1.0 | 0 (PR + tag) | 30 min |
| 10 | Annoncer aux autres et suivre les changements | 0 | continu |

Total : **22 commits**, à faire sur votre branche noyau.

## Règles pour tous vos commits

Format : `type(portée): résumé à l'impératif`, 72 caractères maximum, en français.
Types : `feat`, `fix`, `test`, `docs`, `build`, `ci`, `chore`, `refactor`.
Avant chaque commit : `dotnet build` doit réussir (dès que la solution existe).

---

## Tâche 0 — Préparer et vérifier

```powershell
dotnet --version        # doit afficher 10.x
git --version
git clone https://github.com/ORGANISATION/DEPOT.git
cd DEPOT
git switch <votre-branche-noyau>
```

- Si votre SDK n'est pas la version 10, soit vous l'installez, soit vous changez `<TargetFramework>` dans `Directory.Build.props` (par exemple `net8.0`).
- Copiez ce dossier dans le dépôt, **sans** valider encore. Vous allez l'ajouter fichier par fichier, tâche par tâche.
- Exécutez une fois `dotnet build` et `dotnet test` à la fin de la tâche 6. Si quelque chose échoue, envoyez-moi le message d'erreur.

---

## Tâche 1 — Socle du dépôt

But : que tout le monde compile et formate le code de la même façon.

```powershell
git add .gitignore .gitattributes
git commit -m "chore: ajoute .gitignore et .gitattributes"

git add .editorconfig
git commit -m "chore: ajoute les conventions de code .editorconfig"

git add Directory.Build.props
git commit -m "build: centralise les réglages communs (net10, nullable, avertissements en erreurs)"
```

Pourquoi : `Directory.Build.props` s'applique automatiquement aux projets de Jean, Joseph, Andy et Gabrielle. Les conventions se retrouvent ainsi dans tout le dépôt sans que personne n'ait à y penser.

---

## Tâche 2 — Solution et projet Core

```powershell
dotnet new sln -n ScamShield
dotnet sln add src/ScamShield.Core/ScamShield.Core.csproj

git add ScamShield.sln* src/ScamShield.Core/ScamShield.Core.csproj
git commit -m "build: crée la solution ScamShield et le projet Core"
```

> Remarque : .NET 10 crée par défaut un fichier `ScamShield.slnx` ; les anciennes versions créent `ScamShield.sln`. Les deux conviennent, ajoutez celui qui existe.

```powershell
git add README.md
git commit -m "docs: ajoute le README initial"
```

---

## Tâche 3 — Modèles du noyau

Dossier : `src/ScamShield.Core/Models/`. L'ordre compte : chaque commit ne dépend que de ce qui existe déjà.

**Commit 1 — la demande d'analyse**
```powershell
git add src/ScamShield.Core/Models/ScanTrigger.cs src/ScamShield.Core/Models/ScanRequest.cs
git commit -m "feat(core): ajoute ScanRequest et ScanTrigger"
```

**Commit 2 — les mesures et raisons de risque**
```powershell
git add src/ScamShield.Core/Models/FeatureNames.cs src/ScamShield.Core/Models/FeatureVector.cs `
        src/ScamShield.Core/Models/RiskReason.cs src/ScamShield.Core/Models/RiskReasonCodes.cs `
        src/ScamShield.Core/Models/FileAnalysis.cs
git commit -m "feat(core): ajoute FileAnalysis, FeatureVector et RiskReason"
```

**Commit 3 — verdict et seuils**
```powershell
git add src/ScamShield.Core/Models/Verdict.cs src/ScamShield.Core/Models/VerdictPolicy.cs `
        src/ScamShield.Core/Models/RiskAssessment.cs
git commit -m "feat(core): ajoute Verdict, VerdictPolicy et RiskAssessment"
```

**Commit 4 — le résultat final**
```powershell
git add src/ScamShield.Core/Models/ScanResult.cs
git commit -m "feat(core): ajoute ScanResult avec règle menace connue = dangereux"
```

**Commit 5 — base de menaces et quarantaine**
```powershell
git add src/ScamShield.Core/Models/ThreatRecord.cs src/ScamShield.Core/Models/QuarantineEntry.cs
git commit -m "feat(core): ajoute ThreatRecord et QuarantineEntry"
```

À retenir :
- Les modèles sont des `record` **immuables** : un objet ne change plus après sa création, donc pas de bug de modification partagée entre modules.
- `RiskAssessment` et `VerdictPolicy` refusent les valeurs invalides dès leur création (principe : échouer tôt et clairement).
- `FeatureNames` est la liste que **Andy et Gabrielle doivent valider ensemble en semaine 1**. Si vous la modifiez, incrémentez `FeatureVector.CurrentSchemaVersion`.

---

## Tâche 4 — Interfaces (les contrats)

Dossier : `src/ScamShield.Core/Abstractions/`.

```powershell
git add src/ScamShield.Core/Abstractions/IFileAnalyzer.cs src/ScamShield.Core/Abstractions/IRiskScorer.cs
git commit -m "feat(core): ajoute les interfaces IFileAnalyzer et IRiskScorer"

git add src/ScamShield.Core/Abstractions/IQuarantineService.cs src/ScamShield.Core/Abstractions/IThreatRepository.cs
git commit -m "feat(core): ajoute les interfaces IQuarantineService et IThreatRepository"
```

| Interface | Implémentée par |
|-----------|-----------------|
| `IFileAnalyzer` | Engine (Andy) |
| `IThreatRepository` | Engine (Andy) |
| `IRiskScorer` | AI (Gabrielle), et scorer de secours à base de règles |
| `IQuarantineService` | Agent (Jean) |

---

## Tâche 5 — Implémentations factices (Fakes)

But : chacun peut développer et tester **sans attendre les autres**. Joseph affiche de fausses alertes, Jean teste sa file d'attente avec un faux analyseur.

```powershell
dotnet sln add src/ScamShield.Fakes/ScamShield.Fakes.csproj

git add ScamShield.sln* src/ScamShield.Fakes/ScamShield.Fakes.csproj `
        src/ScamShield.Fakes/FakeFileAnalyzer.cs src/ScamShield.Fakes/FakeRiskScorer.cs
git commit -m "feat(fakes): ajoute FakeFileAnalyzer et FakeRiskScorer"

git add src/ScamShield.Fakes/InMemoryThreatRepository.cs src/ScamShield.Fakes/InMemoryQuarantineService.cs
git commit -m "feat(fakes): ajoute les dépôts en mémoire (menaces et quarantaine)"
```

Règle du faux analyseur : un chemin contenant « eicar » est traité comme une menace connue. Cela permet de simuler la démo sans aucun vrai fichier dangereux.

---

## Tâche 6 — Tests unitaires

```powershell
dotnet sln add tests/ScamShield.Core.Tests/ScamShield.Core.Tests.csproj

git add ScamShield.sln* tests/ScamShield.Core.Tests/ScamShield.Core.Tests.csproj
git commit -m "build: ajoute le projet de tests ScamShield.Core.Tests"

git add tests/ScamShield.Core.Tests/VerdictPolicyTests.cs
git commit -m "test(core): vérifie les seuils de VerdictPolicy"

git add tests/ScamShield.Core.Tests/RiskAssessmentTests.cs tests/ScamShield.Core.Tests/ScanResultTests.cs
git commit -m "test(core): vérifie RiskAssessment et la règle menace connue de ScanResult"

git add tests/ScamShield.Core.Tests/FakePipelineTests.cs
git commit -m "test(core): ajoute le test du trajet complet avec les Fakes"
```

**Vérification obligatoire** (ne continuez pas sans elle) :

```powershell
dotnet build
dotnet test
```

Tous les tests doivent passer. `FakePipelineTests` est le modèle des futurs tests d'intégration : même trajet, avec les vrais modules à la place des Fakes.

---

## Tâche 7 — Intégration continue

```powershell
git add .github/workflows/ci.yml
git commit -m "ci: compile et teste à chaque push avec GitHub Actions"
git push
```

Ouvrez l'onglet **Actions** de GitHub : le workflow doit devenir vert. Ensuite, dans le README, remplacez `ORGANISATION/DEPOT` par le vrai nom du dépôt pour que le badge s'affiche.

---

## Tâche 8 — Documentation et modèle de PR

```powershell
git add CONTRIBUTING.md
git commit -m "docs: ajoute le guide de contribution (branches, commits, PR)"

git add .github/pull_request_template.md
git commit -m "docs: ajoute le modèle de pull request"

git add README.md
git commit -m "docs: renseigne l'adresse du dépôt et le badge CI"
```

---

## Tâche 9 — Protéger `principal` et livrer Core v0.1.0

1. Sur GitHub : **Settings → Branches → Add rule** pour `principal` :
   - exiger une pull request avant fusion ;
   - exiger une relecture ;
   - exiger que la CI soit verte (« Compilation et tests »).
2. Ouvrez une PR de votre branche noyau vers `principal`, avec le modèle rempli. Demandez à Jean de relire.
3. Après fusion, posez une étiquette de version :

```powershell
git switch principal
git pull
git tag -a v0.1.0-core -m "Contrats Core v0.1.0"
git push origin v0.1.0-core
```

---

## Tâche 10 — Annoncer aux autres

Envoyez ce message au groupe :

> **Core v0.1.0 est sur `principal`.** Mettez vos branches à jour (`git merge principal`). Les interfaces sont dans `ScamShield.Core/Abstractions`, des Fakes sont dans `ScamShield.Fakes` pour travailler sans attendre. **Andy et Gabrielle :** validez ensemble `FeatureNames` cette semaine, avant d'écrire le moteur ou d'entraîner le modèle. Tout changement de Core passe par une PR que je relis.

Ensuite, votre rôle devient continu :
- relire les PR qui touchent Core (c'est vous qui gardez les contrats stables) ;
- répondre aux demandes de changement de contrat (ajouter plutôt que modifier) ;
- semaine 4 : brancher les vrais modules (voir la suite ci-dessous).

---

## Suite : tâches d'intégration (semaines 2 à 4)

À faire une fois que les autres livrent leurs modules. Chaque ligne est un groupe de commits.

| Tâche | Exemple de commit |
|-------|-------------------|
| Créer `ScamShield.Integration.Tests` | `build: ajoute le projet de tests d'intégration` |
| Brancher l'injection de dépendances dans l'Agent | `feat(agent): configure l'injection de dépendances` |
| Test EICAR de bout en bout (vrais modules) | `test(integration): vérifie le trajet EICAR complet` |
| Brancher le scorer de secours à base de règles | `feat(core): ajoute RuleBasedRiskScorer derrière IRiskScorer` |
| Versionner un changement de contrat | `feat(core)!: ajoute X au vecteur de caractéristiques (schéma v2)` |
| Phase 2 : contrats du Doctor | `feat(core): ajoute DiagnosticRequest, DiagnosticResult et ISystemDiagnoser` |

Un point d'exclamation après la portée (`feat(core)!:`) signale un changement qui casse les autres modules : prévenez l'équipe avant de fusionner.

## Pourquoi cette structure est professionnelle

- **Contrats d'abord** : les cinq membres travaillent en parallèle dès le premier jour.
- **Immuabilité et validation** : les modèles ne peuvent pas être créés dans un état invalide.
- **Fakes** : aucun module n'est bloqué par un autre.
- **Tests + CI** : chaque push est vérifié automatiquement.
- **Historique propre** : un commit = une idée, lisible par n'importe quel relecteur.
