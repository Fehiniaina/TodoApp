using TodoApp.Domain.Tags;
using TodoApp.Features.Tags.Shared;

namespace TodoApp.UnitTests.Features.Tags.Shareds;

public sealed class TagColorVisualsTests
{
    [Fact]
    public void Every_color_has_its_own_css_class()
    {
        Assert.All(Enum.GetValues<TagColor>(), c => Assert.True(TagColorVisuals.HasVisual(c), c.ToString()));

        var classes = Enum.GetValues<TagColor>().Select(TagColorVisuals.CssFor).ToList();
        Assert.Equal(classes.Count, classes.Distinct(StringComparer.Ordinal).Count());
    }
}
