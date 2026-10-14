---
paths:
  - "**/*.razor"
  - "**/*.razor.cs"
---

# Blazor component rules
- Component lives next to its use case: Features/Tasks/[UseCase]/[ComponentName].razor
- Move logic to a .razor.cs code-behind when it exceeds ~20 lines
- @inject in .razor files; constructor injection in .razor.cs code-behind
- Call handlers from component code, never from markup expressions
- Never inject AppDbContext into a component
- Render errors from the handler's Result; no try/catch for business errors
- Components that subscribe to TaskChangeNotifier implement IDisposable
  and unsubscribe in Dispose
- UI updates triggered by background events go through InvokeAsync(StateHasChanged)
- No JavaScript interop unless no Blazor alternative exists
