---
paths:
  - "tests/**/*.cs"
---

# Testing rules
- xUnit for test framework
- Moq for mocking
- bUnit for component tests
- Testcontainers for integration tests (PostgreSQL)
- Respawn for database cleanup between tests
- Bogus for test data
- NetArchTest.Rules for architecture tests (Domain has no outward dependencies)
- Test naming: [Method]_[Scenario]_[ExpectedResult]
- Projects: TodoApp.UnitTests (handlers, validators, components),
  TodoApp.IntegrationTests (handlers against real PostgreSQL),
  TodoApp.ArchitectureTests (layering rules)
