using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TodoApp.Components;
using TodoApp.Components.Account;
using TodoApp.Data;
using TodoApp.Features;
using TodoApp.Features.Notifications.Realtime;
using TodoApp.Features.Shared;
using TodoApp.Features.Shared.Notifications;
using TodoApp.Features.Shared.Time;
using TodoApp.Infrastructure;
using TodoApp.Infrastructure.BackgroundJobs;
using TodoApp.Infrastructure.Configurations;
using TodoApp.Infrastructure.Persistence;
using TodoApp.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddSingleton<INotificationBroadcaster, InMemoryNotificationBroadcaster>();
builder.Services.AddSingleton<NotificationPublishingInterceptor>();

builder.Services.AddDbContextFactory<ApplicationDbContext>((sp, options) => {
    options.UseTodoAppNpgsql(connectionString, npgsql => npgsql
        .EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null
        ))
    .AddInterceptors(sp.GetRequiredService<NotificationPublishingInterceptor>());

    if (builder.Environment.IsDevelopment())
    {
        options.LogTo(Console.WriteLine, LogLevel.Information)
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
    }
});
// Identity stores require a scoped DbContext; bridge it to the singleton
// factory so handlers still get a short-lived context per call.
builder.Services.AddScoped<ApplicationDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        options.SignIn.RequireConfirmedAccount = !builder.Environment.IsDevelopment();
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddTaskFeatures(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

builder.Services.AddScoped<NotificationService>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<UserTimeZone>();
builder.Services.AddScoped<IUserClock, UserClock>();

builder.Services.AddHostedService<TrashPurgeService>();

builder.Services
    .AddOptions<TrashPurgeOptions>()
    .Bind(builder.Configuration.GetSection(TrashPurgeOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<RecurringTaskOptions>()
    .Bind(builder.Configuration.GetSection(RecurringTaskOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<RecurringTaskScheduler>();
builder.Services.AddHostedService<RecurringTaskService>();
builder.Services.AddHostedService<DatabaseSeederHostedService>();

builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = builder.Environment.IsDevelopment()
        ? StatusCodes.Status307TemporaryRedirect
        : StatusCodes.Status308PermanentRedirect;
});

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(60);
    options.IncludeSubDomains = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseSerilogRequestLogging();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.UseHsts();
app.UseHttpsRedirection();

await app.RunAsync();
