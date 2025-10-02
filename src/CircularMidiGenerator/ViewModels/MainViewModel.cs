using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
    private readonly ILaneController _laneController;
    private readonly IMarkerTriggerService? _markerTriggerService;
    private readonly ITimingService? _timingService;
    private readonly IQuantizationService? _quantizationService;
    private readonly IPersistenceService? _persistenceService;
    private readonly Services.IFileDialogService? _fileDialogService;
    private readonly CompositeDisposable _disposables = new();
    
    private double _bpm = 120.0;
    private bool _isPlaying;
    private bool _isAbletonSyncEnabled;
    private double _playheadAngle;
    private bool _isQuantizationEnabled;
    private string _selectedQuantizationDivision = "1/16";
    private string _statusMessage = "Ready";
    private bool _isMidiConnected;
    private string _selectedMidiDevice = "No device selected";

    public MainViewModel(
        ILogger<MainViewModel> logger, 
        ILaneController laneController, 
        IMarkerTriggerService? markerTriggerService = null,
        ITimingService? timingService = null,
        IQuantizationService? quantizationService = null,
        IPersistenceService? persistenceService = null,
        Services.IFileDialogService? fileDialogService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _laneController = laneController ?? throw new ArgumentNullException(nameof(laneController));
        _markerTriggerService = markerTriggerService;
        _timingService = timingService;
        _quantizationService = quantizationService;
        _persistenceService = persistenceService;
        _fileDialogService = fileDialogService;
        
        // Initialize default lanes
        _laneController.InitializeDefaultLanes();
        
        // Initialize commands
        PlayToggleCommand = ReactiveCommand.Create(ExecutePlayToggle);
        SaveConfigurationCommand = ReactiveCommand.CreateFromTask(ExecuteSaveConfiguration);
        LoadConfigurationCommand = ReactiveCommand.CreateFromTask(ExecuteLoadConfiguration);
        
        // Initialize lane control commands
        MuteLaneCommand = ReactiveCommand.Create<int>(ExecuteMuteLane);
        SoloLaneCommand = ReactiveCommand.Create<int>(ExecuteSoloLane);
        ClearAllSoloCommand = ReactiveCommand.Create(ExecuteClearAllSolo);
        StopAllNotesCommand = ReactiveCommand.Create(ExecuteStopAllNotes);
        
        // Initialize quantization commands
        ToggleQuantizationCommand = ReactiveCommand.Create(ExecuteToggleQuantization);
        SetQuantizationDivisionCommand = ReactiveCommand.Create<string>(ExecuteSetQuantizationDivision);
        
        // Initialize menu commands
        NewProjectCommand = ReactiveCommand.Create(ExecuteNewProject);
        SaveAsCommand = ReactiveCommand.CreateFromTask(ExecuteSaveAs);
        ShowAboutCommand = ReactiveCommand.Create(ExecuteShowAbout);
        ShowUserGuideCommand = ReactiveCommand.Create(ExecuteShowUserGuide);
        SelectAllMarkersCommand = ReactiveCommand.Create(ExecuteSelectAllMarkers);
        ClearAllMarkersCommand = ReactiveCommand.Create(ExecuteClearAllMarkers);
        
        // Subscribe to lane controller events
        _laneController.LaneStateChanged += OnLaneStateChanged;
        _laneController.MarkerAssignmentChanged += OnMarkerAssignmentChanged;
        
        // Subscribe to marker trigger events if service is available
        if (_markerTriggerService != null)
        {
            _markerTriggerService.MarkerProcessed += OnMarkerProcessed;
        }
        
        // Subscribe to timing service events if available
        if (_timingService != null)
        {
            _timingService.PlayheadMoved += OnPlayheadMoved;
            _timingService.MarkerTriggered += OnMarkerTriggered;
        }
        
        // Set up reactive property subscriptions
        SetupReactiveSubscriptions();
        
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
        set
        {
            if (this.RaiseAndSetIfChanged(ref _isPlaying, value))
            {
                // Notify dependent properties
                this.RaisePropertyChanged(nameof(PlayButtonText));
                this.RaisePropertyChanged(nameof(PlayButtonColor));
            }
        }
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
        get => _laneController.SelectedLaneId;
        set
        {
            if (_laneController.SelectedLaneId != value)
            {
                _laneController.SelectedLaneId = value;
                this.RaisePropertyChanged();
            }
        }
    }

    /// <summary>
    /// Collection of lanes from the lane controller
    /// </summary>
    public ObservableCollection<Lane> Lanes => _laneController.Lanes;

    /// <summary>
    /// Currently selected lane
    /// </summary>
    public Lane? SelectedLane => _laneController.GetSelectedLane();

    /// <summary>
    /// Whether quantization is enabled for the selected lane
    /// </summary>
    public bool IsQuantizationEnabled
    {
        get => _isQuantizationEnabled;
        set => this.RaiseAndSetIfChanged(ref _isQuantizationEnabled, value);
    }

    /// <summary>
    /// Selected quantization division (1/4, 1/8, 1/16, 1/32)
    /// </summary>
    public string SelectedQuantizationDivision
    {
        get => _selectedQuantizationDivision;
        set => this.RaiseAndSetIfChanged(ref _selectedQuantizationDivision, value);
    }

    /// <summary>
    /// Available quantization divisions
    /// </summary>
    public ObservableCollection<string> QuantizationDivisions { get; } = new()
    {
        "1/4", "1/8", "1/16", "1/32"
    };

    /// <summary>
    /// Current status message for the application
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    /// <summary>
    /// Whether MIDI device is connected
    /// </summary>
    public bool IsMidiConnected
    {
        get => _isMidiConnected;
        set => this.RaiseAndSetIfChanged(ref _isMidiConnected, value);
    }

    /// <summary>
    /// Currently selected MIDI device name
    /// </summary>
    public string SelectedMidiDevice
    {
        get => _selectedMidiDevice;
        set => this.RaiseAndSetIfChanged(ref _selectedMidiDevice, value);
    }

    /// <summary>
    /// Text for the play/stop toggle button
    /// </summary>
    public string PlayButtonText => IsPlaying ? "⏹ Stop" : "🎵 Play";

    /// <summary>
    /// Background color for the play/stop toggle button
    /// </summary>
    public string PlayButtonColor => IsPlaying ? "#FF6B9D" : "#06FFA5";

    #endregion

    #region Commands

    /// <summary>
    /// Command to toggle playback (play/stop)
    /// </summary>
    public ReactiveCommand<Unit, Unit> PlayToggleCommand { get; }

    /// <summary>
    /// Command to save configuration
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveConfigurationCommand { get; }

    /// <summary>
    /// Command to load configuration
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadConfigurationCommand { get; }

    /// <summary>
    /// Command to toggle mute state for a lane
    /// </summary>
    public ReactiveCommand<int, Unit> MuteLaneCommand { get; }

    /// <summary>
    /// Command to toggle solo state for a lane
    /// </summary>
    public ReactiveCommand<int, Unit> SoloLaneCommand { get; }

    /// <summary>
    /// Command to clear all solo states
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearAllSoloCommand { get; }

    /// <summary>
    /// Command to stop all currently playing notes (panic)
    /// </summary>
    public ReactiveCommand<Unit, Unit> StopAllNotesCommand { get; }

    /// <summary>
    /// Command to toggle quantization for the selected lane
    /// </summary>
    public ReactiveCommand<Unit, Unit> ToggleQuantizationCommand { get; }

    /// <summary>
    /// Command to set quantization division
    /// </summary>
    public ReactiveCommand<string, Unit> SetQuantizationDivisionCommand { get; }

    /// <summary>
    /// Command to create a new project
    /// </summary>
    public ReactiveCommand<Unit, Unit> NewProjectCommand { get; }

    /// <summary>
    /// Command to save project as (with file dialog)
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveAsCommand { get; }

    /// <summary>
    /// Command to show about dialog
    /// </summary>
    public ReactiveCommand<Unit, Unit> ShowAboutCommand { get; }

    /// <summary>
    /// Command to show user guide
    /// </summary>
    public ReactiveCommand<Unit, Unit> ShowUserGuideCommand { get; }

    /// <summary>
    /// Command to select all markers
    /// </summary>
    public ReactiveCommand<Unit, Unit> SelectAllMarkersCommand { get; }

    /// <summary>
    /// Command to clear all markers
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearAllMarkersCommand { get; }

    #endregion

    #region Command Implementations

    private void ExecutePlayToggle()
    {
        if (IsPlaying)
        {
            ExecuteStop();
        }
        else
        {
            ExecutePlay();
        }
    }

    private void ExecutePlay()
    {
        try
        {
            _logger.LogInformation("Starting playback at {BPM} BPM", BPM);
            
            // Start timing service if available
            if (_timingService != null)
            {
                _timingService.SetBPM(BPM);
                _timingService.EnableAbletonSync(IsAbletonSyncEnabled);
                _timingService.Start();
            }
            
            // Update markers in trigger service if available
            _markerTriggerService?.UpdateMarkers();
            
            IsPlaying = true;
            StatusMessage = $"Playing at {BPM:F1} BPM";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start playback");
            StatusMessage = "Failed to start playback";
        }
    }

    private void ExecuteStop()
    {
        try
        {
            _logger.LogInformation("Stopping playback");
            
            // Stop timing service if available
            _timingService?.Stop();
            
            // Stop all active notes when stopping playback
            _markerTriggerService?.StopAllNotes();
            
            IsPlaying = false;
            StatusMessage = "Stopped";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop playback");
            StatusMessage = "Failed to stop playback";
        }
    }

    private void ExecuteMuteLane(int laneId)
    {
        try
        {
            var lane = _laneController.GetLane(laneId);
            if (lane != null)
            {
                var newMuteState = !lane.IsMuted;
                _laneController.SetLaneMute(laneId, newMuteState);
                _logger.LogInformation("Lane {LaneName} mute state changed to {IsMuted}", lane.Name, newMuteState);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle mute for lane {LaneId}", laneId);
        }
    }

    private void ExecuteSoloLane(int laneId)
    {
        try
        {
            var lane = _laneController.GetLane(laneId);
            if (lane != null)
            {
                var newSoloState = !lane.IsSoloed;
                _laneController.SetLaneSolo(laneId, newSoloState);
                _logger.LogInformation("Lane {LaneName} solo state changed to {IsSoloed}", lane.Name, newSoloState);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle solo for lane {LaneId}", laneId);
        }
    }

    private void ExecuteClearAllSolo()
    {
        try
        {
            _laneController.ClearAllSolo();
            _logger.LogInformation("Cleared all solo states");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear all solo states");
        }
    }

    private void ExecuteStopAllNotes()
    {
        try
        {
            _markerTriggerService?.StopAllNotes();
            _logger.LogInformation("Stopped all active notes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop all notes");
        }
    }

    private async System.Threading.Tasks.Task ExecuteSaveConfiguration()
    {
        try
        {
            _logger.LogInformation("Saving configuration");
            
            if (_persistenceService != null && _fileDialogService != null)
            {
                var filePath = await _fileDialogService.ShowSaveFileDialogAsync(
                    "Save Configuration", 
                    "project.cmg", 
                    "Circular MIDI Generator Files|*.cmg|All Files|*.*");
                
                if (!string.IsNullOrEmpty(filePath))
                {
                    var config = CreateProjectConfiguration();
                    await _persistenceService.SaveConfigurationAsync(config, filePath);
                    StatusMessage = "Configuration saved successfully";
                }
                else
                {
                    StatusMessage = "Save cancelled";
                }
            }
            else
            {
                StatusMessage = "Save service not available";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration");
            StatusMessage = "Failed to save configuration";
        }
    }

    private async System.Threading.Tasks.Task ExecuteLoadConfiguration()
    {
        try
        {
            _logger.LogInformation("Loading configuration");
            
            if (_persistenceService != null && _fileDialogService != null)
            {
                var filePath = await _fileDialogService.ShowOpenFileDialogAsync(
                    "Load Configuration", 
                    "Circular MIDI Generator Files|*.cmg|All Files|*.*");
                
                if (!string.IsNullOrEmpty(filePath))
                {
                    var config = await _persistenceService.LoadConfigurationAsync(filePath);
                    ApplyProjectConfiguration(config);
                    StatusMessage = "Configuration loaded successfully";
                }
                else
                {
                    StatusMessage = "Load cancelled";
                }
            }
            else
            {
                StatusMessage = "Load service not available";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration");
            StatusMessage = "Failed to load configuration";
        }
    }

    private void ExecuteToggleQuantization()
    {
        try
        {
            var selectedLane = SelectedLane;
            if (selectedLane != null && _quantizationService != null)
            {
                var currentSettings = _quantizationService.GetQuantization(selectedLane.Id);
                currentSettings.Enabled = !currentSettings.Enabled;
                _quantizationService.SetQuantization(selectedLane.Id, currentSettings);
                
                IsQuantizationEnabled = currentSettings.Enabled;
                StatusMessage = $"Quantization {(currentSettings.Enabled ? "enabled" : "disabled")} for {selectedLane.Name}";
                
                _logger.LogInformation("Quantization toggled for lane {LaneName}: {Enabled}", 
                    selectedLane.Name, currentSettings.Enabled);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle quantization");
            StatusMessage = "Failed to toggle quantization";
        }
    }

    private void ExecuteSetQuantizationDivision(string division)
    {
        try
        {
            var selectedLane = SelectedLane;
            if (selectedLane != null && _quantizationService != null && !string.IsNullOrEmpty(division))
            {
                var currentSettings = _quantizationService.GetQuantization(selectedLane.Id);
                currentSettings.Division = division;
                _quantizationService.SetQuantization(selectedLane.Id, currentSettings);
                
                SelectedQuantizationDivision = division;
                StatusMessage = $"Quantization set to {division} for {selectedLane.Name}";
                
                _logger.LogInformation("Quantization division set for lane {LaneName}: {Division}", 
                    selectedLane.Name, division);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set quantization division");
            StatusMessage = "Failed to set quantization division";
        }
    }

    private void ExecuteNewProject()
    {
        try
        {
            _logger.LogInformation("Creating new project");
            
            // Stop playback if active
            if (IsPlaying)
            {
                ExecuteStop();
            }
            
            // Clear all lanes and reset to defaults
            _laneController.ClearAllLanes();
            _laneController.InitializeDefaultLanes();
            
            // Reset properties to defaults
            BPM = 120.0;
            IsAbletonSyncEnabled = false;
            IsQuantizationEnabled = false;
            SelectedQuantizationDivision = "1/16";
            
            StatusMessage = "New project created";
            _logger.LogInformation("New project created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create new project");
            StatusMessage = "Failed to create new project";
        }
    }

    private async System.Threading.Tasks.Task ExecuteSaveAs()
    {
        try
        {
            _logger.LogInformation("Save As requested");
            
            if (_persistenceService != null && _fileDialogService != null)
            {
                var filePath = await _fileDialogService.ShowSaveFileDialogAsync(
                    "Save Project As", 
                    "project.cmg", 
                    "Circular MIDI Generator Files|*.cmg|All Files|*.*");
                
                if (!string.IsNullOrEmpty(filePath))
                {
                    var config = CreateProjectConfiguration();
                    await _persistenceService.SaveConfigurationAsync(config, filePath);
                    StatusMessage = "Project saved successfully";
                }
                else
                {
                    StatusMessage = "Save cancelled";
                }
            }
            else
            {
                StatusMessage = "Save service not available";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save project as");
            StatusMessage = "Failed to save project";
        }
    }

    private void ExecuteShowAbout()
    {
        try
        {
            _logger.LogInformation("Showing about dialog");
            StatusMessage = "About Circular MIDI Generator - A playful MIDI pattern creator";
            
            // TODO: Show actual about dialog when UI service is available
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show about dialog");
        }
    }

    private void ExecuteShowUserGuide()
    {
        try
        {
            _logger.LogInformation("Showing user guide");
            StatusMessage = "User Guide: Click to place markers, drag to move, drag outside to remove";
            
            // TODO: Show actual user guide when UI service is available
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show user guide");
        }
    }

    private void ExecuteSelectAllMarkers()
    {
        try
        {
            _logger.LogInformation("Selecting all markers");
            
            // TODO: Implement marker selection when CircularCanvas supports it
            var totalMarkers = Lanes.Sum(lane => lane.Markers.Count);
            StatusMessage = $"Selected all {totalMarkers} markers";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to select all markers");
            StatusMessage = "Failed to select markers";
        }
    }

    private void ExecuteClearAllMarkers()
    {
        try
        {
            _logger.LogInformation("Clearing all markers");
            
            var totalMarkers = 0;
            foreach (var lane in Lanes)
            {
                totalMarkers += lane.Markers.Count;
                lane.Markers.Clear();
            }
            
            // Update markers in trigger service
            _markerTriggerService?.UpdateMarkers();
            
            StatusMessage = $"Cleared {totalMarkers} markers";
            _logger.LogInformation("Cleared {TotalMarkers} markers from all lanes", totalMarkers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear all markers");
            StatusMessage = "Failed to clear markers";
        }
    }

    #endregion

    #region Helper Methods

    private void SetupReactiveSubscriptions()
    {
        // React to BPM changes
        this.WhenAnyValue(x => x.BPM)
            .Where(bpm => bpm > 0)
            .Subscribe(bpm =>
            {
                _timingService?.SetBPM(bpm);
                if (!IsPlaying)
                {
                    StatusMessage = $"BPM set to {bpm:F1}";
                }
            })
            .DisposeWith(_disposables);

        // React to Ableton sync changes
        this.WhenAnyValue(x => x.IsAbletonSyncEnabled)
            .Subscribe(enabled =>
            {
                _timingService?.EnableAbletonSync(enabled);
                StatusMessage = $"Ableton sync {(enabled ? "enabled" : "disabled")}";
            })
            .DisposeWith(_disposables);

        // React to selected lane changes
        this.WhenAnyValue(x => x.SelectedLaneId)
            .Subscribe(laneId =>
            {
                UpdateQuantizationPropertiesForSelectedLane();
            })
            .DisposeWith(_disposables);
    }

    private void UpdateQuantizationPropertiesForSelectedLane()
    {
        var selectedLane = SelectedLane;
        if (selectedLane != null && _quantizationService != null)
        {
            var settings = _quantizationService.GetQuantization(selectedLane.Id);
            IsQuantizationEnabled = settings.Enabled;
            SelectedQuantizationDivision = settings.Division;
        }
    }

    private ProjectConfiguration CreateProjectConfiguration()
    {
        return new ProjectConfiguration
        {
            BPM = BPM,
            IsAbletonSyncEnabled = IsAbletonSyncEnabled,
            Lanes = new System.Collections.Generic.List<Lane>(Lanes),
            Version = "1.0",
            Created = DateTime.UtcNow
        };
    }

    private void ApplyProjectConfiguration(ProjectConfiguration config)
    {
        BPM = config.BPM;
        IsAbletonSyncEnabled = config.IsAbletonSyncEnabled;
        
        // Clear existing lanes and add loaded ones
        _laneController.ClearAllLanes();
        foreach (var lane in config.Lanes)
        {
            _laneController.AddLane(lane.Name, lane.MidiChannel);
            var addedLane = _laneController.GetLane(lane.Id);
            if (addedLane != null)
            {
                addedLane.IsMuted = lane.IsMuted;
                addedLane.IsSoloed = lane.IsSoloed;
                addedLane.ThemeColor = lane.ThemeColor;
                
                // Restore markers
                foreach (var marker in lane.Markers)
                {
                    addedLane.Markers.Add(marker);
                }
                
                // Restore quantization settings
                if (_quantizationService != null && lane.Quantization != null)
                {
                    _quantizationService.SetQuantization(lane.Id, lane.Quantization);
                }
            }
        }
        
        // Update UI properties
        UpdateQuantizationPropertiesForSelectedLane();
    }

    #endregion

    #region Lane Management Methods

    /// <summary>
    /// Add a new lane
    /// </summary>
    public Lane AddLane(string name, int midiChannel)
    {
        return _laneController.AddLane(name, midiChannel);
    }

    /// <summary>
    /// Remove a lane
    /// </summary>
    public bool RemoveLane(int laneId)
    {
        return _laneController.RemoveLane(laneId);
    }

    /// <summary>
    /// Set mute state for a lane
    /// </summary>
    public void SetLaneMute(int laneId, bool isMuted)
    {
        _laneController.SetLaneMute(laneId, isMuted);
    }

    /// <summary>
    /// Set solo state for a lane
    /// </summary>
    public void SetLaneSolo(int laneId, bool isSoloed)
    {
        _laneController.SetLaneSolo(laneId, isSoloed);
    }

    /// <summary>
    /// Clear all solo states
    /// </summary>
    public void ClearAllSolo()
    {
        _laneController.ClearAllSolo();
    }

    /// <summary>
    /// Assign a marker to a specific lane
    /// </summary>
    public void AssignMarkerToLane(Marker marker, int laneId)
    {
        _laneController.AssignMarkerToLane(marker, laneId);
    }

    #endregion

    #region Event Handlers

    private void OnLaneStateChanged(object? sender, LaneStateChangedEventArgs e)
    {
        _logger.LogDebug("Lane {LaneId} state changed: {PropertyName} = {NewValue}", 
            e.LaneId, e.PropertyName, e.NewValue);
        
        // Raise property changed for UI updates
        this.RaisePropertyChanged(nameof(Lanes));
        
        // If selected lane changed, update the property
        if (e.LaneId == SelectedLaneId)
        {
            this.RaisePropertyChanged(nameof(SelectedLane));
        }
    }

    private void OnMarkerAssignmentChanged(object? sender, MarkerAssignmentChangedEventArgs e)
    {
        _logger.LogDebug("Marker {MarkerId} moved from lane {OldLane} to lane {NewLane}", 
            e.Marker.Id, e.OldLaneId, e.NewLaneId);
        
        // Update markers in trigger service when assignments change
        _markerTriggerService?.UpdateMarkers();
        
        // Raise property changed for UI updates
        this.RaisePropertyChanged(nameof(Lanes));
    }

    private void OnMarkerProcessed(object? sender, MarkerTriggerEventArgs e)
    {
        _logger.LogDebug("Marker {MarkerId} processed: Triggered={WasTriggered}, Lane={LaneName}", 
            e.Marker.Id, e.WasTriggered, e.Lane.Name);
        
        // This event can be used for visual feedback in the UI
        // For example, highlighting triggered markers or showing mute/solo feedback
    }

    private void OnPlayheadMoved(object? sender, PlayheadEventArgs e)
    {
        // Update playhead angle on UI thread
        PlayheadAngle = e.CurrentAngle;
    }

    private void OnMarkerTriggered(object? sender, MarkerTriggeredEventArgs e)
    {
        _logger.LogDebug("Marker {MarkerId} triggered at angle {Angle}°", e.Marker.Id, e.TriggerAngle);
        
        // This event can be used for visual feedback when markers are triggered
        // The marker's IsActive property will already be set by the timing service
    }

    #endregion

    public void Dispose()
    {
        // Unsubscribe from lane controller events
        _laneController.LaneStateChanged -= OnLaneStateChanged;
        _laneController.MarkerAssignmentChanged -= OnMarkerAssignmentChanged;
        
        // Unsubscribe from marker trigger service events
        if (_markerTriggerService != null)
        {
            _markerTriggerService.MarkerProcessed -= OnMarkerProcessed;
        }
        
        // Unsubscribe from timing service events
        if (_timingService != null)
        {
            _timingService.PlayheadMoved -= OnPlayheadMoved;
            _timingService.MarkerTriggered -= OnMarkerTriggered;
        }
        
        _disposables?.Dispose();
        _logger.LogInformation("MainViewModel disposed");
    }
}