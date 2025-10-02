using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Service for managing multiple lanes and their coordination
/// </summary>
public class LaneController : ILaneController
{
    private readonly ILogger<LaneController> _logger;
    private readonly IQuantizationService _quantizationService;
    private int _selectedLaneId;
    private int _nextLaneId;

    public LaneController(ILogger<LaneController> logger, IQuantizationService quantizationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _quantizationService = quantizationService ?? throw new ArgumentNullException(nameof(quantizationService));
        
        Lanes = new ObservableCollection<Lane>();
        _nextLaneId = 0;
        
        _logger.LogInformation("LaneController initialized");
    }

    #region Events

    public event EventHandler<LaneStateChangedEventArgs>? LaneStateChanged;
    public event EventHandler<MarkerAssignmentChangedEventArgs>? MarkerAssignmentChanged;

    #endregion

    #region Properties

    public ObservableCollection<Lane> Lanes { get; }

    public int SelectedLaneId
    {
        get => _selectedLaneId;
        set
        {
            if (_selectedLaneId != value)
            {
                var oldValue = _selectedLaneId;
                _selectedLaneId = value;
                _logger.LogDebug("Selected lane changed from {OldLane} to {NewLane}", oldValue, value);
            }
        }
    }

    #endregion

    #region Lane Management

    public Lane? GetLane(int laneId)
    {
        return Lanes.FirstOrDefault(l => l.Id == laneId);
    }

    public Lane? GetSelectedLane()
    {
        return GetLane(SelectedLaneId);
    }

    public Lane AddLane(string name, int midiChannel)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Lane name cannot be empty", nameof(name));
        
        if (midiChannel < 1 || midiChannel > 16)
            throw new ArgumentOutOfRangeException(nameof(midiChannel), "MIDI channel must be between 1 and 16");

        var lane = new Lane(_nextLaneId++, name, midiChannel);
        
        // Set default quantization for the new lane
        _quantizationService.SetQuantization(lane.Id, lane.Quantization);
        
        Lanes.Add(lane);
        
        _logger.LogInformation("Added lane: {LaneName} (ID: {LaneId}, Channel: {MidiChannel})", 
            name, lane.Id, midiChannel);
        
