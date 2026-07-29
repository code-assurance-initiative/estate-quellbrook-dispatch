using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Quellbrook.Dispatch.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> and by the migrations bundle. The connection string comes from the environment, never from
/// the command line, so it does not show up in process listings or CI logs.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DispatchDbContext>
{
    public DispatchDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Dispatch")
            ?? "Host=localhost;Database=dispatch;Username=dispatch";
        var options = new DbContextOptionsBuilder<DispatchDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new DispatchDbContext(options);
    }
}
