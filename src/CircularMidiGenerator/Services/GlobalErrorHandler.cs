using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;
using MessageBox.Avalonia;

namespace CircularMidiGenerator.Services;

/// <summary>
/// Error severity levels for user feedback
/// </summary>
public enum ErrorSeverity
{
    Information,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Error context information
/// </summary>
public class ErrorContext
{
    public string Operation { get; set; } = string.Empty;
    public string UserMessage { get; set; } = string.Empty;
    public string? TechnicalDetails { get; set; }
    public ErrorSeverity Severity { get; set; }
    public Exception? Exception { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public bool ShowToUser { get; set; } = true;
    public string? SuggestedAction { get; set; }
}

/// <summary>
/// Global error handler for the application
/// </summary>
public class GlobalErrorHandler
{
    private readonly ILogger<GlobalErrorHandler> _logger;

    public event EventHandler<ErrorContext>? ErrorOccurred;

    public GlobalErrorHandler(ILogger<GlobalErrorHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handle an error with context information
    /// </summary>
    public async Task HandleErrorAsync(ErrorContext context)
    {
        // Log the error
        var logLevel = context.Severity switch
        {
            ErrorSeverity.Information => LogLevel.Information,
            ErrorSeverity.Warning => LogLevel.Warning,
            ErrorSeverity.Error => LogLevel.Error,
            ErrorSeverity.Critical => LogLevel.Critical,
            _ => LogLevel.Error
        };

        if (context.Exception != null)
        {
            _logger.Log(logLevel, context.Exception, 
                "Error in {Operation}: {UserMessage}. Technical details: {TechnicalDetails}",
                context.Operation, context.UserMessage, context.TechnicalDetails);
        }
        else
        {
            _logger.Log(logLevel, 
                "Error in {Operation}: {UserMessage}. Technical details: {TechnicalDetails}",
                context.Operation, context.UserMessage, context.TechnicalDetails);
        }

        // Fire event for subscribers
        ErrorOccurred?.Invoke(this, context);

        // Show user notification if requested
        if (context.ShowToUser)
        {
            await ShowUserNotificationAsync(context);
        }

        // Handle critical errors
        if (context.Severity == ErrorSeverity.Critical)
        {
            await HandleCriticalErrorAsync(context);
        }
    }

    /// <summary>
    /// Handle MIDI-related errors
    /// </summary>
    public async Task HandleMidiErrorAsync(Exception exception, string operation)
    {
        var context = new ErrorContext
        {
            Operation = $"MIDI: {operation}",
            UserMessage = GetUserFriendlyMidiMessage(exception, operation),
            TechnicalDetails = exception.Message,
            Severity = ErrorSeverity.Error,
            Exception = exception,
            SuggestedAction = GetMidiErrorSuggestion(exception, operation)
        };

        await HandleErrorAsync(context);
    }

    /// <summary>
    /// Handle timing/synchronization errors
    /// </summary>
    public async Task HandleTimingErrorAsync(Exception exception, string operation)
    {
        var context = new ErrorContext
        {
            Operation = $"Timing: {operation}",
            UserMessage = GetUserFriendlyTimingMessage(exception, operation),
            TechnicalDetails = exception.Message,
            Severity = ErrorSeverity.Warning,
            Exception = exception,
            SuggestedAction = "Check your system's audio settings and try restarting the timing engine."
        };

        await HandleErrorAsync(context);
    }

    /// <summary>
    /// Handle file I/O errors
    /// </summary>
    public async Task HandleFileErrorAsync(Exception exception, string operation, string filePath)
    {
        var context = new ErrorContext
        {
            Operation = $"File: {operation}",
            UserMessage = GetUserFriendlyFileMessage(exception, operation, filePath),
            TechnicalDetails = $"{exception.Message} (File: {filePath})",
            Severity = ErrorSeverity.Error,
            Exception = exception,
            SuggestedAction = GetFileErrorSuggestion(exception, operation)
        };

        await HandleErrorAsync(context);
    }

    /// <summary>
    /// Handle UI-related errors
    /// </summary>
    public async Task HandleUIErrorAsync(Exception exception, string operation)
    {
        var context = new ErrorContext
        {
            Operation = $"UI: {operation}",
            UserMessage = "An interface error occurred. The application will continue running.",
            TechnicalDetails = exception.Message,
            Severity = ErrorSeverity.Warning,
            Exception = exception,
            SuggestedAction = "Try refreshing the interface or restarting the application if the problem persists."
        };

        await HandleErrorAsync(context);
    }

    private async Task ShowUserNotificationAsync(ErrorContext context)
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow != null)
            {
                var title = context.Severity switch
                {
                    ErrorSeverity.Information => "Information",
                    ErrorSeverity.Warning => "Warning",
                    ErrorSeverity.Error => "Error",
                    ErrorSeverity.Critical => "Critical Error",
                    _ => "Notification"
                };

                var message = context.UserMessage;
                if (!string.IsNullOrEmpty(context.SuggestedAction))
                {
                    message += $"\n\nSuggested action: {context.SuggestedAction}";
                }

                // Show message box
                var messageBox = MessageBoxManager.GetMessageBoxStandard(title, message);
                await messageBox.ShowWindowDialogAsync(desktop.MainWindow);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show user notification");
        }
    }