        return lane;
    }

    public bool RemoveLane(int laneId)
    {
        var lane = GetLane(laneId);
        if (lane == null)
        {
            _logger.LogWarning("Attempted to remove non-existent lane: {LaneId}", laneId);
            return false;
        }

        // Don't allow removing the last lane
        if (Lanes.Count <= 1)
        {
            _logger.LogWarning("Cannot remove the last remaining lane");
            return false;
        }

        // Reassign markers to the first available lane
        var targetLane = Lanes.FirstOrDefault(l => l.Id != laneId);
        if (targetLane != null && lane.Markers.Count > 0)
        {
            var markersToMove = lane.Markers.ToList();
            foreach (var marker in markersToMove)
            {
                MoveMarkerToLane(marker, targetLane.Id);
            }
            
            _logger.LogInformation("Moved {MarkerCount} markers from lane {LaneId} to lane {TargetLaneId}", 
                markersToMove.Count, laneId, targetLane.Id);
        }

        Lanes.Remove(lane);
        
        // Update selected lane if necessary
        if (SelectedLaneId == laneId)
        {
            SelectedLaneId = Lanes.FirstOrDefault()?.Id ?? 0;
        }

        _logger.LogInformation("Removed lane: {LaneName} (ID: {LaneId})", lane.Name, laneId);
        return true;
    }

    #endregion

    #region Marker Assignment

    public void AssignMarkerToLane(Marker marker, int laneId)
    {
        if (marker == null)
            throw new ArgumentNullException(nameof(marker));

        var lane = GetLane(laneId);
        if (lane == null)
            throw new ArgumentException($"Lane with ID {laneId} not found", nameof(laneId));

        var oldLaneId = marker.Lane;
        
        // Remove from old lane if it exists
        if (oldLaneId != laneId)
        {
            var oldLane = GetLane(oldLaneId);
            oldLane?.RemoveMarker(marker);
        }

        // Add to new lane
        lane.AddMarker(marker);
        
        MarkerAssignmentChanged?.Invoke(this, new MarkerAssignmentChangedEventArgs(marker, oldLaneId, laneId));
        
        _logger.LogDebug("Assigned marker {MarkerId} to lane {LaneId}", marker.Id, laneId);
    }

    public void MoveMarkerToLane(Marker marker, int targetLaneId)
    {
        if (marker == null)
            throw new ArgumentNullException(nameof(marker));

        var targetLane = GetLane(targetLaneId);
        if (targetLane == null)
            throw new ArgumentException($"Target lane with ID {targetLaneId} not found", nameof(targetLaneId));

        var sourceLane = GetLane(marker.Lane);
        var oldLaneId = marker.Lane;

        // Remove from source lane
        sourceLane?.RemoveMarker(marker);
        
        // Add to target lane
        targetLane.AddMarker(marker);
        
        MarkerAssignmentChanged?.Invoke(this, new MarkerAssignmentChangedEventArgs(marker, oldLaneId, targetLaneId));
        
        _logger.LogDebug("Moved marker {MarkerId} from lane {SourceLane} to lane {TargetLane}", 
            marker.Id, oldLaneId, targetLaneId);
    }

    #endregion

    #region Mute/Solo Management

    public void SetLaneMute(int laneId, bool isMuted)
    {
        var lane = GetLane(laneId);
        if (lane == null)
        {
            _logger.LogWarning("Attempted to set mute on non-existent lane: {LaneId}", laneId);
            return;
        }

        var oldValue = lane.IsMuted;
        if (oldValue != isMuted)
        {
            lane.IsMuted = isMuted;
            
            LaneStateChanged?.Invoke(this, new LaneStateChangedEventArgs(laneId, nameof(Lane.IsMuted), oldValue, isMuted));
            
            _logger.LogInformation("Lane {LaneId} ({LaneName}) mute state changed to {IsMuted}", 
                laneId, lane.Name, isMuted);
        }
    }

    public void SetLaneSolo(int laneId, bool isSoloed)
    {
        var lane = GetLane(laneId);
        if (lane == null)
        {
            _logger.LogWarning("Attempted to set solo on non-existent lane: {LaneId}", laneId);
            return;
        }

        var oldValue = lane.IsSoloed;
        if (oldValue != isSoloed)
        {
            lane.IsSoloed = isSoloed;
            
            LaneStateChanged?.Invoke(this, new LaneStateChangedEventArgs(laneId, nameof(Lane.IsSoloed), oldValue, isSoloed));
            
            _logger.LogInformation("Lane {LaneId} ({LaneName}) solo state changed to {IsSoloed}", 
                laneId, lane.Name, isSoloed);
        }
    }

    public void ClearAllSolo()
    {
        var soloedLanes = Lanes.Where(l => l.IsSoloed).ToList();
        
        foreach (var lane in soloedLanes)
        {
            SetLaneSolo(lane.Id, false);
        }
        
        if (soloedLanes.Count > 0)
        {
            _logger.LogInformation("Cleared solo state from {Count} lanes", soloedLanes.Count);
        }
    }

    public bool AnyLanesSoloed()
    {
        return Lanes.Any(l => l.IsSoloed);
    }

    public IEnumerable<Lane> GetActiveLanes()
    {
        var anyLanesSoloed = AnyLanesSoloed();
        return Lanes.Where(l => l.ShouldProduceOutput(anyLanesSoloed));
    }

    public bool ShouldLaneProduceOutput(int laneId)
    {
        var lane = GetLane(laneId);
        if (lane == null) return false;
        
        return lane.ShouldProduceOutput(AnyLanesSoloed());
    }

    #endregion

    #region Quantization Management

    public void SetLaneQuantization(int laneId, QuantizationSettings settings)
    {
        var lane = GetLane(laneId);
        if (lane == null)
        {
            _logger.LogWarning("Attempted to set quantization on non-existent lane: {LaneId}", laneId);
            return;
        }

        var oldSettings = lane.Quantization;
        lane.Quantization = settings ?? throw new ArgumentNullException(nameof(settings));
        
        // Update quantization service
        _quantizationService.SetQuantization(laneId, settings);
        
        LaneStateChanged?.Invoke(this, new LaneStateChangedEventArgs(laneId, nameof(Lane.Quantization), oldSettings, settings));
        
        _logger.LogDebug("Updated quantization settings for lane {LaneId}: Enabled={Enabled}, Division={Division}", 
            laneId, settings.Enabled, settings.Division);
    }

    public QuantizationSettings? GetLaneQuantization(int laneId)
    {
        return GetLane(laneId)?.Quantization;
    }

    #endregion

    #region Marker Queries

    public IEnumerable<Marker> GetAllMarkers()
    {
        return Lanes.SelectMany(l => l.Markers);
    }

    public IEnumerable<Marker> GetActiveMarkers()
    {
        return GetActiveLanes().SelectMany(l => l.Markers);
    }

    #endregion

    #region Initialization

    public void InitializeDefaultLanes()
    {
        _logger.LogInformation("Initializing default lanes");
        
        ClearAllLanes();
        
        // Create default lanes with different MIDI channels
        AddLane("Drums", 1);
        AddLane("Bass", 2);
        AddLane("Lead", 3);
        AddLane("Pad", 4);
        
        // Set the first lane as selected
        SelectedLaneId = Lanes.FirstOrDefault()?.Id ?? 0;
        
        _logger.LogInformation("Initialized {Count} default lanes", Lanes.Count);
    }

    public void ClearAllLanes()
    {
        var laneCount = Lanes.Count;
        Lanes.Clear();
        _nextLaneId = 0;
        SelectedLaneId = 0;
        
        if (laneCount > 0)
        {
            _logger.LogInformation("Cleared {Count} lanes", laneCount);
        }
    }

    #endregion
}