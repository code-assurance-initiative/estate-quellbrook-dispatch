using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quellbrook.Dispatch.Application.Abstractions;
using Quellbrook.Dispatch.Application.Queries;
using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Domain.Fleet;
using Quellbrook.Dispatch.Domain.Routes;
using Quellbrook.Dispatch.Infrastructure.Inbox;
using Quellbrook.Dispatch.Infrastructure.Messaging;
using Quellbrook.Dispatch.Infrastructure.Outbox;
using Quellbrook.Dispatch.Infrastructure.Persistence;
using RabbitMQ.Client;

namespace Quellbrook.Dispatch.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddDispatchInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DispatchDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("Dispatch")));
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<DispatchDbContext>());
        services.AddScoped<IConsignmentRepository, EfConsignmentRepository>();
        services.AddScoped<IRouteRepository, EfRouteRepository>();
        services.AddScoped<IFleetRepository, EfFleetRepository>();
        services.AddScoped<IDispatchQueries, DispatchQueries>();

        services.AddOptions<RabbitMqOptions>()
            .BindConfiguration(RabbitMqOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IConnectionFactory>(provider =>
            RabbitMqConnectionProvider.CreateFactory(provider.GetRequiredService<IOptions<RabbitMqOptions>>()));
        services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
        services.AddSingleton<IOutboxPublisher, RabbitMqPublisher>();
        services.AddOptions<OutboxOptions>()
            .BindConfiguration(OutboxOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddHostedService<OutboxRelay>();
        services.AddSingleton<InboxProcessor>();
        services.AddHostedService<OrderEventsConsumer>();

        services.AddHealthChecks().AddDbContextCheck<DispatchDbContext>("database", tags: ["ready"]);
        return services;
    }
}
