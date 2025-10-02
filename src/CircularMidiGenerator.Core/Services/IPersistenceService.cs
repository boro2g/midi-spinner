using System;
using System.Threading.Tasks;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Service interface for saving and loading project configurations
/// </summary>
public interface IPersistenceService
{
    /// <summary>
    /// Saves a project configuration to a file
    /// </summary>
    /// <param name="configuration">The configuration to save</param>
    /// <param name="filePath">Path where to save the file</param>
    /// <returns>Task representing the async operation</returns>
    Task SaveConfigurationAsync(ProjectConfiguration configuration, string filePath);

    /// <summary>
    /// Loads a project configuration from a file
    /// </summary>
    /// <param name="filePath">Path to the configuration file</param>
    /// <returns>The loaded configuration</returns>
    Task<ProjectConfiguration> LoadConfigurationAsync(string filePath);

    /// <summary>
    /// Validates a configuration file without fully loading it
    /// </summary>
    /// <param name="filePath">Path to the configuration file</param>
    /// <returns>True if the file is valid, false otherwise</returns>
    Task<bool> ValidateConfigurationFileAsync(string filePath);

    /// <summary>
    /// Creates a backup of an existing configuration file
    /// </summary>
    /// <param name="filePath">Path to the original file</param>
    /// <returns>Path to the backup file</returns>
    Task<string> CreateBackupAsync(string filePath);

    /// <summary>
    /// Restores a configuration from a backup file
    /// </summary>
    /// <param name="backupFilePath">Path to the backup file</param>
    /// <param name="targetFilePath">Path where to restore the file</param>
    /// <returns>Task representing the async operation</returns>
    Task RestoreFromBackupAsync(string backupFilePath, string targetFilePath);

    /// <summary>
    /// Gets the default configuration
    /// </summary>
    /// <returns>A new default configuration</returns>
    ProjectConfiguration GetDefaultConfiguration();

    /// <summary>
    /// Event fired when a configuration is successfully saved
    /// </summary>
    event EventHandler<ConfigurationSavedEventArgs>? ConfigurationSaved;

    /// <summary>
    /// Event fired when a configuration is successfully loaded
    /// </summary>
    event EventHandler<ConfigurationLoadedEventArgs>? ConfigurationLoaded;

    /// <summary>
    /// Event fired when an error occurs during persistence operations
    /// </summary>
    event EventHandler<PersistenceErrorEventArgs>? PersistenceError;
}

/// <summary>
/// Event arguments for configuration saved event
/// </summary>
public class ConfigurationSavedEventArgs : EventArgs
{
    public string FilePath { get; }
    public ProjectConfiguration Configuration { get; }
    public DateTime SavedAt { get; }

    public ConfigurationSavedEventArgs(string filePath, ProjectConfiguration configuration)
    {
        FilePath = filePath;
        Configuration = configuration;
        SavedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event arguments for configuration loaded event
/// </summary>
public class ConfigurationLoadedEventArgs : EventArgs
{
    public string FilePath { get; }
    public ProjectConfiguration Configuration { get; }
    public DateTime LoadedAt { get; }

    public ConfigurationLoadedEventArgs(string filePath, ProjectConfiguration configuration)
    {
        FilePath = filePath;
        Configuration = configuration;
        LoadedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event arguments for persistence error event
/// </summary>
public class PersistenceErrorEventArgs : EventArgs
{
    public string Operation { get; }
    public string FilePath { get; }
    public Exception Exception { get; }
    public DateTime OccurredAt { get; }

    public PersistenceErrorEventArgs(string operation, string filePath, Exception exception)
    {
        Operation = operation;
        FilePath = filePath;
        Exception = exception;
        OccurredAt = DateTime.UtcNow;
    }
}