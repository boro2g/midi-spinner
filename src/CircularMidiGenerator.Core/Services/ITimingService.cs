using System;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Event arguments for playhead movement
/// </summary>
public class PlayheadEventArgs : EventArgs
{
    public double CurrentAngle { get; }
    public TimeSpan ElapsedTime { get; }
    
    public PlayheadEventArgs(double currentAngle, TimeSpan elapsedTime)
    {
        CurrentAngle = currentAngle;
        ElapsedTime = elapsedTime;
    }
}

/// <summary>
/// Event arguments for marker triggering
/// </summary>
public class MarkerTriggeredEventArgs : EventArgs
{
    public Marker Marker { get; }
    public double TriggerAngle { get; }
    
    public MarkerTriggeredEventArgs(Marker marker, double triggerAngle)
    {
        Marker = marker;
        TriggerAngle = triggerAngle;
    }
}

/// <summary>
/// Service for timing, disk rotation, and playhead management
/// </summary>
public interface ITimingService
{
    /// <summary>
    /// Fired when playhead position changes
    /// </summary>
    event EventHandler<PlayheadEventArgs>? PlayheadMoved;
    
    /// <summary>
    /// Fired when a marker is triggered by the playhead
    /// </summary>
    event EventHandler<MarkerTriggeredEventArgs>? MarkerTriggered;
    
    /// <summary>
    /// Start disk rotation and timing
    /// </summary>
    void Start();
    
    /// <summary>
    /// Stop disk rotation and timing
    /// </summary>
    void Stop();
    
    /// <summary>
    /// Set the BPM for disk rotation speed
    /// </summary>
    /// <param name="bpm">Beats per minute</param>
    void SetBPM(double bpm);
    
    /// <summary>
    /// Enable or disable Ableton Live synchronization
    /// </summary>
    /// <param name="enabled">Whether to sync with Ableton</param>
    void EnableAbletonSync(bool enabled);
    
    /// <summary>
    /// Current playhead angle in degrees (0-360)
    /// </summary>
    double CurrentAngle { get; }
    
    /// <summary>
    /// Whether timing service is currently playing
    /// </summary>
    bool IsPlaying { get; }
    
    /// <summary>
    /// Current BPM setting
    /// </summary>
    double CurrentBPM { get; }
    
    /// <summary>
    /// Whether Ableton sync is enabled
    /// </summary>
    bool IsAbletonSyncEnabled { get; }
}