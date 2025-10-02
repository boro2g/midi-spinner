using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Service for saving and loading project configurations with JSON serialization
/// </summary>
public class PersistenceService : IPersistenceService
{
    private readonly ILogger<PersistenceService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string BackupExtension = ".backup";
    private const string ConfigurationExtension = ".cmg";

    public event EventHandler<ConfigurationSavedEventArgs>? ConfigurationSaved;
    public event EventHandler<ConfigurationLoadedEventArgs>? ConfigurationLoaded;
    public event EventHandler<PersistenceErrorEventArgs>? PersistenceError;

    public PersistenceService(ILogger<PersistenceService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <inheritdoc />
    public async Task SaveConfigurationAsync(ProjectConfiguration configuration, string filePath)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
        
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        try
        {
            _logger.LogInformation("Saving configuration to {FilePath}", filePath);

            // Validate configuration before saving
            var validationErrors = configuration.Validate();
            if (validationErrors.Count > 0)
            {
                var errorMessage = $"Configuration validation failed: {string.Join(", ", validationErrors)}";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // Create backup if file already exists
            if (File.Exists(filePath))
            {
                await CreateBackupAsync(filePath);
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Update last modified timestamp
            configuration.Touch();

            // Serialize to JSON
            var jsonString = JsonSerializer.Serialize(configuration, _jsonOptions);
            
            // Write to file atomically using temporary file
            var tempFilePath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempFilePath, jsonString);
            
            // Move temp file to final location (atomic operation)
            File.Move(tempFilePath, filePath, overwrite: true);

            _logger.LogInformation("Configuration saved successfully to {FilePath}", filePath);
            ConfigurationSaved?.Invoke(this, new ConfigurationSavedEventArgs(filePath, configuration));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to {FilePath}", filePath);
            PersistenceError?.Invoke(this, new PersistenceErrorEventArgs("Save", filePath, ex));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProjectConfiguration> LoadConfigurationAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Configuration file not found: {filePath}");

        try
        {
            _logger.LogInformation("Loading configuration from {FilePath}", filePath);

            // Read file content
            var jsonString = await File.ReadAllTextAsync(filePath);
            
            if (string.IsNullOrWhiteSpace(jsonString))
                throw new InvalidDataException("Configuration file is empty");

            // Deserialize from JSON
            var configuration = JsonSerializer.Deserialize<ProjectConfiguration>(jsonString, _jsonOptions);
            
            if (configuration == null)
                throw new InvalidDataException("Failed to deserialize configuration from JSON");

            // Validate loaded configuration
            var validationErrors = configuration.Validate();
            if (validationErrors.Count > 0)
            {
                var errorMessage = $"Loaded configuration is invalid: {string.Join(", ", validationErrors)}";
                _logger.LogWarning(errorMessage);
                // Don't throw here, allow loading of slightly invalid configs with warnings
            }

            // Check version compatibility
            if (!configuration.IsCompatibleWith("1.0"))
            {
                _logger.LogWarning("Configuration version {Version} may not be fully compatible with current application version", 
                    configuration.Version);
            }

            _logger.LogInformation("Configuration loaded successfully from {FilePath}", filePath);
            ConfigurationLoaded?.Invoke(this, new ConfigurationLoadedEventArgs(filePath, configuration));
            
            return configuration;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON configuration file {FilePath}", filePath);
            var wrappedException = new InvalidDataException($"Invalid JSON in configuration file: {ex.Message}", ex);
            PersistenceError?.Invoke(this, new PersistenceErrorEventArgs("Load", filePath, wrappedException));
            throw wrappedException;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from {FilePath}", filePath);
            PersistenceError?.Invoke(this, new PersistenceErrorEventArgs("Load", filePath, ex));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateConfigurationFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        if (!File.Exists(filePath))
            return false;

        try
        {
            _logger.LogDebug("Validating configuration file {FilePath}", filePath);

            // Try to load and validate the configuration
            var configuration = await LoadConfigurationAsync(filePath);
            var validationErrors = configuration.Validate();
            
            var isValid = validationErrors.Count == 0;
            _logger.LogDebug("Configuration file {FilePath} validation result: {IsValid}", filePath, isValid);
            
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Configuration file {FilePath} validation failed", filePath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<string> CreateBackupAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Cannot create backup: file not found: {filePath}");

        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"{Path.GetFileNameWithoutExtension(filePath)}_{timestamp}{BackupExtension}";
            var backupFilePath = Path.Combine(Path.GetDirectoryName(filePath) ?? "", backupFileName);

            _logger.LogInformation("Creating backup of {FilePath} to {BackupFilePath}", filePath, backupFilePath);

            // Copy file to backup location
            await Task.Run(() => File.Copy(filePath, backupFilePath, overwrite: true));

            _logger.LogInformation("Backup created successfully: {BackupFilePath}", backupFilePath);
            return backupFilePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup of {FilePath}", filePath);
            PersistenceError?.Invoke(this, new PersistenceErrorEventArgs("Backup", filePath, ex));
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

            // Validate backup file first
            var isValid = await ValidateConfigurationFileAsync(backupFilePath);
            if (!isValid)
            {
                throw new InvalidDataException($"Backup file is not a valid configuration: {backupFilePath}");
            }

            // Ensure target directory exists
            var directory = Path.GetDirectoryName(targetFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Copy backup to target location
            await Task.Run(() => File.Copy(backupFilePath, targetFilePath, overwrite: true));

            _logger.LogInformation("Successfully restored from backup to {TargetFilePath}", targetFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore from backup {BackupFilePath} to {TargetFilePath}", backupFilePath, targetFilePath);
            PersistenceError?.Invoke(this, new PersistenceErrorEventArgs("Restore", backupFilePath, ex));
            throw;
        }
    }

    /// <inheritdoc />
    public ProjectConfiguration GetDefaultConfiguration()
    {
        _logger.LogDebug("Creating default configuration");
        
        var defaultConfig = new ProjectConfiguration("New Project", 120.0)
        {
            IsAbletonSyncEnabled = false,
            GlobalQuantization = new QuantizationSettings
            {
                Enabled = true,
                Division = "1/16"
            }
        };

        return defaultConfig;
    }
}