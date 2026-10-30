using TodoApp.Domain.Tasks;
using TodoApp.Features.Tasks.Shared;

namespace TodoApp.UnitTests.Features.Tasks.Shareds;

public sealed class DueDateFormatTests
{
    private static readonly DateOnly Today = new(2026, 10, 30);

    [Fact]
    public void Same_year_omits_the_year()
    {
        Assert.Equal("31 oct.", DueDateFormat.Short(new DateOnly(2026, 10, 31), Today));
    }

    [Fact]
    public void Previous_year_shows_the_year()
    {
        Assert.Equal("27 déc. 2025", DueDateFormat.Short(new DateOnly(2025, 12, 27), Today));
    }

    [Fact]
    public void Next_year_shows_the_year()
    {
        Assert.Equal("3 janv. 2027", DueDateFormat.Short(new DateOnly(2027, 1, 3), Today));
    }

    [Fact]
    public void Year_boundary_depends_on_today_not_on_distance()
    {
        // Le 31 décembre, le lendemain (1er janvier) affiche son année, même à un jour d'écart.
        var newYearsEve = new DateOnly(2026, 12, 31);

        Assert.Equal("1 janv. 2027", DueDateFormat.Short(new DateOnly(2027, 1, 1), newYearsEve));
    }

    [Fact]
    public void Every_due_status_has_its_own_css_class()
    {
        var classes = Enum.GetValues<DueStatus>().Select(DueDateBadge.CssFor).ToList();

        Assert.Equal(classes.Count, classes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(classes, css => Assert.False(css.Any(char.IsUpper), css));
    }
}
