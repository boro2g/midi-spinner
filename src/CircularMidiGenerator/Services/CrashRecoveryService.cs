using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CircularMidiGenerator.Core.Services;
using CircularMidiGenerator.Core.Models;
using System.Text.Json;

namespace CircularMidiGenerator.Services;

/// <summary>
/// Service for handling application crashes and recovery
/// </summary>
public class CrashRecoveryService
{
    private readonly IPersistenceService _persistenceService;
    private readonly ILaneController _laneController;
    private readonly ILogger<CrashRecoveryService> _logger;
    private readonly string _crashRecoveryPath;
    private readonly string _autoSavePath;

    public CrashRecoveryService(
        IPersistenceService persistenceService,
        ILaneController laneController,
        ILogger<CrashRecoveryService> logger)
    {
        _persistenceService = persistenceService;
        _laneController = laneController;
        _logger = logger;

        // Set up recovery paths
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "CircularMidiGenerator");
        Directory.CreateDirectory(appFolder);

        _crashRecoveryPath = Path.Combine(appFolder, "crash_recovery.json");
        _autoSavePath = Path.Combine(appFolder, "autosave.json");
    }

    /// <summary>
    /// Check if there's a crash recovery file available
    /// </summary>
    public bool HasCrashRecoveryData()
    {
        return File.Exists(_crashRecoveryPath) || File.Exists(_autoSavePath);
    }

    /// <summary>
    /// Get information about available recovery data
    /// </summary>
    public async Task<RecoveryInfo?> GetRecoveryInfoAsync()
    {
        try
        {
            RecoveryInfo? crashInfo = null;
            RecoveryInfo? autoSaveInfo = null;

            // Check crash recovery file
            if (File.Exists(_crashRecoveryPath))
            {
                var crashFileInfo = new FileInfo(_crashRecoveryPath);
                crashInfo = new RecoveryInfo
                {
                    Type = RecoveryType.CrashRecovery,
                    FilePath = _crashRecoveryPath,
                    LastModified = crashFileInfo.LastWriteTime,
                    Description = "Configuration from unexpected shutdown"
                };
            }

            // Check autosave file
            if (File.Exists(_autoSavePath))
            {
                var autoSaveFileInfo = new FileInfo(_autoSavePath);
                autoSaveInfo = new RecoveryInfo
                {
                    Type = RecoveryType.AutoSave,
                    FilePath = _autoSavePath,
                    LastModified = autoSaveFileInfo.LastWriteTime,
                    Description = "Automatically saved configuration"
                };
            }

            // Return the most recent recovery option
            if (crashInfo != null && autoSaveInfo != null)
            {
                return crashInfo.LastModified > autoSaveInfo.LastModified ? crashInfo : autoSaveInfo;
            }

            return crashInfo ?? autoSaveInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking recovery data");
            return null;
        }
    }

    /// <summary>
    /// Recover configuration from crash recovery or autosave
    /// </summary>
    public async Task<ProjectConfiguration?> RecoverConfigurationAsync(RecoveryType type)
    {
        try
        {
            var filePath = type == RecoveryType.CrashRecovery ? _crashRecoveryPath : _autoSavePath;
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Recovery file not found: {FilePath}", filePath);
                return null;
            }

            _logger.LogInformation("Recovering configuration from {Type}: {FilePath}", type, filePath);

            var configuration = await _persistenceService.LoadConfigurationAsync(filePath);
            
            _logger.LogInformation("Successfully recovered configuration with {LaneCount} lanes and {MarkerCount} markers",
                configuration.Lanes?.Count ?? 0,
                configuration.Lanes?.Sum(l => l.Markers?.Count ?? 0) ?? 0);

            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recover configuration from {Type}", type);
            return null;
        }
    }

    /// <summary>
    /// Save current state for crash recovery
    /// </summary>
    public async Task SaveCrashRecoveryDataAsync()
    {
        try
        {
            var configuration = CreateCurrentConfiguration();
            await SaveConfigurationToFileAsync(configuration, _crashRecoveryPath);
            
            _logger.LogDebug("Crash recovery data saved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save crash recovery data");
        }
    }

    /// <summary>
    /// Auto-save current state periodically
    /// </summary>
    public async Task AutoSaveAsync()
    {
        try
        {
            var configuration = CreateCurrentConfiguration();
            await SaveConfigurationToFileAsync(configuration, _autoSavePath);
            
            _logger.LogDebug("Auto-save completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-save failed");
        }
    }

    /// <summary>
    /// Clear recovery data after successful save
    /// </summary>
    public void ClearRecoveryData()
    {
        try
        {
            if (File.Exists(_crashRecoveryPath))
            {
                File.Delete(_crashRecoveryPath);
                _logger.LogDebug("Crash recovery data cleared");
            }

            if (File.Exists(_autoSavePath))
            {
                File.Delete(_autoSavePath);
                _logger.LogDebug("Auto-save data cleared");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear recovery data");
        }
    }

    /// <summary>
    /// Initialize crash recovery monitoring
    /// </summary>
    public void InitializeCrashRecovery()
    {
        try
        {
            // Set up unhandled exception handlers
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            _logger.LogInformation("Crash recovery monitoring initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize crash recovery");
        }
    }

    /// <summary>
    /// Clean up crash recovery monitoring
    /// </summary>
    public void Cleanup()
    {
        try
        {
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

            _logger.LogInformation("Crash recovery monitoring cleaned up");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during crash recovery cleanup");
        }
    }

    private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _logger.LogCritical("Unhandled exception occurred, saving crash recovery data");
        
        try
        {
            await SaveCrashRecoveryDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save crash recovery data during unhandled exception");
        }
    }

    private async void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "Unobserved task exception occurred");
        
        try
        {
            await SaveCrashRecoveryDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save crash recovery data during unobserved task exception");
        }

        e.SetObserved(); // Prevent application termination
    }

    private ProjectConfiguration CreateCurrentConfiguration()
    {
        return new ProjectConfiguration
        {
            Version = "1.0",
            Created = DateTime.UtcNow,
            BPM = 120.0, // Default BPM - should be retrieved from timing service
            IsAbletonSyncEnabled = false, // Should be retrieved from sync service
            Lanes = _laneController.Lanes.ToList()
        };
    }

    private async Task SaveConfigurationToFileAsync(ProjectConfiguration configuration, string filePath)
    {
        var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await File.WriteAllTextAsync(filePath, json);
    }
}

/// <summary>
/// Recovery information
/// </summary>
public class RecoveryInfo
{
    public RecoveryType Type { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Type of recovery data
/// </summary>
public enum RecoveryType
{
    CrashRecovery,
    AutoSave
}