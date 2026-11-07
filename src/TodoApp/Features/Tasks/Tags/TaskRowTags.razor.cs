using Microsoft.AspNetCore.Components;
using TodoApp.Features.Tags.Shared;
using TodoApp.Features.Tasks.Tags.GetTaskTags;

namespace TodoApp.Features.Tasks.Tags;

public sealed partial class TaskRowTags
{
    [Parameter, EditorRequired]
    public IReadOnlyList<TagDto> Tags { get; set; } = [];
}
