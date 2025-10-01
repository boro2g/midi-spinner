---
inclusion: always
---

# Avalonia UI Development Patterns

## MVVM Architecture

### ViewModel Best Practices
- Inherit from `ReactiveObject` for property change notifications
- Use `ReactiveCommand` for command bindings
- Keep ViewModels testable by avoiding direct UI dependencies
- Use dependency injection for service dependencies
- Implement `IDisposable` when ViewModels hold resources

### View-ViewModel Binding
- Use `{Binding}` syntax for data binding
- Prefer `OneWay` binding for read-only data
- Use `TwoWay` binding sparingly, only for user input
- Implement `INotifyDataErrorInfo` for validation
- Use converters for data transformation in bindings

### Reactive Extensions Integration
- Use `WhenAnyValue` for property change reactions
- Implement `ReactiveCommand` with proper error handling
- Use `ObservableAsPropertyHelper` for computed properties
- Handle subscriptions properly to prevent memory leaks

## Custom Control Development

### Control Inheritance
- Inherit from appropriate base classes (`UserControl`, `Control`, `Canvas`)
- Override `OnApplyTemplate` for templated controls
- Use `TemplatedControl` for reusable, styleable controls
- Implement proper measure/arrange logic for layout

### Dependency Properties
```csharp
public static readonly StyledProperty<double> AngleProperty =
    AvaloniaProperty.Register<CircularCanvas, double>(nameof(Angle), 0.0);

public double Angle
{
    get => GetValue(AngleProperty);
    set => SetValue(AngleProperty, value);
}
```

### Event Handling
- Use routed events for bubbling behavior
- Implement proper event cleanup in Dispose methods
- Use weak event patterns for long-lived subscriptions
- Handle both mouse and touch events for cross-platform support

## Rendering and Graphics

### Custom Drawing
- Override `Render` method for custom drawing
- Use `DrawingContext` efficiently - minimize draw calls
- Cache expensive calculations and reuse drawing objects
- Use transforms for rotation and scaling operations

### Performance Optimization
- Use `InvalidateVisual()` judiciously to trigger redraws
- Implement dirty region tracking for complex controls
- Use hardware acceleration when available
- Profile rendering performance with large numbers of elements

### Animation
- Use Avalonia's animation system for smooth transitions
- Prefer transforms over property animations for performance
- Use easing functions for natural motion
- Implement proper animation cleanup

## Layout and Styling

### XAML Best Practices
- Use meaningful names for controls (`x:Name`)
- Organize resources logically in ResourceDictionaries
- Use styles for consistent appearance
- Implement proper data templates for collections

### Responsive Design
- Use Grid and DockPanel for flexible layouts
- Implement proper sizing constraints
- Test on different screen sizes and DPI settings
- Use vector graphics for scalable icons

### Theming
- Define colors and brushes in resource dictionaries
- Use theme-aware resources for light/dark mode support
- Implement consistent spacing and typography
- Create reusable style templates

## Example Patterns

### Custom Control Template
```csharp
public class CircularCanvas : Canvas
{
    private readonly List<MarkerVisual> _markers = new();
    private double _currentAngle;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        var position = e.GetPosition(this);
        var angle = CalculateAngle(position);
        
        OnMarkerPlaced?.Invoke(new MarkerPlacedEventArgs(angle, position));
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        // Draw disk background
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var radius = Math.Min(Bounds.Width, Bounds.Height) / 2 - 10;
        
        context.DrawEllipse(DiskBrush, DiskPen, center, radius, radius);
        
        // Draw markers
        foreach (var marker in _markers)
        {
            DrawMarker(context, marker, center, radius);
        }
        
        // Draw playhead
        DrawPlayhead(context, center, radius);
    }
}
```

### Reactive ViewModel
```csharp
public class MainViewModel : ReactiveObject, IDisposable
{
    private readonly IMidiService _midiService;
    private readonly CompositeDisposable _disposables = new();

    public MainViewModel(IMidiService midiService)
    {
        _midiService = midiService;
        
        PlayCommand = ReactiveCommand.CreateFromTask(ExecutePlay);
        StopCommand = ReactiveCommand.Create(ExecuteStop);
        
        // React to BPM changes
        this.WhenAnyValue(x => x.BPM)
            .Where(bpm => bpm > 0)
            .Subscribe(bpm => _timingService.SetBPM(bpm))
            .DisposeWith(_disposables);
    }

    public ReactiveCommand<Unit, Unit> PlayCommand { get; }
    public ReactiveCommand<Unit, Unit> StopCommand { get; }

    private async Task ExecutePlay()
    {
        try
        {
            await _midiService.StartAsync();
            IsPlaying = true;
        }
        catch (Exception ex)
        {
            // Handle error
        }
    }

    public void Dispose()
    {
        _disposables?.Dispose();
    }
}
```

### Data Binding in XAML
```xml
<UserControl x:Class="CircularMidiGenerator.Views.MainView">
  <Grid>
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    
    <!-- Controls -->
    <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10">
      <Button Content="Play" Command="{Binding PlayCommand}"/>
      <Button Content="Stop" Command="{Binding StopCommand}"/>
      <NumericUpDown Value="{Binding BPM}" Minimum="60" Maximum="200"/>
    </StackPanel>
    
    <!-- Circular Canvas -->
    <local:CircularCanvas Grid.Row="1" 
                         Markers="{Binding Markers}"
                         PlayheadAngle="{Binding PlayheadAngle}"
                         IsQuantizationEnabled="{Binding IsQuantizationEnabled}"/>
  </Grid>
</UserControl>
```

## Cross-Platform Considerations

### Platform-Specific Code
- Use conditional compilation for platform differences
- Test on all target platforms (Windows, macOS, Linux)
- Handle different input methods (mouse, touch, stylus)
- Consider platform-specific UI conventions

### Resource Management
- Use platform-appropriate file paths
- Handle different screen densities and DPI settings
- Test with various system themes and accessibility settings
- Implement proper resource disposal patterns

### Performance
- Profile on lower-end hardware
- Test with different graphics drivers
- Optimize for both desktop and mobile scenarios
- Monitor memory usage across platforms