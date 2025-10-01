using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// MIDI device manager implementation with connection monitoring and automatic reconnection
/// </summary>
public class MidiDeviceManager : IMidiDeviceManager
{
    private readonly ILogger<MidiDeviceManager> _logger;
    private readonly Timer _monitoringTimer;
    private readonly object _deviceLock = new object();
    
    private MidiDevice? _activeDevice;
    private OutputDevice? _outputDevice;
    private bool _isMonitoring;
    private bool _disposed;
    private List<MidiDevice> _lastKnownDevices = new();
    private DateTime _lastConnectionAttempt = DateTime.MinValue;
    private readonly TimeSpan _reconnectionCooldown = TimeSpan.FromSeconds(5);
    private int _reconnectionAttempts;
    private const int MaxReconnectionAttempts = 3;

    public event EventHandler<DeviceConnectionEventArgs>? DeviceConnectionChanged;
    public event EventHandler? DevicesChanged;

    public MidiDeviceManager(ILogger<MidiDeviceManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Timer for monitoring device connections
        _monitoringTimer = new Timer(OnMonitoringTimerTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    public MidiDevice? ActiveDevice => _activeDevice;
    public bool IsActiveDeviceConnected => _outputDevice != null && _activeDevice != null;

    public async Task StartMonitoringAsync()
    {
        try
        {
            _logger.LogInformation("Starting MIDI device monitoring");
            
            // Initial device enumeration
            _lastKnownDevices = await GetAvailableDevicesAsync();
            
            _isMonitoring = true;
            _monitoringTimer.Change(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
            
            _logger.LogInformation("MIDI device monitoring started, found {DeviceCount} devices", _lastKnownDevices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start MIDI device monitoring");
            throw;
        }
    }

    public void StopMonitoring()
    {
        try
        {
            _logger.LogInformation("Stopping MIDI device monitoring");
            
            _isMonitoring = false;
            _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            _logger.LogInformation("MIDI device monitoring stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping MIDI device monitoring");
        }
    }

    public async Task<List<MidiDevice>> GetAvailableDevicesAsync()
    {
        try
        {
            return await Task.Run(() =>
            {
                var devices = OutputDevice.GetAll()
                    .Select((info, index) => new MidiDevice(info.Name, index))
                    .ToList();
                
                _logger.LogDebug("Enumerated {DeviceCount} MIDI output devices", devices.Count);
                return devices;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate MIDI devices");
            return new List<MidiDevice>();
        }
    }

    public async Task<bool> SetActiveDeviceAsync(MidiDevice device)
    {
        if (device == null)
            throw new ArgumentNullException(nameof(device));

        try
        {
            _logger.LogInformation("Setting active MIDI device: {DeviceName}", device.Name);
            
            lock (_deviceLock)
            {
                // Dispose existing device
                _outputDevice?.Dispose();
                _outputDevice = null;
                _activeDevice = null;
                _reconnectionAttempts = 0;
            }

            // Test the device first
            if (!await TestDeviceAsync(device))
            {
                _logger.LogWarning("Device test failed for: {DeviceName}", device.Name);
                return false;
            }

            // Attempt to open the device
            var success = await Task.Run(() =>
            {
                try
                {
                    var deviceInfos = OutputDevice.GetAll().ToList();
                    if (device.Id >= 0 && device.Id < deviceInfos.Count)
                    {
                        var outputDevice = OutputDevice.GetByName(deviceInfos[device.Id].Name);
                        
                        lock (_deviceLock)
                        {
                            _outputDevice = outputDevice;
                            _activeDevice = device;
                            _reconnectionAttempts = 0;
                        }
                        
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to open MIDI device: {DeviceName}", device.Name);
                    return false;
                }
            });

            if (success)
            {
                _logger.LogInformation("Successfully set active MIDI device: {DeviceName}", device.Name);
                DeviceConnectionChanged?.Invoke(this, new DeviceConnectionEventArgs(device, true));
            }
            else
            {
                _logger.LogWarning("Failed to set active MIDI device: {DeviceName}", device.Name);
                DeviceConnectionChanged?.Invoke(this, new DeviceConnectionEventArgs(device, false, "Failed to open device"));
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting active MIDI device: {DeviceName}", device.Name);
            DeviceConnectionChanged?.Invoke(this, new DeviceConnectionEventArgs(device, false, ex.Message));
            return false;
        }
    }

    public async Task<bool> ReconnectActiveDeviceAsync()
    {
        var activeDevice = _activeDevice;
        if (activeDevice == null)
        {
            _logger.LogWarning("No active device to reconnect");
            return false;
        }

        // Check cooldown period
        if (DateTime.UtcNow - _lastConnectionAttempt < _reconnectionCooldown)
        {
            _logger.LogDebug("Reconnection attempt skipped due to cooldown period");
            return false;
        }

        // Check max attempts
        if (_reconnectionAttempts >= MaxReconnectionAttempts)
        {
            _logger.LogWarning("Maximum reconnection attempts reached for device: {DeviceName}", activeDevice.Name);
            return false;
        }

        _lastConnectionAttempt = DateTime.UtcNow;
        _reconnectionAttempts++;

        _logger.LogInformation("Attempting to reconnect to device: {DeviceName} (attempt {Attempt}/{MaxAttempts})", 
            activeDevice.Name, _reconnectionAttempts, MaxReconnectionAttempts);

        return await SetActiveDeviceAsync(activeDevice);
    }

    public async Task<bool> TestDeviceAsync(MidiDevice device)
    {
        if (device == null)
            return false;

        try
        {
            return await Task.Run(() =>
            {
                try
                {
                    var deviceInfos = OutputDevice.GetAll().ToList();
                    if (device.Id >= 0 && device.Id < deviceInfos.Count)
                    {
                        // Try to create and immediately dispose a test connection
                        using var testDevice = OutputDevice.GetByName(deviceInfos[device.Id].Name);
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Device test failed for: {DeviceName}", device.Name);
                    return false;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing MIDI device: {DeviceName}", device.Name);
            return false;
        }
    }

    private async void OnMonitoringTimerTick(object? state)
    {
        if (!_isMonitoring || _disposed)
            return;

        try
        {
            // Check for device list changes
            var currentDevices = await GetAvailableDevicesAsync();
            
            if (!DeviceListsEqual(_lastKnownDevices, currentDevices))
            {
                _logger.LogInformation("MIDI device list changed: {OldCount} -> {NewCount} devices", 
                    _lastKnownDevices.Count, currentDevices.Count);
                
                _lastKnownDevices = currentDevices;
                DevicesChanged?.Invoke(this, EventArgs.Empty);
            }

            // Check active device connection
            CheckActiveDeviceConnection();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in device monitoring timer");
        }
    }

    private void CheckActiveDeviceConnection()
    {
        var activeDevice = _activeDevice;
        if (activeDevice == null)
            return;

        bool isConnected;
        lock (_deviceLock)
        {
            isConnected = _outputDevice != null;
        }

        // Test if device is still responsive
        if (isConnected)
        {
            try
            {
                // Send a test message to verify the device is still working
                lock (_deviceLock)
                {
                    if (_outputDevice != null)
                    {
                        // Send a very quiet note on/off to test connectivity
                        var testNote = new NoteOnEvent((SevenBitNumber)60, (SevenBitNumber)1) { Channel = (FourBitNumber)0 };
                        _outputDevice.SendEvent(testNote);
                        
                        var testNoteOff = new NoteOffEvent((SevenBitNumber)60, (SevenBitNumber)0) { Channel = (FourBitNumber)0 };
                        _outputDevice.SendEvent(testNoteOff);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Active device connection test failed: {DeviceName}", activeDevice.Name);
                isConnected = false;
                
                lock (_deviceLock)
                {
                    _outputDevice?.Dispose();
                    _outputDevice = null;
                }
                
                DeviceConnectionChanged?.Invoke(this, new DeviceConnectionEventArgs(activeDevice, false, "Device became unresponsive"));
                
                // Attempt automatic reconnection
                _ = Task.Run(() => ReconnectActiveDeviceAsync());
            }
        }
        else
        {
            // Device is not connected, try to reconnect
            _ = Task.Run(() => ReconnectActiveDeviceAsync());
        }
    }

    private static bool DeviceListsEqual(List<MidiDevice> list1, List<MidiDevice> list2)
    {
        if (list1.Count != list2.Count)
            return false;

        var names1 = list1.Select(d => d.Name).OrderBy(n => n).ToList();
        var names2 = list2.Select(d => d.Name).OrderBy(n => n).ToList();

        return names1.SequenceEqual(names2);
    }

    public OutputDevice? GetOutputDevice()
    {
        lock (_deviceLock)
        {
            return _outputDevice;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                StopMonitoring();
                _monitoringTimer?.Dispose();
                
                lock (_deviceLock)
                {
                    _outputDevice?.Dispose();
                    _outputDevice = null;
                    _activeDevice = null;
                }
                
                _logger.LogInformation("MIDI device manager disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MIDI device manager disposal");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}