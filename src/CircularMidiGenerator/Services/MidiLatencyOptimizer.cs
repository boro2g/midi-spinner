using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CircularMidiGenerator.Core.Services;

namespace CircularMidiGenerator.Services;

/// <summary>
/// MIDI event with timing information
/// </summary>
public class TimedMidiEvent
{
    public int Channel { get; set; }
    public int Note { get; set; }
    public int Velocity { get; set; }
    public bool IsNoteOn { get; set; }
    public long ScheduledTicks { get; set; }
    public double LatencyMs { get; set; }
}

/// <summary>
/// Service for optimizing MIDI output latency and timing precision
/// </summary>
public class MidiLatencyOptimizer : IDisposable
{
    private readonly IMidiService _midiService;
    private readonly ILogger<MidiLatencyOptimizer> _logger;
    private readonly PerformanceMonitor? _performanceMonitor;
    
    private readonly ConcurrentQueue<TimedMidiEvent> _eventQueue = new();
    private readonly Timer _processingTimer;
    private readonly Stopwatch _timingStopwatch = Stopwatch.StartNew();
    
    private readonly object _statsLock = new();
    private readonly Queue<double> _latencyHistory = new();
    private double _averageLatency;
    private double _jitter;
    private int _processedEvents;
    private bool _disposed;

    // Optimization settings
    private int _bufferSizeMs = 10; // 10ms buffer for event scheduling
    private int _maxQueueSize = 1000;
    private bool _useHighPriorityThread = true;

    public MidiLatencyOptimizer(
        IMidiService midiService, 
        ILogger<MidiLatencyOptimizer> logger,
        PerformanceMonitor? performanceMonitor = null)
    {
        _midiService = midiService;
        _logger = logger;
        _performanceMonitor = performanceMonitor;

        // Use high-frequency timer for precise timing
        _processingTimer = new Timer(ProcessMidiEvents, null, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1));

        // Set thread priority for better timing
        if (_useHighPriorityThread)
        {
            try
            {
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not set high thread priority for MIDI processing");
            }
        }

