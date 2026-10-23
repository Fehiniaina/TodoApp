using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TodoApp.Data;

namespace TodoApp.IntegrationTests.Infrastructure;

/// <summary>
/// Minimal <see cref="IDbContextFactory{TContext}"/> backed by the test fixture,
/// so integration tests can hand a real factory to the handler under test.
/// </summary>
internal sealed class TestDbContextFactory(PostgresFixture fixture, params IInterceptor[] interceptors) : IDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext() => fixture.CreateDbContext(interceptors);
}
