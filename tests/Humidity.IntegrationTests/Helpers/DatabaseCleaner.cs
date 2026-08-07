using Humidity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Humidity.IntegrationTests.Helpers;

public static class DatabaseCleaner
{
    public static async Task CleanDatabaseAsync(HumidityDbContext context)
    {
        // Удаляем все данные (сначала дочерние таблицы)
        await context.Measurements.ExecuteDeleteAsync();
        await context.Vehicles.ExecuteDeleteAsync();

        // Отсоединяем все отслеживаемые сущности
        foreach (var entry in context.ChangeTracker.Entries())
        {
            entry.State = EntityState.Detached;
        }
    }
}