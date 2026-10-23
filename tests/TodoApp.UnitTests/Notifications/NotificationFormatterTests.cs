using System;
using System.Collections.Generic;
using System.Text;
using TodoApp.Features.Notifications.Shared;

namespace TodoApp.UnitTests.Notifications;

public sealed class NotificationFormatterTests
{
    private static readonly DateTime Now = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, "À l'instant")]
    [InlineData(5, "Il y a 5 min")]
    [InlineData(59, "Il y a 59 min")]
    [InlineData(60, "Il y a 1 h")]
    [InlineData(23 * 60, "Il y a 23 h")]
    [InlineData(30 * 60, "Hier")]
    [InlineData(3 * 24 * 60, "Il y a 3 j")]
    public void Ago_returns_relative_label(int minutesAgo, string expected)
    {
        var label = NotificationFormatter.Ago(Now.AddMinutes(-minutesAgo), Now);

        Assert.Equal(expected, label);
    }

    [Fact]
    public void Ago_returns_short_date_after_a_week()
    {
        var label = NotificationFormatter.Ago(new DateTime(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc), Now);

        Assert.Equal("12 oct.", label);
    }

    [Theory]
    [InlineData("Karim Mansour", "KM")]
    [InlineData("toky", "T")]
    [InlineData("Jean Paul Rakoto", "JP")]
    public void Initials_takes_first_letters_of_two_first_words(string name, string expected)
    {
        Assert.Equal(expected, NotificationFormatter.Initials(name));
    }
}
