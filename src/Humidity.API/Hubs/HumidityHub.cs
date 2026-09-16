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
/// Клиент может подписаться на нужные каналы через метод SubscribeToChannel.
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
    public async Task SubscribeToChannel(params string[] channels)
    {
        foreach (var channel in channels)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, channel);
            _logger.LogInformation("Клиент {ConnectionId} подписан на {Channel}",
                Context.ConnectionId, channel);
        }
    }

    /// <summary>
    /// Отписаться от каналов.
    /// </summary>
    public async Task UnsubscribeFromChannel(params string[] channels)
    {
        foreach (var channel in channels)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, channel);
        }
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("SignalR: подключился клиент {ConnectionId}, user={User}",
            Context.ConnectionId,
            Context.User?.Identity?.Name ?? "anonymous");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("SignalR: отключился клиент {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}