using System;
using System.Reactive.Disposables;
using ReactiveUI;
using Microsoft.Extensions.Logging;
using CircularMidiGenerator.Core.Services;
using CircularMidiGenerator.Services;

namespace CircularMidiGenerator.ViewModels;

/// <summary>
/// Connection status for various services
/// </summary>
public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Error,
    Unknown
}

/// <summary>
/// ViewModel for displaying application status indicators
/// </summary>
public class StatusIndicatorViewModel : ReactiveObject, IDisposable
{
    private readonly IMidiDeviceManager _deviceManager;
    private readonly IAbletonSyncService _abletonSync;
    private readonly ITimingService _timingService;
    private readonly ServiceHealthMonitor _healthMonitor;
    private readonly ILogger<StatusIndicatorViewModel> _logger;
    private readonly CompositeDisposable _disposables = new();

    private ConnectionStatus _midiStatus = ConnectionStatus.Unknown;
    private ConnectionStatus _abletonStatus = ConnectionStatus.Disconnected;
    private string _midiDeviceName = "No device";
    private string _statusMessage = "Initializing...";
    private bool _hasErrors;
    private int _errorCount;
    private ServiceHealthStatus _overallHealth = ServiceHealthStatus.Unknown;

    public StatusIndicatorViewModel(
        IMidiDeviceManager deviceManager,
        IAbletonSyncService abletonSync,
        ITimingService timingService,
        ServiceHealthMonitor healthMonitor,
        ILogger<StatusIndicatorViewModel> logger)
    {
        _deviceManager = deviceManager;
        _abletonSync = abletonSync;
        _timingService = timingService;
        _healthMonitor = healthMonitor;
        _logger = logger;

        InitializeSubscriptions();
        UpdateStatus();
    }

    /// <summary>
    /// MIDI connection status
    /// </summary>
    public ConnectionStatus MidiStatus
    {
        get => _midiStatus;
        private set => this.RaiseAndSetIfChanged(ref _midiStatus, value);
    }

    /// <summary>
    /// Ableton Live sync status
    /// </summary>
    public ConnectionStatus AbletonStatus
    {
        get => _abletonStatus;
        private set => this.RaiseAndSetIfChanged(ref _abletonStatus, value);
    }

    /// <summary>
    /// Name of the active MIDI device
    /// </summary>
    public string MidiDeviceName
    {
        get => _midiDeviceName;
        private set => this.RaiseAndSetIfChanged(ref _midiDeviceName, value);
    }

    /// <summary>
    /// Overall status message
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    /// <summary>
    /// Whether there are any errors
    /// </summary>
    public bool HasErrors
    {
        get => _hasErrors;
        private set => this.RaiseAndSetIfChanged(ref _hasErrors, value);
    }

    /// <summary>
    /// Number of errors
    /// </summary>
    public int ErrorCount
    {
        get => _errorCount;
        private set => this.RaiseAndSetIfChanged(ref _errorCount, value);
    }

    /// <summary>
    /// Overall application health
    /// </summary>
    public ServiceHealthStatus OverallHealth
    {
        get => _overallHealth;
        private set => this.RaiseAndSetIfChanged(ref _overallHealth, value);
    }

    /// <summary>
    /// Color for MIDI status indicator
    /// </summary>
    public string MidiStatusColor => MidiStatus switch
    {
        ConnectionStatus.Connected => "#4CAF50", // Green
        ConnectionStatus.Connecting => "#FF9800", // Orange
        ConnectionStatus.Disconnected => "#757575", // Gray
        ConnectionStatus.Error => "#F44336", // Red
        _ => "#9E9E9E" // Light Gray
    };

    /// <summary>
    /// Color for Ableton status indicator
    /// </summary>
    public string AbletonStatusColor => AbletonStatus switch
    {
        ConnectionStatus.Connected => "#4CAF50", // Green
        ConnectionStatus.Connecting => "#FF9800", // Orange
        ConnectionStatus.Disconnected => "#757575", // Gray
        ConnectionStatus.Error => "#F44336", // Red
        _ => "#9E9E9E" // Light Gray
    };

    /// <summary>
    /// Color for overall health indicator
    /// </summary>
    public string HealthStatusColor => OverallHealth switch
    {
        ServiceHealthStatus.Healthy => "#4CAF50", // Green
        ServiceHealthStatus.Degraded => "#FF9800", // Orange
        ServiceHealthStatus.Unhealthy => "#F44336", // Red
        _ => "#9E9E9E" // Light Gray
    };

