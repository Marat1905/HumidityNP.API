using AutoMapper;
using Humidity.Application.DTOs;
using Humidity.Domain.Entities;

namespace Humidity.Application;

/// <summary>
/// Профиль конфигурации AutoMapper для преобразования между сущностями доменного слоя (Domain)
/// и DTO-объектами, используемыми в слое приложения (Application) и на уровне API.
/// AutoMapper автоматически умеет маппить коллекции (List, IEnumerable и т.п.),
/// если настроен поэлементный маппинг, поэтому отдельные правила для коллекций не требуются.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ==========================================
        // Маппинг для Vehicle
        // ==========================================

        // Сущность Vehicle -> DTO VehicleDto
        // Прямой маппинг DateTime в DateTime.
        // Сериализатор JSON (System.Text.Json) автоматически отдаст формат ISO 8601 на границе API.
        CreateMap<Vehicle, VehicleDto>();

        // DTO CreateVehicleRequest -> Сущность Vehicle
        // Используется при создании новой записи о машине.
        CreateMap<CreateVehicleRequest, Vehicle>();

        // DTO UpdateVehicleRequest -> Сущность Vehicle
        // При обновлении игнорируем null-значения, чтобы не перезаписывать существующие данные пустотой.
        // Это позволяет выполнять частичное обновление (partial update) без потери данных.
        CreateMap<UpdateVehicleRequest, Vehicle>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // ==========================================
        // Маппинг для HumidityMeasurement
        // ==========================================

        // Сущность HumidityMeasurement -> DTO MeasurementDto
        // Дополнительно формируем вычисляемые поля:
        // - Source: преобразуем Enum MeasurementSource в строку для удобного отображения на клиенте.
        // - DisplayValue: формируем человекочитаемое отображаемое значение влажности с учётом знака.
        CreateMap<HumidityMeasurement, MeasurementDto>()
            .ForMember(dest => dest.Source, opt => opt.MapFrom(src => src.Source.ToString()))
            .ForMember(dest => dest.DisplayValue, opt => opt.MapFrom(src => $"{src.Sign} {src.HumidityValue}%"));

        // DTO CreateMeasurementRequest -> Сущность HumidityMeasurement
        // Используется при создании нового замера влажности.
        CreateMap<CreateMeasurementRequest, HumidityMeasurement>();

        // DTO UpdateMeasurementRequest -> Сущность HumidityMeasurement
        // При обновлении игнорируем null-значения, чтобы не перезаписывать существующие данные пустотой.
        // Это позволяет выполнять частичное обновление (partial update) без потери данных.
        CreateMap<UpdateMeasurementRequest, HumidityMeasurement>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}