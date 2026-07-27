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
    /// ВНИМАНИЕ: Исправлена ошибка KeyNotFoundException путём безопасного получения свойств через TryGetProperty.
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
            _logger.LogError("В ответе 1С не найден элемент return. XML: {XmlContent}", xmlContent);
            throw new InvalidOperationException("В ответе 1С не найден элемент return.");
        }

        // Извлекаем JSON-текст из элемента return
        var jsonText = returnElement.Value.Trim();

        // Проверяем, не пустой ли JSON
        if (string.IsNullOrEmpty(jsonText))
        {
            _logger.LogError("Элемент return пуст. XML: {XmlContent}", xmlContent);
            throw new InvalidOperationException("Элемент return пуст.");
        }

        // Десериализуем JSON в промежуточную структуру
        using var document = JsonDocument.Parse(jsonText);
        var root = document.RootElement;

        // Безопасно получаем свойство "#value" (используем TryGetProperty вместо GetProperty)
        if (!root.TryGetProperty("#value", out var valueElement))
        {
            _logger.LogError("В JSON-ответе отсутствует свойство #value. JSON: {JsonText}", jsonText);
            throw new InvalidOperationException("Некорректный формат ответа от 1С: отсутствует #value.");
        }

        // Получаем список колонок и их имена
        if (!valueElement.TryGetProperty("column", out var columnElement))
        {
            _logger.LogError("В JSON-ответе отсутствует свойство column. JSON: {JsonText}", jsonText);
            throw new InvalidOperationException("Некорректный формат ответа от 1С: отсутствует column.");
        }

        var columnNames = new List<string>();
        foreach (var col in columnElement.EnumerateArray())
        {
            // Безопасно получаем имя колонки
            if (col.TryGetProperty("Name", out var nameProp) &&
                nameProp.TryGetProperty("#value", out var nameValue))
            {
                columnNames.Add(nameValue.GetString() ?? string.Empty);
            }
            else
            {
                _logger.LogWarning("Пропущена колонка без имени: {Col}", col);
            }
        }

        // Если не удалось получить имена колонок – возвращаем пустой результат
        if (columnNames.Count == 0)
        {
            _logger.LogWarning("Не найдено ни одной колонки в ответе 1С.");
            return Enumerable.Empty<OneCVehicleDto>();
        }

        // Получаем строки данных
        if (!valueElement.TryGetProperty("row", out var rowElement))
        {
            // Если строк нет – это не ошибка, просто возвращаем пустой список
            _logger.LogInformation("В ответе 1С нет строк (row).");
            return Enumerable.Empty<OneCVehicleDto>();
        }

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

            // Извлекаем значения с проверкой наличия ключей (используем GetValueOrDefault)
            // Номер пропуска обязателен
            if (!dict.TryGetValue("НомерПропуска", out var number) || string.IsNullOrEmpty(number))
            {
                _logger.LogWarning("Пропущена строка без номера пропуска.");
                continue;
            }

            var date = ParseDateTimeOffset(dict.GetValueOrDefault("ДатаПропуска"));
            var entryDate = ParseDateTimeOffset(dict.GetValueOrDefault("ДатаВъезда"));

            // Дата пропуска и дата въезда обязательны
            if (date == null || entryDate == null)
            {
                _logger.LogWarning("Пропущена строка с некорректной датой: Номер={Number}", number);
                continue;
            }

            var dto = new OneCVehicleDto
            {
                Number = number,
                Date = date.Value,
                EntryDate = entryDate.Value,
                ExitDate = ParseDateTimeOffset(dict.GetValueOrDefault("ДатаВыезда")),
                VehicleBrand = dict.GetValueOrDefault("ТранспортМарка") ?? string.Empty,
                VehiclePlate = dict.GetValueOrDefault("ТранспортГосНомер") ?? string.Empty,
                Trailer = dict.GetValueOrDefault("Прицеп") ?? string.Empty,
                Counterparty = dict.GetValueOrDefault("Поставщик") ?? string.Empty,
                Inn = dict.GetValueOrDefault("ИННПоставщика"),
                Driver = dict.GetValueOrDefault("Водитель") ?? string.Empty
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

/// <summary>
/// Вспомогательные методы расширения для безопасной работы со словарём.
/// </summary>
internal static class DictionaryExtensions
{
    /// <summary>
    /// Безопасно получает значение из словаря по ключу; если ключ отсутствует, возвращает default (null для ссылочных типов).
    /// </summary>
    public static TValue? GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
        where TKey : notnull
    {
        dict.TryGetValue(key, out var value);
        return value;
    }
}