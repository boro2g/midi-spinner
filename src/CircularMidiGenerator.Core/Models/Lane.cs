using System.Collections.ObjectModel;
using System.Drawing;

namespace CircularMidiGenerator.Core.Models;

/// <summary>
/// Represents a lane with independent settings for multi-channel MIDI output
/// </summary>
public class Lane
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// MIDI channel for this lane (1-16)
    /// </summary>
    public int MidiChannel { get; set; } = 1;
    
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
    /// Theme color for visual grouping
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
        ThemeColor = Color.Blue;
    }
}