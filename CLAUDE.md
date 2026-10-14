# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status
No source code exists yet — there is no `src/`, `tests/`, `.sln`, or `.csproj`, and this is not a git repository. Only planning configuration is present (`CLAUDE.md`, `.claude/rules/`, `.claude/skills/`). Everything below is the specification to follow when scaffolding and building the project, not a description of existing code.

Additional path-scoped rules (auto-attached by Claude Code for matching files) live in `.claude/rules/`: `blazor-components.md`, `di-registration.md`, `infrastructure.md`, `testing.md`. A `create-use-case` skill in `.claude/skills/SKILL.md` encodes the step-by-step for adding a new task use case.

## Overview
TodoApp - Task management web app (Blazor Server monolith).

## Tech Stack
- .NET 10, Blazor Web App with Interactive Server render mode
- SignalR (built into Blazor Server circuits) for real-time UI updates
- EF Core 10 with PostgreSQL via Npgsql (Neon, snake_case naming convention)
- ASP.NET Core Identity with cookie authentication
- FluentValidation for command validation
- Result<T> pattern for error handling (no exceptions for business logic)
- Serilog for structured logging
- Hosting: Azure App Service (WebSockets ON, ARR affinity ON)
- CI/CD: GitHub Actions

## Architecture
- Single-project monolith with Vertical Slice Architecture
- Domain is isolated: no EF Core, Blazor, or ASP.NET references in Domain/
- CQRS with manual handlers (no MediatR, no interfaces per handler)
- Components call handlers directly; no service layer in between
- Manual mapping with extension methods
- Real-time: a singleton TaskChangeNotifier publishes task events;
  components subscribe and re-render. No custom SignalR hub.
- DI composition lives behind one `AddTaskFeatures(services, configuration)`
  call in Program.cs; handlers and validators are assembly-scanned
  (`RegisterHandlersFromAssemblyContaining<Program>`,
  `AddValidatorsFromAssemblyContaining<Program>`), never registered by hand

## Project Structure
- src/TodoApp/Domain/ - Entities, value objects, enums
- src/TodoApp/Features/Tasks/[UseCase]/ - Handler, validator, mapping, component
- src/TodoApp/Features/Shared/ - Errors, notifier, shared components
- src/TodoApp/Infrastructure/ - AppDbContext, EF configurations, migrations
- src/TodoApp/Common/ - Result<T>, Error, IHandler marker interface
- src/TodoApp/Components/ - App.razor, Routes.razor, layout, account pages
- tests/ - UnitTests, IntegrationTests, ArchitectureTests projects

## File Naming
- Features/Tasks/[UseCase]/[UseCase].Handler.cs
- Features/Tasks/[UseCase]/[UseCase].Validator.cs (commands only)
- Features/Tasks/[UseCase]/[UseCase].Mapping.cs (if 2+ mappings)
- Features/Shared/Errors/TaskErrors.cs

## Code Conventions
- Positional records for commands, queries, and view models
- File-scoped namespaces
- Primary constructors for dependency injection in C# classes
- Sealed classes for implementations
- Internal by default, public only when Blazor or tests require it

## Patterns We Use
- Result<T> for all handler return types
- IHandler marker interface; handlers and validators are auto-registered,
  never register them manually
- FluentValidation validators per command, executed inside the handler
- IDbContextFactory<AppDbContext>: short-lived DbContext per handler call
- AsNoTracking() for all read queries
- Every query filters by the current user's OwnerId

## Patterns We Do NOT Use
- Repository pattern
- AutoMapper or any mapping library
- MediatR or any mediator library
- Exceptions for business logic flow
- JWT authentication (cookie auth only)

## Testing
- xUnit, with Moq for mocking and bUnit for component tests
- Integration tests run handlers against a real PostgreSQL via Testcontainers;
  Respawn resets state between tests
- Bogus generates test data (also used for Development-only DB seeding)
- NetArchTest.Rules enforces layering (e.g. Domain has no outward dependencies)
- Test naming: [Method]_[Scenario]_[ExpectedResult]
- Three projects: TodoApp.UnitTests (handlers, validators, components),
  TodoApp.IntegrationTests (handlers against real PostgreSQL),
  TodoApp.ArchitectureTests (layering rules)

## Commands
- Build: dotnet build
- Test everything: dotnet test
- Test one project: dotnet test tests/TodoApp.UnitTests
- Test one case: dotnet test --filter "FullyQualifiedName~[Name]"
- Run: dotnet run --project src/TodoApp
- Add migration: dotnet ef migrations add [Name] --project src/TodoApp

## Rules
- Run dotnet build and dotnet test before marking any task complete
- Never apply migrations to a remote database; only generate them
- Never read, write, or log connection strings or secrets
- Plans live in plans/ at the repository root
