using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using ReactiveUI;
using Microsoft.Extensions.Logging;

namespace CircularMidiGenerator.Services;

/// <summary>
/// Notification types
/// </summary>
public enum NotificationType
{
    Information,
    Success,
    Warning,
    Error
}

/// <summary>
/// A user notification
/// </summary>
public class Notification : ReactiveObject
{
    private bool _isVisible = true;

    public string Id { get; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public TimeSpan? AutoDismissAfter { get; set; }
    public string? ActionText { get; set; }
    public Action? Action { get; set; }

    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    /// <summary>
    /// Color for the notification based on type
    /// </summary>
    public string NotificationColor => Type switch
    {
        NotificationType.Information => "#2196F3", // Blue
        NotificationType.Success => "#4CAF50", // Green
        NotificationType.Warning => "#FF9800", // Orange
        NotificationType.Error => "#F44336", // Red
        _ => "#757575" // Gray
    };

    /// <summary>
    /// Icon for the notification based on type
    /// </summary>
    public string NotificationIcon => Type switch
    {
        NotificationType.Information => "ℹ",
        NotificationType.Success => "✓",
        NotificationType.Warning => "⚠",
        NotificationType.Error => "✗",
        _ => "•"
    };
}

/// <summary>
/// Service for managing user notifications
/// </summary>
public class NotificationService : ReactiveObject, IDisposable
{
    private readonly ILogger<NotificationService> _logger;
    private readonly CompositeDisposable _disposables = new();

    public ObservableCollection<Notification> Notifications { get; } = new();

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Show an information notification
    /// </summary>
    public void ShowInfo(string title, string message, TimeSpan? autoDismiss = null)
    {
        ShowNotification(new Notification
        {
            Title = title,
            Message = message,
            Type = NotificationType.Information,
            AutoDismissAfter = autoDismiss ?? TimeSpan.FromSeconds(5)
        });
    }

    /// <summary>
    /// Show a success notification
    /// </summary>
    public void ShowSuccess(string title, string message, TimeSpan? autoDismiss = null)
    {
        ShowNotification(new Notification
        {
            Title = title,
            Message = message,
            Type = NotificationType.Success,
            AutoDismissAfter = autoDismiss ?? TimeSpan.FromSeconds(3)
        });
    }

    /// <summary>
    /// Show a warning notification
    /// </summary>
    public void ShowWarning(string title, string message, TimeSpan? autoDismiss = null)
    {
        ShowNotification(new Notification
        {
            Title = title,
            Message = message,
            Type = NotificationType.Warning,
            AutoDismissAfter = autoDismiss ?? TimeSpan.FromSeconds(8)
        });
    }

    /// <summary>
    /// Show an error notification
    /// </summary>
    public void ShowError(string title, string message, string? actionText = null, Action? action = null)
    {
        ShowNotification(new Notification
        {
            Title = title,
            Message = message,
            Type = NotificationType.Error,
            ActionText = actionText,
            Action = action,
            AutoDismissAfter = null // Errors don't auto-dismiss
        });
    }

    /// <summary>
    /// Show a notification with action
    /// </summary>
    public void ShowWithAction(string title, string message, NotificationType type, 
        string actionText, Action action, TimeSpan? autoDismiss = null)
    {
        ShowNotification(new Notification
        {
            Title = title,
            Message = message,
            Type = type,
            ActionText = actionText,
            Action = action,
            AutoDismissAfter = autoDismiss
        });
    }

    /// <summary>
    /// Show MIDI connection status
    /// </summary>
    public void ShowMidiStatus(string deviceName, bool isConnected)
    {
        if (isConnected)
        {
            ShowSuccess("MIDI Connected", $"Connected to {deviceName}");
        }
        else
        {
            ShowWarning("MIDI Disconnected", $"Lost connection to {deviceName}");
        }
    }

    /// <summary>
    /// Show Ableton sync status
    /// </summary>
    public void ShowAbletonStatus(bool isConnected, double? tempo = null)
    {
        if (isConnected)
        {
            var message = tempo.HasValue ? $"Synced at {tempo:F1} BPM" : "Synchronization active";
            ShowSuccess("Ableton Connected", message);
        }
        else
        {
            ShowInfo("Ableton Disconnected", "Using internal timing");
        }
    }

    /// <summary>
    /// Show file operation result
    /// </summary>
    public void ShowFileOperation(string operation, string fileName, bool success, string? errorMessage = null)
    {
        if (success)
        {
            ShowSuccess($"{operation} Successful", $"File: {fileName}");
        }
        else
        {
            ShowError($"{operation} Failed", $"File: {fileName}\n{errorMessage ?? "Unknown error"}");
        }
    }

    /// <summary>
    /// Show service health notification
    /// </summary>
    public void ShowServiceHealth(string serviceName, ServiceHealthStatus status, string? description = null)
    {
        var message = description ?? $"{serviceName} status changed";

        switch (status)
        {
            case ServiceHealthStatus.Healthy:
                ShowSuccess("Service Restored", $"{serviceName} is now healthy");
                break;
            case ServiceHealthStatus.Degraded:
                ShowWarning("Service Warning", $"{serviceName}: {message}");
                break;
            case ServiceHealthStatus.Unhealthy:
                ShowError("Service Error", $"{serviceName}: {message}");
                break;
        }
    }

    private void ShowNotification(Notification notification)
    {
        try
        {
            // Add to collection
            Notifications.Insert(0, notification); // Add to top

            // Set up auto-dismiss if specified
            if (notification.AutoDismissAfter.HasValue)
            {
                Task.Delay(notification.AutoDismissAfter.Value)
                    .ContinueWith(_ => DismissNotification(notification.Id));
            }

            // Limit number of notifications
            while (Notifications.Count > 10)
            {
                Notifications.RemoveAt(Notifications.Count - 1);
            }

            _logger.LogDebug("Notification shown: {Type} - {Title}: {Message}", 
                notification.Type, notification.Title, notification.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing notification");
        }
    }

    /// <summary>
    /// Dismiss a notification by ID
    /// </summary>
    public void DismissNotification(string id)
    {
        try
        {
            var notification = Notifications.FirstOrDefault(n => n.Id == id);
            if (notification != null)
            {
                notification.IsVisible = false;
                
                // Remove after animation
                Task.Delay(300).ContinueWith(_ =>
                {
                    if (Notifications.Contains(notification))
                    {
                        Notifications.Remove(notification);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing notification");
        }
    }

    /// <summary>
    /// Clear all notifications
    /// </summary>
    public void ClearAll()
    {
        try
        {
            foreach (var notification in Notifications.ToList())
            {
                notification.IsVisible = false;
            }

            Task.Delay(300).ContinueWith(_ => Notifications.Clear());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing notifications");
        }
    }

    /// <summary>
    /// Execute notification action
    /// </summary>
    public void ExecuteAction(string notificationId)
    {
        try
        {
            var notification = Notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification?.Action != null)
            {
                notification.Action.Invoke();
                DismissNotification(notificationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing notification action");
        }
    }

    public void Dispose()
    {
        _disposables?.Dispose();
    }
}