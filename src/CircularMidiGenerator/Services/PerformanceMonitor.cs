using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace CircularMidiGenerator.Services;

/// <summary>
/// Performance metrics for monitoring
/// </summary>
public class PerformanceMetrics : ReactiveObject
{
    private double _frameRate;
    private double _cpuUsage;
    private long _memoryUsage;
    private double _midiLatency;
    private int _activeMarkers;
    private double _renderTime;

    /// <summary>
    /// Current frame rate (FPS)
    /// </summary>
    public double FrameRate
    {
        get => _frameRate;
        set => this.RaiseAndSetIfChanged(ref _frameRate, value);
    }

    /// <summary>
    /// CPU usage percentage
    /// </summary>
    public double CpuUsage
    {
        get => _cpuUsage;
        set => this.RaiseAndSetIfChanged(ref _cpuUsage, value);
    }

    /// <summary>
    /// Memory usage in MB
    /// </summary>
    public long MemoryUsage
    {
        get => _memoryUsage;
        set => this.RaiseAndSetIfChanged(ref _memoryUsage, value);
    }

    /// <summary>
    /// MIDI output latency in milliseconds
    /// </summary>
    public double MidiLatency
    {
        get => _midiLatency;
        set => this.RaiseAndSetIfChanged(ref _midiLatency, value);
    }

    /// <summary>
    /// Number of active markers being processed
    /// </summary>
    public int ActiveMarkers
    {
        get => _activeMarkers;
        set => this.RaiseAndSetIfChanged(ref _activeMarkers, value);
    }

    /// <summary>
    /// Average render time per frame in milliseconds
    /// </summary>
    public double RenderTime
    {
        get => _renderTime;
        set => this.RaiseAndSetIfChanged(ref _renderTime, value);
    }
}

