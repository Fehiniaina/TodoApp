namespace TodoApp.Features.Tags.SearchTags;

internal sealed record SearchTagsQuery(string? Term, int Limit = 10);
