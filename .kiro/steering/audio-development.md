---
inclusion: always
---

# Real-Time Audio Development Guidelines

## Timing and Synchronization

### High-Precision Timing
- Use `Stopwatch` class for high-resolution timing measurements
- Avoid `DateTime.Now` for timing-critical operations
- Implement custom timing loops for audio-rate processing
- Use dedicated timing threads separate from UI threads

### MIDI Timing Best Practices
- Buffer MIDI events to smooth out timing jitter
- Use timestamp-based scheduling for precise note timing
- Implement proper note-off handling to prevent stuck notes
- Account for MIDI device latency in timing calculations

### Thread Safety
- Use lock-free data structures for audio threads
- Minimize or eliminate locks in real-time code paths
- Use atomic operations for simple shared state
- Implement proper memory barriers for multi-threaded access

## DryWetMIDI Integration Patterns

### Device Management
```csharp
public class MidiDeviceManager : IDisposable
{
    private OutputDevice _outputDevice;
    private readonly object _deviceLock = new object();

    public async Task<IEnumerable<MidiDevice>> GetAvailableDevicesAsync()
    {
        return OutputDevice.GetAll()
            .Select(info => new MidiDevice(info.Name, info.Id));
    }

    public void SetOutputDevice(int deviceId)
    {
        lock (_deviceLock)
        {
            _outputDevice?.Dispose();
            _outputDevice = OutputDevice.GetById(deviceId);
        }
    }

    public void SendNoteOn(int channel, int note, int velocity)
    {
        var noteOnEvent = new NoteOnEvent((SevenBitNumber)note, (SevenBitNumber)velocity)
        {
            Channel = (FourBitNumber)channel
        };
        
        _outputDevice?.SendEvent(noteOnEvent);
    }
}
```

### Event Scheduling
```csharp
public class MidiScheduler
{
    private readonly Playback _playback;
    private readonly TempoMap _tempoMap;

    public void ScheduleNote(int channel, int note, int velocity, TimeSpan delay)
    {
        var noteOn = new NoteOnEvent((SevenBitNumber)note, (SevenBitNumber)velocity)
        {
            Channel = (FourBitNumber)channel
        };

        var scheduledTime = _playback.GetCurrentTime<MetricTimeSpan>().Add(delay);
        _playback.EventPlayed += (sender, e) => HandleScheduledEvent(e, noteOn, scheduledTime);
    }
}
```

## Performance Optimization

### Memory Management
- Pre-allocate collections and reuse objects in hot paths
- Use object pooling for frequently created MIDI events
- Avoid garbage collection in audio threads
- Monitor memory pressure and implement backpressure

### CPU Optimization
- Profile audio processing code regularly
- Use SIMD instructions for mathematical operations when possible
- Minimize system calls in real-time threads
- Implement efficient algorithms for angle calculations and collision detection

### Latency Minimization
- Use small buffer sizes for low-latency audio
- Minimize processing in audio callbacks
- Use lock-free queues for inter-thread communication
- Profile and optimize critical code paths

## Error Handling in Audio Context

### Graceful Degradation
```csharp
public class RobustMidiService : IMidiService
{
    private OutputDevice _primaryDevice;
    private OutputDevice _fallbackDevice;

    public void SendNoteOn(int channel, int note, int velocity)
    {
        try
        {
            _primaryDevice?.SendEvent(CreateNoteOnEvent(channel, note, velocity));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary MIDI device failed, attempting fallback");
            
            try
            {
                _fallbackDevice?.SendEvent(CreateNoteOnEvent(channel, note, velocity));
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Both primary and fallback MIDI devices failed");
                // Continue without MIDI output rather than crashing
            }
        }
    }
}
```

### Device Reconnection
- Implement automatic device reconnection logic
- Handle device disconnection gracefully
- Provide user feedback for device status
- Cache device configurations for quick recovery

## Synchronization with External DAWs

### Ableton Live Integration
```csharp
public class AbletonSyncService
{
    private readonly Timer _syncTimer;
    private double _lastKnownTempo = 120.0;

    public void EnableSync()
    {
        // Implementation depends on Ableton Live's sync protocol
        // This might involve MIDI clock, OSC, or other protocols
        _syncTimer.Start();
    }

    private void OnSyncReceived(double tempo, double position)
    {
        if (Math.Abs(tempo - _lastKnownTempo) > 0.1)
        {
            _lastKnownTempo = tempo;
            TempoChanged?.Invoke(tempo);
        }
        
        PositionChanged?.Invoke(position);
    }
}
```

### Clock Synchronization
- Implement MIDI clock synchronization for tempo sync
- Handle tempo changes smoothly without audio glitches
- Provide fallback to internal clock when sync is lost
- Account for network latency in remote synchronization

## Testing Audio Code

### Unit Testing Strategies
- Mock MIDI devices for consistent testing
- Test timing calculations with known inputs
- Verify note on/off pairing correctness
- Test error handling and recovery scenarios

### Integration Testing
- Test with real MIDI devices when possible
- Verify timing accuracy under load
- Test device reconnection scenarios
- Measure and validate latency requirements

### Performance Testing
```csharp
[Fact]
public void MidiOutput_ShouldMaintainLowLatency()
{
    var stopwatch = Stopwatch.StartNew();
    var latencies = new List<TimeSpan>();

    for (int i = 0; i < 1000; i++)
    {
        var start = stopwatch.Elapsed;
        _midiService.SendNoteOn(1, 60, 100);
        var end = stopwatch.Elapsed;
        
        latencies.Add(end - start);
    }

    var averageLatency = latencies.Average(l => l.TotalMilliseconds);
    Assert.True(averageLatency < 1.0, $"Average latency {averageLatency}ms exceeds 1ms threshold");
}
```

## Common Pitfalls to Avoid

### Threading Issues
- Never call UI methods from audio threads
- Don't use Thread.Sleep() in audio code
- Avoid blocking operations in real-time threads
- Be careful with shared state between threads

### MIDI Protocol Issues
- Always send note-off events to prevent stuck notes
- Respect MIDI channel limits (1-16)
- Handle MIDI overflow gracefully
- Implement proper MIDI reset functionality

### Performance Traps
- Don't allocate memory in audio hot paths
- Avoid expensive operations like file I/O in real-time code
- Be careful with floating-point precision in timing calculations
- Profile regularly to catch performance regressions