using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// High-precision timing service for disk rotation and playhead management
/// </summary>
public class TimingService : ITimingService, IDisposable
{
    private readonly ILogger<TimingService> _logger;
    private readonly IAbletonSyncService _abletonSyncService;
    private readonly Timer _timingTimer;
    private readonly Stopwatch _stopwatch;
    private readonly object _timingLock = new object();
    
    private bool _disposed;
    private bool _isPlaying;
    private double _currentBPM = 120.0;
    private bool _isAbletonSyncEnabled;
    private double _currentAngle;
    private DateTime _lastUpdate = DateTime.UtcNow;
    private readonly List<Marker> _trackedMarkers = new();
    private readonly Dictionary<Guid, DateTime> _markerTriggerHistory = new();
    
    // Timing constants
    private const double TIMER_INTERVAL_MS = 16.67; // ~60 FPS for smooth animation
    private const double TRIGGER_THRESHOLD_DEGREES = 2.0; // Degrees around 12 o'clock for triggering
    private const double DOUBLE_TRIGGER_PREVENTION_MS = 50; // Minimum time between triggers for same marker
    
    public event EventHandler<PlayheadEventArgs>? PlayheadMoved;
    public event EventHandler<MarkerTriggeredEventArgs>? MarkerTriggered;

    public TimingService(ILogger<TimingService> logger, IAbletonSyncService abletonSyncService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _abletonSyncService = abletonSyncService ?? throw new ArgumentNullException(nameof(abletonSyncService));
        
        _stopwatch = new Stopwatch();
        
        // High-precision timer for smooth disk rotation
        _timingTimer = new Timer(OnTimingTick, null, Timeout.Infinite, Timeout.Infinite);
        
        // Subscribe to Ableton sync events
        _abletonSyncService.TempoChanged += OnAbletonTempoChanged;
        _abletonSyncService.SyncLost += OnAbletonSyncLost;
        
        _logger.LogInformation("TimingService initialized with {Interval}ms timer interval", TIMER_INTERVAL_MS);
    }

    public double CurrentAngle => _currentAngle;
    public bool IsPlaying => _isPlaying;
    public double CurrentBPM => _currentBPM;
    public bool IsAbletonSyncEnabled => _isAbletonSyncEnabled;

    public void Start()
    {
        lock (_timingLock)
        {
            if (_isPlaying)
            {
                _logger.LogDebug("TimingService already playing, ignoring start request");
                return;
            }

            _logger.LogInformation("Starting disk rotation at {BPM} BPM", _currentBPM);
            
            _isPlaying = true;
            _lastUpdate = DateTime.UtcNow;
            _stopwatch.Restart();
            
            // Start high-precision timer
            _timingTimer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(TIMER_INTERVAL_MS));
            
            // If Ableton sync is enabled, try to connect
            if (_isAbletonSyncEnabled)
            {
                Task.Run(() =>
                {
                    try
                    {
                        if (!_abletonSyncService.IsConnected)
                        {
                            _abletonSyncService.Connect();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to connect to Ableton Live during start");
                    }
                });
            }
        }
    }

    public void Stop()
    {
        lock (_timingLock)
        {
            if (!_isPlaying)
            {
                _logger.LogDebug("TimingService already stopped, ignoring stop request");
                return;
            }

            _logger.LogInformation("Stopping disk rotation");
            
            _isPlaying = false;
            _stopwatch.Stop();
            
            // Stop timer
            _timingTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            // Clear marker trigger history
            _markerTriggerHistory.Clear();
        }
    }

    public void SetBPM(double bpm)
    {
        if (bpm <= 0)
            throw new ArgumentOutOfRangeException(nameof(bpm), "BPM must be greater than 0");

        lock (_timingLock)
        {
            var oldBPM = _currentBPM;
            _currentBPM = bpm;
            
            _logger.LogDebug("BPM changed from {OldBPM} to {NewBPM}", oldBPM, bpm);
            
            // Update Ableton sync service fallback tempo
            if (_abletonSyncService != null)
            {
                _abletonSyncService.SetFallbackTempo(bpm);
            }
        }
    }

