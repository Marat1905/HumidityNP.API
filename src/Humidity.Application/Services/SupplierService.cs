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
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Запрос списка поставщиков за период с {From} по {To}, страница {Page}, размер {Size}, поиск '{Search}'",
            from, to, pageNumber, pageSize, search ?? "<нет>");

        var result = await _measurementRepository.GetSuppliersSummaryAsync(
            from, to, pageNumber, pageSize, search, cancellationToken);

        _logger.LogInformation("Получено {Count} поставщиков из {TotalCount}", result.Items.Count(), result.TotalCount);
        return result;
    }

    /// <summary>
    /// Получить детальную информацию по поставщику (ИНН) за период.
    /// Пагинация и сортировка выполняются на стороне сервера.
    /// </summary>
    public async Task<SupplierDetailsDto> GetSupplierDetailsAsync(
        string inn,
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
        bool sortDescending,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Запрос деталей поставщика с ИНН {Inn} за период с {From} по {To}, страница {Page}, размер {Size}, сортировка {Order}",
            inn, from, to, pageNumber, pageSize, sortDescending ? "по убыванию" : "по возрастанию");

        var details = await _measurementRepository.GetSupplierDetailsAsync(
            inn, from, to, pageNumber, pageSize, sortDescending, cancellationToken);

        _logger.LogInformation(
            "Для поставщика {Inn} получено {VehicleCount} машин на странице (всего {TotalCount})",
            inn, details.Vehicles.Items.Count(), details.Vehicles.TotalCount);
        return details;
    }

    /// <summary>
    /// Получить полный список машин поставщика за период для построения графика.
    /// Пагинация не применяется, сортировка выполняется на стороне сервера.
    /// </summary>
    public async Task<IEnumerable<SupplierVehicleSummaryDto>> GetSupplierVehiclesForChartAsync(
        string inn,
        DateTimeOffset from,
        DateTimeOffset to,
        bool sortDescending,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Запрос машин для графика поставщика с ИНН {Inn} за период с {From} по {To}, сортировка {Order}",
            inn, from, to, sortDescending ? "по убыванию" : "по возрастанию");

        var result = await _measurementRepository.GetSupplierVehiclesForChartAsync(
            inn, from, to, sortDescending, cancellationToken);

        _logger.LogInformation("Для графика поставщика {Inn} получено {Count} машин", inn, result.Count());
        return result;
    }

    /// <summary>
    /// Получить топ-N поставщиков по средней влажности за период с байесовской коррекцией.
    /// </summary>
    public async Task<IEnumerable<SupplierDto>> GetTopSuppliersAsync(
        int top,
        bool ascending,
        DateTimeOffset from,
        DateTimeOffset to,
        double priorWeight,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Запрос топ-{Top} поставщиков за период с {From} по {To}, сортировка: {Ascending}, priorWeight={PriorWeight}",
            top, from, to,
            ascending ? "по возрастанию (хорошие)" : "по убыванию (плохие)",
            priorWeight);

        var result = await _measurementRepository.GetTopSuppliersAsync(
            top, ascending, from, to, priorWeight, cancellationToken);

        _logger.LogInformation("Получено {Count} поставщиков (байесовская коррекция: priorWeight={PriorWeight})",
            result.Count(), priorWeight);
        return result;
    }
}