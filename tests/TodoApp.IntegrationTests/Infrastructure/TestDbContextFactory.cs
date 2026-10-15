using Microsoft.EntityFrameworkCore;
using TodoApp.Data;

namespace TodoApp.IntegrationTests.Infrastructure;

/// <summary>
/// Minimal <see cref="IDbContextFactory{TContext}"/> backed by the test fixture,
/// so integration tests can hand a real factory to the handler under test.
/// </summary>
internal sealed class TestDbContextFactory(PostgresFixture fixture) : IDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext() => fixture.CreateDbContext();
}
