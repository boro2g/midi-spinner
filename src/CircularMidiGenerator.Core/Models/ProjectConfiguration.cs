using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;

namespace CircularMidiGenerator.Core.Models;

/// <summary>
/// Configuration model for project persistence with JSON serialization support
/// </summary>
public class ProjectConfiguration
{
    private double _bpm = 120.0;
    private string _version = "1.0";

    /// <summary>
    /// Beats per minute for disk rotation speed (60-300 BPM)
    /// </summary>
    [JsonPropertyName("bpm")]
    [Range(60.0, 300.0, ErrorMessage = "BPM must be between 60 and 300")]
    public double BPM 
    { 
        get => _bpm;
        set
        {
            if (value < 60.0 || value > 300.0)
                throw new ArgumentOutOfRangeException(nameof(value), "BPM must be between 60 and 300");
            _bpm = value;
            LastModified = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Whether Ableton Live synchronization is enabled
    /// </summary>
    [JsonPropertyName("abletonSyncEnabled")]
    public bool IsAbletonSyncEnabled { get; set; }
    
    /// <summary>
    /// List of lanes with their settings and markers
    /// </summary>
    [JsonPropertyName("lanes")]
    [Required]
    public List<Lane> Lanes { get; set; } = new();
    
    /// <summary>
    /// Global quantization settings (can be overridden per lane)
    /// </summary>
    [JsonPropertyName("globalQuantization")]
    [Required]
    public QuantizationSettings GlobalQuantization { get; set; } = new();
    
    /// <summary>
    /// Configuration file version for compatibility tracking
    /// </summary>
    [JsonPropertyName("version")]
    [Required]
    public string Version 
    { 
        get => _version;
        set => _version = value ?? "1.0";
    }
    
    /// <summary>
    /// Timestamp when configuration was created
    /// </summary>
    [JsonPropertyName("created")]
    public DateTime Created { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Timestamp when configuration was last modified
    /// </summary>
    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional project name for identification
    /// </summary>
    [JsonPropertyName("projectName")]
    public string? ProjectName { get; set; }

    /// <summary>
    /// Optional project description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    public ProjectConfiguration()
    {
        // Initialize with default lanes
        InitializeDefaultLanes();
    }

    /// <summary>
    /// Creates a new configuration with specified parameters
    /// </summary>
    /// <param name="projectName">Name of the project</param>
    /// <param name="bpm">Initial BPM setting</param>
    public ProjectConfiguration(string? projectName, double bpm = 120.0)
    {
        ProjectName = projectName;
        BPM = bpm;
        InitializeDefaultLanes();
    }

    /// <summary>
    /// Validates the configuration for consistency and correctness
    /// </summary>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        // Validate BPM range
        if (BPM < 60.0 || BPM > 300.0)
            errors.Add("BPM must be between 60 and 300");

        // Validate version format
        if (string.IsNullOrWhiteSpace(Version))
            errors.Add("Version cannot be empty");

        // Validate lanes
        if (Lanes == null || Lanes.Count == 0)
            errors.Add("At least one lane is required");
        else
        {
            // Check for duplicate lane IDs
            var duplicateIds = Lanes.GroupBy(l => l.Id)
                                   .Where(g => g.Count() > 1)
                                   .Select(g => g.Key);
            
            foreach (var duplicateId in duplicateIds)
                errors.Add($"Duplicate lane ID found: {duplicateId}");

            // Check for duplicate MIDI channels
            var duplicateChannels = Lanes.GroupBy(l => l.MidiChannel)
                                        .Where(g => g.Count() > 1)
                                        .Select(g => g.Key);
            
            foreach (var duplicateChannel in duplicateChannels)
                errors.Add($"Duplicate MIDI channel found: {duplicateChannel}");

            // Validate individual lanes
            for (int i = 0; i < Lanes.Count; i++)
            {
                var lane = Lanes[i];
                if (string.IsNullOrWhiteSpace(lane.Name))
                    errors.Add($"Lane {i} must have a name");
                
                if (lane.MidiChannel < 1 || lane.MidiChannel > 16)
                    errors.Add($"Lane {i} MIDI channel must be between 1 and 16");
            }
        }

        return errors;
    }

    /// <summary>
    /// Checks if the configuration version is compatible with the current application version
    /// </summary>
    /// <param name="currentVersion">Current application version</param>
    /// <returns>True if compatible, false otherwise</returns>
    public bool IsCompatibleWith(string currentVersion)
    {
        // Simple version compatibility check
        // In a real application, this would implement semantic versioning logic
        return Version == currentVersion || 
               Version.StartsWith("1.") && currentVersion.StartsWith("1.");
    }

    /// <summary>
    /// Updates the last modified timestamp
    /// </summary>
    public void Touch()
    {
        LastModified = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the total number of markers across all lanes
    /// </summary>
    /// <returns>Total marker count</returns>
    public int GetTotalMarkerCount()
    {
        return Lanes?.Sum(lane => lane.Markers?.Count ?? 0) ?? 0;
    }

    /// <summary>
    /// Gets all markers from all lanes
    /// </summary>
    /// <returns>Enumerable of all markers</returns>
    public IEnumerable<Marker> GetAllMarkers()
    {
        return Lanes?.SelectMany(lane => lane.Markers ?? Enumerable.Empty<Marker>()) 
               ?? Enumerable.Empty<Marker>();
    }

    /// <summary>
    /// Clears all markers from all lanes
    /// </summary>
    public void ClearAllMarkers()
    {
        foreach (var lane in Lanes ?? Enumerable.Empty<Lane>())
        {
            lane.ClearMarkers();
        }
        Touch();
    }

    private void InitializeDefaultLanes()
    {
        Lanes.Clear();
        Lanes.Add(new Lane(0, "Drums", 1));
        Lanes.Add(new Lane(1, "Bass", 2));
        Lanes.Add(new Lane(2, "Lead", 3));
        Lanes.Add(new Lane(3, "Pad", 4));
    }
}