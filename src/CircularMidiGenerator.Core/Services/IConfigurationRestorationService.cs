using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Service interface for restoring configurations and recreating application state
/// </summary>
public interface IConfigurationRestorationService
{
    /// <summary>
    /// Restores the application state from a configuration
    /// </summary>
    /// <param name="configuration">The configuration to restore</param>
    /// <returns>Task representing the async operation</returns>
    Task RestoreConfigurationAsync(ProjectConfiguration configuration);

    /// <summary>
    /// Recreates all markers from the configuration with exact positions and properties
    /// </summary>
    /// <param name="configuration">The configuration containing marker data</param>
    /// <returns>Task representing the async operation</returns>
    Task RecreateMarkersAsync(ProjectConfiguration configuration);

    /// <summary>
    /// Restores lane settings including quantization and MIDI channels
    /// </summary>
    /// <param name="configuration">The configuration containing lane settings</param>
    /// <returns>Task representing the async operation</returns>
    Task RestoreLaneSettingsAsync(ProjectConfiguration configuration);

    /// <summary>
    /// Restores global settings like BPM and Ableton sync
    /// </summary>
    /// <param name="configuration">The configuration containing global settings</param>
    /// <returns>Task representing the async operation</returns>
    Task RestoreGlobalSettingsAsync(ProjectConfiguration configuration);

    /// <summary>
    /// Validates that the configuration is compatible with the current application state
    /// </summary>
    /// <param name="configuration">The configuration to validate</param>
    /// <returns>Validation result with any compatibility issues</returns>
    Task<ConfigurationCompatibilityResult> ValidateCompatibilityAsync(ProjectConfiguration configuration);

    /// <summary>
    /// Clears the current application state before restoration
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    Task ClearCurrentStateAsync();

    /// <summary>
    /// Event fired when configuration restoration starts
    /// </summary>
    event EventHandler<ConfigurationRestorationStartedEventArgs>? RestorationStarted;

    /// <summary>
    /// Event fired when configuration restoration completes
    /// </summary>
    event EventHandler<ConfigurationRestorationCompletedEventArgs>? RestorationCompleted;

    /// <summary>
    /// Event fired when an error occurs during restoration
    /// </summary>
    event EventHandler<ConfigurationRestorationErrorEventArgs>? RestorationError;
}

/// <summary>
/// Result of configuration compatibility validation
/// </summary>
public class ConfigurationCompatibilityResult
{
    public bool IsCompatible { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public string? RecommendedAction { get; set; }
}

/// <summary>
/// Event arguments for restoration started event
/// </summary>
public class ConfigurationRestorationStartedEventArgs : EventArgs
{
    public ProjectConfiguration Configuration { get; }
    public DateTime StartedAt { get; }

    public ConfigurationRestorationStartedEventArgs(ProjectConfiguration configuration)
    {
        Configuration = configuration;
        StartedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event arguments for restoration completed event
/// </summary>
public class ConfigurationRestorationCompletedEventArgs : EventArgs
{
    public ProjectConfiguration Configuration { get; }
    public DateTime CompletedAt { get; }
    public TimeSpan Duration { get; }
    public int MarkersRestored { get; }
    public int LanesRestored { get; }

    public ConfigurationRestorationCompletedEventArgs(ProjectConfiguration configuration, DateTime startedAt, int markersRestored, int lanesRestored)
    {
        Configuration = configuration;
        CompletedAt = DateTime.UtcNow;
        Duration = CompletedAt - startedAt;
        MarkersRestored = markersRestored;
        LanesRestored = lanesRestored;
    }
}

/// <summary>
/// Event arguments for restoration error event
/// </summary>
public class ConfigurationRestorationErrorEventArgs : EventArgs
{
    public string Phase { get; }
    public Exception Exception { get; }
    public ProjectConfiguration? Configuration { get; }
    public DateTime OccurredAt { get; }

    public ConfigurationRestorationErrorEventArgs(string phase, Exception exception, ProjectConfiguration? configuration = null)
    {
        Phase = phase;
        Exception = exception;
        Configuration = configuration;
        OccurredAt = DateTime.UtcNow;
    }
}