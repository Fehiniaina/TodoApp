using TodoApp.Domain.Tags;

namespace TodoApp.Features.Tags.Shared;

#pragma warning disable VSSpell001 // Spell Check
public sealed record TagDto(Guid Id, string Name, TagColor Color);
#pragma warning restore VSSpell001 // Spell Check
