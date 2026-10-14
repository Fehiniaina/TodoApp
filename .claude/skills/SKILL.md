---
name: create-use-case
description: Create a new task use case (command or query) following our project
conventions (vertical slice, IHandler, Result<T>, FluentValidation,
TaskChangeNotifier, Blazor component wiring, tests).
Use whenever the user asks to add a feature, action, page, filter, or operation
on tasks (create, edit, complete, delete, list, filter, sort, prioritize).
Do not use for EF Core migrations, login/account pages (ASP.NET Core Identity),
styling-only UI changes, or bug fixes inside an existing use case.
---

# Create a Task Use Case

1. Decide the type: command (changes data) or query (reads data).
   If unclear, ask before writing code.
2. Create the folder `src/TodoApp/Features/Tasks/<UseCase>/`.
3. Define the request and response as positional records in
   `<UseCase>.Handler.cs`.
4. Implement the handler in `<UseCase>.Handler.cs`:
   - Sealed class implementing `IHandler`, primary constructor injecting
     `IDbContextFactory<AppDbContext>`
   - Return `Result<T>`; add new errors to `Features/Shared/Errors/TaskErrors.cs`
   - Filter every query by the current user's `OwnerId`
   - Queries: `AsNoTracking()`
5. Commands only:
   - Create `<UseCase>.Validator.cs` and run it at the start of the handler
   - After `SaveChangesAsync`, publish the matching event on `TaskChangeNotifier`
6. Wire the UI: add or update the component in the same folder, or call the
   handler from the existing page that owns this action.
7. Write tests:
   - Unit test for the validator (commands)
   - Integration test for the handler: success case, validation failure,
     and another user's task returning `TaskErrors.NotFound`
8. Run `dotnet build` and `dotnet test`. Fix all failures before reporting done.
