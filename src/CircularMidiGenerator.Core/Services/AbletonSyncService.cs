using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Core;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Ableton Live synchronization service implementation
/// </summary>
public class AbletonSyncService : IAbletonSyncService
{
    private readonly ILogger<AbletonSyncService> _logger;
    private readonly Timer _syncTimer;
    private readonly object _syncLock = new object();
    
    private InputDevice? _midiClockInput;
    private bool _disposed;
    private bool _isConnected;
    private double _currentTempo = 120.0;
    private double _fallbackTempo = 120.0;
    private double _currentPosition;
    private DateTime _lastClockReceived = DateTime.MinValue;
    private int _clockCount;
    private readonly TimeSpan _syncTimeout = TimeSpan.FromSeconds(2);

    public event EventHandler<TempoChangedEventArgs>? TempoChanged;
    public event EventHandler<SyncPositionEventArgs>? PositionChanged;
    public event EventHandler? SyncLost;

    public AbletonSyncService(ILogger<AbletonSyncService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Timer to check for sync timeout and update position
        _syncTimer = new Timer(OnSyncTimerTick, null, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50));
    }

    public bool IsConnected => _isConnected;
    public double CurrentTempo => _currentTempo;
    public double CurrentPosition => _currentPosition;

    public bool Connect()
    {
        try
        {
            _logger.LogInformation("Attempting to connect to Ableton Live for synchronization");
            
            lock (_syncLock)
            {
                // Disconnect existing connection
                Disconnect();
                
                // Try to find a MIDI input device that might be Ableton Live
                var inputDevices = InputDevice.GetAll();
                foreach (var deviceInfo in inputDevices)
                {
                    // Look for devices that might be Ableton Live
                    if (deviceInfo.Name.Contains("Ableton", StringComparison.OrdinalIgnoreCase) ||
                        deviceInfo.Name.Contains("Live", StringComparison.OrdinalIgnoreCase) ||
                        deviceInfo.Name.Contains("IAC", StringComparison.OrdinalIgnoreCase)) // macOS Inter-App Communication
                    {
                        try
                        {
                            _midiClockInput = InputDevice.GetByName(deviceInfo.Name);
                            _midiClockInput.EventReceived += OnMidiEventReceived;
                            _midiClockInput.StartEventsListening();
                            
                            _isConnected = true;
                            _logger.LogInformation("Connected to MIDI device for sync: {DeviceName}", deviceInfo.Name);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to connect to MIDI device: {DeviceName}", deviceInfo.Name);
                            _midiClockInput?.Dispose();
                            _midiClockInput = null;
                        }
                    }
                }
                
                // If no Ableton-specific device found, try to use any available MIDI input
                if (!_isConnected && inputDevices.Any())
                {
                    try
                    {
                        var firstDevice = inputDevices.First();
                        _midiClockInput = InputDevice.GetByName(firstDevice.Name);
                        _midiClockInput.EventReceived += OnMidiEventReceived;
                        _midiClockInput.StartEventsListening();
                        
                        _isConnected = true;
                        _logger.LogInformation("Connected to first available MIDI device for sync: {DeviceName}", firstDevice.Name);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to connect to any MIDI input device");
                        _midiClockInput?.Dispose();
                        _midiClockInput = null;
                    }
                }
                
                _logger.LogWarning("No suitable MIDI input devices found for Ableton Live synchronization");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Ableton Live");
            _isConnected = false;
            return false;
        }
    }

    public void Disconnect()
    {
        lock (_syncLock)
        {
            try
            {
                if (_midiClockInput != null)
                {
                    _midiClockInput.EventReceived -= OnMidiEventReceived;
                    _midiClockInput.StopEventsListening();
                    _midiClockInput.Dispose();
                    _midiClockInput = null;
                }
                
                _isConnected = false;
                _currentTempo = _fallbackTempo;
                _logger.LogInformation("Disconnected from Ableton Live, using fallback tempo: {Tempo} BPM", _fallbackTempo);
                
                // Notify about tempo change to fallback
                TempoChanged?.Invoke(this, new TempoChangedEventArgs(_currentTempo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Ableton Live disconnection");
            }
        }
    }

    public void SetFallbackTempo(double tempo)
    {
        if (tempo <= 0)
            throw new ArgumentOutOfRangeException(nameof(tempo), "Tempo must be greater than 0");
        
        _fallbackTempo = tempo;
        
        if (!_isConnected)
        {
            var oldTempo = _currentTempo;
            _currentTempo = _fallbackTempo;
            
            if (Math.Abs(oldTempo - _currentTempo) > 0.1)
            {
                _logger.LogDebug("Updated fallback tempo to {Tempo} BPM", _fallbackTempo);
                TempoChanged?.Invoke(this, new TempoChangedEventArgs(_currentTempo));
            }
        }
    }

    private void OnMidiEventReceived(object? sender, MidiEventReceivedEventArgs e)
    {
        try
        {
            switch (e.Event)
            {
                case TimingClockEvent:
                    HandleMidiClock();
                    break;
                    
                case StartEvent:
                    HandleMidiStart();
                    break;
                    
                case StopEvent:
                    HandleMidiStop();
                    break;
                    
                case ContinueEvent:
                    HandleMidiContinue();
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MIDI sync event");
        }
    }

    private void HandleMidiClock()
    {
        lock (_syncLock)
        {
            var now = DateTime.UtcNow;
            _lastClockReceived = now;
            _clockCount++;
            
            // MIDI clock sends 24 pulses per quarter note
            // Calculate tempo based on clock timing
            if (_clockCount >= 24)
            {
                var timeSinceLastMeasure = now - _lastClockReceived.AddMilliseconds(-_clockCount * 20); // Rough estimate
                if (timeSinceLastMeasure.TotalMilliseconds > 0)
                {
                    var newTempo = 60000.0 / (timeSinceLastMeasure.TotalMilliseconds / (_clockCount / 24.0));
                    
                    if (Math.Abs(newTempo - _currentTempo) > 0.5 && newTempo > 60 && newTempo < 200)
                    {
                        _currentTempo = newTempo;
                        TempoChanged?.Invoke(this, new TempoChangedEventArgs(_currentTempo));
                        _logger.LogDebug("Tempo updated from MIDI clock: {Tempo} BPM", _currentTempo);
                    }
                }
                _clockCount = 0;
            }
            
            // Update position (simplified - in real implementation this would be more sophisticated)
            _currentPosition += 1.0 / 24.0; // Each clock is 1/24 of a quarter note
            PositionChanged?.Invoke(this, new SyncPositionEventArgs(_currentPosition));
        }
    }

    private void HandleMidiStart()
    {
        lock (_syncLock)
        {
            _currentPosition = 0;
            _clockCount = 0;
            _logger.LogDebug("MIDI start received, resetting position");
            PositionChanged?.Invoke(this, new SyncPositionEventArgs(_currentPosition));
        }
    }

    private void HandleMidiStop()
    {
        lock (_syncLock)
        {
            _logger.LogDebug("MIDI stop received");
            // Position remains where it stopped
        }
    }

    private void HandleMidiContinue()
    {
        lock (_syncLock)
        {
            _logger.LogDebug("MIDI continue received");
            // Continue from current position
        }
    }

    private void OnSyncTimerTick(object? state)
    {
        try
        {
            lock (_syncLock)
            {
                // Check for sync timeout
                if (_isConnected && DateTime.UtcNow - _lastClockReceived > _syncTimeout)
                {
                    _logger.LogWarning("MIDI sync timeout, falling back to manual tempo");
                    _isConnected = false;
                    _currentTempo = _fallbackTempo;
                    
                    SyncLost?.Invoke(this, EventArgs.Empty);
                    TempoChanged?.Invoke(this, new TempoChangedEventArgs(_currentTempo));
                }
                
                // Update position based on current tempo when not receiving clock
                if (!_isConnected)
                {
                    var deltaTime = 0.05; // 50ms timer interval
                    var beatsPerSecond = _currentTempo / 60.0;
                    _currentPosition += beatsPerSecond * deltaTime;
                    
                    PositionChanged?.Invoke(this, new SyncPositionEventArgs(_currentPosition));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in sync timer tick");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _syncTimer?.Dispose();
                Disconnect();
                _logger.LogInformation("Ableton sync service disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Ableton sync service disposal");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}