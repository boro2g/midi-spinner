using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CircularMidiGenerator.Core.Services;

namespace CircularMidiGenerator.Services;

/// <summary>
/// Service health status
/// </summary>
public enum ServiceHealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

/// <summary>
/// Health check result for a service
/// </summary>
public class ServiceHealthResult
{
    public string ServiceName { get; set; } = string.Empty;
    public ServiceHealthStatus Status { get; set; }
    public string? Description { get; set; }
    public Exception? Exception { get; set; }
    public DateTime CheckedAt { get; set; }
}

/// <summary>
/// Event arguments for service health changes
/// </summary>
public class ServiceHealthChangedEventArgs : EventArgs
{
    public ServiceHealthResult Result { get; }

    public ServiceHealthChangedEventArgs(ServiceHealthResult result)
    {
        Result = result;
    }
}

/// <summary>
/// Monitors the health of critical application services
/// </summary>
public class ServiceHealthMonitor : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ServiceHealthMonitor> _logger;
    private readonly Timer _healthCheckTimer;
    private readonly Dictionary<string, ServiceHealthResult> _lastResults = new();
    private bool _disposed;

    public event EventHandler<ServiceHealthChangedEventArgs>? ServiceHealthChanged;

    public ServiceHealthMonitor(IServiceProvider serviceProvider, ILogger<ServiceHealthMonitor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Check health every 30 seconds
        _healthCheckTimer = new Timer(PerformHealthChecks, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Get the current health status of all monitored services
    /// </summary>
    public IReadOnlyDictionary<string, ServiceHealthResult> GetCurrentHealth()
    {
        return new Dictionary<string, ServiceHealthResult>(_lastResults);
    }

    /// <summary>
    /// Get overall application health status
    /// </summary>
    public ServiceHealthStatus GetOverallHealth()
    {
        if (_lastResults.Count == 0) return ServiceHealthStatus.Unknown;

        var hasUnhealthy = false;
        var hasDegraded = false;

        foreach (var result in _lastResults.Values)
        {
            switch (result.Status)
            {
                case ServiceHealthStatus.Unhealthy:
                    hasUnhealthy = true;
                    break;
                case ServiceHealthStatus.Degraded:
                    hasDegraded = true;
                    break;
            }
        }

        if (hasUnhealthy) return ServiceHealthStatus.Unhealthy;
        if (hasDegraded) return ServiceHealthStatus.Degraded;
        return ServiceHealthStatus.Healthy;
    }

    private async void PerformHealthChecks(object? state)
    {
        if (_disposed) return;

        try
        {
            var tasks = new List<Task<ServiceHealthResult>>
            {
                CheckMidiServiceHealth(),
                CheckTimingServiceHealth(),
                CheckDeviceManagerHealth(),
                CheckLaneControllerHealth()
            };

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                var previousResult = _lastResults.TryGetValue(result.ServiceName, out var prev) ? prev : null;
                _lastResults[result.ServiceName] = result;

                // Fire event if status changed
                if (previousResult?.Status != result.Status)
                {
                    _logger.LogInformation("Service {ServiceName} health changed from {OldStatus} to {NewStatus}: {Description}",
                        result.ServiceName, previousResult?.Status ?? ServiceHealthStatus.Unknown, result.Status, result.Description);
                    
                    ServiceHealthChanged?.Invoke(this, new ServiceHealthChangedEventArgs(result));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing health checks");
        }
    }

    private async Task<ServiceHealthResult> CheckMidiServiceHealth()
    {
        try
        {
            var midiService = _serviceProvider.GetRequiredService<IMidiService>();
            var deviceManager = _serviceProvider.GetRequiredService<IMidiDeviceManager>();

            var isConnected = deviceManager.IsActiveDeviceConnected;
            var hasDevice = deviceManager.ActiveDevice != null;

            if (!hasDevice)
            {
                return new ServiceHealthResult
                {
                    ServiceName = "MIDI Service",
                    Status = ServiceHealthStatus.Degraded,
                    Description = "No MIDI device selected",
                    CheckedAt = DateTime.UtcNow
                };
            }

            if (!isConnected)
            {
                return new ServiceHealthResult
                {
                    ServiceName = "MIDI Service",
                    Status = ServiceHealthStatus.Unhealthy,
                    Description = "MIDI device not connected",
                    CheckedAt = DateTime.UtcNow
                };
            }

            return new ServiceHealthResult
            {
                ServiceName = "MIDI Service",
                Status = ServiceHealthStatus.Healthy,
                Description = $"Connected to {deviceManager.ActiveDevice?.Name}",
                CheckedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new ServiceHealthResult
            {
                ServiceName = "MIDI Service",
                Status = ServiceHealthStatus.Unhealthy,
                Description = "Service error",
                Exception = ex,
                CheckedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<ServiceHealthResult> CheckTimingServiceHealth()
    {
        try
        {
            var timingService = _serviceProvider.GetRequiredService<ITimingService>();
            
            // Check if timing service is responsive
            var currentAngle = timingService.CurrentAngle;
            var isPlaying = timingService.IsPlaying;
            var currentBPM = timingService.CurrentBPM;

            if (currentBPM <= 0)
            {
                return new ServiceHealthResult
                {
                    ServiceName = "Timing Service",
                    Status = ServiceHealthStatus.Degraded,
                    Description = "Invalid BPM setting",
                    CheckedAt = DateTime.UtcNow
                };
            }

            return new ServiceHealthResult
            {
                ServiceName = "Timing Service",
                Status = ServiceHealthStatus.Healthy,
                Description = $"BPM: {currentBPM:F1}, Playing: {isPlaying}",
                CheckedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new ServiceHealthResult
            {
                ServiceName = "Timing Service",
                Status = ServiceHealthStatus.Unhealthy,
                Description = "Service error",
                Exception = ex,
                CheckedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<ServiceHealthResult> CheckDeviceManagerHealth()
    {
        try
        {
            var deviceManager = _serviceProvider.GetRequiredService<IMidiDeviceManager>();
            var devices = await deviceManager.GetAvailableDevicesAsync();

            if (devices.Count == 0)
            {
                return new ServiceHealthResult
                {
                    ServiceName = "Device Manager",
                    Status = ServiceHealthStatus.Degraded,
                    Description = "No MIDI devices available",
                    CheckedAt = DateTime.UtcNow
                };
            }

            return new ServiceHealthResult
            {
                ServiceName = "Device Manager",
                Status = ServiceHealthStatus.Healthy,
                Description = $"{devices.Count} MIDI devices available",
                CheckedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new ServiceHealthResult
            {
                ServiceName = "Device Manager",
                Status = ServiceHealthStatus.Unhealthy,
                Description = "Service error",
                Exception = ex,
                CheckedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<ServiceHealthResult> CheckLaneControllerHealth()
    {
        try
        {
            var laneController = _serviceProvider.GetRequiredService<ILaneController>();
            var lanes = laneController.Lanes;
            var markerCount = laneController.GetAllMarkers().Count();

            return new ServiceHealthResult
            {
                ServiceName = "Lane Controller",
                Status = ServiceHealthStatus.Healthy,
                Description = $"{lanes.Count} lanes, {markerCount} markers",
                CheckedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new ServiceHealthResult
            {
                ServiceName = "Lane Controller",
                Status = ServiceHealthStatus.Unhealthy,
                Description = "Service error",
                Exception = ex,
                CheckedAt = DateTime.UtcNow
            };
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _healthCheckTimer?.Dispose();
            _disposed = true;
        }
    }
}