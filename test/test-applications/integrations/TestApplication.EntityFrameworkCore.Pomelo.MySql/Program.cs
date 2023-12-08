// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore;
using TestApplication.EntityFrameworkCore.Pomelo.MySql;
using TestApplication.Shared;

ConsoleHelper.WriteSplashScreen(args);

var mySqlPort = GetMySqlPort(args);

var connectionString = $@"Server=127.0.0.1;Port={mySqlPort};Uid=root;Database=TestDatabase";

var serverVersion = ServerVersion.AutoDetect(connectionString);

var contextOptions = new DbContextOptionsBuilder<TestDbContext>()
    .UseMySql(connectionString, serverVersion)
    .Options;

await using (var context = new TestDbContext(contextOptions))
{
    await context.Database.EnsureDeletedAsync();
    await context.Database.EnsureCreatedAsync();
    await context.AddAsync(new TestItem { Name = "TestItem" });
    await context.SaveChangesAsync();
}

await using (var context = new TestDbContext(contextOptions))
{
    foreach (var testItem in context.Set<TestItem>())
    {
        Console.WriteLine($"{testItem.Id} {testItem.Name}");
    }
}

static string GetMySqlPort(string[] args)
{
    if (args.Length > 1)
    {
        return args[1];
    }

    return "3306";
}
