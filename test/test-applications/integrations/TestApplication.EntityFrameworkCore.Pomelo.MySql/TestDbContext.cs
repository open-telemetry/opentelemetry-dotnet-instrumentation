// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore;

namespace TestApplication.EntityFrameworkCore.Pomelo.MySql;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestItem>(
            b =>
            {
                b.Property("Id");
                b.HasKey("Id");
                b.Property(e => e.Name);
            });
    }
}
