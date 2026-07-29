using Microsoft.EntityFrameworkCore;
using Quellbrook.Dispatch.Infrastructure.Persistence;

namespace Quellbrook.Dispatch.UnitTests.Infrastructure;

public sealed class DesignTimeDbContextFactoryTests
{
    [Fact]
    public void TheDesignTimeContextTargetsPostgreSQL()
    {
        using var context = new DesignTimeDbContextFactory().CreateDbContext([]);

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
    }
}
