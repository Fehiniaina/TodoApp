namespace TodoApp.Common;

/// <summary>
/// Marker interface for command/query handlers. Concrete implementations are
/// auto-registered by <c>RegisterHandlersFromAssemblyContaining</c>; never
/// register a handler by hand.
/// </summary>
public interface IHandler;
