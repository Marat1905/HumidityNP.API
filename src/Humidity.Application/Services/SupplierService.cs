using AutoMapper;
using Humidity.Application.Interfaces;
using Humidity.Domain.Common;
using Humidity.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Humidity.Application.Services;

public class SupplierService : ISupplierService
{
    private readonly IMeasurementRepository _measurementRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SupplierService> _logger;

    public SupplierService(
        IMeasurementRepository measurementRepository,
        IMapper mapper,
        ILogger<SupplierService> logger)
    {
        _measurementRepository = measurementRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<SupplierDto>> GetSuppliersAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Запрос списка поставщиков за период с {From} по {To}, страница {Page}, размер {Size}",
            from, to, pageNumber, pageSize);

        var result = await _measurementRepository.GetSuppliersSummaryAsync(from, to, pageNumber, pageSize, cancellationToken);

        _logger.LogInformation("Получено {Count} поставщиков из {TotalCount}", result.Items.Count(), result.TotalCount);
        return result;
    }

    public async Task<SupplierDetailsDto> GetSupplierDetailsAsync(
        string inn,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Запрос деталей поставщика с ИНН {Inn} за период с {From} по {To}", inn, from, to);

        var details = await _measurementRepository.GetSupplierDetailsAsync(inn, from, to, cancellationToken);

        _logger.LogInformation("Для поставщика {Inn} получено {VehicleCount} машин", inn, details.Vehicles.Count);
        return details;
    }

    /// <summary>
    /// Получить топ-N поставщиков по средней влажности за период.
    /// </summary>
    public async Task<IEnumerable<SupplierDto>> GetTopSuppliersAsync(
        int top,
        bool ascending,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Запрос топ-{Top} поставщиков за период с {From} по {To}, сортировка: {Ascending}",
            top, from, to, ascending ? "по возрастанию (хорошие)" : "по убыванию (плохие)");

        var result = await _measurementRepository.GetTopSuppliersAsync(top, ascending, from, to, cancellationToken);
        _logger.LogInformation("Получено {Count} поставщиков", result.Count());
        return result;
    }
}