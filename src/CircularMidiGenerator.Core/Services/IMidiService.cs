using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CircularMidiGenerator.Core.Services;

/// <summary>
/// MIDI device information
/// </summary>
public record MidiDevice(string Name, int Id);

/// <summary>
/// Service for MIDI output and device management
/// </summary>
public interface IMidiService : IDisposable
{
    /// <summary>
    /// Initialize MIDI service and enumerate devices
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// Send MIDI note on message
    /// </summary>
    /// <param name="channel">MIDI channel (1-16)</param>
    /// <param name="note">MIDI note number (0-127)</param>
    /// <param name="velocity">Note velocity (1-127)</param>
    void SendNoteOn(int channel, int note, int velocity);
    
    /// <summary>
    /// Send MIDI note off message
    /// </summary>
    /// <param name="channel">MIDI channel (1-16)</param>
    /// <param name="note">MIDI note number (0-127)</param>
    void SendNoteOff(int channel, int note);
    
    /// <summary>
    /// Get list of available MIDI output devices
    /// </summary>
    Task<List<MidiDevice>> GetAvailableDevicesAsync();
    
    /// <summary>
    /// Set the active MIDI output device
    /// </summary>
    /// <param name="device">Device to use for output</param>
    void SetOutputDevice(MidiDevice device);
    
    /// <summary>
    /// Attempt to connect to Ableton Live for synchronization
    /// </summary>
    /// <returns>True if connection successful</returns>
    bool ConnectToAbleton();
    
    /// <summary>
    /// Disconnect from Ableton Live
    /// </summary>
    void DisconnectFromAbleton();
    
    /// <summary>
    /// Whether currently connected to Ableton Live
    /// </summary>
    bool IsConnectedToAbleton { get; }
}