/// <summary>
/// Performance monitoring and optimization service
/// </summary>
public class PerformanceMonitor : IDisposable
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly Timer _monitoringTimer;
    private readonly Stopwatch _frameStopwatch = new();
    private readonly Queue<double> _frameTimeHistory = new();
    private readonly Queue<double> _renderTimeHistory = new();
    private readonly Queue<double> _midiLatencyHistory = new();
    
    private PerformanceCounter? _cpuCounter;
    private Process _currentProcess;
    private bool _disposed;
    private int _frameCount;
    private double _lastFrameTime;

    public PerformanceMetrics Metrics { get; } = new();

    public event EventHandler<PerformanceWarningEventArgs>? PerformanceWarning;

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger;
        _currentProcess = Process.GetCurrentProcess();
        
        InitializePerformanceCounters();
        
        // Monitor performance every second
        _monitoringTimer = new Timer(UpdateMetrics, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        
        _frameStopwatch.Start();
    }

    /// <summary>
    /// Record a frame render time
    /// </summary>
    public void RecordFrameTime(double renderTimeMs)
    {
        var currentTime = _frameStopwatch.Elapsed.TotalMilliseconds;
        var frameTime = currentTime - _lastFrameTime;
        _lastFrameTime = currentTime;

        // Track frame times
        _frameTimeHistory.Enqueue(frameTime);
        if (_frameTimeHistory.Count > 60) // Keep last 60 frames
        {
            _frameTimeHistory.Dequeue();
        }

        // Track render times
        _renderTimeHistory.Enqueue(renderTimeMs);
        if (_renderTimeHistory.Count > 60)
        {
            _renderTimeHistory.Dequeue();
        }

        _frameCount++;

        // Check for performance issues
        CheckPerformanceThresholds(frameTime, renderTimeMs);
    }

    /// <summary>
    /// Record MIDI output latency
    /// </summary>
    public void RecordMidiLatency(double latencyMs)
    {
        _midiLatencyHistory.Enqueue(latencyMs);
        if (_midiLatencyHistory.Count > 100) // Keep last 100 measurements
        {
            _midiLatencyHistory.Dequeue();
        }

        // Update metrics
        Metrics.MidiLatency = _midiLatencyHistory.Count > 0 ? _midiLatencyHistory.Average() : 0;

        // Check for high latency
        if (latencyMs > 10.0) // More than 10ms is concerning for real-time audio
        {
            PerformanceWarning?.Invoke(this, new PerformanceWarningEventArgs
            {
                Type = PerformanceWarningType.HighMidiLatency,
                Message = $"High MIDI latency detected: {latencyMs:F2}ms",
                Value = latencyMs,
                Threshold = 10.0
            });
        }
    }

    /// <summary>
    /// Update active marker count
    /// </summary>
    public void UpdateActiveMarkers(int count)
    {
        Metrics.ActiveMarkers = count;
    }

    /// <summary>
    /// Get performance optimization suggestions
    /// </summary>
    public List<string> GetOptimizationSuggestions()
    {
        var suggestions = new List<string>();

        if (Metrics.FrameRate < 30)
        {
            suggestions.Add("Frame rate is low. Consider reducing visual effects or marker count.");
        }

        if (Metrics.CpuUsage > 80)
        {
            suggestions.Add("High CPU usage detected. Close other applications or reduce processing load.");
        }

        if (Metrics.MemoryUsage > 500) // 500MB
        {
            suggestions.Add("High memory usage. Consider restarting the application periodically.");
        }

        if (Metrics.MidiLatency > 5)
        {
            suggestions.Add("MIDI latency is high. Check audio driver settings and buffer sizes.");
        }

        if (Metrics.RenderTime > 16.67) // More than 60fps target
        {
            suggestions.Add("Rendering is slow. Consider reducing visual complexity.");
        }

        if (Metrics.ActiveMarkers > 1000)
        {
            suggestions.Add("Large number of markers may impact performance. Consider organizing into groups.");
        }

        return suggestions;
    }

    /// <summary>
    /// Force garbage collection and memory cleanup
    /// </summary>
    public void OptimizeMemory()
    {
        try
        {
            _logger.LogInformation("Performing memory optimization...");
            
            var beforeMemory = GC.GetTotalMemory(false) / 1024 / 1024;
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var afterMemory = GC.GetTotalMemory(false) / 1024 / 1024;
            var freed = beforeMemory - afterMemory;
            
            _logger.LogInformation("Memory optimization completed. Freed {FreedMB}MB (Before: {BeforeMB}MB, After: {AfterMB}MB)",
                freed, beforeMemory, afterMemory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during memory optimization");
        }
    }

    private void InitializePerformanceCounters()
    {
        try
        {
            // Initialize CPU counter (may not work on all platforms)
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue(); // First call returns 0, so we call it once
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not initialize CPU performance counter");
        }
    }

    private void UpdateMetrics(object? state)
    {
        if (_disposed) return;

        try
        {
            // Update frame rate
            if (_frameTimeHistory.Count > 0)
            {
                var avgFrameTime = _frameTimeHistory.Average();
                Metrics.FrameRate = avgFrameTime > 0 ? 1000.0 / avgFrameTime : 0;
            }

            // Update render time
            if (_renderTimeHistory.Count > 0)
            {
                Metrics.RenderTime = _renderTimeHistory.Average();
            }

            // Update CPU usage
            try
            {
                if (_cpuCounter != null)
                {
                    Metrics.CpuUsage = _cpuCounter.NextValue();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not read CPU usage");
            }

            // Update memory usage
            _currentProcess.Refresh();
            Metrics.MemoryUsage = _currentProcess.WorkingSet64 / 1024 / 1024; // Convert to MB

            // Log performance metrics periodically
            if (_frameCount % 300 == 0) // Every 5 minutes at 60fps
            {
                _logger.LogInformation("Performance: FPS={FPS:F1}, CPU={CPU:F1}%, Memory={Memory}MB, MIDI Latency={Latency:F2}ms",
                    Metrics.FrameRate, Metrics.CpuUsage, Metrics.MemoryUsage, Metrics.MidiLatency);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating performance metrics");
        }
    }

    private void CheckPerformanceThresholds(double frameTime, double renderTime)
    {
        // Check for dropped frames (less than 30 FPS)
        if (frameTime > 33.33) // 30 FPS threshold
        {
            PerformanceWarning?.Invoke(this, new PerformanceWarningEventArgs
            {
                Type = PerformanceWarningType.DroppedFrames,
                Message = $"Frame rate dropped below 30 FPS (frame time: {frameTime:F2}ms)",
                Value = frameTime,
                Threshold = 33.33
            });
        }

        // Check for slow rendering
        if (renderTime > 16.67) // 60 FPS target
        {
            PerformanceWarning?.Invoke(this, new PerformanceWarningEventArgs
            {
                Type = PerformanceWarningType.SlowRendering,
                Message = $"Slow rendering detected: {renderTime:F2}ms",
                Value = renderTime,
                Threshold = 16.67
            });
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _monitoringTimer?.Dispose();
            _cpuCounter?.Dispose();
            _currentProcess?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Performance warning event arguments
/// </summary>
public class PerformanceWarningEventArgs : EventArgs
{
    public PerformanceWarningType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public double Value { get; set; }
    public double Threshold { get; set; }
}

/// <summary>
/// Types of performance warnings
/// </summary>
public enum PerformanceWarningType
{
    DroppedFrames,
    SlowRendering,
    HighMidiLatency,
    HighCpuUsage,
    HighMemoryUsage
}