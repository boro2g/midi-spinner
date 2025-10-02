using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Service for restoring configurations and recreating application state
/// </summary>
public class ConfigurationRestorationService : IConfigurationRestorationService
{
    private readonly ILogger<ConfigurationRestorationService> _logger;
    private readonly ILaneController _laneController;
    private readonly ITimingService _timingService;
    private readonly IQuantizationService _quantizationService;
    private readonly IAbletonSyncService _abletonSyncService;

    public event EventHandler<ConfigurationRestorationStartedEventArgs>? RestorationStarted;
    public event EventHandler<ConfigurationRestorationCompletedEventArgs>? RestorationCompleted;
    public event EventHandler<ConfigurationRestorationErrorEventArgs>? RestorationError;

    public ConfigurationRestorationService(
        ILogger<ConfigurationRestorationService> logger,
        ILaneController laneController,
        ITimingService timingService,
        IQuantizationService quantizationService,
        IAbletonSyncService abletonSyncService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _laneController = laneController ?? throw new ArgumentNullException(nameof(laneController));
        _timingService = timingService ?? throw new ArgumentNullException(nameof(timingService));
        _quantizationService = quantizationService ?? throw new ArgumentNullException(nameof(quantizationService));
        _abletonSyncService = abletonSyncService ?? throw new ArgumentNullException(nameof(abletonSyncService));
    }

    /// <inheritdoc />
    public async Task RestoreConfigurationAsync(ProjectConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Starting configuration restoration for project: {ProjectName}", 
                configuration.ProjectName ?? "Unnamed");

            RestorationStarted?.Invoke(this, new ConfigurationRestorationStartedEventArgs(configuration));

            // Validate compatibility first
            var compatibilityResult = await ValidateCompatibilityAsync(configuration);
            if (!compatibilityResult.IsCompatible)
            {
                var errorMessage = $"Configuration is not compatible: {string.Join(", ", compatibilityResult.Errors)}";
                throw new InvalidOperationException(errorMessage);
            }

            // Log warnings if any
            foreach (var warning in compatibilityResult.Warnings)
            {
                _logger.LogWarning("Configuration compatibility warning: {Warning}", warning);
            }

            // Clear current state
            await ClearCurrentStateAsync();

            // Restore in specific order to maintain dependencies
            await RestoreGlobalSettingsAsync(configuration);
            await RestoreLaneSettingsAsync(configuration);
            await RecreateMarkersAsync(configuration);

            var markersRestored = configuration.GetTotalMarkerCount();
            var lanesRestored = configuration.Lanes?.Count ?? 0;

            _logger.LogInformation("Configuration restoration completed. Restored {MarkersCount} markers across {LanesCount} lanes", 
                markersRestored, lanesRestored);

