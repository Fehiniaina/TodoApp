namespace TodoApp.Features.Shared.Time;

public interface IUserClock
{
    DateOnly Today { get; }
    DateTime ToLocal(DateTime utc);
}
