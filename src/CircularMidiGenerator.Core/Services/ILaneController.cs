using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Event arguments for lane state changes
/// </summary>
public class LaneStateChangedEventArgs : EventArgs
{
    public int LaneId { get; }
    public string PropertyName { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }

    public LaneStateChangedEventArgs(int laneId, string propertyName, object? oldValue, object? newValue)
    {
        LaneId = laneId;
        PropertyName = propertyName;
        OldValue = oldValue;
        NewValue = newValue;
    }
}

/// <summary>
/// Event arguments for marker assignment changes
/// </summary>
public class MarkerAssignmentChangedEventArgs : EventArgs
{
    public Marker Marker { get; }
    public int OldLaneId { get; }
    public int NewLaneId { get; }

    public MarkerAssignmentChangedEventArgs(Marker marker, int oldLaneId, int newLaneId)
    {
        Marker = marker;
        OldLaneId = oldLaneId;
        NewLaneId = newLaneId;
    }
}

/// <summary>
/// Service for managing multiple lanes and their coordination
/// </summary>
public interface ILaneController
{
    /// <summary>
    /// Fired when a lane's state changes (mute, solo, quantization, etc.)
    /// </summary>
    event EventHandler<LaneStateChangedEventArgs>? LaneStateChanged;
    
    /// <summary>
    /// Fired when a marker is assigned to a different lane
    /// </summary>
    event EventHandler<MarkerAssignmentChangedEventArgs>? MarkerAssignmentChanged;
    
    /// <summary>
    /// Collection of all lanes
    /// </summary>
    ObservableCollection<Lane> Lanes { get; }
    
    /// <summary>
    /// Currently selected lane ID for new marker placement
    /// </summary>
    int SelectedLaneId { get; set; }
    
    /// <summary>
    /// Get a lane by its ID
    /// </summary>
    /// <param name="laneId">Lane ID</param>
    /// <returns>Lane instance or null if not found</returns>
    Lane? GetLane(int laneId);
    
    /// <summary>
    /// Get the currently selected lane
    /// </summary>
    /// <returns>Selected lane instance or null if invalid selection</returns>
    Lane? GetSelectedLane();
    
    /// <summary>
    /// Add a new lane with the specified settings
    /// </summary>
    /// <param name="name">Lane name</param>
    /// <param name="midiChannel">MIDI channel (1-16)</param>
    /// <returns>Created lane instance</returns>
    Lane AddLane(string name, int midiChannel);
    
    /// <summary>
    /// Remove a lane and reassign its markers to the default lane
    /// </summary>
    /// <param name="laneId">Lane ID to remove</param>
    /// <returns>True if lane was removed</returns>
    bool RemoveLane(int laneId);
    
    /// <summary>
    /// Assign a marker to a specific lane
    /// </summary>
    /// <param name="marker">Marker to assign</param>
    /// <param name="laneId">Target lane ID</param>
    void AssignMarkerToLane(Marker marker, int laneId);
    
    /// <summary>
    /// Move a marker from one lane to another
    /// </summary>
    /// <param name="marker">Marker to move</param>
    /// <param name="targetLaneId">Target lane ID</param>
    void MoveMarkerToLane(Marker marker, int targetLaneId);
    
    /// <summary>
    /// Set mute state for a lane
    /// </summary>
    /// <param name="laneId">Lane ID</param>
    /// <param name="isMuted">Mute state</param>
    void SetLaneMute(int laneId, bool isMuted);
    
    /// <summary>
    /// Set solo state for a lane
    /// </summary>
    /// <param name="laneId">Lane ID</param>
    /// <param name="isSoloed">Solo state</param>
    void SetLaneSolo(int laneId, bool isSoloed);
    
    /// <summary>
    /// Clear solo state from all lanes
    /// </summary>
    void ClearAllSolo();
    
    /// <summary>
    /// Check if any lanes are currently soloed
    /// </summary>
    /// <returns>True if any lanes are soloed</returns>
    bool AnyLanesSoloed();
    
    /// <summary>
    /// Get all lanes that should produce MIDI output based on mute/solo state
    /// </summary>
    /// <returns>Collection of active lanes</returns>
    IEnumerable<Lane> GetActiveLanes();
    
    /// <summary>
    /// Set quantization settings for a specific lane
    /// </summary>
    /// <param name="laneId">Lane ID</param>
    /// <param name="settings">Quantization settings</param>
    void SetLaneQuantization(int laneId, QuantizationSettings settings);
    
    /// <summary>
    /// Get quantization settings for a specific lane
    /// </summary>
    /// <param name="laneId">Lane ID</param>
    /// <returns>Quantization settings or null if lane not found</returns>
    QuantizationSettings? GetLaneQuantization(int laneId);
    
    /// <summary>
    /// Check if a lane should produce MIDI output based on current mute/solo state
    /// </summary>
    /// <param name="laneId">Lane ID to check</param>
    /// <returns>True if lane should produce output</returns>
    bool ShouldLaneProduceOutput(int laneId);
    
    /// <summary>
    /// Get all markers from all lanes
    /// </summary>
    /// <returns>Collection of all markers</returns>
    IEnumerable<Marker> GetAllMarkers();
    
    /// <summary>
    /// Get all markers from active lanes only
    /// </summary>
    /// <returns>Collection of markers from active lanes</returns>
    IEnumerable<Marker> GetActiveMarkers();
    
    /// <summary>
    /// Initialize default lanes
    /// </summary>
    void InitializeDefaultLanes();
    
    /// <summary>
    /// Clear all lanes and their markers
    /// </summary>
    void ClearAllLanes();
}