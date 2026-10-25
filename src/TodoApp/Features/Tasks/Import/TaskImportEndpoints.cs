namespace TodoApp.Features.Tasks.Import;

internal static class TaskImportEndpoints
{
    public const string TemplateRoute = "/tasks/import/template";

    public static IEndpointRouteBuilder MapTaskImportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(TemplateRoute, () => Results.File(
                TaskImportTemplate.Create(),
                TaskImportTemplate.ContentType,
                TaskImportTemplate.FileName))
            .RequireAuthorization()
            .ExcludeFromDescription();

        return app;
    }
}
