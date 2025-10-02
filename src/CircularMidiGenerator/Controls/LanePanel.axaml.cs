using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using CircularMidiGenerator.Core.Models;
using CircularMidiGenerator.ViewModels;

namespace CircularMidiGenerator.Controls;

/// <summary>
/// Lane management panel control with mute/solo buttons and visual grouping
/// </summary>
public partial class LanePanel : UserControl
{
    public LanePanel()
    {
        InitializeComponent();
        
        // Set up data context binding for lanes
        DataContextChanged += OnDataContextChanged;
        
        // Set up event handlers for buttons
        SetupEventHandlers();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Update the ItemsControl when DataContext changes
        if (DataContext is MainViewModel viewModel)
        {
            var lanesControl = this.FindControl<ItemsControl>("LanesItemsControl");
            if (lanesControl != null)
            {
                lanesControl.ItemsSource = viewModel.Lanes;
            }
        }
    }

    private void SetupEventHandlers()
    {
        // Find and set up global control buttons
        var clearAllSoloButton = this.FindControl<Button>("ClearAllSoloButton");
        var stopAllNotesButton = this.FindControl<Button>("StopAllNotesButton");
        
        if (clearAllSoloButton != null)
        {
            clearAllSoloButton.Click += OnClearAllSoloClick;
        }
        
        if (stopAllNotesButton != null)
        {
            stopAllNotesButton.Click += OnStopAllNotesClick;
        }
    }

    private void OnClearAllSoloClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.ClearAllSolo();
        }
    }

    private void OnStopAllNotesClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            // Execute the stop all notes command directly
            viewModel.StopAllNotesCommand.Execute().Subscribe();
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        // Set up event handlers for dynamically created buttons
        SetupDynamicEventHandlers();
    }

    private void SetupDynamicEventHandlers()
    {
        var lanesControl = this.FindControl<ItemsControl>("LanesItemsControl");
        if (lanesControl?.ItemsSource is ObservableCollection<Lane> lanes)
        {
            // Subscribe to collection changes to handle new lanes
            lanes.CollectionChanged += (s, e) => UpdateButtonEventHandlers();
        }
        
        UpdateButtonEventHandlers();
    }

    private void UpdateButtonEventHandlers()
    {
        // Find all mute and solo buttons in the visual tree and set up event handlers
        var muteButtons = this.GetLogicalDescendants().OfType<Button>().Where(b => b.Name == "MuteButton");
        var soloButtons = this.GetLogicalDescendants().OfType<Button>().Where(b => b.Name == "SoloButton");
        var laneItems = this.GetLogicalDescendants().OfType<Border>().Where(b => b.Name == "LaneItemBorder");
        
        foreach (var button in muteButtons)
        {
            button.Click -= OnMuteButtonClick; // Remove existing handler
            button.Click += OnMuteButtonClick;
        }
        
        foreach (var button in soloButtons)
        {
            button.Click -= OnSoloButtonClick; // Remove existing handler
            button.Click += OnSoloButtonClick;
        }
        
        foreach (var border in laneItems)
        {
            border.PointerPressed -= OnLaneItemClick; // Remove existing handler
            border.PointerPressed += OnLaneItemClick;
        }
    }

    private void OnMuteButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int laneId && DataContext is MainViewModel viewModel)
        {
            viewModel.SetLaneMute(laneId, !viewModel.Lanes.FirstOrDefault(l => l.Id == laneId)?.IsMuted ?? false);
        }
    }

    private void OnSoloButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int laneId && DataContext is MainViewModel viewModel)
        {
            viewModel.SetLaneSolo(laneId, !viewModel.Lanes.FirstOrDefault(l => l.Id == laneId)?.IsSoloed ?? false);
        }
    }

    private void OnLaneItemClick(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is int laneId && DataContext is MainViewModel viewModel)
        {
            // Set the selected lane
            viewModel.SelectedLaneId = laneId;
            
            // Update visual selection state
            UpdateLaneSelection();
        }
    }

    private void UpdateLaneSelection()
    {
        if (DataContext is not MainViewModel viewModel) return;
        
        var laneItems = this.GetLogicalDescendants().OfType<Border>().Where(b => b.Name == "LaneItemBorder");
        
        foreach (var border in laneItems)
        {
            if (border.Tag is int laneId)
            {
                // Add or remove selected class based on whether this is the selected lane
                if (laneId == viewModel.SelectedLaneId)
                {
                    border.Classes.Add("selected");
                }
                else
                {
                    border.Classes.Remove("selected");
                }
            }
        }
    }
}