using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quellbrook.Dispatch.Domain.Consignments;
using Quellbrook.Dispatch.Domain.Fleet;
using Quellbrook.Dispatch.Domain.Routes;

namespace Quellbrook.Dispatch.Infrastructure.Persistence;

internal sealed class ConsignmentConfiguration : IEntityTypeConfiguration<Consignment>
{
    public void Configure(EntityTypeBuilder<Consignment> builder)
    {
        builder.ToTable("consignments");
        builder.HasKey(consignment => consignment.Id);
        builder.Property(consignment => consignment.Id).HasConversion(id => id.Value, value => new ConsignmentId(value));
        builder.Property(consignment => consignment.RouteId).HasConversion(new ValueConverter<RouteId, Guid>(id => id.Value, value => new RouteId(value)));
        builder.Property(consignment => consignment.ServiceLevel).HasConversion<string>().HasMaxLength(16);
        builder.Property(consignment => consignment.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(consignment => consignment.Proof).HasConversion<string>().HasMaxLength(16);
        builder.Property(consignment => consignment.CountryCode).HasMaxLength(2);
        builder.Property(consignment => consignment.PostalCode).HasMaxLength(10);
        builder.Property(consignment => consignment.Zone).HasMaxLength(16);
        builder.HasIndex(consignment => consignment.OrderId).IsUnique();
        builder.HasIndex(consignment => consignment.RouteId);
        builder.Ignore(consignment => consignment.DomainEvents);
    }
}

internal sealed class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.ToTable("routes");
        builder.HasKey(route => route.Id);
        builder.Property(route => route.Id).HasConversion(id => id.Value, value => new RouteId(value));
        builder.Property(route => route.DriverId).HasConversion(id => id.Value, value => new DriverId(value));
        builder.Property(route => route.VehicleId).HasConversion(id => id.Value, value => new VehicleId(value));
        builder.Property(route => route.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(route => route.Depot).HasMaxLength(16);
        builder.Property(route => route.Zone).HasMaxLength(16);
        builder.HasIndex(route => new { route.ServiceDate, route.Zone });
        builder.HasIndex(route => new { route.DriverId, route.ServiceDate }).IsUnique();
        builder.OwnsMany(route => route.Stops, stops =>
        {
            stops.ToTable("route_stops");
            stops.WithOwner().HasForeignKey("RouteId");
            stops.Property(stop => stop.ConsignmentId).HasConversion(id => id.Value, value => new ConsignmentId(value));
            stops.HasKey("RouteId", nameof(RouteStop.ConsignmentId));
        });
        builder.Navigation(route => route.Stops).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(route => route.DomainEvents);
        builder.Ignore(route => route.LoadGrams);
    }
}

internal sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("drivers");
        builder.HasKey(driver => driver.Id);
        builder.Property(driver => driver.Id).HasConversion(id => id.Value, value => new DriverId(value));
        builder.Property(driver => driver.DisplayName).HasMaxLength(40);
        builder.Property(driver => driver.Depot).HasMaxLength(16);
        builder.Property(driver => driver.Licence).HasConversion<string>().HasMaxLength(4);
        builder.Ignore(driver => driver.ShiftHours);
    }
}

internal sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");
        builder.HasKey(vehicle => vehicle.Id);
        builder.Property(vehicle => vehicle.Id).HasConversion(id => id.Value, value => new VehicleId(value));
        builder.Property(vehicle => vehicle.Registration).HasMaxLength(16);
        builder.Property(vehicle => vehicle.Depot).HasMaxLength(16);
        builder.Property(vehicle => vehicle.Kind).HasConversion<string>().HasMaxLength(8);
        builder.HasIndex(vehicle => vehicle.Registration).IsUnique();
        builder.Ignore(vehicle => vehicle.RequiredLicence);
    }
}
