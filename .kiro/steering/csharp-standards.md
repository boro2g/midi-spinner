---
inclusion: always
---

# C# Development Standards

## Code Style and Conventions

### Naming Conventions
- Use PascalCase for classes, methods, properties, and public fields
- Use camelCase for private fields, local variables, and parameters
- Use UPPER_CASE for constants
- Prefix interfaces with 'I' (e.g., `IMidiService`)
- Use descriptive names that clearly indicate purpose

### File Organization
- One class per file, with filename matching the class name
- Group related classes in appropriate namespaces
- Use folder structure that mirrors namespace hierarchy
- Keep using statements at the top, organized alphabetically

### Method and Class Design
- Follow Single Responsibility Principle - each class/method should have one clear purpose
- Keep methods small and focused (ideally under 20 lines)
- Use dependency injection for service dependencies
- Prefer composition over inheritance
- Make classes immutable when possible

### Error Handling
- Use specific exception types rather than generic Exception
- Always include meaningful error messages
- Use try-catch blocks judiciously - don't catch exceptions you can't handle
- Log errors appropriately with context information
- Use async/await pattern for I/O operations

### Performance Considerations
- Use `StringBuilder` for string concatenation in loops
- Prefer `List<T>` over `ArrayList` for type safety and performance
- Use `using` statements for disposable resources
- Consider memory allocation patterns in real-time audio code
- Use object pooling for frequently created/destroyed objects

### Async Programming
- Always use `ConfigureAwait(false)` in library code
- Don't mix async and sync code (avoid `.Result` or `.Wait()`)
- Use `CancellationToken` for long-running operations
- Prefer `Task.Run` for CPU-bound work, not I/O-bound work

## Real-Time Audio Specific Guidelines

### Timing Precision
- Use high-resolution timers (`Stopwatch`, `QueryPerformanceCounter`)
- Avoid garbage collection in audio threads
- Pre-allocate collections and reuse objects
- Use lock-free data structures where possible

### MIDI Processing
- Handle MIDI messages on dedicated threads
- Use proper timing for note on/off pairs
- Implement proper cleanup for stuck notes
- Buffer MIDI events to prevent timing jitter

### Memory Management
- Minimize allocations in hot paths
- Use structs for small, immutable data
- Implement proper disposal patterns
- Monitor memory usage in real-time scenarios

## Example Code Patterns

### Service Implementation
```csharp
public class MidiService : IMidiService, IDisposable
{
    private readonly ILogger<MidiService> _logger;
    private OutputDevice _outputDevice;
    private bool _disposed;

    public MidiService(ILogger<MidiService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Implementation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MIDI service");
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _outputDevice?.Dispose();
            _disposed = true;
        }
    }
}
```

### Reactive Property Pattern
```csharp
public class MainViewModel : ReactiveObject
{
    private double _bpm = 120.0;
    private bool _isPlaying;

    public double BPM
    {
        get => _bpm;
        set => this.RaiseAndSetIfChanged(ref _bpm, value);
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set => this.RaiseAndSetIfChanged(ref _isPlaying, value);
    }
}
```

### Domain Model with Validation
```csharp
public class Marker
{
    private double _angle;
    private int _velocity;

    public double Angle
    {
        get => _angle;
        set
        {
            if (value < 0 || value >= 360)
                throw new ArgumentOutOfRangeException(nameof(value), "Angle must be between 0 and 360 degrees");
            _angle = value;
        }
    }

    public int Velocity
    {
        get => _velocity;
        set
        {
            if (value < 1 || value > 127)
                throw new ArgumentOutOfRangeException(nameof(value), "Velocity must be between 1 and 127");
            _velocity = value;
        }
    }
}
```