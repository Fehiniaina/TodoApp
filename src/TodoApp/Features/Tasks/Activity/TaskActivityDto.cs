using TodoApp.Domain.Tasks.Activity;

namespace TodoApp.Features.Tasks.Activity;

/// <summary>Valeurs déjà résolues pour l'affichage (noms à la place des Id utilisateur).</summary>
public sealed record TaskActivityDto(
    Guid Id,
    TaskActivityType Type,
    string ActorName,
    string? OldValue,
    string? NewValue,
    DateTime OccurredAtUtc,
    string? ActorId);
