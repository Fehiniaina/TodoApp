---
paths:
  - "src/TodoApp/Infrastructure/**"
---

# Infrastructure rules
- One IEntityTypeConfiguration per entity:
  Infrastructure/Configurations/[Entity]Configuration.cs
- All mapping in configurations; no data annotations on Domain entities
- snake_case naming via UseSnakeCaseNamingConvention; no manual column names
- Migrations are generated only, never applied to a remote database
- Development seeding uses Bogus and runs only in the Development environment
