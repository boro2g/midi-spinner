namespace CircularMidiGenerator.Core.Models;

/// <summary>
/// Settings for quantization grid snapping
/// </summary>
public class QuantizationSettings
{
    /// <summary>
    /// Whether quantization is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Note division for quantization (e.g., "1/16", "1/8", "1/4", "1/32")
    /// </summary>
    public string Division { get; set; } = "1/16";

    /// <summary>
    /// Gets the number of grid lines for the current division
    /// </summary>
    public int GridLineCount => Division switch
    {
        "1/4" => 4,
        "1/8" => 8,
        "1/16" => 16,
        "1/32" => 32,
        _ => 16
    };

    /// <summary>
    /// Gets the angle between grid lines in degrees
    /// </summary>
    public double GridAngleStep => 360.0 / GridLineCount;
}