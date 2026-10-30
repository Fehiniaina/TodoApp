using TodoApp.Domain.Tags;

namespace TodoApp.UnitTests.Domain.Tags;

public sealed class TagTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Name_is_cleaned_and_normalized()
    {
        var tag = Tag.Create("  front   end ", TagColor.Blue, "owner", Now);

        Assert.Equal("front end", tag.Name);
        Assert.Equal("FRONT END", tag.NormalizedName);
    }

    [Theory]
    [InlineData("Frontend", "frontend")]
    [InlineData("Front end", "  FRONT   END ")]
    public void Normalize_makes_equivalent_names_equal(string a, string b)
    {
        Assert.Equal(Tag.Normalize(a), Tag.Normalize(b));
    }

    [Fact]
    public void Name_of_max_length_is_accepted()
    {
        var name = new string('a', Tag.MaxNameLength);

        Assert.Equal(name, Tag.Create(name, TagColor.Gray, "owner", Now).Name);
    }

    [Fact]
    public void Name_too_long_is_rejected()
    {
        Assert.Throws<ArgumentException>(
            () => Tag.Create(new string('a', Tag.MaxNameLength + 1), TagColor.Gray, "owner", Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_name_is_rejected(string name)
    {
        Assert.ThrowsAny<ArgumentException>(() => Tag.Create(name, TagColor.Gray, "owner", Now));
    }

    [Fact]
    public void Rename_updates_the_normalized_name()
    {
        var tag = Tag.Create("Frontend", TagColor.Blue, "owner", Now);

        tag.Rename("UI");

        Assert.Equal("UI", tag.Name);
        Assert.Equal("UI", tag.NormalizedName);
    }

    [Fact]
    public void Undefined_color_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Tag.Create("Frontend", (TagColor)99, "owner", Now));
    }
}
