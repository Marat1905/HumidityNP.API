using Grpc.Core;
using Humidity.Contracts.Protos;
using Humidity.Notification.Service.Data;
using Humidity.Notification.Service.Options;
using Humidity.Notification.Service.Services;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Humidity.Notification.Service.gRPC;

/// <summary>
/// gRPC-реализация сервиса NotificationService.
/// </summary>
public class NotificationGrpcService : NotificationService.NotificationServiceBase
{
    private readonly MongoDbContext _mongo;
    private readonly EmailSender _emailSender;
    private readonly RecipientsOptions _defaultRecipients;
    private readonly ILogger<NotificationGrpcService> _logger;

    public NotificationGrpcService(
        MongoDbContext mongo,
        EmailSender emailSender,
        IOptions<RecipientsOptions> defaultRecipients,
        ILogger<NotificationGrpcService> logger)
    {
        _mongo = mongo;
        _emailSender = emailSender;
        _defaultRecipients = defaultRecipients.Value;
        _logger = logger;
    }

    public override async Task<GetNotificationLogsResponse> GetNotificationLogs(
        GetNotificationLogsRequest request,
        ServerCallContext context)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 200 ? 50 : request.PageSize;

        var builder = Builders<Models.NotificationLog>.Filter;
        var filter = builder.Empty;

        if (DateTime.TryParse(request.FromDate, out var from))
        {
            filter &= builder.Gte(x => x.SentAt, from.ToUniversalTime());
        }
        if (DateTime.TryParse(request.ToDate, out var to))
        {
            filter &= builder.Lte(x => x.SentAt, to.ToUniversalTime());
        }

        var totalCount = (int)await _mongo.Notifications.CountDocumentsAsync(filter);

        var items = await _mongo.Notifications
            .Find(filter)
            .SortByDescending(x => x.SentAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(context.CancellationToken);

        var response = new GetNotificationLogsResponse { TotalCount = totalCount };
        response.Items.AddRange(items.Select(x => new NotificationLogDto
        {
            Id = x.Id,
            ShiftType = x.ShiftType,
            ShiftDate = x.ShiftStart.ToString("O"),
            SentAt = x.SentAt.ToString("O"),
            RecipientCount = x.Recipients.Count,
            Status = x.Status,
            ErrorMessage = x.ErrorMessage ?? string.Empty,
            VehiclesCount = x.VehiclesCount,
            TotalMeasurements = x.TotalMeasurements,
            OverallAverageHumidity = x.OverallAverageHumidity
        }));

        return response;
    }

    public override async Task<SendTestNotificationResponse> SendTestNotification(
        SendTestNotificationRequest request,
        ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return new SendTestNotificationResponse
                {
                    Success = false,
                    Message = "Email получателя не указан."
                };
            }

            await _emailSender.SendAsync(
                new[] { request.Email },
                "Тестовое уведомление Humidity",
                "<p>Это тестовое письмо от сервиса Humidity.Notification.Service. " +
                "Если вы его получили — SMTP настроен корректно.</p>");

            return new SendTestNotificationResponse
            {
                Success = true,
                Message = $"Тестовое письмо отправлено на {request.Email}."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки тестового письма на {Email}", request.Email);
            return new SendTestNotificationResponse
            {
                Success = false,
                Message = $"Ошибка: {ex.Message}"
            };
        }
    }
}