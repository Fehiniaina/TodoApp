namespace TodoApp.Features.Shared.Time;

public interface IUserClock
{
    DateOnly Today { get; }
    DateTime ToLocal(DateTime utc);
}

internal sealed class UserClock(TimeProvider timeProvider, UserTimeZone userTimeZone) : IUserClock
{
    public DateOnly Today => DateOnly.FromDateTime(ToLocal(timeProvider.GetUtcNow().UtcDateTime));

    public DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), userTimeZone.TimeZone);
}