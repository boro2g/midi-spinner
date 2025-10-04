using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Service that coordinates marker triggering with lane mute/solo state and MIDI output
/// </summary>
public class MarkerTriggerService : IMarkerTriggerService, IDisposable
{
    private readonly ILogger<MarkerTriggerService> _logger;
    private readonly ILaneController _laneController;
    private readonly IMidiService _midiService;
    private readonly ITimingService _timingService;
    private readonly Dictionary<int, HashSet<int>> _activeNotes = new(); // Channel -> Set of active note numbers
    private bool _disposed;

    public MarkerTriggerService(
        ILogger<MarkerTriggerService> logger,
        ILaneController laneController,
        IMidiService midiService,
        ITimingService timingService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _laneController = laneController ?? throw new ArgumentNullException(nameof(laneController));
        _midiService = midiService ?? throw new ArgumentNullException(nameof(midiService));
        _timingService = timingService ?? throw new ArgumentNullException(nameof(timingService));
        
        // Initialize active notes tracking for all MIDI channels
        for (int channel = 1; channel <= 16; channel++)
        {
            _activeNotes[channel] = new HashSet<int>();
        }
        
        _logger.LogInformation("MarkerTriggerService initialized");
    }

    public event EventHandler<MarkerTriggerEventArgs>? MarkerProcessed;

    public void Initialize()
    {
        // Subscribe to timing service events
        _timingService.MarkerTriggered += OnMarkerTriggered;
        
        // Subscribe to lane controller events to handle mute/solo changes
        _laneController.LaneStateChanged += OnLaneStateChanged;
        
        // Update markers initially
        UpdateMarkers();
        
        _logger.LogInformation("MarkerTriggerService initialized and listening for events");
    }

    public void ProcessMarkerTrigger(Marker marker, double triggerAngle)
    {
        if (marker == null)
        {
            _logger.LogWarning("Attempted to process null marker trigger");
            return;
        }

        // Only process markers when timing service is playing
        if (!_timingService.IsPlaying)
        {
            _logger.LogDebug("Skipping marker {MarkerId} trigger - timing service not playing", marker.Id);
            return;
        }

        var lane = _laneController.GetLane(marker.Lane);
        if (lane == null)
        {
            _logger.LogWarning("Marker {MarkerId} belongs to non-existent lane {LaneId}", marker.Id, marker.Lane);
            return;
        }

        var shouldTrigger = _laneController.ShouldLaneProduceOutput(lane.Id);
        
        if (shouldTrigger)
        {
            TriggerMarkerMidi(marker, lane);
            _logger.LogDebug("Triggered marker {MarkerId} in lane {LaneName} (Channel {Channel})", 
                marker.Id, lane.Name, lane.MidiChannel);
        }
        else
        {
            _logger.LogDebug("Skipped marker {MarkerId} in muted/non-solo lane {LaneName}", 
                marker.Id, lane.Name);
        }

        // Fire event regardless of whether it was actually triggered
        MarkerProcessed?.Invoke(this, new MarkerTriggerEventArgs(marker, lane, triggerAngle, shouldTrigger));
    }

    public void UpdateMarkers()
    {
        var allMarkers = _laneController.GetAllMarkers().ToList();
        _timingService.SetMarkers(allMarkers);
        
        _logger.LogDebug("Updated timing service with {Count} markers", allMarkers.Count);
    }

    public void StopAllNotes()
    {
        _logger.LogInformation("Stopping all active MIDI notes (panic)");
        
        foreach (var channelNotes in _activeNotes)
        {
            var channel = channelNotes.Key;
            var activeNotes = channelNotes.Value.ToList(); // Copy to avoid modification during iteration
            
            foreach (var note in activeNotes)
            {
                try
                {
                    _midiService.SendNoteOff(channel, note);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send note off for channel {Channel}, note {Note}", channel, note);
                }
            }
            
            channelNotes.Value.Clear();
        }
        
        _logger.LogInformation("All active notes stopped");
    }

    private void TriggerMarkerMidi(Marker marker, Lane lane)
    {
        try
        {
            var midiNote = marker.GetMidiNote();
            var channel = lane.MidiChannel;
            
            // Send note on
            _midiService.SendNoteOn(channel, midiNote, marker.Velocity);
            
            // Track active note
            _activeNotes[channel].Add(midiNote);
            
            // Calculate note duration based on marker's NoteLength and current BPM
            // NoteLength is a fraction of a whole note (0.25 = quarter note, 0.125 = eighth note, etc.)
            var currentBPM = _timingService.CurrentBPM;
            var wholeNoteDurationMs = (60.0 / currentBPM) * 4 * 1000; // 4 beats per whole note
            var noteOffDelayMs = wholeNoteDurationMs * marker.NoteLength;
            var noteOffDelay = TimeSpan.FromMilliseconds(Math.Max(50, noteOffDelayMs)); // Minimum 50ms
            
            _logger.LogDebug("Triggering MIDI note {Note} on channel {Channel} with velocity {Velocity}, duration {Duration}ms", 
                midiNote, channel, marker.Velocity, noteOffDelay.TotalMilliseconds);
            
            System.Threading.Tasks.Task.Delay(noteOffDelay).ContinueWith(_ =>
            {
                try
                {
                    _midiService.SendNoteOff(channel, midiNote);
                    _activeNotes[channel].Remove(midiNote);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send delayed note off for channel {Channel}, note {Note}", 
                        channel, midiNote);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger MIDI for marker {MarkerId}", marker.Id);
        }
    }

    private void OnMarkerTriggered(object? sender, MarkerTriggeredEventArgs e)
    {
        ProcessMarkerTrigger(e.Marker, e.TriggerAngle);
    }

    private void OnLaneStateChanged(object? sender, LaneStateChangedEventArgs e)
    {
        // Handle immediate mute/solo changes
        if (e.PropertyName == nameof(Lane.IsMuted) || e.PropertyName == nameof(Lane.IsSoloed))
        {
            var lane = _laneController.GetLane(e.LaneId);
            if (lane == null) return;

            // If lane was muted or is no longer soloed (and other lanes are soloed), stop its active notes
            var shouldProduceOutput = _laneController.ShouldLaneProduceOutput(e.LaneId);
            if (!shouldProduceOutput)
            {
                StopNotesForChannel(lane.MidiChannel);
                _logger.LogDebug("Stopped active notes for muted/non-solo lane {LaneName} (Channel {Channel})", 
                    lane.Name, lane.MidiChannel);
            }
        }
    }

    private void StopNotesForChannel(int channel)
    {
        if (!_activeNotes.TryGetValue(channel, out var activeNotes))
            return;

        var notesToStop = activeNotes.ToList(); // Copy to avoid modification during iteration
        
        foreach (var note in notesToStop)
        {
            try
            {
                _midiService.SendNoteOff(channel, note);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop note {Note} on channel {Channel}", note, channel);
            }
        }
        
        activeNotes.Clear();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                // Stop all active notes before disposing
                StopAllNotes();
                
                // Unsubscribe from events
                _timingService.MarkerTriggered -= OnMarkerTriggered;
                _laneController.LaneStateChanged -= OnLaneStateChanged;
                
                _logger.LogInformation("MarkerTriggerService disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MarkerTriggerService disposal");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}