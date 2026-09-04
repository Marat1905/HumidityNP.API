using Humidity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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

            // Конфигурация нового поля OneCGuid с уникальным индексом для контроля уникальности на уровне БД.
            // IsUnicode(false) оптимизирует хранение, так как GUID содержит только латинские символы и цифры.
            entity.Property(e => e.OneCGuid).HasMaxLength(50).IsUnicode(false);
            entity.HasIndex(e => e.OneCGuid).IsUnique();

            entity.Property(e => e.Counterparty).HasMaxLength(200);
            entity.Property(e => e.Inn).HasMaxLength(12); // ИНН может быть 10 или 12 символов
            entity.Property(e => e.VehicleBrand).HasMaxLength(100);
            entity.Property(e => e.VehiclePlate).HasMaxLength(20);
            entity.Property(e => e.Trailer).HasMaxLength(20);
            entity.Property(e => e.Driver).HasMaxLength(200);

            // Новые поля для разгрузки
            entity.Property(e => e.StackNumber).HasMaxLength(50);

            entity.HasMany(e => e.Measurements)
                  .WithOne(m => m.Vehicle)
                  .HasForeignKey(m => m.VehicleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Number);
            entity.HasIndex(e => e.VehiclePlate);
            // Индекс для StackNumber (опционально)
            entity.HasIndex(e => e.StackNumber);
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