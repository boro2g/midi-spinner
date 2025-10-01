using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using ReactiveUI;
using Microsoft.Extensions.Logging;
using CircularMidiGenerator.Core.Models;
using CircularMidiGenerator.Core.Services;

namespace CircularMidiGenerator.ViewModels;

/// <summary>
/// Main ViewModel for the application with reactive properties
/// </summary>
public class MainViewModel : ReactiveObject, IDisposable
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly CompositeDisposable _disposables = new();
    
    private double _bpm = 120.0;
    private bool _isPlaying;
    private bool _isAbletonSyncEnabled;
    private double _playheadAngle;
    private int _selectedLaneId;

    public MainViewModel(ILogger<MainViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Initialize collections
        Lanes = new ObservableCollection<Lane>();
        InitializeDefaultLanes();
        
        // Initialize commands
        PlayCommand = ReactiveCommand.Create(ExecutePlay);
        StopCommand = ReactiveCommand.Create(ExecuteStop);
        SaveConfigurationCommand = ReactiveCommand.Create(ExecuteSaveConfiguration);
        LoadConfigurationCommand = ReactiveCommand.Create(ExecuteLoadConfiguration);
        
        _logger.LogInformation("MainViewModel initialized");
    }

    #region Properties

    /// <summary>
    /// Current BPM setting
    /// </summary>
    public double BPM
    {
        get => _bpm;
        set => this.RaiseAndSetIfChanged(ref _bpm, value);
    }

    /// <summary>
    /// Whether playback is active
    /// </summary>
    public bool IsPlaying
    {
        get => _isPlaying;
        set => this.RaiseAndSetIfChanged(ref _isPlaying, value);
    }

    /// <summary>
    /// Whether Ableton Live sync is enabled
    /// </summary>
    public bool IsAbletonSyncEnabled
    {
        get => _isAbletonSyncEnabled;
        set => this.RaiseAndSetIfChanged(ref _isAbletonSyncEnabled, value);
    }

    /// <summary>
    /// Current playhead angle in degrees
    /// </summary>
    public double PlayheadAngle
    {
        get => _playheadAngle;
        set => this.RaiseAndSetIfChanged(ref _playheadAngle, value);
    }

    /// <summary>
    /// Currently selected lane ID
    /// </summary>
    public int SelectedLaneId
    {
        get => _selectedLaneId;
        set => this.RaiseAndSetIfChanged(ref _selectedLaneId, value);
    }

    /// <summary>
    /// Collection of lanes
    /// </summary>
    public ObservableCollection<Lane> Lanes { get; }

    #endregion

    #region Commands

    /// <summary>
    /// Command to start playback
    /// </summary>
    public ReactiveCommand<Unit, Unit> PlayCommand { get; }

    /// <summary>
    /// Command to stop playback
    /// </summary>
    public ReactiveCommand<Unit, Unit> StopCommand { get; }

    /// <summary>
    /// Command to save configuration
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveConfigurationCommand { get; }

    /// <summary>
    /// Command to load configuration
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadConfigurationCommand { get; }

    #endregion

    #region Command Implementations

    private void ExecutePlay()
    {
        try
        {
            _logger.LogInformation("Starting playback at {BPM} BPM", BPM);
            IsPlaying = true;
            // TODO: Start timing service when implemented
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start playback");
        }
    }

    private void ExecuteStop()
    {
        try
        {
            _logger.LogInformation("Stopping playback");
            IsPlaying = false;
            // TODO: Stop timing service when implemented
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop playback");
        }
    }

    private void ExecuteSaveConfiguration()
    {
        try
        {
            _logger.LogInformation("Saving configuration");
            // TODO: Implement save functionality when persistence service is available
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration");
        }
    }

    private void ExecuteLoadConfiguration()
    {
        try
        {
            _logger.LogInformation("Loading configuration");
            // TODO: Implement load functionality when persistence service is available
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration");
        }
    }

    #endregion

    #region Private Methods

    private void InitializeDefaultLanes()
    {
        Lanes.Clear();
        Lanes.Add(new Lane(0, "Drums", 1) { ThemeColor = System.Drawing.Color.Red });
        Lanes.Add(new Lane(1, "Bass", 2) { ThemeColor = System.Drawing.Color.Blue });
        Lanes.Add(new Lane(2, "Lead", 3) { ThemeColor = System.Drawing.Color.Green });
        Lanes.Add(new Lane(3, "Pad", 4) { ThemeColor = System.Drawing.Color.Purple });
        
        SelectedLaneId = 0;
    }

    #endregion

    public void Dispose()
    {
        _disposables?.Dispose();
        _logger.LogInformation("MainViewModel disposed");
    }
}