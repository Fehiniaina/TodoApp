# Plan : Créer une tâche (use case `CreateTask`)

## Objectif
Permettre à un utilisateur **connecté** de créer une tâche avec :
- un **titre** (obligatoire),
- une **description** (facultative),
- une **priorité** (obligatoire : `Low` | `Medium` | `High`).

Type de use case : **command** (modifie les données) → handler + validator + événement `TaskChangeNotifier`, conformément au skill `create-use-case`.

---

## État actuel du code (vérifié)

Déjà en place :
- `Common/` : `Result<T>` (avec conversions implicites), `Error` + `ErrorType`, `IHandler` (marqueur), `HandlerRegistration` (scan auto des handlers).
- `Features/Shared/TaskChangeNotifier.cs` : singleton minimal, expose seulement `event Action? Changed` + `NotifyChanged()`.
- `Features/DependencyInjection.cs` : `AddTaskFeatures` enregistre le notifier (singleton), scanne handlers et validators (`includeInternalTypes: true`).
- `Data/ApplicationDbContext.cs` : `IdentityDbContext<ApplicationUser>` — enregistré via `AddDbContextFactory<ApplicationDbContext>` avec `UseNpgsql` + `UseSnakeCaseNamingConvention` dans `Program.cs`.
- Projets de tests : `UnitTests` (bUnit, Moq — référence `TodoApp`), `IntegrationTests` (Testcontainers.PostgreSql, Respawn — **sans** `ProjectReference` vers `TodoApp`), `ArchitectureTests` (NetArchTest).

Manquant (à créer) :
- Aucune entité `Domain/` (dossier vide), aucun `Features/Tasks/`, aucun `TaskErrors`, aucune config EF, aucune migration « tasks ».
- Aucun harnais d'intégration (fixture Testcontainers + Respawn).

### Écarts documentation ↔ réalité (décisions prises)
1. **`AppDbContext` vs `ApplicationDbContext`** — CLAUDE.md / skill parlent de `AppDbContext` ; le contexte réel est `ApplicationDbContext`. → **On utilise `ApplicationDbContext`** (réalité), pour les `DbSet` et l'injection `IDbContextFactory<ApplicationDbContext>`.
2. **Emplacement des configs EF** — la règle `infrastructure.md` impose `Infrastructure/Configurations/[Entity]Configuration.cs`, mais le `DbContext` vit dans `Data/`. → **On garde le contexte dans `Data/`** et on place la config dans `Infrastructure/Configurations/`, appliquée via `ApplyConfigurationsFromAssembly`.
3. **`TaskChangeNotifier` minimal** — pas d'event args par type d'action. → **On appelle `NotifyChanged()`** après `SaveChangesAsync`. (Enrichissement possible plus tard, hors scope.)
4. **Récupération du `OwnerId`** — le handler reste sans dépendance ASP.NET : **le composant lit l'id utilisateur depuis `AuthenticationState`** et le passe dans la commande. Le handler affecte `OwnerId` à l'entité.

---

## Décisions de conception

- **Entité `TaskItem`** (`Domain/Tasks/TaskItem.cs`) : `Id` (Guid), `OwnerId` (string, = `ApplicationUser.Id`), `Title`, `Description?`, `Priority` (enum), `CreatedAtUtc`. Pas d'attributs EF ni de dépendance externe dans le Domain (respecte `ArchitectureTests`).
- **Enum `TaskPriority`** (`Domain/Tasks/TaskPriority.cs`) : `Low`, `Medium`, `High`. Stocké en **string** via `HasConversion<string>()` (lisibilité en base).
- **Commande / réponse** : records positionnels dans `CreateTask.Handler.cs`.
  - `CreateTaskCommand(string OwnerId, string Title, string? Description, TaskPriority Priority)`
  - `CreateTaskResult(Guid Id, string Title, string? Description, TaskPriority Priority, DateTime CreatedAtUtc)`
