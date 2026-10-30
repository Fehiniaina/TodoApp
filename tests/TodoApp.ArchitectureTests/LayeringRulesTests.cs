using System.Reflection;
using System.Text.RegularExpressions;
using NetArchTest.Rules;

namespace TodoApp.ArchitectureTests;

public sealed class LayeringRulesTests
{
    // Chargement par nom : pas besoin d'InternalsVisibleTo pour référencer un type interne.
    private static readonly Assembly App = Assembly.Load("TodoApp");

    [Fact]
    public void Domain_does_not_depend_on_infrastructure_or_features()
    {
        var result = Types.InAssembly(App)
            .That().ResideInNamespace("TodoApp.Domain")
            .ShouldNot().HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "TodoApp.Data",
                "TodoApp.Infrastructure",
                "TodoApp.Features",
                "TodoApp.Components")
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Handwritten_migration_code_does_not_use_domain_enum_members()
    {
        // Leçon des pièges 2 et 3 : une migration est une photo FIGÉE. Elle n'utilise jamais
        // un MEMBRE d'enum du domaine (TaskType.Task, (int)TaskItemStatus.Resolved) : un renommage ou
        // un réordonnancement changerait son sens sans erreur. Les valeurs s'écrivent en dur.
        // Le TYPE seul reste autorisé (AlterColumn<TaskPriority> généré pour les enums PostgreSQL) :
        // un renommage du type casse la compilation, ce qui est bruyant donc sûr.
        var domainEnums = App.GetTypes()
            .Where(t => t.IsEnum && t.Namespace?.StartsWith("TodoApp.Domain", StringComparison.Ordinal) == true)
            .Select(t => Regex.Escape(t.Name))
            .ToList();

        var memberAccess = new Regex(
            $@"\b(?:{string.Join('|', domainEnums)})\.[A-Z]\w*",
            RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));

        var migrationsDirectory = Path.Combine(SolutionRoot(), "src", "TodoApp", "Data", "Migrations");

        var offenders = Directory.EnumerateFiles(migrationsDirectory, "*.cs")
            .Where(path => !path.EndsWith(".Designer.cs", StringComparison.Ordinal))
            .Where(path => !path.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal))
            .Select(path => (File: Path.GetFileName(path), Match: memberAccess.Match(File.ReadAllText(path))))
            .Where(x => x.Match.Success)
            .Select(x => $"{x.File} : {x.Match.Value}")
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Handlers_are_internal_and_sealed()
    {
        var result = Types.InAssembly(App)
            .That().ResideInNamespace("TodoApp.Features")
            .And().AreClasses()
            .And().HaveNameEndingWith("Handler", StringComparison.Ordinal)
            .Should().BeSealed()
            .And().NotBePublic()
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Specifications_are_sealed()
    {
        var result = Types.InAssembly(App)
            .That().AreClasses()
            .And().HaveNameEndingWith("Specification", StringComparison.Ordinal)
            .And().AreNotAbstract()
            .Should().BeSealed()
            .GetResult();

        AssertSuccessful(result);
    }

    private static void AssertSuccessful(TestResult result) =>
        Assert.True(
            result.IsSuccessful,
            "Types en infraction : " + string.Join(", ", result.FailingTypeNames ?? []));

    /// <summary>Remonte depuis le dossier de sortie des tests jusqu'au fichier de solution.</summary>
    private static string SolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !directory.EnumerateFiles("*.sln*").Any())
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Racine de la solution introuvable depuis " + AppContext.BaseDirectory);
    }
}
