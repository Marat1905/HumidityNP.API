using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Humidity.Domain.Entities;
using Humidity.Domain.Enums;

namespace Humidity.Infrastructure.Data;

/// <summary>
/// Контекст базы данных для работы с машинами и замерами влажности.
/// Использует PostgreSQL, автоматически управляет временными метками.
/// </summary>
public class HumidityDbContext : DbContext
{
    public HumidityDbContext(DbContextOptions<HumidityDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Набор сущностей машин.
    /// </summary>
    public DbSet<Vehicle> Vehicles { get; set; }

    /// <summary>
    /// Набор сущностей замеров влажности.
    /// </summary>
    public DbSet<HumidityMeasurement> Measurements { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Конфигурация Vehicle
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Number).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Counterparty).HasMaxLength(200);
            entity.Property(e => e.WorkType).HasMaxLength(100);
            entity.Property(e => e.VehicleBrand).HasMaxLength(100);
            entity.Property(e => e.VehiclePlate).HasMaxLength(20);
            entity.Property(e => e.Trailer).HasMaxLength(20);
            entity.Property(e => e.Driver).HasMaxLength(200);
            entity.Property(e => e.Loader).HasMaxLength(200);
            entity.Property(e => e.Expeditor).HasMaxLength(200);
            entity.Property(e => e.Department).HasMaxLength(100);

            entity.HasMany(e => e.Measurements)
                  .WithOne(m => m.Vehicle)
                  .HasForeignKey(m => m.VehicleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Number);
            entity.HasIndex(e => e.VehiclePlate);
        });

        // Конфигурация HumidityMeasurement
        modelBuilder.Entity<HumidityMeasurement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MeasurementType).HasMaxLength(50);
            entity.Property(e => e.Material).HasMaxLength(100);

            // Храним перечисления как строки в базе данных для читаемости
            entity.Property(e => e.Source)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(e => e.Sign)
                .HasConversion<string>()
                .HasMaxLength(10);

            entity.HasIndex(e => e.VehicleId);
            entity.HasIndex(e => e.Timestamp);
        });

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entityEntry in entries)
        {
            if (entityEntry.State == EntityState.Added)
            {
                entityEntry.Entity.CreatedAt = DateTimeOffset.UtcNow;
            }

            entityEntry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}