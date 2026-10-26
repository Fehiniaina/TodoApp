using System;
using System.Collections.Generic;
using System.Text;

namespace TodoApp.E2ETests.Infrastructure;

/// <summary>
/// Regroupe tous les tests E2E dans une même collection xUnit :
/// un seul serveur et une seule base, tests exécutés en série.
/// </summary>
[CollectionDefinition(Name)]
public sealed class E2EGroup : ICollectionFixture<E2EAppFixture>
{
    public const string Name = "E2E";
}
