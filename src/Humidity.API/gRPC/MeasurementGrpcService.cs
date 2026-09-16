using Grpc.Core;
using Humidity.Contracts.Protos;
using Humidity.Domain.Enums;
using Humidity.Domain.Interfaces;

namespace Humidity.API.gRPC;

/// <summary>
/// gRPC-сервер статистики по смене. Используется Notification.Service-ом
/// для получения данных при формировании письма.
/// </summary>
public class MeasurementGrpcService : MeasurementGrpc.MeasurementGrpcBase
{
    private readonly IMeasurementRepository _repo;
    private readonly ILogger<MeasurementGrpcService> _logger;

    public MeasurementGrpcService(
        IMeasurementRepository repo,
        ILogger<MeasurementGrpcService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public override async Task<GetShiftStatisticsResponse> GetShiftStatistics(
        GetShiftStatisticsRequest request,
        ServerCallContext context)
    {
        var from = DateTimeOffset.Parse(request.FromDate).ToUniversalTime();
        var to = DateTimeOffset.Parse(request.ToDate).ToUniversalTime();

        _logger.LogInformation("gRPC GetShiftStatistics: {From} — {To}", from, to);

        // Получаем «сырые» замеры, попавшие в диапазон по времени выезда машины.
        // Это соответствует семантике смены: все замеры машины привязаны к смене,
        // в которую машина выехала.
        var paged = await _repo.GetByVehicleExitDateRangePagedAsync(
            from, to,
            pageNumber: 1,
            pageSize: 20000,
            sortDescending: true,
            cancellationToken: context.CancellationToken);

        var response = new GetShiftStatisticsResponse();

        if (paged.Items == null || !paged.Items.Any())
        {
            return response;
        }

        // Группировка по машине.
        var grouped = paged.Items
            .GroupBy(m => m.VehicleId)
            .Select(g =>
            {
                var first = g.First();
                return new VehicleHumidityDto
                {
                    VehicleId = g.Key.ToString(),
                    Number = first.Vehicle?.Number ?? string.Empty,
                    VehiclePlate = first.Vehicle?.VehiclePlate ?? string.Empty,
                    Counterparty = first.Vehicle?.Counterparty ?? string.Empty,
                    MeasurementsCount = g.Count(),
                    AverageHumidity = g.Average(m => m.HumidityValue),
                    MinHumidity = g.Min(m => m.HumidityValue),
                    MaxHumidity = g.Max(m => m.HumidityValue)
                };
            })
            .OrderBy(v => v.Number)
            .ToList();

        response.Vehicles.AddRange(grouped);
        response.TotalMeasurements = paged.Items.Count();
        response.OverallAverage = paged.Items.Average(m => m.HumidityValue);
        response.OverallMin = paged.Items.Min(m => m.HumidityValue);
        response.OverallMax = paged.Items.Max(m => m.HumidityValue);

        return response;
    }
}