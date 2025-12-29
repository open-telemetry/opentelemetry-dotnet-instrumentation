// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore;

namespace TestApplication.EntityFrameworkCore;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by EF Core.
internal sealed class TestDbContext : DbContext
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. This class is instantiated by EF Core.
{
    public TestDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<TestItem>(
            b =>
            {
                b.Property("Id");
                b.HasKey("Id");
                b.Property(e => e.Name);
            });
    }
}
