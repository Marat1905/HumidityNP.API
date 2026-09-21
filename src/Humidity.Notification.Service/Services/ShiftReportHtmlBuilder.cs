using System.Text;
using Humidity.Contracts.Events;
using Humidity.Contracts.Protos;

namespace Humidity.Notification.Service.Services;

/// <summary>
/// Формирует HTML-тело письма с отчётом по смене.
/// </summary>
public class ShiftReportHtmlBuilder
{
    /// <summary>
    /// Сформировать HTML-тело письма.
    /// </summary>
    /// <param name="evt">Событие окончания смены.</param>
    /// <param name="stats">Агрегированная статистика за период смены.</param>
    /// <param name="timeZone">Часовой пояс площадки для отображения локального времени.</param>
    public string Build(ShiftEndedEvent evt, GetShiftStatisticsResponse stats, TimeZoneInfo timeZone)
    {
        var localStart = TimeZoneInfo.ConvertTime(evt.ShiftStart, timeZone);
        var localEnd = TimeZoneInfo.ConvertTime(evt.ShiftEnd, timeZone);

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"ru\"><head><meta charset=\"utf-8\">");
        sb.Append("<style>");
        sb.Append("body{font-family:Arial,sans-serif;background:#f5f7fb;color:#1f2937;padding:20px;}");
        sb.Append(".container{max-width:900px;margin:0 auto;background:#fff;border-radius:12px;box-shadow:0 2px 8px rgba(0,0,0,0.06);padding:24px;}");
        sb.Append("h1{color:#1e40af;font-size:22px;margin-bottom:8px;}");
        sb.Append("h2{color:#111827;font-size:16px;margin-top:24px;margin-bottom:12px;border-bottom:1px solid #e5e7eb;padding-bottom:6px;}");
        sb.Append(".kpi{display:flex;flex-wrap:wrap;gap:12px;margin:16px 0;}");
        sb.Append(".kpi .card{flex:1 1 160px;background:#f9fafb;border:1px solid #e5e7eb;border-radius:10px;padding:12px;}");
        sb.Append(".kpi .label{font-size:12px;color:#6b7280;text-transform:uppercase;letter-spacing:.04em;}");
        sb.Append(".kpi .value{font-size:22px;font-weight:700;color:#111827;margin-top:4px;}");
        sb.Append("table{width:100%;border-collapse:collapse;margin-top:8px;font-size:14px;}");
        sb.Append("th,td{padding:8px 10px;border-bottom:1px solid #e5e7eb;text-align:left;}");
        sb.Append("th{background:#f3f4f6;color:#374151;font-weight:600;}");
        sb.Append("tr:hover td{background:#f9fafb;}");
        sb.Append(".good{color:#059669;font-weight:600;}");
        sb.Append(".warn{color:#d97706;font-weight:600;}");
        sb.Append(".bad{color:#dc2626;font-weight:600;}");
        sb.Append(".footer{margin-top:24px;font-size:12px;color:#9ca3af;text-align:center;}");
        sb.Append("</style></head><body>");

        sb.Append("<div class=\"container\">");
        sb.Append("<h1>Отчёт по влажности макулатуры за смену</h1>");
        sb.Append($"<p>Смена: <strong>{(evt.ShiftType == "day" ? "Дневная (08:00–20:00)" : "Ночная (20:00–08:00)")}</strong><br>");
        sb.Append($"Период: <strong>{localStart:dd.MM.yyyy HH:mm}</strong> — <strong>{localEnd:dd.MM.yyyy HH:mm}</strong></p>");

        // KPI-карточки
        sb.Append("<div class=\"kpi\">");
        sb.Append("<div class=\"card\"><div class=\"label\">Машин с замерами</div><div class=\"value\">")
          .Append(stats.Vehicles.Count).Append("</div></div>");
        sb.Append("<div class=\"card\"><div class=\"label\">Всего замеров</div><div class=\"value\">")
          .Append(stats.TotalMeasurements).Append("</div></div>");
        sb.Append("<div class=\"card\"><div class=\"label\">Средняя влажность</div><div class=\"value\">")
          .Append(stats.OverallAverage.ToString("F1")).Append("%</div></div>");
        sb.Append("<div class=\"card\"><div class=\"label\">Мин / Макс</div><div class=\"value\">")
          .Append(stats.OverallMin.ToString("F1")).Append("% / ")
          .Append(stats.OverallMax.ToString("F1")).Append("%</div></div>");
        sb.Append("</div>");

        // Таблица по машинам
        sb.Append("<h2>Машины за смену</h2>");
        if (stats.Vehicles.Count == 0)
        {
            sb.Append("<p style=\"color:#6b7280\">За выбранную смену замеры отсутствуют.</p>");
        }
        else
        {
            sb.Append("<table><thead><tr>");
            sb.Append("<th>№</th><th>Машина</th><th>Поставщик</th><th>Замеров</th>");
            sb.Append("<th>Средняя</th><th>Мин</th><th>Макс</th>");
            sb.Append("</tr></thead><tbody>");

            var index = 1;
            foreach (var v in stats.Vehicles.OrderBy(v => v.Number))
            {
                var avgClass = v.AverageHumidity < 10 ? "good"
                    : v.AverageHumidity < 15 ? "warn"
                    : "bad";

                sb.Append("<tr>");
                sb.Append("<td>").Append(index++).Append("</td>");
                sb.Append("<td>").Append(v.Number)
                  .Append(" (").Append(v.VehiclePlate).Append(")</td>");
                sb.Append("<td>").Append(System.Net.WebUtility.HtmlEncode(v.Counterparty)).Append("</td>");
                sb.Append("<td>").Append(v.MeasurementsCount).Append("</td>");
                sb.Append("<td class=\"").Append(avgClass).Append("\">")
                  .Append(v.AverageHumidity.ToString("F1")).Append("%</td>");
                sb.Append("<td>").Append(v.MinHumidity.ToString("F1")).Append("%</td>");
                sb.Append("<td>").Append(v.MaxHumidity.ToString("F1")).Append("%</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");
        }

        sb.Append("<div class=\"footer\">");
        sb.Append("Письмо сформировано автоматически сервисом Humidity.Notification.Service.<br>");
        sb.Append($"Время формирования: {DateTimeOffset.UtcNow:dd.MM.yyyy HH:mm} UTC");
        sb.Append("</div>");

        sb.Append("</div></body></html>");

        return sb.ToString();
    }
}