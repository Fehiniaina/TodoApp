using Microsoft.AspNetCore.Components.Authorization;

namespace TodoApp.Infrastructure.Http;

internal static class CurrentUserEndpointFilter
{
    /// <summary>
    /// Rend ICurrentUser utilisable dans un endpoint minimal API, en alimentant
    /// l'AuthenticationStateProvider avec l'utilisateur de la requête HTTP.
    /// </summary>
    public static TBuilder WithCurrentUser<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            var provider = context.HttpContext.RequestServices.GetRequiredService<AuthenticationStateProvider>();

            if (provider is IHostEnvironmentAuthenticationStateProvider hostProvider)
            {
                hostProvider.SetAuthenticationState(
                    Task.FromResult(new AuthenticationState(context.HttpContext.User)));
            }

            return await next(context);
        });
}
