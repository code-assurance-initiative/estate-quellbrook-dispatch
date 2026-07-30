using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Quellbrook.Dispatch.Application.Abstractions;
using Quellbrook.Dispatch.Application.Intake;
using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Domain.Fleet;
using Quellbrook.Dispatch.Domain.Routes;
using Quellbrook.Dispatch.Infrastructure.Persistence;

namespace Quellbrook.Dispatch.UnitTests.TestSupport;

/// <summary>A SQLite database and a service provider with the real repositories over it, one scope per call.</summary>
internal sealed class DbFixture : IDisposable
{
    private readonly SqliteDispatchDb _db = new();

    public DbFixture()
    {
        Services = new ServiceCollection()
            .AddScoped(_ => _db.CreateContext())
            .AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<DispatchDbContext>())
            .AddScoped<IConsignmentRepository, EfConsignmentRepository>()
            .AddScoped<IRouteRepository, EfRouteRepository>()
            .AddScoped<IFleetRepository, EfFleetRepository>()
            .AddSingleton<TimeProvider>(Time)
            .AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>))
            .AddScoped<OrderPlacedHandler>()
            .BuildServiceProvider();
    }

    public FakeTimeProvider Time { get; } = new(DispatchData.Now);

    public ServiceProvider Services { get; }

    public DispatchDbContext Context() => _db.CreateContext();

    public async Task<T> SeedAsync<T>(Func<DispatchDbContext, T> seed)
    {
        using var context = Context();
        var value = seed(context);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return value;
    }

    public void Dispose()
    {
        Services.Dispose();
        _db.Dispose();
    }
}
