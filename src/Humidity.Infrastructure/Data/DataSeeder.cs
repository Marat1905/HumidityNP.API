using Microsoft.EntityFrameworkCore;
using Humidity.Domain.Entities;
using Humidity.Domain.Enums;

namespace Humidity.Infrastructure.Data;

/// <summary>
/// Класс для инициализации базы данных тестовыми данными.
/// Заполняет таблицы Vehicle и HumidityMeasurement вымышленными записями
/// для демонстрации работы системы контроля влажности макулатуры.
/// </summary>
public static class DataSeeder
{
    /// <summary>
    /// Выполняет заполнение БД, если она пуста (нет ни одной машины).
    /// </summary>
    public static async Task SeedAsync(HumidityDbContext context)
    {
        // Если в таблице Vehicles уже есть записи, пропускаем инициализацию
        if (await context.Vehicles.AnyAsync())
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        // ==========================================
        // 1. Создаём тестовые машины (Vehicle)
        // ==========================================
        var vehicles = new List<Vehicle>
        {
            new Vehicle
            {
                Number = "Я-9310099848",
                Date = now.AddDays(-3),
                ArrivalDate = now.AddDays(-2).AddHours(-4),
                EntryDate = now.AddDays(-2).AddHours(-2),
                ExitDate = null, // ещё на площадке
                Counterparty = "ООО «Вторресурс»",
                WorkType = "Выгрузка макулатуры",
                VehicleBrand = "КАМАЗ 65115",
                VehiclePlate = "А777ВХ 116",
                Trailer = "ВХ 7777",
                Driver = "Сергеев Пётр Николаевич",
                Loader = "Кузнецов Алексей Викторович",
                Expeditor = "Егорова Марина Сергеевна",
                Department = "Сырьевой цех №3"
            },
            new Vehicle
            {
                Number = "Я-9310099850",
                Date = now.AddDays(-6),
                ArrivalDate = now.AddDays(-5).AddHours(-6),
                EntryDate = now.AddDays(-5).AddHours(-3),
                ExitDate = now.AddDays(-4), // уже выехал
                Counterparty = "АО «ЭкоПак»",
                WorkType = "Забор пробы",
                VehicleBrand = "FAW J6",
                VehiclePlate = "О456КМ 116",
                Trailer = "Т456ХХ 116",
                Driver = "Иванов Алексей Дмитриевич",
                Loader = "Морозов Денис Владимирович",
                Expeditor = "Соколова Ирина Петровна",
                Department = "Лаборатория качества"
            },
            new Vehicle
            {
                Number = "Я-9310099855",
                Date = now.AddHours(-8),
                ArrivalDate = now.AddHours(-6),
                EntryDate = now.AddHours(-4),
                ExitDate = null,
                Counterparty = "ООО «Макулатура Сервис»",
                WorkType = "Приёмка",
                VehicleBrand = "MAN TGS",
                VehiclePlate = "В789ЕК 116",
                Trailer = "Т222ВВ 116",
                Driver = "Волков Иван Сергеевич",
                Loader = "Зайцев Артём Павлович",
                Expeditor = "Новикова Екатерина Владимировна",
                Department = "Склад №7"
            },
            new Vehicle
            {
                Number = "Я-9310099856",
                Date = now.AddDays(-1),
                ArrivalDate = now.AddHours(-10),
                EntryDate = now.AddHours(-8),
                ExitDate = null,
                Counterparty = "ООО «Бумажные технологии»",
                WorkType = "Выгрузка картона",
                VehicleBrand = "Volvo FH",
                VehiclePlate = "О123РР 116",
                Trailer = "РР 9876",
                Driver = "Калинин Андрей Олегович",
                Loader = "Белозёров Сергей Иванович",
                Expeditor = "Федорова Ольга Александровна",
                Department = "Цех переработки №2"
            }
        };

        await context.Vehicles.AddRangeAsync(vehicles);
        await context.SaveChangesAsync();

        // ==========================================
        // 2. Создаём тестовые замеры влажности
        // ==========================================
        var measurements = new List<HumidityMeasurement>
        {
            // Замеры для машины 1 (М-2025-001)
            new HumidityMeasurement
            {
                VehicleId = vehicles[0].Id,
                HumidityValue = 12.4,
                TemperatureC = 21.5,
                MeasurementType = "BLE_Sensor_v2", // может быть null, но заполняем
                Material = "Картон гофрированный",
                Source = MeasurementSource.Auto,
                Timestamp = now.AddDays(-2).AddHours(-1),
                Sign = SignType.None
            },
            new HumidityMeasurement
            {
                VehicleId = vehicles[0].Id,
                HumidityValue = 13.1,
                TemperatureC = 20.8,
                MeasurementType = "BLE_Sensor_v2",
                Material = "Картон гофрированный",
                Source = MeasurementSource.Auto,
                Timestamp = now.AddDays(-2).AddHours(1),
                Sign = SignType.None
            },
            new HumidityMeasurement
            {
                VehicleId = vehicles[0].Id,
                HumidityValue = 11.9,
                TemperatureC = 19.2,
                MeasurementType = "Infrared_Humidity", // другой тип
                Material = "Макулатура смешанная",
                Source = MeasurementSource.Manual, // ручной замер
                Timestamp = now.AddDays(-1).AddHours(-3),
                Sign = SignType.None
            },

            // Замеры для машины 2 (М-2025-002) – уже выехала
            new HumidityMeasurement
            {
                VehicleId = vehicles[1].Id,
                HumidityValue = 8.7,
                TemperatureC = 18.0,
                MeasurementType = "BLE_Sensor_v2",
                Material = "Бумага офисная",
                Source = MeasurementSource.Auto,
                Timestamp = now.AddDays(-5).AddHours(-2),
                Sign = SignType.Less // ниже нормы
            },
            new HumidityMeasurement
            {
                VehicleId = vehicles[1].Id,
                HumidityValue = 15.2,
                TemperatureC = 22.3,
                MeasurementType = "Manual", // можно и null, но пусть будет
                Material = "Журналы",
                Source = MeasurementSource.Manual,
                Timestamp = now.AddDays(-4).AddHours(-10),
                Sign = SignType.Greater // выше нормы
            },

            // Замеры для машины 3 (М-2025-003)
            new HumidityMeasurement
            {
                VehicleId = vehicles[2].Id,
                HumidityValue = 9.8,
                TemperatureC = 20.1,
                MeasurementType = "BLE_Sensor_v3",
                Material = "Картон макулатурный",
                Source = MeasurementSource.Auto,
                Timestamp = now.AddHours(-3),
                Sign = SignType.None
            },

            // Замер для машины 4 (М-2025-004) – с null в MeasurementType и Material (демонстрация)
            new HumidityMeasurement
            {
                VehicleId = vehicles[3].Id,
                HumidityValue = 14.0,
                TemperatureC = 21.0,
                MeasurementType = null, // допустимо
                Material = null,        // допустимо
                Source = MeasurementSource.Manual,
                Timestamp = now.AddHours(-2),
                Sign = SignType.None
            }
        };

        await context.Measurements.AddRangeAsync(measurements);
        await context.SaveChangesAsync();
    }
}