    private async Task HandleCriticalErrorAsync(ErrorContext context)
    {
        _logger.LogCritical("Critical error occurred, initiating graceful shutdown");
        
        try
        {
            // Attempt graceful shutdown
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.TryShutdown();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate graceful shutdown");
        }
    }

    private string GetUserFriendlyMidiMessage(Exception exception, string operation)
    {
        return operation.ToLower() switch
        {
            var op when op.Contains("device") => "MIDI device connection failed. Please check your MIDI device connections.",
            var op when op.Contains("output") => "Failed to send MIDI data. Your MIDI device may be disconnected.",
            var op when op.Contains("initialize") => "Failed to initialize MIDI system. Please check your MIDI drivers.",
            _ => "A MIDI error occurred. Please check your MIDI device connections and settings."
        };
    }

    private string GetUserFriendlyTimingMessage(Exception exception, string operation)
    {
        return operation.ToLower() switch
        {
            var op when op.Contains("sync") => "Synchronization with external software failed. Falling back to internal timing.",
            var op when op.Contains("bpm") => "Invalid tempo setting. Please check your BPM value.",
            var op when op.Contains("start") => "Failed to start timing engine. Audio playback may be affected.",
            _ => "A timing error occurred. Audio synchronization may be affected."
        };
    }

    private string GetUserFriendlyFileMessage(Exception exception, string operation, string filePath)
    {
        var fileName = System.IO.Path.GetFileName(filePath);
        
        return operation.ToLower() switch
        {
            var op when op.Contains("save") => $"Failed to save configuration to '{fileName}'. Please check file permissions.",
            var op when op.Contains("load") => $"Failed to load configuration from '{fileName}'. The file may be corrupted.",
            var op when op.Contains("backup") => $"Failed to create backup of '{fileName}'. Save operation may be risky.",
            _ => $"File operation failed for '{fileName}'. Please check file permissions and disk space."
        };
    }

    private string GetMidiErrorSuggestion(Exception exception, string operation)
    {
        return operation.ToLower() switch
        {
            var op when op.Contains("device") => "Try reconnecting your MIDI device or selecting a different device.",
            var op when op.Contains("output") => "Check MIDI device connections and try restarting the application.",
            var op when op.Contains("initialize") => "Restart the application or check MIDI driver installation.",
            _ => "Check MIDI device connections and application settings."
        };
    }

    private string GetFileErrorSuggestion(Exception exception, string operation)
    {
        return operation.ToLower() switch
        {
            var op when op.Contains("save") => "Try saving to a different location or check disk space.",
            var op when op.Contains("load") => "Try loading a different file or restore from backup.",
            var op when op.Contains("backup") => "Ensure you have write permissions to the backup location.",
            _ => "Check file permissions and available disk space."
        };
    }
}

/// <summary>
/// Static helper for easy access to global error handling
/// </summary>
public static class ErrorHandler
{
    private static GlobalErrorHandler? _instance;

    public static void Initialize(GlobalErrorHandler handler)
    {
        _instance = handler;
    }

    public static async Task HandleAsync(ErrorContext context)
    {
        if (_instance != null)
        {
            await _instance.HandleErrorAsync(context);
        }
    }

    public static async Task HandleMidiAsync(Exception exception, string operation)
    {
        if (_instance != null)
        {
            await _instance.HandleMidiErrorAsync(exception, operation);
        }
    }

    public static async Task HandleTimingAsync(Exception exception, string operation)
    {
        if (_instance != null)
        {
            await _instance.HandleTimingErrorAsync(exception, operation);
        }
    }

    public static async Task HandleFileAsync(Exception exception, string operation, string filePath)
    {
        if (_instance != null)
        {
            await _instance.HandleFileErrorAsync(exception, operation, filePath);
        }
    }

    public static async Task HandleUIAsync(Exception exception, string operation)
    {
        if (_instance != null)
        {
            await _instance.HandleUIErrorAsync(exception, operation);
        }
    }
}