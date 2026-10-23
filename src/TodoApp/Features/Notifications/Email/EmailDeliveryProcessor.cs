using System.Collections.Frozen;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TodoApp.Data;
using TodoApp.Domain.Notifications;
using TodoApp.Features.Users;
using TodoApp.Infrastructure.Email;
using TodoApp.Infrastructure.Email.Templating;

namespace TodoApp.Features.Notifications.Email;

internal sealed partial class EmailDeliveryProcessor(
    IDbContextFactory<ApplicationDbContext> factory,
    IEmailTransport transport,
    IEmailRenderer renderer,
    IEnumerable<INotificationEmailComposer> composers,
    TimeProvider timeProvider,
    IOptions<EmailDeliveryOptions> options,
    ILogger<EmailDeliveryProcessor> logger)
{
    private const string SystemActor = "Système";

    private readonly FrozenDictionary<NotificationType, INotificationEmailComposer> _composers =
        composers.ToFrozenDictionary(c => c.Type);

    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var deliveries = await db.EmailDeliveries
            .TagWith("EmailDeliveryProcessor : lot à envoyer")
            .AsTracking()
            .Include(e => e.Notification)
            .Where(e => e.Status == EmailDeliveryStatus.Pending && e.NextAttemptAtUtc <= nowUtc)
            .OrderBy(e => e.NextAttemptAtUtc)
            .Take(settings.BatchSize)
            .ToListAsync(cancellationToken);

        if (deliveries.Count == 0)
        {
            return 0;
        }

        var users = await LoadUsersAsync(db, deliveries, cancellationToken);
        var taskTitles = await LoadTaskTitlesAsync(db, deliveries, cancellationToken);

        foreach (var delivery in deliveries)
        {
            await ProcessAsync(delivery, users, taskTitles, settings, nowUtc, cancellationToken);

            // Sauvegarde après chaque email : un crash au milieu du lot ne renvoie pas ceux déjà partis.
            await db.SaveChangesAsync(cancellationToken);
        }

        return deliveries.Count;
    }

    private async Task ProcessAsync(
        EmailDelivery delivery,
        Dictionary<string, ApplicationUser> users,
        Dictionary<Guid, string> taskTitles,
        EmailDeliveryOptions settings,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var notification = delivery.Notification;

        if (nowUtc - delivery.CreatedAtUtc > settings.ExpireAfter)
        {
            delivery.Expire();
            LogExpired(logger, delivery.Id);
            return;
        }

        if (!taskTitles.TryGetValue(notification.TaskId, out var taskTitle))
        {
            // Tâche mise à la corbeille entre les deux requêtes : le prochain cycle l'exclura via le filtre.
            return;
        }

        if (!users.TryGetValue(notification.RecipientId, out var recipient)
            || string.IsNullOrWhiteSpace(recipient.Email))
        {
            delivery.MarkFailed("Le destinataire n'a pas d'adresse email.");
            LogNoAddress(logger, delivery.Id, notification.RecipientId);
            return;
        }

        if (!_composers.TryGetValue(notification.Type, out var composer))
        {
            delivery.MarkFailed($"Aucun email défini pour le type {notification.Type}.");
            LogNoComposer(logger, delivery.Id, notification.Type);
            return;
        }

        EmailMessage message;
        try
        {
            message = await BuildMessageAsync(delivery, taskTitle, recipient, users, composer, settings);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Erreur de template : réessayer ne la corrigera pas, l'envoi est abandonné.
            delivery.MarkFailed($"Rendu impossible : {ex.Message}");
            LogRenderFailed(logger, ex, delivery.Id);
            return;
        }

        try
        {
            await transport.SendAsync(message, cancellationToken);
            delivery.MarkSent(nowUtc);
            LogSent(logger, delivery.Id, delivery.Attempts);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            delivery.ScheduleRetry(nowUtc, ex.Message, settings.MaxAttempts, settings.MaxBackoff);
            LogSendFailed(logger, ex, delivery.Id, delivery.Attempts, delivery.Status);
        }
    }

    private async Task<EmailMessage> BuildMessageAsync(
        EmailDelivery delivery,
        string taskTitle,
        ApplicationUser recipient,
        Dictionary<string, ApplicationUser> users,
        INotificationEmailComposer composer,
        EmailDeliveryOptions settings)
    {
        var notification = delivery.Notification;

        var actorName = notification.ActorId is not null && users.TryGetValue(notification.ActorId, out var actor)
            ? DisplayName(actor)
            : SystemActor;

        var context = new NotificationEmailContext(
            notification,
            taskTitle,
            DisplayName(recipient),
            actorName,
            settings.AppBaseUrl);

        var email = await renderer.RenderAsync(composer.Compose(context));

        return new EmailMessage(
            recipient.Email!,
            DisplayName(recipient),
            email.Subject,
            email.Html,
            email.Text,
            MessageId: $"{delivery.Id}@todoapp.local");
    }

    private static string DisplayName(ApplicationUser user) =>
        UserSummary.ToDisplayName(user.FirstName, user.LastName, user.Email);

    private static async Task<Dictionary<string, ApplicationUser>> LoadUsersAsync(
        ApplicationDbContext db,
        List<EmailDelivery> deliveries,
        CancellationToken cancellationToken)
    {
        var ids = deliveries
            .SelectMany(d => new[] { d.Notification.RecipientId, d.Notification.ActorId })
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return await db.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, StringComparer.Ordinal, cancellationToken);
    }

    private static async Task<Dictionary<Guid, string>> LoadTaskTitlesAsync(
        ApplicationDbContext db,
        List<EmailDelivery> deliveries,
        CancellationToken cancellationToken)
    {
        var taskIds = deliveries
            .Select(d => d.Notification.TaskId)
            .Distinct()
            .ToList();

        return await db.Tasks
            .TagWith("EmailDeliveryProcessor : titres des tâches")
            .Where(t => taskIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Title })
            .ToDictionaryAsync(t => t.Id, t => t.Title, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email {DeliveryId} envoyé (tentative {Attempts})")]
    private static partial void LogSent(ILogger logger, Guid deliveryId, int attempts);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Échec d'envoi de l'email {DeliveryId} (tentative {Attempts}, statut {Status})")]
    private static partial void LogSendFailed(ILogger logger, Exception exception, Guid deliveryId, int attempts, EmailDeliveryStatus status);

    [LoggerMessage(Level = LogLevel.Information, Message = "Email {DeliveryId} expiré, non envoyé")]
    private static partial void LogExpired(ILogger logger, Guid deliveryId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Email {DeliveryId} abandonné : l'utilisateur {UserId} n'a pas d'adresse")]
    private static partial void LogNoAddress(ILogger logger, Guid deliveryId, string userId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Email {DeliveryId} abandonné : aucun composer pour le type {Type}")]
    private static partial void LogNoComposer(ILogger logger, Guid deliveryId, NotificationType type);

    [LoggerMessage(Level = LogLevel.Error, Message = "Email {DeliveryId} abandonné : échec du rendu du template")]
    private static partial void LogRenderFailed(ILogger logger, Exception exception, Guid deliveryId);
}
