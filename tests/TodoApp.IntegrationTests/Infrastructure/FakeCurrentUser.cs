using System;
using System.Collections.Generic;
using System.Text;
using TodoApp.Features.Shared;

namespace TodoApp.IntegrationTests.Infrastructure;

internal sealed class FakeCurrentUser(string id): ICurrentUser
{
    public Task<string> GetIdAsync() => Task.FromResult(id);
}
