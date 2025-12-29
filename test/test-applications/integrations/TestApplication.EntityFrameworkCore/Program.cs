// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TestApplication.EntityFrameworkCore;
using TestApplication.Shared;

ConsoleHelper.WriteSplashScreen(args);

await using var inMemoryDatabase = CreateInMemoryDatabase();

var contextOptions = new DbContextOptionsBuilder<TestDbContext>()
    .UseSqlite(inMemoryDatabase)
    .Options;

await using var connection = RelationalOptionsExtension.Extract(contextOptions).Connection;

await using (var context = new TestDbContext(contextOptions))
{
    await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
    await context.AddAsync(new TestItem { Name = "TestItem" }).ConfigureAwait(false);
    await context.SaveChangesAsync().ConfigureAwait(false);
}

await using (var context = new TestDbContext(contextOptions))
{
    foreach (var testItem in context.Set<TestItem>())
    {
        Console.WriteLine($"{testItem.Id} {testItem.Name}");
    }
}

static DbConnection CreateInMemoryDatabase()
{
    var connection = new SqliteConnection("Filename=:memory:");
    connection.Open();
    return connection;
}