    public void EnableAbletonSync(bool enabled)
    {
        lock (_timingLock)
        {
            if (_isAbletonSyncEnabled == enabled)
                return;

            _isAbletonSyncEnabled = enabled;
            _logger.LogInformation("Ableton sync {Status}", enabled ? "enabled" : "disabled");

            if (enabled)
            {
                // Try to connect to Ableton Live
                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(100); // Small delay to avoid blocking
                        if (_abletonSyncService.Connect())
                        {
                            _logger.LogInformation("Successfully connected to Ableton Live");
                        }
                        else
                        {
                            _logger.LogWarning("Failed to connect to Ableton Live, using manual BPM");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error connecting to Ableton Live");
                    }
                });
            }
            else
            {
                // Disconnect from Ableton Live
                _abletonSyncService.Disconnect();
            }
        }
    }

    /// <summary>
    /// Registers markers for triggering detection
    /// </summary>
    /// <param name="markers">Collection of markers to track</param>
    public void SetMarkers(IEnumerable<Marker> markers)
    {
        lock (_timingLock)
        {
            _trackedMarkers.Clear();
            _trackedMarkers.AddRange(markers ?? Enumerable.Empty<Marker>());
            
            _logger.LogDebug("Updated tracked markers count: {Count}", _trackedMarkers.Count);
        }
    }

    private void OnTimingTick(object? state)
    {
        try
        {
            if (!_isPlaying)
                return;

            lock (_timingLock)
            {
                var now = DateTime.UtcNow;
                var deltaTime = (now - _lastUpdate).TotalSeconds;
                _lastUpdate = now;

                // Calculate rotation speed based on current BPM
                // One full rotation = one measure (4 beats)
                // Degrees per second = (BPM / 60) * (360 / 4) = BPM * 1.5
                var degreesPerSecond = GetEffectiveBPM() * 1.5;
                var angleDelta = degreesPerSecond * deltaTime;

                // Update current angle
                _currentAngle = (_currentAngle + angleDelta) % 360.0;

                // Fire playhead moved event
                var elapsedTime = _stopwatch.Elapsed;
                PlayheadMoved?.Invoke(this, new PlayheadEventArgs(_currentAngle, elapsedTime));

                // Check for marker triggering
                CheckMarkerTriggering(_currentAngle);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in timing tick");
        }
    }

    private double GetEffectiveBPM()
    {
        // Use Ableton tempo if sync is enabled and connected, otherwise use manual BPM
        if (_isAbletonSyncEnabled && _abletonSyncService.IsConnected)
        {
            return _abletonSyncService.CurrentTempo;
        }
        return _currentBPM;
    }

    private void CheckMarkerTriggering(double currentAngle)
    {
        var now = DateTime.UtcNow;
        
        foreach (var marker in _trackedMarkers)
        {
            // Calculate where this marker appears relative to the stationary playhead at 12 o'clock
            // As the disk rotates, markers move in the opposite direction relative to the playhead
            var markerPositionRelativeToPlayhead = (marker.Angle - currentAngle + 360) % 360;
            
            // Check if marker is within trigger threshold of 12 o'clock (0 degrees)
            var angleDifference = CalculateAngleDifference(markerPositionRelativeToPlayhead, 0);
            
            if (angleDifference <= TRIGGER_THRESHOLD_DEGREES)
            {
                // Check for double-trigger prevention
                if (_markerTriggerHistory.TryGetValue(marker.Id, out var lastTrigger))
                {
                    var timeSinceLastTrigger = (now - lastTrigger).TotalMilliseconds;
                    if (timeSinceLastTrigger < DOUBLE_TRIGGER_PREVENTION_MS)
                    {
                        continue; // Skip this trigger to prevent double-triggering
                    }
                }

                // Trigger the marker
                TriggerMarker(marker, markerPositionRelativeToPlayhead);
                _markerTriggerHistory[marker.Id] = now;
            }
        }

        // Clean up old trigger history entries (older than 1 second)
        var cutoffTime = now.AddSeconds(-1);
        var keysToRemove = _markerTriggerHistory
            .Where(kvp => kvp.Value < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();
        
        foreach (var key in keysToRemove)
        {
            _markerTriggerHistory.Remove(key);
        }
    }

    private static double CalculateAngleDifference(double angle1, double angle2)
    {
        // Calculate the shortest angular distance between two angles
        // Account for circular nature (359° is close to 0°)
        var difference = Math.Abs(angle1 - angle2);
        
        // Handle wrap-around case
        if (difference > 180)
        {
            difference = 360 - difference;
        }
        
        return difference;
    }

    private void TriggerMarker(Marker marker, double triggerAngle)
    {
        try
        {
            // Update marker state
            marker.IsActive = true;
            marker.LastTriggered = DateTime.UtcNow;
            
            _logger.LogDebug("Triggering marker {MarkerId} at angle {Angle}° (trigger angle: {TriggerAngle}°)", 
                marker.Id, marker.Angle, triggerAngle);
            
            // Fire marker triggered event
            MarkerTriggered?.Invoke(this, new MarkerTriggeredEventArgs(marker, triggerAngle));
            
            // Schedule marker deactivation after a short delay
            Task.Delay(100).ContinueWith(_ =>
            {
                marker.IsActive = false;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering marker {MarkerId}", marker.Id);
        }
    }

    private void OnAbletonTempoChanged(object? sender, TempoChangedEventArgs e)
    {
        lock (_timingLock)
        {
            _logger.LogDebug("Ableton tempo changed to {Tempo} BPM", e.Tempo);
            // The effective BPM will be updated automatically in GetEffectiveBPM()
        }
    }

    private void OnAbletonSyncLost(object? sender, EventArgs e)
    {
        _logger.LogWarning("Ableton sync lost, falling back to manual BPM: {BPM}", _currentBPM);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                Stop();
                
                _timingTimer?.Dispose();
                _stopwatch?.Stop();
                
                // Unsubscribe from Ableton sync events
                if (_abletonSyncService != null)
                {
                    _abletonSyncService.TempoChanged -= OnAbletonTempoChanged;
                    _abletonSyncService.SyncLost -= OnAbletonSyncLost;
                }
                
                _logger.LogInformation("TimingService disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during TimingService disposal");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}