    private void InitializeSubscriptions()
    {
        // Subscribe to device manager events
        _deviceManager.DeviceConnectionChanged += OnDeviceConnectionChanged;
        _deviceManager.DevicesChanged += OnDevicesChanged;

        // Subscribe to Ableton sync events
        _abletonSync.TempoChanged += OnAbletonTempoChanged;
        _abletonSync.SyncLost += OnAbletonSyncLost;

        // Subscribe to health monitor events
        _healthMonitor.ServiceHealthChanged += OnServiceHealthChanged;

        // Clean up subscriptions on disposal
        Disposable.Create(() =>
        {
            _deviceManager.DeviceConnectionChanged -= OnDeviceConnectionChanged;
            _deviceManager.DevicesChanged -= OnDevicesChanged;
            _abletonSync.TempoChanged -= OnAbletonTempoChanged;
            _abletonSync.SyncLost -= OnAbletonSyncLost;
            _healthMonitor.ServiceHealthChanged -= OnServiceHealthChanged;
        }).DisposeWith(_disposables);
    }

    private void OnDeviceConnectionChanged(object? sender, DeviceConnectionEventArgs e)
    {
        MidiStatus = e.IsConnected ? ConnectionStatus.Connected : 
                    string.IsNullOrEmpty(e.ErrorMessage) ? ConnectionStatus.Disconnected : ConnectionStatus.Error;
        
        MidiDeviceName = e.Device?.Name ?? "No device";
        
        UpdateStatus();
        
        _logger.LogInformation("MIDI device connection changed: {DeviceName} - {Status}", 
            e.Device?.Name, e.IsConnected ? "Connected" : "Disconnected");
    }

    private void OnDevicesChanged(object? sender, EventArgs e)
    {
        // Device list changed, update status
        UpdateStatus();
    }

    private void OnAbletonTempoChanged(object? sender, TempoChangedEventArgs e)
    {
        AbletonStatus = ConnectionStatus.Connected;
        UpdateStatus();
        
        _logger.LogDebug("Ableton tempo changed to {Tempo} BPM", e.Tempo);
    }

    private void OnAbletonSyncLost(object? sender, EventArgs e)
    {
        AbletonStatus = ConnectionStatus.Disconnected;
        UpdateStatus();
        
        _logger.LogWarning("Ableton Live synchronization lost");
    }

    private void OnServiceHealthChanged(object? sender, ServiceHealthChangedEventArgs e)
    {
        var result = e.Result;
        
        // Update error count
        if (result.Status == ServiceHealthStatus.Unhealthy)
        {
            ErrorCount++;
            HasErrors = true;
        }
        else if (result.Status == ServiceHealthStatus.Healthy && HasErrors)
        {
            ErrorCount = Math.Max(0, ErrorCount - 1);
            HasErrors = ErrorCount > 0;
        }

        // Update overall health
        OverallHealth = _healthMonitor.GetOverallHealth();
        
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        try
        {
            // Update Ableton status based on service state
            AbletonStatus = _abletonSync.IsConnected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;

            // Update MIDI status and device name
            if (_deviceManager.ActiveDevice != null)
            {
                MidiDeviceName = _deviceManager.ActiveDevice.Name;
                MidiStatus = _deviceManager.IsActiveDeviceConnected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;
            }
            else
            {
                MidiDeviceName = "No device selected";
                MidiStatus = ConnectionStatus.Disconnected;
            }

            // Update overall health
            OverallHealth = _healthMonitor.GetOverallHealth();

            // Generate status message
            StatusMessage = GenerateStatusMessage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status indicators");
            StatusMessage = "Status update error";
        }
    }

    private string GenerateStatusMessage()
    {
        if (OverallHealth == ServiceHealthStatus.Unhealthy)
        {
            return $"System issues detected ({ErrorCount} errors)";
        }

        if (OverallHealth == ServiceHealthStatus.Degraded)
        {
            return "System running with warnings";
        }

        if (MidiStatus == ConnectionStatus.Connected && AbletonStatus == ConnectionStatus.Connected)
        {
            return $"All systems connected - {MidiDeviceName}";
        }

        if (MidiStatus == ConnectionStatus.Connected)
        {
            return $"MIDI connected - {MidiDeviceName}";
        }

        if (MidiStatus == ConnectionStatus.Disconnected)
        {
            return "MIDI device not connected";
        }

        return "System ready";
    }

    /// <summary>
    /// Refresh all status indicators
    /// </summary>
    public void RefreshStatus()
    {
        UpdateStatus();
    }

    public void Dispose()
    {
        _disposables?.Dispose();
    }
}