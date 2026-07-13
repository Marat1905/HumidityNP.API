using Microsoft.EntityFrameworkCore;
using Humidity.Domain.Entities;
using Humidity.Domain.Enums;

namespace Humidity.Infrastructure.Data;

/// <summary>
/// Класс для инициализации базы данных тестовыми данными.
/// </summary>
public static class DataSeeder
{
    /// <summary>
    /// Выполняет заполнение БД, если она пуста.
    /// </summary>
    public static async Task SeedAsync(HumidityDbContext context)
    {
        // Проверяем, есть ли уже данные, чтобы не дублировать их при каждом перезапуске
        if (await context.Vehicles.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;

        // 1. Создаем тестовые машины
        var vehicles = new List<Vehicle>
        {
            new Vehicle
            {
                Number = "Я-9310099848",
                Date = now.AddDays(-2),
                ArrivalDate = now.AddDays(-1).AddHours(-2),
                EntryDate = now.AddDays(-1).AddHours(-1),
                ExitDate = null, // Машина все еще на площадке (активная)
                Counterparty = "Тандер (Сургут)",
                WorkType = "Разгрузка",
                VehicleBrand = "KAMAZ",
                VehiclePlate = "А123БВ777",
                Trailer = "Т789ХУ777",
                Driver = "Иванов Иван Иванович",
                Loader = "Петров Петр Петрович",
                Expeditor = "Сидоров Сидор Сидорович",
                Department = "Склад сыпучих материалов №1"
            },
            new Vehicle
            {
                Number = "Я-9310099850",
                Date = now.AddDays(-5),
                ArrivalDate = now.AddDays(-4).AddHours(-3),
                EntryDate = now.AddDays(-4).AddHours(-2),
                ExitDate = now.AddDays(-3), // Машина уже уехала
                Counterparty = "ООО СтройТранс",
                WorkType = "Погрузка",
                VehicleBrand = "FAW",
                VehiclePlate = "О456КМ77",
                Trailer = "Т111АА77",
                Driver = "Смирнов Алексей Дмитриевич",
                Loader = "Кузнецов Олег Викторович",
                Expeditor = "Морозова Анна Сергеевна",
                Department = "Склад готовой продукции"
            },
            new Vehicle
            {
                Number = "Я-9310099855",
                Date = now.AddHours(-5),
                ArrivalDate = now.AddHours(-4),
                EntryDate = now.AddHours(-3),
                ExitDate = null, // Активная машина
                Counterparty = "ЗерноТрейд",
                WorkType = "Отбор проб",
                VehicleBrand = "MAN",
                VehiclePlate = "В789ЕК799",
                Trailer = "Т222ВВ799",
                Driver = "Волков Дмитрий Николаевич",
                Loader = "Зайцев Игорь Павлович",
                Expeditor = "Новикова Елена Владимировна",
                Department = "Лаборатория"
            }
        };

        await context.Vehicles.AddRangeAsync(vehicles);
        await context.SaveChangesAsync(); // Сохраняем, чтобы сгенерировались Id и CreatedAt

        // 2. Создаем тестовые замеры влажности для первой и третьей машины
        var measurements = new List<HumidityMeasurement>
        {
            // Замеры для первой машины (KAMAZ)
            new HumidityMeasurement
            {
                VehicleId = vehicles[0].Id,
                HumidityValue = 14.5,
                TemperatureC = 22.1,
                MeasurementType = "BLE_Sensor_v2",
                Material = "Зерно пшеницы",
                Source = MeasurementSource.Auto,
                Timestamp = now.AddDays(-1).AddHours(-0.5),
                Sign = "None"
            },
            new HumidityMeasurement
            {
                VehicleId = vehicles[0].Id,
                HumidityValue = 14.2,
                TemperatureC = 21.8,
                MeasurementType = "BLE_Sensor_v2",
                Material = "Зерно пшеницы",
                Source = MeasurementSource.Auto,
                Timestamp = now.AddHours(-1),
                Sign = "Less"
            },
            // Ручной замер для третьей машины (MAN)
            new HumidityMeasurement
            {
                VehicleId = vehicles[2].Id,
                HumidityValue = 16.0,
                TemperatureC = 19.5,
                MeasurementType = "Manual_Check",
                Material = "Ячмень",
                Source = MeasurementSource.Manual,
                Timestamp = now.AddHours(-2),
                Sign = "Greater"
            }
        };

        await context.Measurements.AddRangeAsync(measurements);
        await context.SaveChangesAsync();
    }
}