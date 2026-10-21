using Microsoft.EntityFrameworkCore;
using TodoApp.Common;
using TodoApp.Data;

namespace TodoApp.Features.Users;

internal sealed class GetAssignableUsersHandler(IDbContextFactory<ApplicationDbContext> factory) : IHandler
{
    public async Task<IReadOnlyList<UserSummary>> HandleAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var users = await db.Users
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ThenBy(u => u.Email)
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
            .ToListAsync(cancellationToken);

        return users
            .Select(u => new UserSummary(u.Id, UserSummary.ToDisplayName(u.FirstName, u.LastName, u.Email)))
            .ToList();
    }
}
