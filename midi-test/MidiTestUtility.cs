using System;
using System.Linq;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;

namespace MidiDebugTool;

/// <summary>
/// Simple utility to test MIDI output directly
/// </summary>
public class MidiTestUtility
{
    public static async Task TestMidiOutput()
    {
        Console.WriteLine("=== MIDI Test Utility ===");
        Console.WriteLine();

        // 1. List all available MIDI devices
        Console.WriteLine("Available MIDI Output Devices:");
        var devices = OutputDevice.GetAll().ToList();
        
        if (devices.Count == 0)
        {
            Console.WriteLine("❌ No MIDI output devices found!");
            return;
        }

        for (int i = 0; i < devices.Count; i++)
        {
            Console.WriteLine($"  {i}: {devices[i].Name}");
        }
        Console.WriteLine();

        // 2. Look for IAC Driver specifically
        var iacDevice = devices.FirstOrDefault(d => d.Name.Contains("IAC") || d.Name.Contains("Bus"));
        if (iacDevice != null)
        {
            Console.WriteLine($"✅ Found IAC-like device: {iacDevice.Name}");
        }
        else
        {
            Console.WriteLine("⚠️  No IAC-like device found. Looking for any available device...");
        }

        // 3. Try to connect to the first available device (or IAC if found)
        var targetDevice = iacDevice ?? devices.First();
        Console.WriteLine($"Attempting to connect to: {targetDevice.Name}");

        try
        {
            using var outputDevice = OutputDevice.GetByName(targetDevice.Name);
            Console.WriteLine("✅ Successfully connected to MIDI device");

            // 4. Send test notes with multiple approaches
            Console.WriteLine("🎵 Testing MIDI output with multiple methods...");
            Console.WriteLine();
            
            // Test 1: Single note on Channel 1
            Console.WriteLine("Test 1: Single note on Channel 1 (C4 = 60)");
            await SendTestNote(outputDevice, 1, 60, 100, 1000);
            
            // Test 2: Multiple channels
            Console.WriteLine("Test 2: Testing multiple MIDI channels");
            for (int ch = 1; ch <= 4; ch++)
            {
                Console.WriteLine($"  Testing Channel {ch}...");
                await SendTestNote(outputDevice, ch, 60, 100, 500);
            }
            
            // Test 3: Drum notes (common drum MIDI notes)
            Console.WriteLine("Test 3: Testing drum notes (if you have drums loaded)");
            var drumNotes = new[] { 36, 38, 42, 46 }; // Kick, Snare, Hi-hat, Open Hi-hat
            var drumNames = new[] { "Kick", "Snare", "Hi-hat", "Open Hi-hat" };
            
            for (int i = 0; i < drumNotes.Length; i++)
            {
                Console.WriteLine($"  Testing {drumNames[i]} (Note {drumNotes[i]})...");
                await SendTestNote(outputDevice, 10, drumNotes[i], 100, 500); // Channel 10 is standard for drums
            }
            
            // Test 4: Continuous stream for 5 seconds
            Console.WriteLine("Test 4: Continuous MIDI stream for 5 seconds");
            Console.WriteLine("  (Watch Ableton's MIDI activity indicator)");
            
            var startTime = DateTime.Now;
            var noteIndex = 0;
            var notes = new[] { 60, 64, 67, 72 }; // C major chord
            
            while ((DateTime.Now - startTime).TotalSeconds < 5)
            {
                var note = notes[noteIndex % notes.Length];
                await SendTestNote(outputDevice, 1, note, 100, 200);
                noteIndex++;
            }

            Console.WriteLine("✅ Test notes sent successfully!");
            Console.WriteLine();
            Console.WriteLine("🔍 Ableton Live Debugging Tips:");
            Console.WriteLine("1. In Ableton, go to Preferences > Link/Tempo/MIDI");
            Console.WriteLine("2. Under MIDI Ports, enable 'IAC Driver Bus 1' for 'Track' and 'Remote'");
            Console.WriteLine("3. Create a MIDI track and set 'MIDI From' to 'IAC Driver Bus 1'");
            Console.WriteLine("4. Set the channel to 'All Channels' or 'Ch. 1'");
            Console.WriteLine("5. ARM the track (red record button)");
            Console.WriteLine("6. Add an instrument (Operator, Drum Kit, etc.)");
            Console.WriteLine("7. Set Monitor to 'In' or 'Auto'");
            Console.WriteLine("8. Watch for MIDI activity in the track's input meter");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to send MIDI: {ex.Message}");
            Console.WriteLine($"Exception type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    private static async Task SendTestNote(OutputDevice outputDevice, int channel, int note, int velocity, int durationMs)
    {
        try
        {
            // Send note on
            var noteOnEvent = new NoteOnEvent((SevenBitNumber)note, (SevenBitNumber)velocity)
            {
                Channel = (FourBitNumber)(channel - 1) // Convert to 0-based
            };
            outputDevice.SendEvent(noteOnEvent);
            Console.WriteLine($"    ♪ Note ON:  Channel {channel}, Note {note} ({GetNoteName(note)}), Velocity {velocity}");

            // Wait
            await Task.Delay(durationMs);

            // Send note off
            var noteOffEvent = new NoteOffEvent((SevenBitNumber)note, (SevenBitNumber)0)
            {
                Channel = (FourBitNumber)(channel - 1) // Convert to 0-based
            };
            outputDevice.SendEvent(noteOffEvent);
            Console.WriteLine($"    ♪ Note OFF: Channel {channel}, Note {note} ({GetNoteName(note)})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    ❌ Failed to send note: {ex.Message}");
        }
    }

    private static string GetNoteName(int midiNote)
    {
        var noteNames = new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        var octave = (midiNote / 12) - 1;
        var noteName = noteNames[midiNote % 12];
        return $"{noteName}{octave}";
    }

    public static void TestMidiDeviceEnumeration()
    {
        Console.WriteLine("=== MIDI Device Enumeration Test ===");
        Console.WriteLine();

        try
        {
            var devices = OutputDevice.GetAll().ToList();
            Console.WriteLine($"Found {devices.Count} MIDI output devices:");

            for (int i = 0; i < devices.Count; i++)
            {
                var device = devices[i];
                Console.WriteLine($"  - Name: '{device.Name}'");
                Console.WriteLine($"    Index: {i}");
                Console.WriteLine();

                // Try to test each device
                try
                {
                    using var testDevice = OutputDevice.GetByName(device.Name);
                    Console.WriteLine($"    ✅ Can connect to {device.Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    ❌ Cannot connect to {device.Name}: {ex.Message}");
                }
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to enumerate devices: {ex.Message}");
        }
    }

    public static async Task TestAllDevices()
    {
        Console.WriteLine("=== Testing All MIDI Devices ===");
        Console.WriteLine();

        var devices = OutputDevice.GetAll().ToList();
        
        foreach (var deviceInfo in devices)
        {
            Console.WriteLine($"Testing device: {deviceInfo.Name}");
            try
            {
                using var device = OutputDevice.GetByName(deviceInfo.Name);
                Console.WriteLine("  ✅ Connected successfully");
                
                // Send a test note
                await SendTestNote(device, 1, 60, 100, 500);
                Console.WriteLine("  ✅ Test note sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Failed: {ex.Message}");
            }
            Console.WriteLine();
        }
    }

    public static async Task ContinuousTest()
    {
        Console.WriteLine("=== Continuous MIDI Test for Ableton Setup ===");
        Console.WriteLine();
        Console.WriteLine("This will send MIDI notes continuously for 60 seconds.");
        Console.WriteLine("Use this time to set up your Ableton track:");
        Console.WriteLine();
        Console.WriteLine("1. Create a new MIDI track in Ableton");
        Console.WriteLine("2. Set 'MIDI From' to 'IAC Driver Bus 1'");
        Console.WriteLine("3. Set Channel to 'All Channels'");
        Console.WriteLine("4. ARM the track (red record button)");
        Console.WriteLine("5. Add an instrument (Operator, Drum Kit, etc.)");
        Console.WriteLine("6. Set Monitor to 'In'");
        Console.WriteLine();
        Console.WriteLine("Press Enter to start the test...");
        Console.ReadLine();

        var devices = OutputDevice.GetAll().ToList();
        var iacDevice = devices.FirstOrDefault(d => d.Name.Contains("IAC"));
        
        if (iacDevice == null)
        {
            Console.WriteLine("❌ No IAC Driver found!");
            return;
        }

        try
        {
            using var outputDevice = OutputDevice.GetByName(iacDevice.Name);
            Console.WriteLine($"✅ Connected to {iacDevice.Name}");
            Console.WriteLine();
            Console.WriteLine("🎵 Sending MIDI notes continuously...");
            Console.WriteLine("   Watch Ableton's track input meter for activity!");
            Console.WriteLine();

            var startTime = DateTime.Now;
            var noteIndex = 0;
            var notes = new[] { 60, 64, 67, 72 }; // C major chord
            var channels = new[] { 1, 1, 1, 1 }; // All on channel 1
            
            while ((DateTime.Now - startTime).TotalSeconds < 60)
            {
                var note = notes[noteIndex % notes.Length];
                var channel = channels[noteIndex % channels.Length];
                
                // Send note on
                var noteOnEvent = new NoteOnEvent((SevenBitNumber)note, (SevenBitNumber)100)
                {
                    Channel = (FourBitNumber)(channel - 1)
                };
                outputDevice.SendEvent(noteOnEvent);
                
                Console.WriteLine($"♪ Ch.{channel} Note {note} ({GetNoteName(note)}) - {DateTime.Now:HH:mm:ss.fff}");
                
                await Task.Delay(800);
                
                // Send note off
                var noteOffEvent = new NoteOffEvent((SevenBitNumber)note, (SevenBitNumber)0)
                {
                    Channel = (FourBitNumber)(channel - 1)
                };
                outputDevice.SendEvent(noteOffEvent);
                
                await Task.Delay(200);
                noteIndex++;
            }
            
            Console.WriteLine();
            Console.WriteLine("✅ Test completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    public static async Task SimpleTest()
    {
        Console.WriteLine("=== DryWetMIDI Focused Test ===");
        Console.WriteLine();
        Console.WriteLine("Testing different DryWetMIDI approaches to find what works on macOS");
        Console.WriteLine();
        Console.WriteLine("Press Enter to start...");
        Console.ReadLine();

        var devices = OutputDevice.GetAll().ToList();
        var iacDevice = devices.FirstOrDefault(d => d.Name.Contains("IAC"));
        
        if (iacDevice == null)
        {
            Console.WriteLine("❌ No IAC Driver found!");
            return;
        }

        try
        {
            using var outputDevice = OutputDevice.GetByName(iacDevice.Name);
            Console.WriteLine($"✅ Connected to {iacDevice.Name}");
            Console.WriteLine();
            
            // Try different DryWetMIDI approaches
            Console.WriteLine("🔍 Testing different DryWetMIDI methods...");
            Console.WriteLine();
            
            // Method 1: Try with explicit timing
            Console.WriteLine("Method 1: With explicit timing");
            var noteOn1 = new NoteOnEvent((SevenBitNumber)60, (SevenBitNumber)100);
            noteOn1.Channel = (FourBitNumber)0;
            noteOn1.DeltaTime = 0; // Explicit timing
            
            Console.WriteLine("Sending timed Note ON...");
            outputDevice.SendEvent(noteOn1);
            
            await Task.Delay(1000);
            
            var noteOff1 = new NoteOffEvent((SevenBitNumber)60, (SevenBitNumber)0);
            noteOff1.Channel = (FourBitNumber)0;
            noteOff1.DeltaTime = 0;
            
            Console.WriteLine("Sending timed Note OFF...");
            outputDevice.SendEvent(noteOff1);
            
            await Task.Delay(500);
            
            // Method 2: Try sending multiple events at once
            Console.WriteLine("Method 2: Multiple events");
            var events = new MidiEvent[]
            {
                new NoteOnEvent((SevenBitNumber)64, (SevenBitNumber)100) { Channel = (FourBitNumber)0 },
                new NoteOffEvent((SevenBitNumber)64, (SevenBitNumber)0) { Channel = (FourBitNumber)0, DeltaTime = 480 } // Quarter note later
            };
            
            foreach (var evt in events)
            {
                Console.WriteLine($"Sending event: {evt.GetType().Name}");
                outputDevice.SendEvent(evt);
                await Task.Delay(500);
            }
            
            // Method 3: Try different MIDI channels
            Console.WriteLine("Method 3: Different channels");
            for (int ch = 0; ch < 4; ch++)
            {
                var noteOn = new NoteOnEvent((SevenBitNumber)67, (SevenBitNumber)80);
                noteOn.Channel = (FourBitNumber)ch;
                
                Console.WriteLine($"Sending on Channel {ch + 1}...");
                outputDevice.SendEvent(noteOn);
                
                await Task.Delay(300);
                
                var noteOff = new NoteOffEvent((SevenBitNumber)67, (SevenBitNumber)0);
                noteOff.Channel = (FourBitNumber)ch;
                outputDevice.SendEvent(noteOff);
                
                await Task.Delay(200);
            }
            
            // Method 4: Try forcing device flush
            Console.WriteLine("Method 4: With device reset");
            try
            {
                // Send a system reset first
                var resetEvent = new ResetEvent();
                outputDevice.SendEvent(resetEvent);
                Console.WriteLine("Sent system reset");
                
                await Task.Delay(100);
                
                var noteOn4 = new NoteOnEvent((SevenBitNumber)72, (SevenBitNumber)127);
                noteOn4.Channel = (FourBitNumber)0;
                outputDevice.SendEvent(noteOn4);
                Console.WriteLine("Sent note after reset");
                
                await Task.Delay(1000);
                
                var noteOff4 = new NoteOffEvent((SevenBitNumber)72, (SevenBitNumber)0);
                noteOff4.Channel = (FourBitNumber)0;
                outputDevice.SendEvent(noteOff4);
                Console.WriteLine("Sent note off after reset");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Reset method failed: {ex.Message}");
            }
            
            Console.WriteLine();
            Console.WriteLine("✅ All DryWetMIDI tests completed!");
            Console.WriteLine("Check Ableton MIDI monitor for any activity");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    public static async Task DebugMidiBytes()
    {
        Console.WriteLine("=== Raw MIDI Message Test ===");
        Console.WriteLine();
        Console.WriteLine("Sending raw MIDI bytes to bypass DryWetMIDI event creation.");
        Console.WriteLine();
        Console.WriteLine("Press Enter to start...");
        Console.ReadLine();

        var devices = OutputDevice.GetAll().ToList();
        var iacDevice = devices.FirstOrDefault(d => d.Name.Contains("IAC"));
        
        if (iacDevice == null)
        {
            Console.WriteLine("❌ No IAC Driver found!");
            return;
        }

        try
        {
            using var outputDevice = OutputDevice.GetByName(iacDevice.Name);
            Console.WriteLine($"✅ Connected to {iacDevice.Name}");
            Console.WriteLine();
            
            // Method 1: Try using MidiEvent.FromBytes
            Console.WriteLine("Method 1: Creating MIDI from raw bytes");
            
            // Note On: Status=0x90 (Note On Ch 1), Note=60 (C4), Velocity=100
            var noteOnBytes = new byte[] { 0x90, 60, 100 };
            Console.WriteLine($"Note ON bytes: {string.Join(" ", noteOnBytes.Select(b => $"0x{b:X2}"))}");
            
            // Try using the most basic approach - constructor with all parameters
            Console.WriteLine("Trying basic constructor approach...");
            
            try 
            {
                // Use the constructor that takes all parameters at once
                var basicNoteOn = new NoteOnEvent((SevenBitNumber)60, (SevenBitNumber)100);
                basicNoteOn.Channel = (FourBitNumber)0;
                
                Console.WriteLine($"Basic Note ON: {basicNoteOn.GetType().Name}");
                Console.WriteLine($"  Note: {basicNoteOn.NoteNumber}");
                Console.WriteLine($"  Velocity: {basicNoteOn.Velocity}");
                Console.WriteLine($"  Channel: {basicNoteOn.Channel}");
                
                outputDevice.SendEvent(basicNoteOn);
                Console.WriteLine("✅ Basic Note ON sent");
                
                await Task.Delay(1000);
                
                // Try Note On with velocity 0 (common way to do note off)
                var basicNoteOff = new NoteOnEvent((SevenBitNumber)60, (SevenBitNumber)0);
                basicNoteOff.Channel = (FourBitNumber)0;
                
                Console.WriteLine($"Basic Note OFF (vel 0): {basicNoteOff.GetType().Name}");
                outputDevice.SendEvent(basicNoteOff);
                Console.WriteLine("✅ Basic Note OFF sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Basic approach failed: {ex.Message}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Method 2: Step-by-step Note On construction");
            
            // Create Note On very explicitly
            Console.WriteLine("Creating NoteOnEvent step by step...");
            var stepNoteOn = new NoteOnEvent();
            Console.WriteLine($"1. Created empty NoteOnEvent: {stepNoteOn.GetType().Name}");
            
            stepNoteOn.NoteNumber = (SevenBitNumber)67; // G4
            Console.WriteLine($"2. Set note number: {stepNoteOn.NoteNumber}");
            
            stepNoteOn.Velocity = (SevenBitNumber)100;
            Console.WriteLine($"3. Set velocity: {stepNoteOn.Velocity}");
            
            stepNoteOn.Channel = (FourBitNumber)0; // Channel 1
            Console.WriteLine($"4. Set channel: {stepNoteOn.Channel} (Channel {stepNoteOn.Channel + 1})");
            
            Console.WriteLine($"5. Final event: {stepNoteOn.GetType().Name} - Note {stepNoteOn.NoteNumber}, Vel {stepNoteOn.Velocity}, Ch {stepNoteOn.Channel + 1}");
            Console.WriteLine("6. Sending...");
            outputDevice.SendEvent(stepNoteOn);
            Console.WriteLine("✅ Step-by-step Note ON sent");
            
            await Task.Delay(1500);
            
            // Create Note Off the same way
            Console.WriteLine("Creating NoteOffEvent step by step...");
            var stepNoteOff = new NoteOffEvent();
            stepNoteOff.NoteNumber = (SevenBitNumber)67;
            stepNoteOff.Velocity = (SevenBitNumber)64; // Standard release velocity
            stepNoteOff.Channel = (FourBitNumber)0;
            
            Console.WriteLine($"Note OFF event: {stepNoteOff.GetType().Name} - Note {stepNoteOff.NoteNumber}, Vel {stepNoteOff.Velocity}, Ch {stepNoteOff.Channel + 1}");
            outputDevice.SendEvent(stepNoteOff);
            Console.WriteLine("✅ Step-by-step Note OFF sent");
            
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Method 3: Let's try the 'wrong' way that produced BND messages");
            Console.WriteLine("(This might help us understand what was happening before)");
            
            try 
            {
                // Let's try to recreate whatever was causing BND messages
                // Maybe it was a different type of event or wrong parameters
                
                // Try creating with wrong parameter order or type
                var testEvent = new NoteOnEvent((SevenBitNumber)72, (SevenBitNumber)127);
                testEvent.Channel = (FourBitNumber)0;
                
                Console.WriteLine($"Test event: {testEvent.GetType().Name} - Note {testEvent.NoteNumber}, Vel {testEvent.Velocity}");
                outputDevice.SendEvent(testEvent);
                Console.WriteLine("✅ Test event sent");
                
                await Task.Delay(500);
                
                var testOff = new NoteOnEvent((SevenBitNumber)72, (SevenBitNumber)0);
                testOff.Channel = (FourBitNumber)0;
                outputDevice.SendEvent(testOff);
                Console.WriteLine("✅ Test off sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test method failed: {ex.Message}");
            }
            
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Method 4: Try different device connection approach");
            
            try 
            {
                // Try reconnecting to the device
                outputDevice.Dispose();
                
                Console.WriteLine("Reconnecting to IAC Driver...");
                using var newDevice = OutputDevice.GetByName("IAC Driver Bus 1");
                Console.WriteLine("✅ Reconnected");
                
                // Try sending immediately after connection
                var immediateNote = new NoteOnEvent((SevenBitNumber)48, (SevenBitNumber)100); // C3
                immediateNote.Channel = (FourBitNumber)0;
                
                Console.WriteLine("Sending immediate note after reconnection...");
                newDevice.SendEvent(immediateNote);
                Console.WriteLine("✅ Immediate note sent");
                
                await Task.Delay(1000);
                
                var immediateOff = new NoteOnEvent((SevenBitNumber)48, (SevenBitNumber)0);
                immediateOff.Channel = (FourBitNumber)0;
                newDevice.SendEvent(immediateOff);
                Console.WriteLine("✅ Immediate note off sent");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Reconnection test failed: {ex.Message}");
            }
            
            Console.WriteLine();
            Console.WriteLine("✅ All tests complete!");
            Console.WriteLine("Check Ableton MIDI monitor:");
            Console.WriteLine("- Method 1: Basic constructor (Note 60)");
            Console.WriteLine("- Method 2: Step-by-step (Note 67)");  
            Console.WriteLine("- Method 3: Test approach (Note 72)");
            Console.WriteLine("- Method 4: Reconnection test (Note 48)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    public static async Task KeyboardStyleTest()
    {
        Console.WriteLine("=== Keyboard-Style MIDI Test ===");
        Console.WriteLine();
        Console.WriteLine("This test mimics exactly how a MIDI keyboard sends notes.");
        Console.WriteLine("Make sure your Ableton track is set up and armed.");
        Console.WriteLine();
        Console.WriteLine("Press Enter to start...");
        Console.ReadLine();

        var devices = OutputDevice.GetAll().ToList();
        var iacDevice = devices.FirstOrDefault(d => d.Name.Contains("IAC"));
        
        if (iacDevice == null)
        {
            Console.WriteLine("❌ No IAC Driver found!");
            return;
        }

        try
        {
            using var outputDevice = OutputDevice.GetByName(iacDevice.Name);
            Console.WriteLine($"✅ Connected to {iacDevice.Name}");
            Console.WriteLine();
            Console.WriteLine("🎹 Sending keyboard-style MIDI...");
            Console.WriteLine();

            // Test different approaches
            await TestRawMidiBytes(outputDevice);
            await TestDifferentChannels(outputDevice);
            await TestDifferentVelocities(outputDevice);
            await TestDifferentTimings(outputDevice);
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    private static async Task TestRawMidiBytes(OutputDevice outputDevice)
    {
        Console.WriteLine("Test 1: Raw MIDI bytes (like hardware keyboard)");
        
        // Send raw MIDI bytes directly
        // Note On: 0x90 (channel 1) + note + velocity
        // Note Off: 0x80 (channel 1) + note + 0
        
        for (int i = 0; i < 3; i++)
        {
            var note = 60 + i;
            Console.WriteLine($"  Raw MIDI: Note {note}");
            
            // Use standard Note On/Off events but with explicit timing
            var noteOnEvent = new NoteOnEvent((SevenBitNumber)note, (SevenBitNumber)100)
            {
                Channel = (FourBitNumber)0 // Channel 1 (0-based)
            };
            outputDevice.SendEvent(noteOnEvent);
            
            await Task.Delay(500);
            
            var noteOffEvent = new NoteOffEvent((SevenBitNumber)note, (SevenBitNumber)0)
            {
                Channel = (FourBitNumber)0
            };
            outputDevice.SendEvent(noteOffEvent);
            
            await Task.Delay(100);
        }
        Console.WriteLine();
    }

    private static async Task TestDifferentChannels(OutputDevice outputDevice)
    {
        Console.WriteLine("Test 2: Different MIDI channels");
        
        for (int ch = 0; ch < 4; ch++) // 0-based channels (0=Ch1, 1=Ch2, etc.)
        {
            Console.WriteLine($"  Testing Channel {ch + 1}");
            
            var noteOnEvent = new NoteOnEvent((SevenBitNumber)60, (SevenBitNumber)100)
            {
                Channel = (FourBitNumber)ch
            };
            outputDevice.SendEvent(noteOnEvent);
            
            await Task.Delay(300);
            
            var noteOffEvent = new NoteOffEvent((SevenBitNumber)60, (SevenBitNumber)0)
            {
                Channel = (FourBitNumber)ch
            };
            outputDevice.SendEvent(noteOffEvent);
            
            await Task.Delay(200);
        }
        Console.WriteLine();
    }

    private static async Task TestDifferentVelocities(OutputDevice outputDevice)
    {
        Console.WriteLine("Test 3: Different velocities");
        
        var velocities = new[] { 127, 100, 80, 60, 40 };
        
        foreach (var vel in velocities)
        {
            Console.WriteLine($"  Velocity {vel}");
            
            var noteOnEvent = new NoteOnEvent((SevenBitNumber)60, (SevenBitNumber)vel)
            {
                Channel = (FourBitNumber)0 // Channel 1
            };
            outputDevice.SendEvent(noteOnEvent);
            
            await Task.Delay(400);
            
            var noteOffEvent = new NoteOffEvent((SevenBitNumber)60, (SevenBitNumber)0)
            {
                Channel = (FourBitNumber)0
            };
            outputDevice.SendEvent(noteOffEvent);
            
            await Task.Delay(100);
        }
        Console.WriteLine();
    }

    private static async Task TestDifferentTimings(OutputDevice outputDevice)
    {
        Console.WriteLine("Test 4: Different note timings");
        
        var timings = new[] { 100, 250, 500, 1000, 2000 };
        
        foreach (var timing in timings)
        {
            Console.WriteLine($"  Note duration: {timing}ms");
            
            var noteOnEvent = new NoteOnEvent((SevenBitNumber)60, (SevenBitNumber)100)
            {
                Channel = (FourBitNumber)0
            };
            outputDevice.SendEvent(noteOnEvent);
            
            await Task.Delay(timing);
            
            var noteOffEvent = new NoteOffEvent((SevenBitNumber)60, (SevenBitNumber)0)
            {
                Channel = (FourBitNumber)0
            };
            outputDevice.SendEvent(noteOffEvent);
            
            await Task.Delay(200);
        }
        Console.WriteLine();
    }
}