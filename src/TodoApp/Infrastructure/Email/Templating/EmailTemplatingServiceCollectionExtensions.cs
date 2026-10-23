namespace TodoApp.Infrastructure.Email.Templating;

internal static class EmailTemplatingServiceCollectionExtensions
{
    public static IServiceCollection AddEmailTemplating(this IServiceCollection services)
    {
        services.AddSingleton<IEmailRenderer, RazorEmailRenderer>();
        return services;
    }

    public static IServiceCollection AddEmailTemplate<TModel, TTemplate>(this IServiceCollection services)
        where TModel : IEmailModel
        where TTemplate : EmailTemplate<TModel>
    {
        services.AddSingleton(new EmailTemplateRegistration(typeof(TModel), typeof(TTemplate)));
        return services;
    }
}
