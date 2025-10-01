using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CircularMidiGenerator.Core.Models;

/// <summary>
/// Configuration model for project persistence
/// </summary>
public class ProjectConfiguration
{
    /// <summary>
    /// Beats per minute for disk rotation speed
    /// </summary>
    public double BPM { get; set; } = 120.0;
    
    /// <summary>
    /// Whether Ableton Live synchronization is enabled
    /// </summary>
    public bool IsAbletonSyncEnabled { get; set; }
    
    /// <summary>
    /// List of lanes with their settings and markers
    /// </summary>
    public List<Lane> Lanes { get; set; } = new();
    
    /// <summary>
    /// Global quantization settings (can be overridden per lane)
    /// </summary>
    public QuantizationSettings GlobalQuantization { get; set; } = new();
    
    /// <summary>
    /// Configuration file version for compatibility tracking
    /// </summary>
    public string Version { get; set; } = "1.0";
    
    /// <summary>
    /// Timestamp when configuration was created
    /// </summary>
    public DateTime Created { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Timestamp when configuration was last modified
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public ProjectConfiguration()
    {
        // Initialize with default lanes
        InitializeDefaultLanes();
    }

    private void InitializeDefaultLanes()
    {
        Lanes.Add(new Lane(0, "Drums", 1));
        Lanes.Add(new Lane(1, "Bass", 2));
        Lanes.Add(new Lane(2, "Lead", 3));
        Lanes.Add(new Lane(3, "Pad", 4));
    }
}