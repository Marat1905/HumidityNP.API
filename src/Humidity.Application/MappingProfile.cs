using AutoMapper;
using Humidity.Application.DTOs;
using Humidity.Domain.Entities;

namespace Humidity.Application;

/// <summary>
/// Профиль конфигурации AutoMapper для преобразования между сущностями и DTO.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ==========================================
        // Маппинг для Vehicle
        // ==========================================
        CreateMap<Vehicle, VehicleDto>()
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date.ToString("O"))) // ISO 8601 формат
            .ForMember(dest => dest.ArrivalDate, opt => opt.MapFrom(src => src.ArrivalDate.ToString("O")))
            .ForMember(dest => dest.EntryDate, opt => opt.MapFrom(src => src.EntryDate.ToString("O")))
            .ForMember(dest => dest.ExitDate, opt => opt.MapFrom(src => src.ExitDate.HasValue ? src.ExitDate.Value.ToString("O") : null));

        CreateMap<CreateVehicleRequest, Vehicle>();

        // При обновлении игнорируем null-значения, чтобы не перезаписывать существующие данные пустотой
        CreateMap<UpdateVehicleRequest, Vehicle>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // ==========================================
        // Маппинг для HumidityMeasurement
        // ==========================================
        CreateMap<HumidityMeasurement, MeasurementDto>()
            .ForMember(dest => dest.Source, opt => opt.MapFrom(src => src.Source.ToString())) // Преобразуем Enum в строку
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp.ToString("O")))
            .ForMember(dest => dest.DisplayValue, opt => opt.MapFrom(src => $"{src.Sign} {src.HumidityValue}%")); // Формируем отображаемое значение

        CreateMap<CreateMeasurementRequest, HumidityMeasurement>();

        // При обновлении игнорируем null-значения
        CreateMap<UpdateMeasurementRequest, HumidityMeasurement>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}