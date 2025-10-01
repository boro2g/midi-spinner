using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// MIDI service implementation using DryWetMIDI library
/// </summary>
public class MidiService : IMidiService
{
    private readonly ILogger<MidiService> _logger;
    private readonly IAbletonSyncService _abletonSyncService;
    private readonly IMidiDeviceManager _deviceManager;
    private bool _disposed;

    public MidiService(ILogger<MidiService> logger, IAbletonSyncService abletonSyncService, IMidiDeviceManager deviceManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _abletonSyncService = abletonSyncService ?? throw new ArgumentNullException(nameof(abletonSyncService));
        _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
        
        // Subscribe to device connection events
        _deviceManager.DeviceConnectionChanged += OnDeviceConnectionChanged;
        _deviceManager.DevicesChanged += OnDevicesChanged;
    }

    public bool IsConnectedToAbleton => _abletonSyncService.IsConnected;

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing MIDI service");
            
            // Initialize device manager and start monitoring
            await _deviceManager.StartMonitoringAsync();
            
            _logger.LogInformation("MIDI service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MIDI service");
            throw;
        }
    }

    public async Task<List<MidiDevice>> GetAvailableDevicesAsync()
    {
        return await _deviceManager.GetAvailableDevicesAsync();
    }

    public void SetOutputDevice(MidiDevice device)
    {
        if (device == null)
            throw new ArgumentNullException(nameof(device));

        try
        {
            var task = _deviceManager.SetActiveDeviceAsync(device);
            task.Wait(); // Synchronous wrapper for async method
            
            if (!task.Result)
            {
                throw new InvalidOperationException($"Failed to set MIDI output device: {device.Name}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set MIDI output device: {DeviceName}", device.Name);
            throw;
        }
    }

    public void SendNoteOn(int channel, int note, int velocity)
    {
        if (channel < 1 || channel > 16)
            throw new ArgumentOutOfRangeException(nameof(channel), "MIDI channel must be between 1 and 16");
        
        if (note < 0 || note > 127)
            throw new ArgumentOutOfRangeException(nameof(note), "MIDI note must be between 0 and 127");
        
        if (velocity < 1 || velocity > 127)
            throw new ArgumentOutOfRangeException(nameof(velocity), "MIDI velocity must be between 1 and 127");

        try
        {
            var noteOnEvent = new NoteOnEvent((SevenBitNumber)note, (SevenBitNumber)velocity)
            {
                Channel = (FourBitNumber)(channel - 1) // DryWetMIDI uses 0-based channels
            };

            var outputDevice = ((MidiDeviceManager)_deviceManager).GetOutputDevice();
            if (outputDevice != null)
            {
                outputDevice.SendEvent(noteOnEvent);
                _logger.LogDebug("Sent note on: Channel={Channel}, Note={Note}, Velocity={Velocity}", 
                    channel, note, velocity);
            }
            else
            {
                _logger.LogWarning("No MIDI output device available for note on");
                
                // Attempt to reconnect if we have an active device configured
                if (_deviceManager.ActiveDevice != null)
                {
                    _ = Task.Run(async () => await _deviceManager.ReconnectActiveDeviceAsync());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send note on: Channel={Channel}, Note={Note}, Velocity={Velocity}", 
                channel, note, velocity);
            
            // Trigger reconnection attempt
            if (_deviceManager.ActiveDevice != null)
            {
                _ = Task.Run(async () => await _deviceManager.ReconnectActiveDeviceAsync());
            }
        }
    }

    public void SendNoteOff(int channel, int note)
    {
        if (channel < 1 || channel > 16)
            throw new ArgumentOutOfRangeException(nameof(channel), "MIDI channel must be between 1 and 16");
        
        if (note < 0 || note > 127)
            throw new ArgumentOutOfRangeException(nameof(note), "MIDI note must be between 0 and 127");

        try
        {
            var noteOffEvent = new NoteOffEvent((SevenBitNumber)note, (SevenBitNumber)0)
            {
                Channel = (FourBitNumber)(channel - 1) // DryWetMIDI uses 0-based channels
            };

            var outputDevice = ((MidiDeviceManager)_deviceManager).GetOutputDevice();
            if (outputDevice != null)
            {
                outputDevice.SendEvent(noteOffEvent);
                _logger.LogDebug("Sent note off: Channel={Channel}, Note={Note}", channel, note);
            }
            else
            {
                _logger.LogWarning("No MIDI output device available for note off");
                
                // Attempt to reconnect if we have an active device configured
                if (_deviceManager.ActiveDevice != null)
                {
                    _ = Task.Run(async () => await _deviceManager.ReconnectActiveDeviceAsync());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send note off: Channel={Channel}, Note={Note}", channel, note);
            
            // Trigger reconnection attempt
            if (_deviceManager.ActiveDevice != null)
            {
                _ = Task.Run(async () => await _deviceManager.ReconnectActiveDeviceAsync());
            }
        }
    }

    public bool ConnectToAbleton()
    {
        try
        {
            _logger.LogInformation("Attempting to connect to Ableton Live");
            return _abletonSyncService.Connect();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Ableton Live");
            return false;
        }
    }

    public void DisconnectFromAbleton()
    {
        try
        {
            _logger.LogInformation("Disconnecting from Ableton Live");
            _abletonSyncService.Disconnect();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect from Ableton Live");
        }
    }

    private void OnDeviceConnectionChanged(object? sender, DeviceConnectionEventArgs e)
    {
        if (e.IsConnected)
        {
            _logger.LogInformation("MIDI device connected: {DeviceName}", e.Device.Name);
        }
        else
        {
            _logger.LogWarning("MIDI device disconnected: {DeviceName}, Error: {Error}", 
                e.Device.Name, e.ErrorMessage ?? "Unknown");
        }
    }

    private void OnDevicesChanged(object? sender, EventArgs e)
    {
        _logger.LogInformation("MIDI device list changed - devices were added or removed");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                // Unsubscribe from events
                _deviceManager.DeviceConnectionChanged -= OnDeviceConnectionChanged;
                _deviceManager.DevicesChanged -= OnDevicesChanged;
                
                DisconnectFromAbleton();
                
                _deviceManager?.Dispose();
                _abletonSyncService?.Dispose();
                
                _logger.LogInformation("MIDI service disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MIDI service disposal");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}