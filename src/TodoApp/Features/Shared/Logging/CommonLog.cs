
using Microsoft.Extensions.Logging;

namespace TodoApp.Features.Shared.Logging;

internal static partial class CommonLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Error,
    Message = "Unexpected error during {Operation}")]
    public static partial void UnexpectedError(this ILogger logger, string operation, Exception exception);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning,
        Message = "{Operation} failed: {ErrorCode} - {ErrorMessage}")]
    public static partial void OperationFailed(this ILogger logger, string operation, string errorCode, string errorMessage);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information,
        Message = "{Operation} succeeded for {EntityId}")]
    public static partial void OperationSucceeded(this ILogger logger, string operation, Guid entityId);

}
