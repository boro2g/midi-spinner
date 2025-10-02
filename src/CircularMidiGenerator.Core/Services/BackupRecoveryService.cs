using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Service for backup and recovery operations
/// </summary>
public class BackupRecoveryService : IBackupRecoveryService
{
    private readonly ILogger<BackupRecoveryService> _logger;
    private readonly IPersistenceService _persistenceService;
    private const string BackupDirectory = "backups";
    private const string AutoBackupPrefix = "auto_";
    private const string ManualBackupPrefix = "manual_";
    private const string BackupExtension = ".backup";

    public event EventHandler<BackupCreatedEventArgs>? BackupCreated;
    public event EventHandler<BackupRestoredEventArgs>? BackupRestored;
    public event EventHandler<RecoveryCompletedEventArgs>? RecoveryCompleted;

    public BackupRecoveryService(
        ILogger<BackupRecoveryService> logger,
        IPersistenceService persistenceService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
    }

    /// <inheritdoc />
    public async Task<string> CreateAutomaticBackupAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Cannot create backup: file not found: {filePath}");

        try
        {
            _logger.LogDebug("Creating automatic backup for {FilePath}", filePath);

            var backupPath = await CreateBackupInternal(filePath, AutoBackupPrefix, isAutomatic: true);
            
            _logger.LogInformation("Automatic backup created: {BackupPath}", backupPath);
            BackupCreated?.Invoke(this, new BackupCreatedEventArgs(backupPath, filePath, isAutomatic: true));
            
            return backupPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create automatic backup for {FilePath}", filePath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> CreateManualBackupAsync(string filePath, string backupName)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        
        if (string.IsNullOrWhiteSpace(backupName))
            throw new ArgumentException("Backup name cannot be null or empty", nameof(backupName));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Cannot create backup: file not found: {filePath}");

        try
        {
            _logger.LogDebug("Creating manual backup '{BackupName}' for {FilePath}", backupName, filePath);

            // Sanitize backup name for file system
            var sanitizedName = SanitizeFileName(backupName);
            var backupPath = await CreateBackupInternal(filePath, $"{ManualBackupPrefix}{sanitizedName}_", isAutomatic: false);
            
            _logger.LogInformation("Manual backup '{BackupName}' created: {BackupPath}", backupName, backupPath);
            BackupCreated?.Invoke(this, new BackupCreatedEventArgs(backupPath, filePath, isAutomatic: false));
            
            return backupPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create manual backup '{BackupName}' for {FilePath}", backupName, filePath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<BackupFileInfo>> GetAvailableBackupsAsync(string configurationFilePath)
    {
        if (string.IsNullOrWhiteSpace(configurationFilePath))
            throw new ArgumentException("Configuration file path cannot be null or empty", nameof(configurationFilePath));

        try
        {
            _logger.LogDebug("Getting available backups for {ConfigurationFilePath}", configurationFilePath);

            var backupDir = GetBackupDirectory(configurationFilePath);
            if (!Directory.Exists(backupDir))
            {
                _logger.LogDebug("Backup directory does not exist: {BackupDir}", backupDir);
                return new List<BackupFileInfo>();
            }

            var configFileName = Path.GetFileNameWithoutExtension(configurationFilePath);
            var backupFiles = Directory.GetFiles(backupDir, $"*{configFileName}*{BackupExtension}")
                                      .OrderByDescending(f => File.GetCreationTime(f))
                                      .ToList();

            var backupInfos = new List<BackupFileInfo>();

            foreach (var backupFile in backupFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(backupFile);
                    var fileName = Path.GetFileNameWithoutExtension(backupFile);
                    var isAutomatic = fileName.Contains(AutoBackupPrefix);
                    var isValid = await ValidateBackupIntegrityAsync(backupFile);

                    backupInfos.Add(new BackupFileInfo
                    {
                        FilePath = backupFile,
                        Name = ExtractBackupName(fileName),
                        CreatedAt = fileInfo.CreationTime,
                        SizeBytes = fileInfo.Length,
                        IsAutomatic = isAutomatic,
                        IsValid = isValid,
                        Description = isAutomatic ? "Automatic backup" : "Manual backup"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process backup file {BackupFile}", backupFile);
                }
            }

            _logger.LogDebug("Found {BackupCount} backup files for {ConfigurationFilePath}", backupInfos.Count, configurationFilePath);
            return backupInfos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available backups for {ConfigurationFilePath}", configurationFilePath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RestoreFromBackupAsync(string backupFilePath, string targetFilePath)
    {
        if (string.IsNullOrWhiteSpace(backupFilePath))
            throw new ArgumentException("Backup file path cannot be null or empty", nameof(backupFilePath));
        
        if (string.IsNullOrWhiteSpace(targetFilePath))
            throw new ArgumentException("Target file path cannot be null or empty", nameof(targetFilePath));

        if (!File.Exists(backupFilePath))
            throw new FileNotFoundException($"Backup file not found: {backupFilePath}");

        try
        {
            _logger.LogInformation("Restoring from backup {BackupFilePath} to {TargetFilePath}", backupFilePath, targetFilePath);

            // Validate backup integrity first
            var isValid = await ValidateBackupIntegrityAsync(backupFilePath);
            if (!isValid)
            {
                throw new InvalidDataException($"Backup file is corrupted or invalid: {backupFilePath}");
            }

            // Use the persistence service to restore
            await _persistenceService.RestoreFromBackupAsync(backupFilePath, targetFilePath);

            _logger.LogInformation("Successfully restored from backup to {TargetFilePath}", targetFilePath);
            BackupRestored?.Invoke(this, new BackupRestoredEventArgs(backupFilePath, targetFilePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore from backup {BackupFilePath} to {TargetFilePath}", backupFilePath, targetFilePath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<RecoveryResult> RecoverCorruptedFileAsync(string corruptedFilePath)
    {
        if (string.IsNullOrWhiteSpace(corruptedFilePath))
            throw new ArgumentException("Corrupted file path cannot be null or empty", nameof(corruptedFilePath));

        var result = new RecoveryResult();
        
        try
        {
            _logger.LogInformation("Attempting to recover corrupted file: {CorruptedFilePath}", corruptedFilePath);

            // Step 1: Try to find and restore from the most recent valid backup
            var backups = await GetAvailableBackupsAsync(corruptedFilePath);
            var validBackups = backups.Where(b => b.IsValid).OrderByDescending(b => b.CreatedAt).ToList();

            if (validBackups.Any())
            {
                var latestBackup = validBackups.First();
                _logger.LogInformation("Found valid backup for recovery: {BackupPath}", latestBackup.FilePath);

                try
                {
                    await RestoreFromBackupAsync(latestBackup.FilePath, corruptedFilePath);
                    result.IsSuccessful = true;
                    result.RecoveredFilePath = corruptedFilePath;
                    result.MethodUsed = RecoveryMethod.BackupRestore;
                    result.ActionsPerformed.Add($"Restored from backup: {latestBackup.Name}");
                    
                    // Load the recovered configuration
                    result.RecoveredConfiguration = await _persistenceService.LoadConfigurationAsync(corruptedFilePath);
                    
                    _logger.LogInformation("Successfully recovered file using backup restore method");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to restore from backup, trying next method");
                    result.Issues.Add($"Backup restore failed: {ex.Message}");
                }
            }

            // Step 2: If backup restore failed, try partial recovery
            if (!result.IsSuccessful)
            {
                _logger.LogInformation("Attempting partial recovery of corrupted file");
                
                try
                {
                    var partialResult = await AttemptPartialRecovery(corruptedFilePath);
                    if (partialResult != null)
                    {
                        var recoveredPath = corruptedFilePath + ".recovered";
                        await _persistenceService.SaveConfigurationAsync(partialResult, recoveredPath);
                        
                        result.IsSuccessful = true;
                        result.RecoveredFilePath = recoveredPath;
                        result.MethodUsed = RecoveryMethod.PartialRecovery;
                        result.RecoveredConfiguration = partialResult;
                        result.ActionsPerformed.Add("Performed partial recovery of configuration data");
                        result.Issues.Add("Some data may have been lost during recovery");
                        
                        _logger.LogInformation("Successfully performed partial recovery");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Partial recovery failed");
                    result.Issues.Add($"Partial recovery failed: {ex.Message}");
                }
            }

            // Step 3: If all else fails, create default configuration
            if (!result.IsSuccessful)
            {
                _logger.LogInformation("Creating default configuration as last resort");
                
                try
                {
                    var defaultPath = corruptedFilePath + ".default";
                    await RestoreDefaultConfigurationAsync(defaultPath);
                    
                    result.IsSuccessful = true;
                    result.RecoveredFilePath = defaultPath;
                    result.MethodUsed = RecoveryMethod.DefaultConfiguration;
                    result.RecoveredConfiguration = _persistenceService.GetDefaultConfiguration();
                    result.ActionsPerformed.Add("Created default configuration");
                    result.Issues.Add("All original data was lost, using default configuration");
                    
                    _logger.LogInformation("Created default configuration as recovery method");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create default configuration");
                    result.Issues.Add($"Default configuration creation failed: {ex.Message}");
                    result.MethodUsed = RecoveryMethod.ManualIntervention;
                }
            }

            _logger.LogInformation("Recovery operation completed. Success: {IsSuccessful}, Method: {Method}", 
                result.IsSuccessful, result.MethodUsed);

            RecoveryCompleted?.Invoke(this, new RecoveryCompletedEventArgs(result, corruptedFilePath));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recovery operation failed for {CorruptedFilePath}", corruptedFilePath);
            result.IsSuccessful = false;
            result.MethodUsed = RecoveryMethod.None;
            result.Issues.Add($"Recovery operation failed: {ex.Message}");
            return result;
        }
    }

    /// <inheritdoc />
    public async Task RestoreDefaultConfigurationAsync(string targetFilePath)
    {
        if (string.IsNullOrWhiteSpace(targetFilePath))
            throw new ArgumentException("Target file path cannot be null or empty", nameof(targetFilePath));

        try
        {
            _logger.LogInformation("Restoring default configuration to {TargetFilePath}", targetFilePath);

            var defaultConfiguration = _persistenceService.GetDefaultConfiguration();
            await _persistenceService.SaveConfigurationAsync(defaultConfiguration, targetFilePath);

            _logger.LogInformation("Default configuration restored successfully to {TargetFilePath}", targetFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore default configuration to {TargetFilePath}", targetFilePath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> CleanupOldBackupsAsync(string configurationFilePath, int maxBackups = 10, TimeSpan? maxAge = null)
    {
        if (string.IsNullOrWhiteSpace(configurationFilePath))
            throw new ArgumentException("Configuration file path cannot be null or empty", nameof(configurationFilePath));

        try
        {
            _logger.LogDebug("Cleaning up old backups for {ConfigurationFilePath}. Max backups: {MaxBackups}, Max age: {MaxAge}", 
                configurationFilePath, maxBackups, maxAge);

            var backups = await GetAvailableBackupsAsync(configurationFilePath);
            var deletedCount = 0;
            var cutoffDate = maxAge.HasValue ? DateTime.UtcNow - maxAge.Value : DateTime.MinValue;

            // Sort backups by creation date (newest first)
            var sortedBackups = backups.OrderByDescending(b => b.CreatedAt).ToList();

            // Keep the most recent backups up to maxBackups limit
            var backupsToDelete = sortedBackups.Skip(maxBackups).ToList();

            // Also delete backups older than maxAge if specified
            if (maxAge.HasValue)
            {
                var oldBackups = sortedBackups.Where(b => b.CreatedAt < cutoffDate && !backupsToDelete.Contains(b));
                backupsToDelete.AddRange(oldBackups);
            }

            foreach (var backup in backupsToDelete)
            {
                try
                {
                    File.Delete(backup.FilePath);
                    deletedCount++;
                    _logger.LogDebug("Deleted old backup: {BackupPath}", backup.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete backup file: {BackupPath}", backup.FilePath);
                }
            }

            _logger.LogInformation("Cleanup completed. Deleted {DeletedCount} old backup files", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old backups for {ConfigurationFilePath}", configurationFilePath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateBackupIntegrityAsync(string backupFilePath)
    {
        if (string.IsNullOrWhiteSpace(backupFilePath))
            return false;

        try
        {
            // Use the persistence service to validate the backup
            return await _persistenceService.ValidateConfigurationFileAsync(backupFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Backup integrity validation failed for {BackupFilePath}", backupFilePath);
            return false;
        }
    }

    private async Task<string> CreateBackupInternal(string filePath, string prefix, bool isAutomatic)
    {
        var backupDir = GetBackupDirectory(filePath);
        Directory.CreateDirectory(backupDir);

        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupFileName = $"{prefix}{fileName}_{timestamp}{BackupExtension}";
        var backupPath = Path.Combine(backupDir, backupFileName);

        await Task.Run(() => File.Copy(filePath, backupPath, overwrite: true));
        
        return backupPath;
    }

    private string GetBackupDirectory(string configurationFilePath)
    {
        var configDir = Path.GetDirectoryName(configurationFilePath) ?? Environment.CurrentDirectory;
        return Path.Combine(configDir, BackupDirectory);
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }

    private string ExtractBackupName(string fileName)
    {
        // Remove prefixes and timestamp to get a readable name
        var name = fileName;
        
        if (name.StartsWith(AutoBackupPrefix))
            name = name.Substring(AutoBackupPrefix.Length);
        else if (name.StartsWith(ManualBackupPrefix))
            name = name.Substring(ManualBackupPrefix.Length);

        // Remove timestamp pattern (e.g., "_20240210_143022")
        var timestampIndex = name.LastIndexOf('_');
        if (timestampIndex > 0)
        {
            var possibleTimestamp = name.Substring(timestampIndex + 1);
            if (possibleTimestamp.Length == 15 && possibleTimestamp.All(char.IsDigit))
            {
                name = name.Substring(0, timestampIndex);
            }
        }

        return name;
    }

    private async Task<ProjectConfiguration?> AttemptPartialRecovery(string corruptedFilePath)
    {
        try
        {
            // Try to read the file and parse what we can
            var content = await File.ReadAllTextAsync(corruptedFilePath);
            
            // Attempt to parse as JSON and recover what we can
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            var recoveredConfig = new ProjectConfiguration();

            // Try to recover basic properties
            if (root.TryGetProperty("bpm", out var bpmElement) && bpmElement.TryGetDouble(out var bpm))
            {
                recoveredConfig.BPM = Math.Clamp(bpm, 60.0, 300.0);
            }

            if (root.TryGetProperty("abletonSyncEnabled", out var syncElement) && syncElement.ValueKind == JsonValueKind.True)
            {
                recoveredConfig.IsAbletonSyncEnabled = syncElement.ValueKind == JsonValueKind.True;
            }

            if (root.TryGetProperty("projectName", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
            {
                recoveredConfig.ProjectName = nameElement.GetString();
            }

            // Try to recover lanes (this is more complex and may fail)
            if (root.TryGetProperty("lanes", out var lanesElement) && lanesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var laneElement in lanesElement.EnumerateArray())
                {
                    try
                    {
                        var lane = JsonSerializer.Deserialize<Lane>(laneElement.GetRawText());
                        if (lane != null)
                        {
                            recoveredConfig.Lanes.Add(lane);
                        }
                    }
                    catch
                    {
                        // Skip corrupted lanes
                    }
                }
            }

            return recoveredConfig;
        }
        catch
        {
            return null;
        }
    }
}