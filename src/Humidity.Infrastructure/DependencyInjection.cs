using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Humidity.Domain.Interfaces;
using Humidity.Infrastructure.Data;
using Humidity.Infrastructure.Repositories;

namespace Humidity.Infrastructure;

/// <summary>
/// Статический класс для регистрации зависимостей инфраструктурного слоя в DI-контейнере.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет в контейнер сервисы инфраструктуры: контекст базы данных и репозитории.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configuration">Конфигурация приложения (для строки подключения).</param>
    /// <returns>Тот же экземпляр <see cref="IServiceCollection"/> для цепочки вызовов.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Добавление контекста БД с PostgreSQL провайдером
        services.AddDbContext<HumidityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Регистрация репозиториев
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IMeasurementRepository, MeasurementRepository>();

        return services;
    }
}