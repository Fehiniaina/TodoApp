using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace TodoApp.Infrastructure.Email.Resilience;

internal static class SmtpResilience
{
    public const string PipelineName = "smtp";

    public static IServiceCollection AddSmtpResilience(this IServiceCollection services)
    {
        var transient = new PredicateBuilder().Handle<Exception>(SmtpErrorClassifier.IsTransient);

        services.AddResiliencePipeline(PipelineName, pipeline => pipeline
            // 1. Retry court : absorbe une coupure de quelques secondes.
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = transient,
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
            })
            // 2. Circuit breaker : si le SMTP est en panne, on arrête d'insister pendant 2 minutes.
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = transient,
                FailureRatio = 0.5,
                MinimumThroughput = 3,
                SamplingDuration = TimeSpan.FromMinutes(1),
                BreakDuration = TimeSpan.FromMinutes(2),
            })
            // 3. Timeout par tentative : un serveur qui ne répond pas ne bloque pas le job.
            .AddTimeout(TimeSpan.FromSeconds(15)));

        return services;
    }
}
