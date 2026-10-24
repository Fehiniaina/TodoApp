using Polly;
using Polly.Registry;

namespace TodoApp.Infrastructure.Email.Resilience;

/// <summary>Décore le transport réel avec le pipeline de résilience SMTP.</summary>
internal sealed class ResilientEmailTransport(
    MailKitEmailTransport inner,
    ResiliencePipelineProvider<string> pipelines) : IEmailTransport
{
    private readonly ResiliencePipeline _pipeline = pipelines.GetPipeline(SmtpResilience.PipelineName);

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken) =>
        await _pipeline.ExecuteAsync(
            async token => await inner.SendAsync(message, token),
            cancellationToken);
}
