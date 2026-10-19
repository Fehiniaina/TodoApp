namespace TodoApp.Features.Shared.Time;

internal sealed class UserClock(TimeProvider timeProvider, UserTimeZone userTimeZone) : IUserClock
{
    public DateOnly Today => DateOnly.FromDateTime(ToLocal(timeProvider.GetUtcNow().UtcDateTime));

    public DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), userTimeZone.TimeZone);
}