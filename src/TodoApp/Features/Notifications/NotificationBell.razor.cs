using System.Data.Common;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TodoApp.Features.Notifications.GetRecent;
using TodoApp.Features.Notifications.GetUnreadCount;
using TodoApp.Features.Notifications.MarkAllRead;
using TodoApp.Features.Notifications.MarkRead;
using TodoApp.Features.Notifications.Shared;

namespace TodoApp.Features.Notifications;

public sealed partial class NotificationBell
{
    private const int PanelSize = 10;

    [Inject] private GetUnreadNotificationCountHandler CountHandler { get; set; } = null!;
    [Inject] private GetRecentNotificationsHandler RecentHandler { get; set; } = null!;
    [Inject] private MarkNotificationReadHandler MarkReadHandler { get; set; } = null!;
    [Inject] private MarkAllNotificationsReadHandler MarkAllReadHandler { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private TimeProvider Time { get; set; } = null!;
    [Inject] private ILogger<NotificationBell> Logger { get; set; } = null!;

    [PersistentState]
    public int? UnreadCount { get; set; }

    private bool _isOpen;
    private IReadOnlyList<NotificationDto>? _items;
    private DateTime _nowUtc;

    private string BadgeText =>
        UnreadCount > 9 ? "9+" : (UnreadCount ?? 0).ToString(CultureInfo.InvariantCulture);

    private string AriaLabel => UnreadCount switch
    {
        null or 0 => "Notifications",
        1 => "Notifications, 1 non lue",
        > 9 => "Notifications, plus de 9 non lues",
        var n => $"Notifications, {n} non lues",
    };

    protected override async Task OnInitializedAsync()
    {
        UnreadCount ??= await CountHandler.HandleAsync();
    }

    private async Task ToggleAsync()
    {
        if (_isOpen)
        {
            Close();
            return;
        }

        _isOpen = true;
        _items = null;
        _nowUtc = Time.GetUtcNow().UtcDateTime;

        try
        {
            _items = await RecentHandler.HandleAsync(new GetRecentNotificationsQuery(PanelSize));
            UnreadCount = await CountHandler.HandleAsync();
        }
        catch (DbException ex)
        {
            LogLoadFailed(Logger, ex);
            _items = [];
        }
    }

    private async Task OpenAsync(NotificationDto item)
    {
        if (!item.IsRead)
        {
            var result = await MarkReadHandler.HandleAsync(new MarkNotificationReadCommand(item.Id));
            if (result.IsSuccess)
            {
                UnreadCount = Math.Max(0, (UnreadCount ?? 1) - 1);
            }
        }

        Close();
        Navigation.NavigateTo($"/tasks?task={item.TaskId}");
    }

    private async Task MarkAllAsReadAsync()
    {
        await MarkAllReadHandler.HandleAsync();

        UnreadCount = 0;
        _items = _items?.Select(i => i with { IsRead = true }).ToList();
    }

    private void Close() => _isOpen = false;

    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (_isOpen && string.Equals(e.Key, "Escape", StringComparison.Ordinal))
        {
            Close();
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Échec du chargement des notifications")]
    private static partial void LogLoadFailed(ILogger logger, Exception exception);
}
