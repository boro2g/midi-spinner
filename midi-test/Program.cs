using System;
using System.Threading.Tasks;

namespace MidiDebugTool;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Circular MIDI Generator - MIDI Debug Tool");
        Console.WriteLine("========================================");
        Console.WriteLine();

        if (args.Length > 0)
        {
            switch (args[0])
            {
                case "--enumerate":
                    MidiTestUtility.TestMidiDeviceEnumeration();
                    break;
                case "--simple":
                    await MidiTestUtility.SimpleTest();
                    break;
                case "--continuous":
                    await MidiTestUtility.ContinuousTest();
                    break;
                default:
                    await MidiTestUtility.TestMidiOutput();
                    break;
            }
        }
        else
        {
            Console.WriteLine("Available options:");
            Console.WriteLine("  (no args)      - Run comprehensive DryWetMIDI test");
            Console.WriteLine("  --enumerate    - List all MIDI devices");
            Console.WriteLine("  --simple       - Simple MIDI test");
            Console.WriteLine("  --continuous   - Continuous test for Ableton setup");
            Console.WriteLine();
            Console.WriteLine("Running DryWetMIDI focused test...");
            Console.WriteLine();
            await MidiTestUtility.SimpleTest();
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}