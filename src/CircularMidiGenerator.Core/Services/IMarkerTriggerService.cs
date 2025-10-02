using System;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Event arguments for marker trigger events with lane context
/// </summary>
public class MarkerTriggerEventArgs : EventArgs
{
    public Marker Marker { get; }
    public Lane Lane { get; }
    public double TriggerAngle { get; }
    public bool WasTriggered { get; }

    public MarkerTriggerEventArgs(Marker marker, Lane lane, double triggerAngle, bool wasTriggered)
    {
        Marker = marker;
        Lane = lane;
        TriggerAngle = triggerAngle;
        WasTriggered = wasTriggered;
    }
}

/// <summary>
/// Service that coordinates marker triggering with lane mute/solo state and MIDI output
/// </summary>
public interface IMarkerTriggerService
{
    /// <summary>
    /// Fired when a marker is processed for triggering (whether it actually triggers or not)
    /// </summary>
    event EventHandler<MarkerTriggerEventArgs>? MarkerProcessed;
    
    /// <summary>
    /// Initialize the service and start listening for timing events
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// Process a marker trigger event from the timing service
    /// </summary>
    /// <param name="marker">Marker that was triggered</param>
    /// <param name="triggerAngle">Angle at which the trigger occurred</param>
    void ProcessMarkerTrigger(Marker marker, double triggerAngle);
    
    /// <summary>
    /// Update the service with current markers from all lanes
    /// </summary>
    void UpdateMarkers();
    
    /// <summary>
    /// Force stop all currently playing notes (panic function)
    /// </summary>
    void StopAllNotes();
}