            RestorationCompleted?.Invoke(this, new ConfigurationRestorationCompletedEventArgs(
                configuration, startTime, markersRestored, lanesRestored));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore configuration");
            RestorationError?.Invoke(this, new ConfigurationRestorationErrorEventArgs("RestoreConfiguration", ex, configuration));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RecreateMarkersAsync(ProjectConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        try
        {
            _logger.LogDebug("Recreating markers from configuration");

            var totalMarkersCreated = 0;

            foreach (var lane in configuration.Lanes ?? Enumerable.Empty<Lane>())
            {
                _logger.LogDebug("Recreating {MarkerCount} markers for lane {LaneId} ({LaneName})", 
                    lane.Markers?.Count ?? 0, lane.Id, lane.Name);

                foreach (var marker in lane.Markers ?? Enumerable.Empty<Marker>())
                {
                    // Validate marker properties before recreation
                    if (marker.Angle < 0 || marker.Angle >= 360)
                    {
                        _logger.LogWarning("Marker has invalid angle {Angle}, skipping", marker.Angle);
                        continue;
                    }

                    if (marker.Velocity < 1 || marker.Velocity > 127)
                    {
                        _logger.LogWarning("Marker has invalid velocity {Velocity}, clamping to valid range", marker.Velocity);
                        marker.Velocity = Math.Clamp(marker.Velocity, 1, 127);
                    }

                    // Recreate marker with exact properties
                    _laneController.AssignMarkerToLane(marker, lane.Id);
                    totalMarkersCreated++;
                }
            }

            _logger.LogInformation("Successfully recreated {TotalMarkers} markers", totalMarkersCreated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recreate markers");
            RestorationError?.Invoke(this, new ConfigurationRestorationErrorEventArgs("RecreateMarkers", ex, configuration));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RestoreLaneSettingsAsync(ProjectConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        try
        {
            _logger.LogDebug("Restoring lane settings from configuration");

            // Clear existing lanes first
            _laneController.ClearAllLanes();

            foreach (var lane in configuration.Lanes ?? Enumerable.Empty<Lane>())
            {
                _logger.LogDebug("Restoring lane {LaneId} ({LaneName}) with MIDI channel {MidiChannel}", 
                    lane.Id, lane.Name, lane.MidiChannel);

                // Restore lane with all its settings
                var restoredLane = _laneController.AddLane(lane.Name, lane.MidiChannel);
                
                // Restore lane-specific settings
                _laneController.SetLaneMute(lane.Id, lane.IsMuted);
                _laneController.SetLaneSolo(lane.Id, lane.IsSoloed);
                
                // Restore quantization settings
                if (lane.Quantization != null)
                {
                    _laneController.SetLaneQuantization(lane.Id, lane.Quantization);
                }
            }

            _logger.LogInformation("Successfully restored {LanesCount} lanes", configuration.Lanes?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore lane settings");
            RestorationError?.Invoke(this, new ConfigurationRestorationErrorEventArgs("RestoreLaneSettings", ex, configuration));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RestoreGlobalSettingsAsync(ProjectConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        try
        {
            _logger.LogDebug("Restoring global settings from configuration");

            // Restore BPM
            _logger.LogDebug("Setting BPM to {BPM}", configuration.BPM);
            _timingService.SetBPM(configuration.BPM);

            // Restore Ableton sync setting
            _logger.LogDebug("Setting Ableton sync enabled: {IsEnabled}", configuration.IsAbletonSyncEnabled);
            if (configuration.IsAbletonSyncEnabled)
            {
                _abletonSyncService.Connect();
            }
            else
            {
                _abletonSyncService.Disconnect();
            }

            // Restore global quantization settings
            if (configuration.GlobalQuantization != null)
            {
                _logger.LogDebug("Restoring global quantization: {Division}, Enabled: {IsEnabled}", 
                    configuration.GlobalQuantization.Division, configuration.GlobalQuantization.Enabled);
                
                // Apply global quantization to all lanes that don't have specific settings
                foreach (var lane in configuration.Lanes ?? Enumerable.Empty<Lane>())
                {
                    if (lane.Quantization == null)
                    {
                        _laneController.SetLaneQuantization(lane.Id, configuration.GlobalQuantization);
                    }
                }
            }

            _logger.LogInformation("Successfully restored global settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore global settings");
            RestorationError?.Invoke(this, new ConfigurationRestorationErrorEventArgs("RestoreGlobalSettings", ex, configuration));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ConfigurationCompatibilityResult> ValidateCompatibilityAsync(ProjectConfiguration configuration)
    {
        var result = new ConfigurationCompatibilityResult { IsCompatible = true };

        try
        {
            _logger.LogDebug("Validating configuration compatibility");

            // Check configuration validity
            var validationErrors = configuration.Validate();
            if (validationErrors.Count > 0)
            {
                result.Errors.AddRange(validationErrors);
                result.IsCompatible = false;
            }

            // Check version compatibility
            if (!configuration.IsCompatibleWith("1.0"))
            {
                result.Warnings.Add($"Configuration version {configuration.Version} may have compatibility issues with current application version 1.0");
            }

            // Check lane count limits
            var maxLanes = 16; // MIDI has 16 channels
            if (configuration.Lanes?.Count > maxLanes)
            {
                result.Errors.Add($"Configuration has {configuration.Lanes.Count} lanes, but maximum supported is {maxLanes}");
                result.IsCompatible = false;
            }

            // Check for duplicate MIDI channels
            var midiChannels = configuration.Lanes?.Select(l => l.MidiChannel).ToList() ?? new System.Collections.Generic.List<int>();
            var duplicateChannels = midiChannels.GroupBy(c => c).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var channel in duplicateChannels)
            {
                result.Warnings.Add($"Multiple lanes are assigned to MIDI channel {channel}");
            }

            // Check marker count for performance
            var totalMarkers = configuration.GetTotalMarkerCount();
            if (totalMarkers > 1000)
            {
                result.Warnings.Add($"Configuration has {totalMarkers} markers, which may impact performance");
            }

            // Set recommendation based on issues found
            if (!result.IsCompatible)
            {
                result.RecommendedAction = "Fix the errors listed above before loading this configuration";
            }
            else if (result.Warnings.Count > 0)
            {
                result.RecommendedAction = "Configuration can be loaded but review the warnings above";
            }

            _logger.LogDebug("Configuration compatibility validation completed. Compatible: {IsCompatible}, Warnings: {WarningCount}, Errors: {ErrorCount}", 
                result.IsCompatible, result.Warnings.Count, result.Errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate configuration compatibility");
            result.IsCompatible = false;
            result.Errors.Add($"Validation failed: {ex.Message}");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task ClearCurrentStateAsync()
    {
        try
        {
            _logger.LogDebug("Clearing current application state");

            // Stop timing service if running
            if (_timingService.IsPlaying)
            {
                _timingService.Stop();
            }

            // Clear all lanes and markers
            _laneController.ClearAllLanes();

            // Reset to default settings
            _timingService.SetBPM(120.0);
            _abletonSyncService.Disconnect();

            _logger.LogDebug("Current application state cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear current state");
            RestorationError?.Invoke(this, new ConfigurationRestorationErrorEventArgs("ClearCurrentState", ex));
            throw;
        }
    }
}