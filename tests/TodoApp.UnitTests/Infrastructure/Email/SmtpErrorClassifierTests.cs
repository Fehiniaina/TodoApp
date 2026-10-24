using System.Net.Sockets;
using MailKit;
using MailKit.Net.Smtp;
using Polly.CircuitBreaker;
using Polly.Timeout;
using TodoApp.Infrastructure.Email.Resilience;

namespace TodoApp.UnitTests.Infrastructure.Email;

public sealed class SmtpErrorClassifierTests
{
    public static TheoryData<Exception> TransientErrors => new()
    {
        new SmtpCommandException(SmtpErrorCode.MessageNotAccepted, SmtpStatusCode.ServiceNotAvailable, "421"),
        new SmtpCommandException(SmtpErrorCode.RecipientNotAccepted, SmtpStatusCode.MailboxBusy, "450"),
        new SocketException((int)SocketError.ConnectionRefused),
        new IOException("connexion coupée"),
        new ServiceNotConnectedException(),
        new TimeoutRejectedException(),
        new InvalidOperationException("erreur inconnue"),
    };

    public static TheoryData<Exception> PermanentErrors => new()
    {
        new SmtpCommandException(SmtpErrorCode.RecipientNotAccepted, SmtpStatusCode.MailboxUnavailable, "550"),
        new SmtpCommandException(SmtpErrorCode.RecipientNotAccepted, SmtpStatusCode.MailboxNameNotAllowed, "553"),
    };

    public static TheoryData<Exception> ConfigurationErrors => new()
    {
        new SmtpCommandException(SmtpErrorCode.UnexpectedStatusCode, SmtpStatusCode.AuthenticationRequired, "530"),
        new MailKit.Security.AuthenticationException("535 identifiants invalides"),
    };

    [Theory]
    [MemberData(nameof(TransientErrors))]
    public void Transient_errors_are_retried(Exception exception)
    {
        Assert.Equal(SmtpFailureKind.Transient, SmtpErrorClassifier.Classify(exception));
        Assert.True(SmtpErrorClassifier.IsTransient(exception));
    }

    [Theory]
    [MemberData(nameof(PermanentErrors))]
    public void Permanent_errors_are_not_retried(Exception exception)
    {
        Assert.Equal(SmtpFailureKind.Permanent, SmtpErrorClassifier.Classify(exception));
        Assert.False(SmtpErrorClassifier.IsTransient(exception));
    }

    [Theory]
    [MemberData(nameof(ConfigurationErrors))]
    public void Configuration_errors_are_not_retried(Exception exception)
    {
        Assert.Equal(SmtpFailureKind.Configuration, SmtpErrorClassifier.Classify(exception));
        Assert.False(SmtpErrorClassifier.IsTransient(exception));
    }

    [Fact]
    public void Open_circuit_and_cancellation_are_never_retried_by_polly()
    {
        Assert.False(SmtpErrorClassifier.IsTransient(new BrokenCircuitException()));
        Assert.False(SmtpErrorClassifier.IsTransient(new OperationCanceledException()));
    }
}
