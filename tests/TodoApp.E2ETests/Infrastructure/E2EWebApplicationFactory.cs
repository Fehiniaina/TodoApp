using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TodoApp.Features.Notifications.Email;
using TodoApp.Infrastructure.BackgroundJobs;
using TodoApp.Infrastructure.Configurations;
using TodoApp.Infrastructure.Email;
using TodoApp.Infrastructure.Seed;

namespace TodoApp.E2ETests.Infrastructure;

internal sealed class E2EWebApplicationFactory(FakeEmailTransport emails) : WebApplicationFactory<Program>
{
    private static readonly Type[] DisabledHostedServices =
    [
        typeof(DatabaseSeederHostedService),
        typeof(TrashPurgeService),
        typeof(RecurringTaskService),
        typeof(EmailDeliveryService),
    ];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseUrls("http://127.0.0.1:0");

        builder.ConfigureTestServices(services =>
        {
            var toRemove = services
                .Where(d => d.ServiceType == typeof(IHostedService)
                         && DisabledHostedServices.Contains(d.ImplementationType))
                .ToList();

            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            services.RemoveAll<IEmailTransport>();
            services.AddSingleton<IEmailTransport>(emails);
        });
    }
}
