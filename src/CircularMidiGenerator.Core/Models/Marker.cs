using System;
using System.Drawing;

namespace CircularMidiGenerator.Core.Models;

/// <summary>
/// Represents a marker on the circular disk that triggers MIDI notes
/// </summary>
public class Marker
{
    private double _angle;
    private int _velocity;

    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Position on circle in degrees (0-360)
    /// </summary>
    public double Angle
    {
        get => _angle;
        set
        {
            if (value < 0 || value >= 360)
                throw new ArgumentOutOfRangeException(nameof(value), "Angle must be between 0 and 360 degrees");
            _angle = value;
        }
    }

    /// <summary>
    /// Color that determines MIDI pitch using chromatic scale mapping
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    /// MIDI velocity (1-127)
    /// </summary>
    public int Velocity
    {
        get => _velocity;
        set
        {
            if (value < 1 || value > 127)
                throw new ArgumentOutOfRangeException(nameof(value), "Velocity must be between 1 and 127");
            _velocity = value;
        }
    }

    /// <summary>
    /// Lane assignment for multi-channel support
    /// </summary>
    public int Lane { get; set; }

    /// <summary>
    /// Indicates if marker is currently being triggered
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Timestamp of last trigger to prevent double-triggering
    /// </summary>
    public DateTime LastTriggered { get; set; }

    public Marker()
    {
        Velocity = 100; // Default velocity
        Color = Color.Red; // Default color
    }

    public Marker(double angle, Color color, int velocity = 100, int lane = 0)
    {
        Angle = angle;
        Color = color;
        Velocity = velocity;
        Lane = lane;
    }
}