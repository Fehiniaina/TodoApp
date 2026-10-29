using Microsoft.EntityFrameworkCore;
using Npgsql.NameTranslation;
using TodoApp.Domain.Tasks;
using TodoApp.Domain.Tasks.Recurrence;
using TodoApp.IntegrationTests.Infrastructure;

namespace TodoApp.IntegrationTests.Data;

[Collection("Postgres")]
public sealed class PostgresEnumContractTests(PostgresFixture fixture)
{
    // Même traducteur que Npgsql par défaut pour MapEnum : Low → low, InValidation → in_validation.
    private static readonly NpgsqlSnakeCaseNameTranslator Translator = new();

    [Fact]
    public Task TaskPriority_matches_database_enum() => AssertMatches<TaskPriority>("task_priority");

    [Fact]
    public Task RecurrenceFrequency_matches_database_enum() => AssertMatches<RecurrenceFrequency>("recurrence_frequency");

    /// <summary>
    /// Renommer un membre C# sans « ALTER TYPE … RENAME VALUE » rend illisibles les lignes existantes.
    /// Ce test échoue dès que le code et le schéma divergent.
    /// </summary>
    private async Task AssertMatches<TEnum>(string postgresType) where TEnum : struct, Enum
    {
        await using var db = fixture.CreateDbContext();

        var databaseLabels = await db.Database
            .SqlQuery<string>($"""
                SELECT e.enumlabel AS "Value"
                FROM pg_type t
                JOIN pg_enum e ON e.enumtypid = t.oid
                WHERE t.typname = {postgresType}
                ORDER BY e.enumlabel
                """)
            .ToListAsync();

        var codeLabels = Enum.GetNames<TEnum>()
            .Select(Translator.TranslateMemberName)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(codeLabels, databaseLabels);
    }
}
