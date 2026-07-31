using Microsoft.Extensions.DependencyInjection;
using Quellbrook.Dispatch.Application.Assignment;
using Quellbrook.Dispatch.Application.Fleet;
using Quellbrook.Dispatch.Application.Intake;
using Quellbrook.Dispatch.Application.Routes;
using Quellbrook.Dispatch.Domain.Assignment;

namespace Quellbrook.Dispatch.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddDispatchApplication(this IServiceCollection services)
    {
        services.AddSingleton(new StandardAssignmentPolicy());
        services.AddScoped<OrderPlacedHandler>();
        services.AddScoped<AssignConsignmentHandler>();
        services.AddScoped<PlanRouteHandler>();
        services.AddScoped<StartRouteHandler>();
        services.AddScoped<RecordDeliveryHandler>();
        services.AddScoped<RegisterFleetHandlers>();
        return services;
    }
}
