---
paths:
  - "src/TodoApp/Program.cs"
  - "src/TodoApp/**/*Extensions.cs"
---

# DI registration rules
- Program.cs calls AddTaskFeatures(services, configuration)
- Auto-scan handlers: RegisterHandlersFromAssemblyContaining<Program>
- Auto-scan validators: AddValidatorsFromAssemblyContaining<Program>
- AddDbContextFactory<AppDbContext> with UseNpgsql + UseSnakeCaseNamingConvention
- TaskChangeNotifier registered as singleton
- Health checks: database + app