- **Validation** (titre obligatoire + longueur, priorité dans l'enum, description longueur max) exécutée **au début du handler**, renvoyant `Error.Validation(...)` → `Result<CreateTaskResult>`.

---

## Étapes d'implémentation

### 1. Domaine
- `src/TodoApp/Domain/Tasks/TaskPriority.cs` — enum `Low|Medium|High`.
- `src/TodoApp/Domain/Tasks/TaskItem.cs` — entité (sealed, ctor/factory qui met `Id`, `CreatedAtUtc`). `internal` par défaut (rendu `public` seulement si tests/Blazor l'exigent).

### 2. Persistance
- `src/TodoApp/Infrastructure/Configurations/TaskItemConfiguration.cs` — `IEntityTypeConfiguration<TaskItem>` : clé, `Title` requis `MaxLength(200)`, `Description` `MaxLength(2000)` nullable, `Priority` `HasConversion<string>().HasMaxLength(20)`, index sur `OwnerId`. Pas de noms de colonnes manuels (snake_case auto).
- `Data/ApplicationDbContext.cs` — ajouter `DbSet<TaskItem> Tasks` + `builder.ApplyConfigurationsFromAssembly(typeof(TaskItemConfiguration).Assembly)` dans `OnModelCreating` (après `base.OnModelCreating`).
- **Migration** : `dotnet ef migrations add AddTasks --project src/TodoApp`. **Générer uniquement — ne jamais appliquer à une base distante.**

### 3. Erreurs partagées
- `src/TodoApp/Features/Shared/Errors/TaskErrors.cs` — classe statique. Pour ce use case : `TitleRequired`, `TitleTooLong`, `InvalidPriority` (type `Validation`). (`NotFound` sera ajouté par les use cases de lecture/édition ultérieurs.)

### 4. Slice `CreateTask`
Dossier `src/TodoApp/Features/Tasks/CreateTask/` :
- `CreateTask.Handler.cs` : records `CreateTaskCommand` / `CreateTaskResult` + `internal sealed class Handler(IDbContextFactory<ApplicationDbContext> factory, TaskChangeNotifier notifier, IValidator<CreateTaskCommand> validator) : IHandler`.
  - `HandleAsync(CreateTaskCommand, CancellationToken)` → `Task<Result<CreateTaskResult>>`.
  - Valider d'abord ; si échec → `Error.Validation`.
  - `await using var db = factory.CreateDbContext();` créer l'entité avec `OwnerId` de la commande, `Add`, `SaveChangesAsync`.
  - `notifier.NotifyChanged();` puis retourner `CreateTaskResult` (mapping manuel, pas de lib).
- `CreateTask.Validator.cs` : `internal sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>` — `Title` NotEmpty + MaxLength(200), `Description` MaximumLength(2000) when non-null, `Priority` `IsInEnum()`. Auto-enregistré (ne pas câbler à la main).

### 5. UI (Blazor, Interactive Server)
- `src/TodoApp/Features/Tasks/CreateTask/CreateTaskForm.razor` (+ `.razor.cs` si > ~20 lignes de logique) :
  - `@attribute [Authorize]` sur la page hôte ; `EditForm` lié à un modèle local (Title / Description / Priority).
  - `@inject` du handler concret (`CreateTask.Handler`) — jamais `ApplicationDbContext`.
  - Lire l'id utilisateur via `[CascadingParameter] Task<AuthenticationState>` → construire `CreateTaskCommand`, appeler le handler.
  - Afficher `Result.Error.Message` en cas d'échec (pas de try/catch pour les erreurs métier) ; réinitialiser le formulaire en cas de succès.
- Page hôte + route : `src/TodoApp/Features/Tasks/Tasks.razor` avec `@page "/tasks"` et `@attribute [Authorize]`, hébergeant `<CreateTaskForm />`.
- `Components/Layout/NavMenu.razor` : ajouter un `NavLink` « Tasks » (href `tasks`), de préférence dans `<Authorized>`.
- `Components/_Imports.razor` : ajouter `@using TodoApp.Features.Tasks.CreateTask` (et `TodoApp.Domain.Tasks` pour l'enum) si nécessaire.

### 6. Tests
- **Unit (`TodoApp.UnitTests`)** : `CreateTaskCommandValidatorTests` — titre vide → échec ; titre > 200 → échec ; priorité hors enum → échec ; description null → OK ; commande valide → OK. Nommage `[Method]_[Scenario]_[ExpectedResult]`.
- **Intégration (`TodoApp.IntegrationTests`)** — harnais à créer (première fois) :
  - Ajouter `<ProjectReference Include="..\..\src\TodoApp\TodoApp.csproj" />` au csproj.
  - Fixture `PostgreSqlContainer` (collection fixture xUnit) qui démarre le conteneur, applique `db.Database.Migrate()`, et Respawn entre les tests.
  - `CreateTaskHandlerTests` : succès (tâche persistée avec bons `Title`/`Priority`/`OwnerId`) ; échec de validation → `Result.IsFailure` + `ErrorType.Validation` ; isolation par propriétaire (une tâche créée pour un `OwnerId` n'est pas visible pour un autre — base de la règle « filtrer par `OwnerId` »).
- **Architecture** : la présence de `TaskItem` dans `Domain/` est couverte par la règle existante « Domain sans dépendances sortantes » — vérifier qu'elle passe toujours.

### 7. Build & vérification
- `dotnet build`
- `dotnet test` (les 3 projets ; l'intégration nécessite Docker pour Testcontainers).
- Corriger tout échec avant de considérer la tâche terminée.

---

## Fichiers créés / modifiés

| Action | Chemin |
|---|---|
| Créer | `src/TodoApp/Domain/Tasks/TaskPriority.cs` |
| Créer | `src/TodoApp/Domain/Tasks/TaskItem.cs` |
| Créer | `src/TodoApp/Infrastructure/Configurations/TaskItemConfiguration.cs` |
| Modifier | `src/TodoApp/Data/ApplicationDbContext.cs` (DbSet + ApplyConfigurations) |
| Créer | `src/TodoApp/Features/Shared/Errors/TaskErrors.cs` |
| Créer | `src/TodoApp/Features/Tasks/CreateTask/CreateTask.Handler.cs` |
| Créer | `src/TodoApp/Features/Tasks/CreateTask/CreateTask.Validator.cs` |
| Créer | `src/TodoApp/Features/Tasks/CreateTask/CreateTaskForm.razor` (+ `.razor.cs` si besoin) |
| Créer | `src/TodoApp/Features/Tasks/Tasks.razor` |
| Modifier | `src/TodoApp/Components/Layout/NavMenu.razor` (lien Tasks) |
| Modifier | `src/TodoApp/Components/_Imports.razor` (usings) |
| Générer | `src/TodoApp/Data/Migrations/*_AddTasks.cs` (ne pas appliquer en distant) |
| Créer | `tests/TodoApp.UnitTests/.../CreateTaskCommandValidatorTests.cs` |
| Modifier | `tests/TodoApp.IntegrationTests/TodoApp.IntegrationTests.csproj` (ProjectReference) |
| Créer | `tests/TodoApp.IntegrationTests/.../PostgresFixture.cs` + `CreateTaskHandlerTests.cs` |

---

## Risques / points d'attention
- **Pas de table `tasks` → migration obligatoire** avant les tests d'intégration ; migration **générée uniquement**, jamais appliquée à une base distante (règle projet).
- **Docker requis** pour les tests Testcontainers ; sinon ces tests échouent localement — à signaler si indisponible.
- **`OwnerId` = `ApplicationUser.Id` (string)** : pas de FK dure vers Identity exigée par le use case ; se limiter à un index sur `OwnerId`.
- **Ne jamais injecter `ApplicationDbContext` dans un composant** ni lire/loguer de secrets/chaînes de connexion.
- Garder le Domain pur (aucune référence EF/Blazor/ASP.NET) pour ne pas casser `ArchitectureTests`.

---

## Points à confirmer (hypothèses retenues faute de précision)
1. Priorité **obligatoire** côté formulaire, valeur par défaut `Medium`. (sinon : rendre facultative avec défaut serveur)
2. Enum à 3 niveaux `Low/Medium/High` stocké en **string**. (sinon : int)
3. Route UI `/tasks`. (sinon : autre emplacement)
