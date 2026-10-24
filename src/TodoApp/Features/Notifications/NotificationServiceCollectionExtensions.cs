using TodoApp.Features.Notifications.Email;
using TodoApp.Features.Notifications.Email.TaskAssigned;
using TodoApp.Features.Notifications.Realtime;
using TodoApp.Infrastructure.Email;
using TodoApp.Infrastructure.Email.Resilience;
using TodoApp.Infrastructure.Email.Templating;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Features.Notifications;

internal static class NotificationServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationFeatures(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Temps réel (étape 4)
        services.AddSingleton<INotificationBroadcaster, InMemoryNotificationBroadcaster>();
        services.AddSingleton<NotificationPublishingInterceptor>();

        //// Email (étape 5)
        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<EmailDeliveryOptions>()
            .Bind(configuration.GetSection(EmailDeliveryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddEmailTemplate<TaskAssignedEmail, TaskAssignedEmailTemplate>();
        services.AddSingleton<INotificationEmailComposer, TaskAssignedEmailComposer>();

        // Transport : MailKit décoré par le pipeline de résilience
        services.AddSmtpResilience();
        services.AddSingleton<MailKitEmailTransport>();
        services.AddSingleton<IEmailTransport, ResilientEmailTransport>();

        // Job d'envoi et son réveil
        services.AddSingleton<EmailDeliverySignal>();
        services.AddSingleton<EmailDeliveryWakeUpInterceptor>();
        services.AddSingleton<EmailDeliveryProcessor>();
        services.AddHostedService<EmailDeliveryService>();

        return services;
    }
}
