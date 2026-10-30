using TodoApp.Domain.Tags;

namespace TodoApp.UnitTests.Domain.Tags;

public sealed class TagColorContractTests
{
    private static readonly Dictionary<TagColor, int> PersistedValues = new()
    {
        [TagColor.Gray] = 0,
        [TagColor.Blue] = 1,
        [TagColor.Green] = 2,
        [TagColor.Yellow] = 3,
        [TagColor.Orange] = 4,
        [TagColor.Red] = 5,
        [TagColor.Purple] = 6,
    };

    [Fact]
    public void Persisted_values_never_change()
    {
        var actual = Enum.GetValues<TagColor>().ToDictionary(c => c, c => (int)c);

        Assert.Equal(PersistedValues, actual);
    }
}
