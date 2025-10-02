using Avalonia.Controls;
using Avalonia.Input;
using CircularMidiGenerator.ViewModels;

namespace CircularMidiGenerator.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Set up keyboard shortcuts
        SetupKeyboardShortcuts();
        
        // Connect CircularCanvas to ViewModel when DataContext changes
        DataContextChanged += OnDataContextChanged;
    }

    private void SetupKeyboardShortcuts()
    {
        // Add global key bindings for common operations
        KeyBindings.Add(new KeyBinding
        {
            Command = ReactiveUI.ReactiveCommand.Create(() => 
            {
                if (DataContext is MainViewModel vm)
                {
                    if (vm.IsPlaying)
                        vm.StopCommand.Execute().Subscribe();
                    else
                        vm.PlayCommand.Execute().Subscribe();
                }
            }),
            Gesture = new KeyGesture(Key.Space)
        });

        KeyBindings.Add(new KeyBinding
        {
            Command = ReactiveUI.ReactiveCommand.Create(() => 
            {
                if (DataContext is MainViewModel vm)
                    vm.StopCommand.Execute().Subscribe();
            }),
            Gesture = new KeyGesture(Key.Escape)
        });

        KeyBindings.Add(new KeyBinding
        {
            Command = ReactiveUI.ReactiveCommand.Create(() => 
            {
                if (DataContext is MainViewModel vm)
                    vm.StopAllNotesCommand.Execute().Subscribe();
            }),
            Gesture = new KeyGesture(Key.OemPeriod, KeyModifiers.Control)
        });

        // File operations
        KeyBindings.Add(new KeyBinding
        {
            Command = ReactiveUI.ReactiveCommand.Create(() => 
            {
                if (DataContext is MainViewModel vm)
                    vm.NewProjectCommand.Execute().Subscribe();
            }),
            Gesture = new KeyGesture(Key.N, KeyModifiers.Control)
        });

        KeyBindings.Add(new KeyBinding
        {
            Command = ReactiveUI.ReactiveCommand.Create(() => 
            {
                if (DataContext is MainViewModel vm)
                    vm.LoadConfigurationCommand.Execute().Subscribe();
            }),
            Gesture = new KeyGesture(Key.O, KeyModifiers.Control)
        });

        KeyBindings.Add(new KeyBinding
        {
            Command = ReactiveUI.ReactiveCommand.Create(() => 
            {
                if (DataContext is MainViewModel vm)
                    vm.SaveConfigurationCommand.Execute().Subscribe();
            }),
            Gesture = new KeyGesture(Key.S, KeyModifiers.Control)
        });

        KeyBindings.Add(new KeyBinding
        {
            Command = ReactiveUI.ReactiveCommand.Create(() => 
            {
                if (DataContext is MainViewModel vm)
                    vm.SaveAsCommand.Execute().Subscribe();
            }),
            Gesture = new KeyGesture(Key.S, KeyModifiers.Control | KeyModifiers.Shift)
        });

        // Edit operations
        KeyBindings.Add(new KeyBinding
        {
            Command = ReactiveUI.ReactiveCommand.Create(() => 
            {
                if (DataContext is MainViewModel vm)
                    vm.SelectAllMarkersCommand.Execute().Subscribe();
            }),
            Gesture = new KeyGesture(Key.A, KeyModifiers.Control)
        });

        KeyBindings.Add(new KeyBinding
        {
            Command = ReactiveUI.ReactiveCommand.Create(() => 
            {
                if (DataContext is MainViewModel vm)
                    vm.ClearAllMarkersCommand.Execute().Subscribe();
            }),
            Gesture = new KeyGesture(Key.Delete, KeyModifiers.Control)
        });
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        // Connect the CircularCanvas to the ViewModel when available
        if (DataContext is MainViewModel viewModel && CircularCanvas != null)
        {
            // The CircularCanvas will bind to the ViewModel through its DataContext
            CircularCanvas.DataContext = viewModel;
        }
    }
}