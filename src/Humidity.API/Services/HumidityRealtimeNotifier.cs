using Humidity.API.Hubs;
using Humidity.Application.Interfaces;
using Humidity.Contracts.Events;
using Microsoft.AspNetCore.SignalR;

namespace Humidity.API.Services;


/// <summary>
/// Реализация на базе SignalR.
/// </summary>
public class HumidityRealtimeNotifier : IHumidityRealtimeNotifier
{
    private readonly IHubContext<HumidityHub> _hub;
    private readonly ILogger<HumidityRealtimeNotifier> _logger;

    public HumidityRealtimeNotifier(
        IHubContext<HumidityHub> hub,
        ILogger<HumidityRealtimeNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public Task NotifyVehicleCreatedAsync(VehicleCreatedEvent evt, CancellationToken ct = default)
    {
        _logger.LogInformation("SignalR: vehicleCreated {Number}", evt.Number);
        return _hub.Clients.Group("vehicles").SendAsync("vehicleCreated", evt, ct);
    }

    public Task NotifyMeasurementCreatedAsync(MeasurementCreatedEvent evt, CancellationToken ct = default)
    {
        _logger.LogInformation("SignalR: measurementCreated vehicle={Number} humidity={Value}",
            evt.VehicleNumber, evt.HumidityValue);
        return _hub.Clients.Group("measurements").SendAsync("measurementCreated", evt, ct);
    }

    public Task NotifyVehicleUpdatedAsync(Guid vehicleId, CancellationToken ct = default)
    {
        _logger.LogInformation("SignalR: vehicleUpdated {VehicleId}", vehicleId);
        return _hub.Clients.Group("vehicles").SendAsync("vehicleUpdated", new { vehicleId }, ct);
    }

    public Task NotifyMeasurementDeletedAsync(Guid measurementId, Guid vehicleId, CancellationToken ct = default)
    {
        _logger.LogInformation("SignalR: measurementDeleted {MeasurementId}", measurementId);
        return _hub.Clients.Group("measurements").SendAsync(
            "measurementDeleted",
            new { measurementId, vehicleId },
            ct);
    }

    public Task NotifyShiftEndedAsync(Guid eventId, string shiftType, DateTimeOffset shiftEnd, CancellationToken ct = default)
    {
        _logger.LogInformation("SignalR: shiftEnded {ShiftType} {ShiftEnd}", shiftType, shiftEnd);
        return _hub.Clients.Group("shift").SendAsync(
            "shiftEnded",
            new { eventId, shiftType, shiftEnd },
            ct);
    }
}