using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CircularMidiGenerator.ViewModels;
using CircularMidiGenerator.Views;
using CircularMidiGenerator.Services;
using CircularMidiGenerator.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CircularMidiGenerator.Core.Services;

namespace CircularMidiGenerator;

public partial class App : Application
{
    public static ServiceProvider? ServiceProvider { get; private set; }
    private ILogger<App>? _logger;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ConfigureServices();
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _logger = ServiceProvider?.GetService<ILogger<App>>();
            
            try
            {
                // Create main window first
                desktop.MainWindow = new MainWindow();
                
                // Initialize core services
                await InitializeServicesAsync();
                
                // Now create and set the view model
                var mainViewModel = ServiceProvider?.GetRequiredService<MainViewModel>();
                desktop.MainWindow.DataContext = mainViewModel;

                // Handle application shutdown
                desktop.ShutdownRequested += OnShutdownRequested;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize application");
                throw;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeServicesAsync()
    {
        if (ServiceProvider == null) return;

        _logger?.LogInformation("Initializing application services...");

        try
        {
            // Initialize MIDI service
            var midiService = ServiceProvider.GetRequiredService<IMidiService>();
            await midiService.InitializeAsync();
            _logger?.LogInformation("MIDI service initialized");

            // Initialize device manager
            var deviceManager = ServiceProvider.GetRequiredService<IMidiDeviceManager>();
            await deviceManager.StartMonitoringAsync();
            _logger?.LogInformation("MIDI device manager started");

            // Initialize lane controller with default lanes
            var laneController = ServiceProvider.GetRequiredService<ILaneController>();
            laneController.InitializeDefaultLanes();
            _logger?.LogInformation("Lane controller initialized");

            // Initialize marker trigger service
            var markerTriggerService = ServiceProvider.GetRequiredService<IMarkerTriggerService>();
            markerTriggerService.Initialize();
            _logger?.LogInformation("Marker trigger service initialized");

            // Start health monitoring
            var healthMonitor = ServiceProvider.GetRequiredService<ServiceHealthMonitor>();
            healthMonitor.ServiceHealthChanged += OnServiceHealthChanged;
            _logger?.LogInformation("Service health monitoring started");

            // Initialize error handling
            var errorHandler = ServiceProvider.GetRequiredService<GlobalErrorHandler>();
            ErrorHandler.Initialize(errorHandler);
            _logger?.LogInformation("Global error handler initialized");

            // Initialize crash recovery
            var crashRecovery = ServiceProvider.GetRequiredService<CrashRecoveryService>();
            crashRecovery.InitializeCrashRecovery();
            _logger?.LogInformation("Crash recovery initialized");

            _logger?.LogInformation("All services initialized successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize services");
            throw;
        }
    }

    private void OnServiceHealthChanged(object? sender, ServiceHealthChangedEventArgs e)
    {
        var result = e.Result;
        var logLevel = result.Status switch
        {
            ServiceHealthStatus.Healthy => LogLevel.Information,
            ServiceHealthStatus.Degraded => LogLevel.Warning,
            ServiceHealthStatus.Unhealthy => LogLevel.Error,
            _ => LogLevel.Information
        };

        _logger?.Log(logLevel, "Service health changed: {ServiceName} is {Status} - {Description}",
            result.ServiceName, result.Status, result.Description);

        if (result.Exception != null)
        {
            _logger?.LogError(result.Exception, "Service {ServiceName} health check failed", result.ServiceName);
        }
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        _logger?.LogInformation("Application shutdown requested, cleaning up services...");
        
        try
        {
            // Clean up crash recovery
            var crashRecovery = ServiceProvider?.GetService<CrashRecoveryService>();
            crashRecovery?.Cleanup();

            // Stop health monitoring
            var healthMonitor = ServiceProvider?.GetService<ServiceHealthMonitor>();
            if (healthMonitor != null)
            {
                healthMonitor.ServiceHealthChanged -= OnServiceHealthChanged;
                healthMonitor.Dispose();
            }

            // Dispose services in reverse order
            ServiceProvider?.Dispose();
            _logger?.LogInformation("Services disposed successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during service cleanup");
        }
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddApplicationLogging();

        // Core services - register all service implementations
        services.AddSingleton<IMidiService, MidiService>();
        services.AddSingleton<ITimingService, TimingService>();
        services.AddSingleton<IQuantizationService, QuantizationService>();
        services.AddSingleton<ILaneController, LaneController>();
        services.AddSingleton<IPersistenceService, PersistenceService>();
        
        // Additional services
        services.AddSingleton<IMidiDeviceManager, MidiDeviceManager>();
        services.AddSingleton<IAbletonSyncService, AbletonSyncService>();
        services.AddSingleton<IMarkerTriggerService, MarkerTriggerService>();
        services.AddSingleton<IMarkerGridService, MarkerGridService>();
        services.AddSingleton<IGridRenderingService, GridRenderingService>();
        services.AddSingleton<IBackupRecoveryService, BackupRecoveryService>();
        services.AddSingleton<IConfigurationRestorationService, ConfigurationRestorationService>();

        // File dialog service
        services.AddSingleton<IFileDialogService, FileDialogService>();

        // Health monitoring
        services.AddSingleton<ServiceHealthMonitor>();

        // Error handling and recovery
        services.AddSingleton<GlobalErrorHandler>();
        services.AddSingleton<CrashRecoveryService>();
        services.AddSingleton<NotificationService>();

        // Performance optimization
        services.AddSingleton<PerformanceMonitor>();
        services.AddSingleton<MidiLatencyOptimizer>();
        services.AddSingleton<PerformanceTestSuite>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<StatusIndicatorViewModel>();

        ServiceProvider = services.BuildServiceProvider();
    }
}