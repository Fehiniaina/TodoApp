using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace TodoApp.Features.Shared;

internal sealed class CurrentUser(AuthenticationStateProvider provider) : ICurrentUser
{
    public async Task<string> GetIdAsync()
    {
        var state = await provider.GetAuthenticationStateAsync();

        return state.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("No authenticated user.");
    }
}
