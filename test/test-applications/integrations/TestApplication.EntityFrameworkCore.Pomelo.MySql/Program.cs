// <copyright file="Program.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using Microsoft.EntityFrameworkCore;
using TestApplication.EntityFrameworkCore.Pomelo.MySql;
using TestApplication.Shared;

ConsoleHelper.WriteSplashScreen(args);

var mySqlPort = GetMySqlPort(args);

var connectionString = $@"Server=127.0.0.1;Port={mySqlPort};Uid=root;Database=TestDatabase";

var serverVersion = ServerVersion.AutoDetect(connectionString);

var contextOptions = new DbContextOptionsBuilder<TestContext>()
    .UseMySql(connectionString, serverVersion)
    .Options;

await using (var context = new TestContext(contextOptions))
{
    await context.Database.EnsureDeletedAsync();
    await context.Database.EnsureCreatedAsync();
    await context.AddAsync(new TestItem { Name = "TestItem" });
    await context.SaveChangesAsync();
}

await using (var context = new TestContext(contextOptions))
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
