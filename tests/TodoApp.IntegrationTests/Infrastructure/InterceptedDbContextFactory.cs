using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TodoApp.Data;

namespace TodoApp.IntegrationTests.Infrastructure;

/// <summary>Fabrique de contextes qui branche un intercepteur (ici, le compteur de requêtes).</summary>
internal sealed class InterceptedDbContextFactory(PostgresFixture fixture, IInterceptor interceptor)
    : IDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext() => fixture.CreateDbContext(interceptor);
}
