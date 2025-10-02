using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Service interface for backup and recovery operations
/// </summary>
public interface IBackupRecoveryService
{
    /// <summary>
    /// Creates an automatic backup before saving a configuration
    /// </summary>
    /// <param name="filePath">Path to the configuration file</param>
    /// <returns>Path to the created backup file</returns>
    Task<string> CreateAutomaticBackupAsync(string filePath);

    /// <summary>
    /// Creates a manual backup with a custom name
    /// </summary>
    /// <param name="filePath">Path to the configuration file</param>
    /// <param name="backupName">Custom name for the backup</param>
    /// <returns>Path to the created backup file</returns>
    Task<string> CreateManualBackupAsync(string filePath, string backupName);

    /// <summary>
    /// Gets a list of available backup files for a configuration
    /// </summary>
    /// <param name="configurationFilePath">Path to the original configuration file</param>
    /// <returns>List of backup file information</returns>
    Task<List<BackupFileInfo>> GetAvailableBackupsAsync(string configurationFilePath);

    /// <summary>
    /// Restores a configuration from a backup file
    /// </summary>
    /// <param name="backupFilePath">Path to the backup file</param>
    /// <param name="targetFilePath">Path where to restore the configuration</param>
    /// <returns>Task representing the async operation</returns>
    Task RestoreFromBackupAsync(string backupFilePath, string targetFilePath);

    /// <summary>
    /// Attempts to recover a corrupted configuration file
    /// </summary>
    /// <param name="corruptedFilePath">Path to the corrupted file</param>
    /// <returns>Recovery result with information about the operation</returns>
    Task<RecoveryResult> RecoverCorruptedFileAsync(string corruptedFilePath);

    /// <summary>
    /// Restores the default configuration
    /// </summary>
    /// <param name="targetFilePath">Path where to save the default configuration</param>
    /// <returns>Task representing the async operation</returns>
    Task RestoreDefaultConfigurationAsync(string targetFilePath);

    /// <summary>
    /// Cleans up old backup files based on retention policy
    /// </summary>
    /// <param name="configurationFilePath">Path to the configuration file</param>
    /// <param name="maxBackups">Maximum number of backups to keep</param>
    /// <param name="maxAge">Maximum age of backups to keep</param>
    /// <returns>Number of backup files deleted</returns>
    Task<int> CleanupOldBackupsAsync(string configurationFilePath, int maxBackups = 10, TimeSpan? maxAge = null);

    /// <summary>
    /// Validates the integrity of a backup file
    /// </summary>
    /// <param name="backupFilePath">Path to the backup file</param>
    /// <returns>True if the backup is valid and can be restored</returns>
    Task<bool> ValidateBackupIntegrityAsync(string backupFilePath);

    /// <summary>
    /// Event fired when a backup is created
    /// </summary>
    event EventHandler<BackupCreatedEventArgs>? BackupCreated;

    /// <summary>
    /// Event fired when a backup is restored
    /// </summary>
    event EventHandler<BackupRestoredEventArgs>? BackupRestored;

    /// <summary>
    /// Event fired when a recovery operation completes
    /// </summary>
    event EventHandler<RecoveryCompletedEventArgs>? RecoveryCompleted;
}

/// <summary>
/// Information about a backup file
/// </summary>
public class BackupFileInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long SizeBytes { get; set; }
    public bool IsAutomatic { get; set; }
    public bool IsValid { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Result of a recovery operation
/// </summary>
public class RecoveryResult
{
    public bool IsSuccessful { get; set; }
    public string? RecoveredFilePath { get; set; }
    public RecoveryMethod MethodUsed { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> ActionsPerformed { get; set; } = new();
    public ProjectConfiguration? RecoveredConfiguration { get; set; }
}

/// <summary>
/// Methods used for recovery
/// </summary>
public enum RecoveryMethod
{
    None,
    BackupRestore,
    PartialRecovery,
    DefaultConfiguration,
    ManualIntervention
}

/// <summary>
/// Event arguments for backup created event
/// </summary>
public class BackupCreatedEventArgs : EventArgs
{
    public string BackupFilePath { get; }
    public string OriginalFilePath { get; }
    public bool IsAutomatic { get; }
    public DateTime CreatedAt { get; }

    public BackupCreatedEventArgs(string backupFilePath, string originalFilePath, bool isAutomatic)
    {
        BackupFilePath = backupFilePath;
        OriginalFilePath = originalFilePath;
        IsAutomatic = isAutomatic;
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event arguments for backup restored event
/// </summary>
public class BackupRestoredEventArgs : EventArgs
{
    public string BackupFilePath { get; }
    public string RestoredFilePath { get; }
    public DateTime RestoredAt { get; }

    public BackupRestoredEventArgs(string backupFilePath, string restoredFilePath)
    {
        BackupFilePath = backupFilePath;
        RestoredFilePath = restoredFilePath;
        RestoredAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event arguments for recovery completed event
/// </summary>
public class RecoveryCompletedEventArgs : EventArgs
{
    public RecoveryResult Result { get; }
    public string OriginalFilePath { get; }
    public DateTime CompletedAt { get; }

    public RecoveryCompletedEventArgs(RecoveryResult result, string originalFilePath)
    {
        Result = result;
        OriginalFilePath = originalFilePath;
        CompletedAt = DateTime.UtcNow;
    }
}