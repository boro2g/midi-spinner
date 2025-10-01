using System;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// Event arguments for tempo change events
/// </summary>
public class TempoChangedEventArgs : EventArgs
{
    public double Tempo { get; }
    public DateTime Timestamp { get; }

    public TempoChangedEventArgs(double tempo)
    {
        Tempo = tempo;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Event arguments for sync position events
/// </summary>
public class SyncPositionEventArgs : EventArgs
{
    public double Position { get; }
    public DateTime Timestamp { get; }

    public SyncPositionEventArgs(double position)
    {
        Position = position;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Service for synchronizing with Ableton Live
/// </summary>
public interface IAbletonSyncService : IDisposable
{
    /// <summary>
    /// Event fired when tempo changes in Ableton Live
    /// </summary>
    event EventHandler<TempoChangedEventArgs>? TempoChanged;
    
    /// <summary>
    /// Event fired when sync position updates
    /// </summary>
    event EventHandler<SyncPositionEventArgs>? PositionChanged;
    
    /// <summary>
    /// Event fired when sync connection is lost
    /// </summary>
    event EventHandler? SyncLost;
    
    /// <summary>
    /// Whether currently connected to Ableton Live
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Current tempo from Ableton Live (or fallback value)
    /// </summary>
    double CurrentTempo { get; }
    
    /// <summary>
    /// Current playback position
    /// </summary>
    double CurrentPosition { get; }
    
    /// <summary>
    /// Attempt to connect to Ableton Live
    /// </summary>
    /// <returns>True if connection successful</returns>
    bool Connect();
    
    /// <summary>
    /// Disconnect from Ableton Live
    /// </summary>
    void Disconnect();
    
    /// <summary>
    /// Set fallback tempo when sync is unavailable
    /// </summary>
    /// <param name="tempo">Fallback tempo in BPM</param>
    void SetFallbackTempo(double tempo);
}