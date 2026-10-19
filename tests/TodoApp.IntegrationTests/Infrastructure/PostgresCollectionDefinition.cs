using System;
using System.Collections.Generic;
using System.Text;

namespace TodoApp.IntegrationTests.Infrastructure;

/// <summary>xUnit collection that shares one <see cref="PostgresFixture"/> across all integration tests.</summary>
[CollectionDefinition("Postgres")]
public class PostgresCollectionDefinition : ICollectionFixture<PostgresFixture>
{
}
