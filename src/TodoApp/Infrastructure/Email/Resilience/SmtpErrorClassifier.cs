using MailKit.Net.Smtp;
using MailKit.Security;
using Polly.CircuitBreaker;
using SslAuthenticationException = System.Security.Authentication.AuthenticationException;
using SmtpAuthenticationException = MailKit.Security.AuthenticationException;

namespace TodoApp.Infrastructure.Email.Resilience;

internal static class SmtpErrorClassifier
{
    public static SmtpFailureKind Classify(Exception exception) => exception switch
    {
        SmtpCommandException { StatusCode: SmtpStatusCode.AuthenticationRequired } => SmtpFailureKind.Configuration,
        SmtpCommandException { StatusCode: var code } when (int)code >= 500 => SmtpFailureKind.Permanent,
        SmtpCommandException => SmtpFailureKind.Transient, // 4xx : 421 service indisponible, 450 boîte occupée…

        SmtpAuthenticationException => SmtpFailureKind.Configuration,
        SslHandshakeException => SmtpFailureKind.Configuration,
        SslAuthenticationException => SmtpFailureKind.Configuration,

        // SocketException, IOException, ServiceNotConnectedException, SmtpProtocolException,
        // TimeoutRejectedException et toute erreur inconnue : on réessaie, dans la limite de MaxAttempts.
        _ => SmtpFailureKind.Transient,
    };

    /// <summary>Utilisé par Polly : ce qui mérite un retry immédiat et compte pour le circuit breaker.</summary>
    public static bool IsTransient(Exception exception) =>
        exception is not (OperationCanceledException or BrokenCircuitException)
        && Classify(exception) == SmtpFailureKind.Transient;
}