        _logger.LogInformation("MIDI latency optimizer initialized with {BufferMs}ms buffer", _bufferSizeMs);
    }

    /// <summary>
    /// Current average latency in milliseconds
    /// </summary>
    public double AverageLatency
    {
        get
        {
            lock (_statsLock)
            {
                return _averageLatency;
            }
        }
    }

    /// <summary>
    /// Current timing jitter in milliseconds
    /// </summary>
    public double Jitter
    {
        get
        {
            lock (_statsLock)
            {
                return _jitter;
            }
        }
    }

    /// <summary>
    /// Number of events processed
    /// </summary>
    public int ProcessedEvents
    {
        get
        {
            lock (_statsLock)
            {
                return _processedEvents;
            }
        }
    }

    /// <summary>
    /// Current queue size
    /// </summary>
    public int QueueSize => _eventQueue.Count;

    /// <summary>
    /// Schedule a MIDI note on event with optimized timing
    /// </summary>
    public void ScheduleNoteOn(int channel, int note, int velocity, TimeSpan delay = default)
    {
        var scheduledTime = _timingStopwatch.ElapsedTicks + (long)(delay.TotalMilliseconds * Stopwatch.Frequency / 1000);
        
        var midiEvent = new TimedMidiEvent
        {
            Channel = channel,
            Note = note,
            Velocity = velocity,
            IsNoteOn = true,
            ScheduledTicks = scheduledTime
        };

        EnqueueEvent(midiEvent);
    }

    /// <summary>
    /// Schedule a MIDI note off event with optimized timing
    /// </summary>
    public void ScheduleNoteOff(int channel, int note, TimeSpan delay = default)
    {
        var scheduledTime = _timingStopwatch.ElapsedTicks + (long)(delay.TotalMilliseconds * Stopwatch.Frequency / 1000);
        
        var midiEvent = new TimedMidiEvent
        {
            Channel = channel,
            Note = note,
            Velocity = 0,
            IsNoteOn = false,
            ScheduledTicks = scheduledTime
        };

        EnqueueEvent(midiEvent);
    }

    /// <summary>
    /// Send immediate MIDI event (bypasses scheduling for real-time events)
    /// </summary>
    public void SendImmediate(int channel, int note, int velocity, bool isNoteOn)
    {
        var startTicks = _timingStopwatch.ElapsedTicks;
        
        try
        {
            if (isNoteOn)
            {
                _midiService.SendNoteOn(channel, note, velocity);
            }
            else
            {
                _midiService.SendNoteOff(channel, note);
            }

            var endTicks = _timingStopwatch.ElapsedTicks;
            var latencyMs = (endTicks - startTicks) * 1000.0 / Stopwatch.Frequency;
            
            UpdateLatencyStats(latencyMs);
            _performanceMonitor?.RecordMidiLatency(latencyMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending immediate MIDI event");
        }
    }

    /// <summary>
    /// Optimize buffer settings based on current performance
    /// </summary>
    public void OptimizeSettings()
    {
        lock (_statsLock)
        {
            // Adjust buffer size based on current latency and jitter
            if (_averageLatency > 5.0) // High latency
            {
                _bufferSizeMs = Math.Min(_bufferSizeMs + 2, 20);
                _logger.LogInformation("Increased MIDI buffer to {BufferMs}ms due to high latency", _bufferSizeMs);
            }
            else if (_averageLatency < 2.0 && _jitter < 1.0) // Low latency and stable
            {
                _bufferSizeMs = Math.Max(_bufferSizeMs - 1, 5);
                _logger.LogDebug("Decreased MIDI buffer to {BufferMs}ms", _bufferSizeMs);
            }

            // Adjust queue size based on usage
            var currentUsage = (double)_eventQueue.Count / _maxQueueSize;
            if (currentUsage > 0.8)
            {
                _maxQueueSize = Math.Min(_maxQueueSize * 2, 5000);
                _logger.LogWarning("Increased MIDI queue size to {QueueSize} due to high usage", _maxQueueSize);
            }
        }
    }

    /// <summary>
    /// Get performance statistics
    /// </summary>
    public MidiPerformanceStats GetPerformanceStats()
    {
        lock (_statsLock)
        {
            return new MidiPerformanceStats
            {
                AverageLatency = _averageLatency,
                Jitter = _jitter,
                ProcessedEvents = _processedEvents,
                QueueSize = _eventQueue.Count,
                BufferSize = _bufferSizeMs,
                MaxQueueSize = _maxQueueSize
            };
        }
    }

    /// <summary>
    /// Clear all pending events (panic function)
    /// </summary>
    public void ClearQueue()
    {
        while (_eventQueue.TryDequeue(out _)) { }
        _logger.LogInformation("MIDI event queue cleared");
    }

    private void EnqueueEvent(TimedMidiEvent midiEvent)
    {
        if (_eventQueue.Count >= _maxQueueSize)
        {
            _logger.LogWarning("MIDI event queue full, dropping event");
            return;
        }

        _eventQueue.Enqueue(midiEvent);
    }

    private void ProcessMidiEvents(object? state)
    {
        if (_disposed) return;

        var currentTicks = _timingStopwatch.ElapsedTicks;
        var eventsProcessed = 0;
        var maxEventsPerCycle = 50; // Limit processing to prevent blocking

        while (eventsProcessed < maxEventsPerCycle && _eventQueue.TryPeek(out var nextEvent))
        {
            // Check if event is ready to be processed
            var timeDifference = (nextEvent.ScheduledTicks - currentTicks) * 1000.0 / Stopwatch.Frequency;
            
            if (timeDifference > _bufferSizeMs)
            {
                break; // Event is not ready yet
            }

            // Dequeue and process the event
            if (_eventQueue.TryDequeue(out var eventToProcess))
            {
                ProcessSingleEvent(eventToProcess, currentTicks);
                eventsProcessed++;
            }
        }

        // Periodically optimize settings
        if (_processedEvents % 1000 == 0 && _processedEvents > 0)
        {
            OptimizeSettings();
        }
    }

    private void ProcessSingleEvent(TimedMidiEvent midiEvent, long currentTicks)
    {
        var processingStartTicks = _timingStopwatch.ElapsedTicks;
        
        try
        {
            if (midiEvent.IsNoteOn)
            {
                _midiService.SendNoteOn(midiEvent.Channel, midiEvent.Note, midiEvent.Velocity);
            }
            else
            {
                _midiService.SendNoteOff(midiEvent.Channel, midiEvent.Note);
            }

            var processingEndTicks = _timingStopwatch.ElapsedTicks;
            var actualLatency = (processingEndTicks - midiEvent.ScheduledTicks) * 1000.0 / Stopwatch.Frequency;
            var processingTime = (processingEndTicks - processingStartTicks) * 1000.0 / Stopwatch.Frequency;

            midiEvent.LatencyMs = Math.Abs(actualLatency);
            
            UpdateLatencyStats(midiEvent.LatencyMs);
            _performanceMonitor?.RecordMidiLatency(midiEvent.LatencyMs);

            lock (_statsLock)
            {
                _processedEvents++;
            }

            // Log timing issues
            if (Math.Abs(actualLatency) > 5.0)
            {
                _logger.LogDebug("MIDI timing deviation: {Latency:F2}ms (processing: {ProcessingTime:F2}ms)", 
                    actualLatency, processingTime);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MIDI event");
        }
    }

    private void UpdateLatencyStats(double latencyMs)
    {
        lock (_statsLock)
        {
            _latencyHistory.Enqueue(latencyMs);
            
            // Keep only recent measurements
            while (_latencyHistory.Count > 100)
            {
                _latencyHistory.Dequeue();
            }

            if (_latencyHistory.Count > 0)
            {
                _averageLatency = _latencyHistory.Average();
                
                // Calculate jitter (standard deviation)
                var variance = _latencyHistory.Sum(l => Math.Pow(l - _averageLatency, 2)) / _latencyHistory.Count;
                _jitter = Math.Sqrt(variance);
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _processingTimer?.Dispose();
            ClearQueue();
            _disposed = true;
            
            _logger.LogInformation("MIDI latency optimizer disposed. Final stats: Avg latency: {AvgLatency:F2}ms, Jitter: {Jitter:F2}ms, Events processed: {EventCount}",
                _averageLatency, _jitter, _processedEvents);
        }
    }
}

/// <summary>
/// MIDI performance statistics
/// </summary>
public class MidiPerformanceStats
{
    public double AverageLatency { get; set; }
    public double Jitter { get; set; }
    public int ProcessedEvents { get; set; }
    public int QueueSize { get; set; }
    public int BufferSize { get; set; }
    public int MaxQueueSize { get; set; }
}