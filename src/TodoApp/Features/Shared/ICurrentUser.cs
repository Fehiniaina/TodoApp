namespace TodoApp.Features.Shared;

internal interface ICurrentUser
{
    Task<string> GetIdAsync();
}
