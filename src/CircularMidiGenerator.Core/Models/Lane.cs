using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;

namespace CircularMidiGenerator.Core.Models;

/// <summary>
/// Represents a lane with independent settings for multi-channel MIDI output
/// </summary>
public class Lane
{
    private int _midiChannel = 1;
    private string _name = string.Empty;

    public int Id { get; set; }
    
    /// <summary>
    /// Display name for the lane
    /// </summary>
    public string Name 
    { 
        get => _name;
        set => _name = value ?? string.Empty;
    }
    
    /// <summary>
    /// MIDI channel for this lane (1-16)
    /// </summary>
    public int MidiChannel 
    { 
        get => _midiChannel;
        set
        {
            if (value < 1 || value > 16)
                throw new ArgumentOutOfRangeException(nameof(value), "MIDI channel must be between 1 and 16");
            _midiChannel = value;
        }
    }
    
    /// <summary>
    /// Whether this lane is muted (no MIDI output)
    /// </summary>
    public bool IsMuted { get; set; }
    
    /// <summary>
    /// Whether this lane is soloed (only soloed lanes produce output)
    /// </summary>
    public bool IsSoloed { get; set; }
    
    /// <summary>
    /// Quantization settings specific to this lane
    /// </summary>
    public QuantizationSettings Quantization { get; set; } = new();
    
    /// <summary>
    /// Theme color for visual grouping and marker identification
    /// </summary>
    public Color ThemeColor { get; set; }
    
    /// <summary>
    /// Collection of markers belonging to this lane
    /// </summary>
    public ObservableCollection<Marker> Markers { get; set; } = new();

    public Lane()
    {
        ThemeColor = Color.Blue; // Default theme color
    }

    public Lane(int id, string name, int midiChannel = 1)
    {
        Id = id;
        Name = name;
        MidiChannel = midiChannel;
        ThemeColor = GenerateThemeColor(id);
    }

    /// <summary>
    /// Adds a marker to this lane and assigns the lane ID
    /// </summary>
    /// <param name="marker">Marker to add</param>
    public void AddMarker(Marker marker)
    {
        if (marker == null)
            throw new ArgumentNullException(nameof(marker));

        marker.Lane = Id;
        Markers.Add(marker);
    }

    /// <summary>
    /// Removes a marker from this lane
    /// </summary>
    /// <param name="marker">Marker to remove</param>
    /// <returns>True if marker was removed, false if not found</returns>
    public bool RemoveMarker(Marker marker)
    {
        return marker != null && Markers.Remove(marker);
    }

    /// <summary>
    /// Removes a marker by its ID
    /// </summary>
    /// <param name="markerId">ID of marker to remove</param>
    /// <returns>True if marker was removed, false if not found</returns>
    public bool RemoveMarker(Guid markerId)
    {
        var marker = Markers.FirstOrDefault(m => m.Id == markerId);
        return marker != null && Markers.Remove(marker);
    }

    /// <summary>
    /// Gets all active markers (currently being triggered)
    /// </summary>
    /// <returns>Collection of active markers</returns>
    public IEnumerable<Marker> GetActiveMarkers()
    {
        return Markers.Where(m => m.IsActive);
    }

    /// <summary>
    /// Clears all markers from this lane
    /// </summary>
    public void ClearMarkers()
    {
        Markers.Clear();
    }

    /// <summary>
    /// Determines if this lane should produce MIDI output based on mute/solo state
    /// </summary>
    /// <param name="anyLanesSoloed">Whether any lanes in the project are soloed</param>
    /// <returns>True if lane should produce output</returns>
    public bool ShouldProduceOutput(bool anyLanesSoloed)
    {
        if (IsMuted) return false;
        if (anyLanesSoloed) return IsSoloed;
        return true;
    }

    /// <summary>
    /// Generates a distinct theme color based on lane ID
    /// </summary>
    /// <param name="laneId">Lane ID to generate color for</param>
    /// <returns>Generated theme color</returns>
    private static Color GenerateThemeColor(int laneId)
    {
        // Generate distinct colors using HSV color space
        var hue = (laneId * 137.5f) % 360; // Golden angle for good distribution
        var saturation = 0.7f;
        var brightness = 0.8f;

        return ColorFromHSV(hue, saturation, brightness);
    }

    /// <summary>
    /// Creates RGB color from HSV values
    /// </summary>
    private static Color ColorFromHSV(float hue, float saturation, float brightness)
    {
        var c = brightness * saturation;
        var x = c * (1 - Math.Abs((hue / 60) % 2 - 1));
        var m = brightness - c;

        float r, g, b;
        if (hue < 60)
        {
            r = c; g = x; b = 0;
        }
        else if (hue < 120)
        {
            r = x; g = c; b = 0;
        }
        else if (hue < 180)
        {
            r = 0; g = c; b = x;
        }
        else if (hue < 240)
        {
            r = 0; g = x; b = c;
        }
        else if (hue < 300)
        {
            r = x; g = 0; b = c;
        }
        else
        {
            r = c; g = 0; b = x;
        }

        return Color.FromArgb(
            (int)Math.Round((r + m) * 255),
            (int)Math.Round((g + m) * 255),
            (int)Math.Round((b + m) * 255)
        );
    }
}