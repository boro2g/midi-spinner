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

    /// <summary>
    /// Converts the marker's color to a MIDI note number using chromatic scale mapping
    /// Red (0°) = C (60), progressing through chromatic scale
    /// </summary>
    /// <param name="baseOctave">Base octave for the chromatic scale (default C4 = 60)</param>
    /// <returns>MIDI note number (0-127)</returns>
    public int GetMidiNote(int baseOctave = 60)
    {
        // Convert RGB color to HSV to get hue angle
        var hue = GetHueFromColor(Color);
        
        // Map hue (0-360°) to chromatic scale (12 semitones)
        var semitone = (int)Math.Round(hue / 30.0) % 12; // 360° / 12 semitones = 30° per semitone
        
        // Calculate MIDI note number
        var midiNote = baseOctave + semitone;
        
        // Ensure note is within valid MIDI range (0-127)
        return Math.Max(0, Math.Min(127, midiNote));
    }

    /// <summary>
    /// Creates a color from a MIDI note number using chromatic scale mapping
    /// </summary>
    /// <param name="midiNote">MIDI note number (0-127)</param>
    /// <param name="saturation">Color saturation (0.0-1.0)</param>
    /// <param name="brightness">Color brightness (0.0-1.0)</param>
    /// <returns>Color representing the MIDI note</returns>
    public static Color GetColorFromMidiNote(int midiNote, float saturation = 0.8f, float brightness = 0.9f)
    {
        if (midiNote < 0 || midiNote > 127)
            throw new ArgumentOutOfRangeException(nameof(midiNote), "MIDI note must be between 0 and 127");

        // Get semitone within octave (0-11)
        var semitone = midiNote % 12;
        
        // Map semitone to hue (0-360°)
        var hue = semitone * 30.0f; // 12 semitones * 30° = 360°
        
        return ColorFromHSV(hue, saturation, brightness);
    }

    /// <summary>
    /// Extracts hue value from RGB color
    /// </summary>
    private static float GetHueFromColor(Color color)
    {
        var r = color.R / 255.0f;
        var g = color.G / 255.0f;
        var b = color.B / 255.0f;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        if (delta == 0) return 0; // Grayscale

        float hue;
        if (max == r)
            hue = ((g - b) / delta) % 6;
        else if (max == g)
            hue = (b - r) / delta + 2;
        else
            hue = (r - g) / delta + 4;

        hue *= 60;
        if (hue < 0) hue += 360;

        return hue;
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