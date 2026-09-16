using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Humidity.API.Hubs;

/// <summary>
/// SignalR-хаб для real-time уведомлений.
///
/// Каналы:
///   • "vehicles"       — события по машинам (создание, выезд, разгрузка);
///   • "measurements"   — события по замерам (создание, редактирование, удаление);
///   • "shift"          — уведомление о завершении смены.
///
/// Клиент может подписаться на нужные каналы через метод SubscribeToChannel,
/// </summary>
[Authorize]
public class HumidityHub : Hub
{
    private readonly ILogger<HumidityHub> _logger;

    public HumidityHub(ILogger<HumidityHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Подписаться на один или несколько каналов.
    /// </summary>
    public async Task SubscribeToChannel(string[] channels)
    {
        if (channels == null || channels.Length == 0)
        {
            _logger.LogWarning(
                "Клиент {ConnectionId} вызвал SubscribeToChannel с пустым массивом.",
                Context.ConnectionId);
            return;
        }

        foreach (var channel in channels)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                continue;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, channel);

            _logger.LogInformation(
                "Клиент {ConnectionId} подписан на {Channel}",
                Context.ConnectionId,
                channel);
        }
    }

    /// <summary>
    /// Отписаться от каналов.
    /// </summary>
    public async Task UnsubscribeFromChannel(string[] channels)
    {
        if (channels == null || channels.Length == 0)
        {
            return;
        }

        foreach (var channel in channels)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                continue;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, channel);

            _logger.LogInformation(
                "Клиент {ConnectionId} отписан от {Channel}",
                Context.ConnectionId,
                channel);
        }
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "SignalR: подключился клиент {ConnectionId}, user={User}",
            Context.ConnectionId,
            Context.User?.Identity?.Name ?? "anonymous");

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Вызывается при отключении клиента.
    /// SignalR автоматически удаляет его из всех групп.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "SignalR: отключился клиент {ConnectionId}",
            Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }
}