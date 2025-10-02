using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CircularMidiGenerator.ViewModels;
using CircularMidiGenerator.Controls;
using System.Collections.ObjectModel;
using CircularMidiGenerator.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace CircularMidiGenerator.Views;

public partial class MainWindow : Window
{
    private CircularCanvas? _circularCanvas;
    private MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        SetupEventHandlers();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void SetupEventHandlers()
    {
        // Find the CircularCanvas control
        _circularCanvas = this.FindControl<CircularCanvas>("CircularCanvas");
        
        if (_circularCanvas != null)
        {
            // Subscribe to CircularCanvas events
            _circularCanvas.MarkerPlaced += OnMarkerPlaced;
            _circularCanvas.MarkerMoved += OnMarkerMoved;
            _circularCanvas.MarkerRemoved += OnMarkerRemoved;
            _circularCanvas.MarkerSelected += OnMarkerSelected;
        }

        // Set up MIDI device controls
        SetupMidiDeviceControls();

        // Subscribe to DataContext changes to get the ViewModel
        this.DataContextChanged += (sender, e) =>
        {
            _viewModel = DataContext as MainViewModel;
            UpdateCanvasBindings();
            
            // Ensure BPM NumericUpDown shows the correct value
            if (_viewModel != null)
            {
                var bpmControl = this.FindControl<Avalonia.Controls.NumericUpDown>("BpmNumericUpDown");
                if (bpmControl != null)
                {
                    // Force the control to update its value using the decimal property
                    bpmControl.Value = _viewModel.BPMDecimal;
                }
                
                // Load MIDI devices
                LoadMidiDevicesAsync();
            }
        };
    }

    private void SetupMidiDeviceControls()
    {
        var midiComboBox = this.FindControl<ComboBox>("MidiDeviceComboBox");
        var refreshButton = this.FindControl<Button>("RefreshMidiButton");

        if (midiComboBox != null)
        {
            midiComboBox.SelectionChanged += OnMidiDeviceSelectionChanged;
        }

        if (refreshButton != null)
        {
            refreshButton.Click += OnRefreshMidiDevicesClick;
        }
    }

    private async void LoadMidiDevicesAsync()
    {
        try
        {
            var midiComboBox = this.FindControl<ComboBox>("MidiDeviceComboBox");
            if (midiComboBox == null) return;

            // Get MIDI device manager from the service provider
            var deviceManager = App.ServiceProvider?.GetService<CircularMidiGenerator.Core.Services.IMidiDeviceManager>();
            if (deviceManager == null) return;

            var devices = await deviceManager.GetAvailableDevicesAsync();
            
            // Create display items
            var deviceItems = new List<MidiDeviceItem>();
            deviceItems.Add(new MidiDeviceItem { Name = "No device selected", Device = null });
            
            foreach (var device in devices)
            {
                deviceItems.Add(new MidiDeviceItem { Name = device.Name, Device = device });
            }

            midiComboBox.ItemsSource = deviceItems;
            midiComboBox.DisplayMemberBinding = new Avalonia.Data.Binding("Name");
            
            // Select current active device
            var activeDevice = deviceManager.ActiveDevice;
            if (activeDevice != null)
            {
                var activeItem = deviceItems.FirstOrDefault(item => item.Device?.Id == activeDevice.Id);
                if (activeItem != null)
                {
                    midiComboBox.SelectedItem = activeItem;
                }
            }
            else
            {
                midiComboBox.SelectedIndex = 0; // Select "No device selected"
            }
        }
        catch (Exception ex)
        {
            // Handle error silently for now
            System.Diagnostics.Debug.WriteLine($"Error loading MIDI devices: {ex.Message}");
        }
    }

    private async void OnMidiDeviceSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            var comboBox = sender as ComboBox;
            var selectedItem = comboBox?.SelectedItem as MidiDeviceItem;
            
            if (selectedItem?.Device != null)
            {
                var deviceManager = App.ServiceProvider?.GetService<CircularMidiGenerator.Core.Services.IMidiDeviceManager>();
                if (deviceManager != null)
                {
                    await deviceManager.SetActiveDeviceAsync(selectedItem.Device);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error selecting MIDI device: {ex.Message}");
        }
    }

    private void OnRefreshMidiDevicesClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        LoadMidiDevicesAsync(); // Fire and forget
    }

    // Helper class for ComboBox items
    private class MidiDeviceItem
    {
        public string Name { get; set; } = "";
        public CircularMidiGenerator.Core.Services.MidiDevice? Device { get; set; }
    }

    private void UpdateCanvasBindings()
    {
        if (_circularCanvas == null || _viewModel == null) return;

        // Bind ViewModel properties to CircularCanvas
        _circularCanvas.Markers = GetAllMarkers();
        _circularCanvas.Lanes = _viewModel.Lanes;
        _circularCanvas.SelectedLaneId = _viewModel.SelectedLaneId;
        _circularCanvas.PlayheadAngle = _viewModel.PlayheadAngle;
        _circularCanvas.DiskRotation = _viewModel.PlayheadAngle;
        _circularCanvas.IsQuantizationEnabled = _viewModel.IsQuantizationEnabled;

        // Set up quantization grid lines (8 divisions for visual markers)
        var gridLines = new List<double>();
        for (int i = 0; i < 8; i++)
        {
            gridLines.Add(i * 45.0); // Every 45 degrees (8 divisions)
        }
        _circularCanvas.GridLines = gridLines;

        // Subscribe to ViewModel property changes
        _viewModel.PropertyChanged += (sender, e) =>
        {
            // Ensure UI updates happen on the UI thread
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (e.PropertyName == nameof(MainViewModel.PlayheadAngle))
                {
                    _circularCanvas.PlayheadAngle = _viewModel.PlayheadAngle;
                    // Rotate the disk based on playhead angle
                    _circularCanvas.DiskRotation = _viewModel.PlayheadAngle;
                }
                else if (e.PropertyName == nameof(MainViewModel.IsQuantizationEnabled))
                {
                    _circularCanvas.IsQuantizationEnabled = _viewModel.IsQuantizationEnabled;
                }
                else if (e.PropertyName == nameof(MainViewModel.SelectedLaneId))
                {
                    _circularCanvas.SelectedLaneId = _viewModel.SelectedLaneId;
                }
                else if (e.PropertyName == nameof(MainViewModel.Lanes))
                {
                    // Update canvas markers when lanes change (e.g., when cleared)
                    _circularCanvas.Markers = GetAllMarkers();
                }
            });
        };
    }

    private ObservableCollection<Marker> GetAllMarkers()
    {
        var allMarkers = new ObservableCollection<Marker>();
        
        if (_viewModel?.Lanes != null)
        {
            foreach (var lane in _viewModel.Lanes)
            {
                foreach (var marker in lane.Markers)
                {
                    allMarkers.Add(marker);
                }
            }
        }
        
        return allMarkers;
    }

    private void OnMarkerPlaced(object? sender, MarkerPlacedEventArgs e)
    {
        if (_viewModel == null) return;

        // Add the marker to the selected lane
        var selectedLane = _viewModel.SelectedLane;
        if (selectedLane != null)
        {
            e.Marker.Lane = selectedLane.Id;
            selectedLane.AddMarker(e.Marker);
            
            // Update the canvas markers collection
            _circularCanvas!.Markers = GetAllMarkers();
        }
    }

    private void OnMarkerMoved(object? sender, MarkerMovedEventArgs e)
    {
        // Marker position is already updated in the marker object
        // Just trigger a refresh if needed
        _circularCanvas?.InvalidateVisual();
    }

    private void OnMarkerRemoved(object? sender, MarkerRemovedEventArgs e)
    {
        if (_viewModel?.Lanes == null) return;

        // Find and remove the marker from its lane
        foreach (var lane in _viewModel.Lanes)
        {
            if (lane.RemoveMarker(e.Marker))
            {
                // Update the canvas markers collection
                _circularCanvas!.Markers = GetAllMarkers();
                break;
            }
        }
    }

    private void OnMarkerSelected(object? sender, MarkerSelectedEventArgs e)
    {
        // Update the selected marker in the canvas
        if (_circularCanvas != null)
        {
            _circularCanvas.SelectedMarker = e.Marker;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        // Unsubscribe from events to prevent memory leaks
        if (_circularCanvas != null)
        {
            _circularCanvas.MarkerPlaced -= OnMarkerPlaced;
            _circularCanvas.MarkerMoved -= OnMarkerMoved;
            _circularCanvas.MarkerRemoved -= OnMarkerRemoved;
            _circularCanvas.MarkerSelected -= OnMarkerSelected;
        }

        base.OnClosed(e);
    }
}