using Humidity.Application.DTOs;
using Humidity.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Humidity.Application.Services;

/// <summary>
/// Реализация клиента 1С с использованием HttpClient и ручным формированием SOAP-запроса.
/// </summary>
public class OneCClient : IOneCClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OneCClient> _logger;
    private readonly OneCIntegrationSettings _settings;

    public OneCClient(
        HttpClient httpClient,
        ILogger<OneCClient> logger,
        IOptions<OneCIntegrationSettings> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = options.Value;
    }

    /// <summary>
    /// Выполняет SOAP-запрос к 1С и возвращает распарсенные данные.
    /// </summary>
    public async Task<IEnumerable<OneCVehicleDto>> GetVehiclesAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        // Формируем SOAP-запрос с переданными датами
        var soapRequest = BuildSoapRequest(from, to);

        using var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

        // Отправляем запрос
        var response = await _httpClient.PostAsync(_settings.ServiceUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        // Парсим ответ
        var vehicles = ParseVehicles(responseContent);

        _logger.LogInformation("Из 1С получено {Count} записей за период с {From} по {To}",
            vehicles.Count(), from, to);

        return vehicles;
    }

    /// <summary>
    /// Формирует SOAP-конверт для метода ПолучитьСписокАвто.
    /// </summary>
    private static string BuildSoapRequest(DateTimeOffset from, DateTimeOffset to)
    {
        return $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tns=""http://localhost/WS/Delmhorst"">
   <soapenv:Header/>
   <soapenv:Body>
      <tns:ПолучитьСписокАвто>
         <tns:ДатаС>{from:yyyy-MM-ddTHH:mm:ss}</tns:ДатаС>
         <tns:ДатаПо>{to:yyyy-MM-ddTHH:mm:ss}</tns:ДатаПо>
      </tns:ПолучитьСписокАвто>
   </soapenv:Body>
</soapenv:Envelope>";
    }

    /// <summary>
    /// Парсит XML-ответ 1С и извлекает таблицу с данными машин.
    /// Формат ответа представляет собой JSON-подобную структуру, вложенную в XML-элемент return.
    /// </summary>
    private IEnumerable<OneCVehicleDto> ParseVehicles(string xmlContent)
    {
        // Загружаем XML
        var doc = XDocument.Parse(xmlContent);
        XNamespace ns = "http://localhost/WS/Delmhorst";

        // Ищем элемент return
        var returnElement = doc.Descendants(ns + "return").FirstOrDefault();
        if (returnElement == null)
        {
            throw new InvalidOperationException("В ответе 1С не найден элемент return.");
        }

        // Извлекаем JSON-текст из элемента return
        var jsonText = returnElement.Value.Trim();

        // Десериализуем JSON в промежуточную структуру
        using var document = JsonDocument.Parse(jsonText);
        var root = document.RootElement;
        var valueElement = root.GetProperty("#value");

        // Получаем список колонок и их имена
        var columnElement = valueElement.GetProperty("column");
        var columnNames = new List<string>();
        foreach (var col in columnElement.EnumerateArray())
        {
            var nameProp = col.GetProperty("Name");
            var name = nameProp.GetProperty("#value").GetString();
            columnNames.Add(name!);
        }

        // Получаем строки данных
        var rowElement = valueElement.GetProperty("row");
        var rows = rowElement.EnumerateArray();

        var result = new List<OneCVehicleDto>();

        foreach (var row in rows)
        {
            var items = row.EnumerateArray().ToList();
            if (items.Count != columnNames.Count)
            {
                _logger.LogWarning("Не совпадает количество колонок ({Columns}) и значений ({Values}) в строке.",
                    columnNames.Count, items.Count);
                continue;
            }

            // Составляем словарь значений по именам колонок
            var dict = new Dictionary<string, string?>();
            for (int i = 0; i < columnNames.Count; i++)
            {
                var valueItem = items[i];
                string? val = null;
                if (valueItem.TryGetProperty("#value", out var valProp))
                {
                    val = valProp.GetString();
                }
                dict[columnNames[i]] = val;
            }

            // Извлекаем значения
            var number = dict["НомерПропуска"] ?? string.Empty;
            var date = ParseDateTimeOffset(dict["ДатаПропуска"]);
            var entryDate = ParseDateTimeOffset(dict["ДатаВъезда"]);
            var exitDate = ParseDateTimeOffset(dict["ДатаВыезда"]);
            var vehicleBrand = dict["ТранспортМарка"] ?? string.Empty;
            var vehiclePlate = dict["ТранспортГосНомер"] ?? string.Empty;
            var trailer = dict["Прицеп"] ?? string.Empty;
            var counterparty = dict["Поставщик"] ?? string.Empty;
            var inn = dict["ИННПоставщика"];
            var driver = dict["Водитель"] ?? string.Empty;

            // Проверяем обязательные поля
            if (string.IsNullOrEmpty(number) || date == null || entryDate == null)
            {
                _logger.LogWarning("Пропущена запись с некорректными данными: Номер={Number}, Дата={Date}",
                    number, date);
                continue;
            }

            var dto = new OneCVehicleDto
            {
                Number = number,
                Date = date.Value,
                EntryDate = entryDate.Value,
                ExitDate = exitDate, // если дата = 0001-01-01, то вернётся null
                VehicleBrand = vehicleBrand,
                VehiclePlate = vehiclePlate,
                Trailer = trailer,
                Counterparty = counterparty,
                Inn = inn,
                Driver = driver
            };

            result.Add(dto);
        }

        return result;
    }

    /// <summary>
    /// Преобразует строку даты в DateTimeOffset с предположением UTC.
    /// Если строка пуста или равна "0001-01-01...", возвращает null.
    /// </summary>
    private static DateTimeOffset? ParseDateTimeOffset(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        // В 1С отсутствие даты обозначается как 0001-01-01T00:00:00
        if (value.StartsWith("0001-01-01"))
            return null;

        // Парсим как DateTime и явно указываем Kind = Utc
        if (DateTime.TryParse(value, out var dt))
        {
            var utc = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return new DateTimeOffset(utc, TimeSpan.Zero);
        }

        return null;
    }
}
