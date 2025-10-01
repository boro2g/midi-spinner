using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Event arguments for device connection events
/// </summary>
public class DeviceConnectionEventArgs : EventArgs
{
    public MidiDevice Device { get; }
    public bool IsConnected { get; }
    public string? ErrorMessage { get; }

    public DeviceConnectionEventArgs(MidiDevice device, bool isConnected, string? errorMessage = null)
    {
        Device = device;
        IsConnected = isConnected;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// Service for managing MIDI device connections and monitoring
/// </summary>
public interface IMidiDeviceManager : IDisposable
{
    /// <summary>
    /// Event fired when a device connection status changes
    /// </summary>
    event EventHandler<DeviceConnectionEventArgs>? DeviceConnectionChanged;
    
    /// <summary>
    /// Event fired when device enumeration changes (devices added/removed)
    /// </summary>
    event EventHandler? DevicesChanged;
    
    /// <summary>
    /// Currently active output device
    /// </summary>
    MidiDevice? ActiveDevice { get; }
    
    /// <summary>
    /// Whether the active device is currently connected
    /// </summary>
    bool IsActiveDeviceConnected { get; }
    
    /// <summary>
    /// Start monitoring device connections
    /// </summary>
    Task StartMonitoringAsync();
    
    /// <summary>
    /// Stop monitoring device connections
    /// </summary>
    void StopMonitoring();
    
    /// <summary>
    /// Get all available MIDI output devices
    /// </summary>
    Task<List<MidiDevice>> GetAvailableDevicesAsync();
    
    /// <summary>
    /// Set the active output device
    /// </summary>
    /// <param name="device">Device to activate</param>
    /// <returns>True if device was successfully activated</returns>
    Task<bool> SetActiveDeviceAsync(MidiDevice device);
    
    /// <summary>
    /// Attempt to reconnect to the active device
    /// </summary>
    /// <returns>True if reconnection was successful</returns>
    Task<bool> ReconnectActiveDeviceAsync();
    
    /// <summary>
    /// Test if a device is currently available and responsive
    /// </summary>
    /// <param name="device">Device to test</param>
    /// <returns>True if device is available</returns>
    Task<bool> TestDeviceAsync(MidiDevice device);
}