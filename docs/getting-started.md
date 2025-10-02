# Getting Started with Circular MIDI Generator

This guide will help you install and start using Circular MIDI Generator in just a few minutes.

## Installation

### Windows

1. **Download the Installer**
   - Go to [Releases](https://github.com/user/circular-midi-generator/releases)
   - Download `CircularMidiGenerator-Setup.exe`

2. **Run the Installer**
   - Double-click the downloaded file
   - Follow the installation wizard
   - Choose installation directory (default recommended)
   - Create desktop shortcut when prompted

3. **First Launch**
   - Launch from Start Menu or desktop shortcut
   - Windows may show a security warning - click "More info" then "Run anyway"
   - The application will initialize and show the main interface

### macOS

1. **Download the DMG**
   - Go to [Releases](https://github.com/user/circular-midi-generator/releases)
   - Download `CircularMidiGenerator.dmg`

2. **Install the Application**
   - Open the downloaded DMG file
   - Drag "Circular MIDI Generator" to the Applications folder
   - Eject the DMG when complete

3. **First Launch**
   - Open Applications folder and double-click the app
   - macOS may show "App can't be opened" - go to System Preferences > Security & Privacy and click "Open Anyway"
   - Grant microphone/audio permissions if prompted

### Linux

1. **Download AppImage**
   - Go to [Releases](https://github.com/user/circular-midi-generator/releases)
   - Download `CircularMidiGenerator.AppImage`

2. **Make Executable and Run**
   ```bash
   chmod +x CircularMidiGenerator.AppImage
   ./CircularMidiGenerator.AppImage
   ```

3. **Optional: Install to System**
   ```bash
   # Move to applications directory
   sudo mv CircularMidiGenerator.AppImage /opt/
   
   # Create desktop entry
   cat > ~/.local/share/applications/circular-midi-generator.desktop << EOF
   [Desktop Entry]
   Name=Circular MIDI Generator
   Exec=/opt/CircularMidiGenerator.AppImage
   Icon=circular-midi-generator
   Type=Application
   Categories=Audio;Music;
   EOF
   ```

## First Steps

### 1. MIDI Setup

Before creating music, you need to set up MIDI output:

1. **Connect Your MIDI Device**
   - Hardware synthesizer via USB or MIDI interface
   - Software synthesizer or DAW running on your computer
   - Virtual MIDI cable (loopMIDI on Windows, IAC Driver on macOS)

2. **Select MIDI Output**
   - In Circular MIDI Generator, look for the MIDI device dropdown
   - Select your desired output device
   - You should see a green connection indicator when successful

3. **Test MIDI Output**
   - Click anywhere on the circular disk to place a marker
   - Press the Play button (▶️)
   - You should hear a note when the marker passes the top (12 o'clock)

### 2. Creating Your First Pattern

Let's create a simple drum pattern:

1. **Place Kick Drum Markers**
   - Click at the 12 o'clock position (top) for beat 1
   - Click at the 6 o'clock position (bottom) for beat 3
   - These red markers will trigger low notes (kick drum sounds)

2. **Add Snare Hits**
   - Click at 3 o'clock and 9 o'clock positions
   - These should appear as different colored markers
   - They'll trigger higher pitched notes (snare sounds)

3. **Start Playback**
   - Press the Play button
   - Watch the playhead rotate and trigger your markers
   - Adjust BPM using the tempo control (try 120 BPM)

4. **Adjust Velocities**
   - Drag markers up and down to change their velocity (volume)
   - Higher positions = louder notes
   - Lower positions = quieter notes

### 3. Understanding the Interface

#### Main Elements
- **Circular Disk**: The main workspace where you place markers
- **Playhead**: Red line at 12 o'clock that triggers notes
- **Markers**: Colored dots that represent MIDI notes
- **Lane Controls**: Buttons on the side for different instrument tracks
- **Transport Controls**: Play, stop, and tempo controls

#### Color Coding
Markers are automatically colored based on their pitch:
- **Red**: C (low notes, good for kick drums)
- **Orange**: C# / Db
- **Yellow**: D
- **Green**: D# / Eb
- **Blue**: E
- **Purple**: F
- And so on through the chromatic scale...

#### Timing
- **Position on circle**: Determines when the note plays
- **12 o'clock**: Beat 1 (downbeat)
- **3 o'clock**: Beat 2
- **6 o'clock**: Beat 3
- **9 o'clock**: Beat 4

### 4. Using Quantization

Quantization helps you place markers precisely on musical beats:

1. **Enable Quantization**
   - Click the "Grid" button to enable quantization
   - You'll see grid lines appear on the disk

2. **Choose Grid Division**
   - Select from 1/4, 1/8, 1/16, or 1/32 notes
   - Finer divisions allow more precise timing

3. **Snap to Grid**
   - When quantization is on, markers automatically snap to grid lines
   - This ensures perfect timing alignment

### 5. Working with Multiple Lanes

Lanes let you create complex arrangements with different instruments:

1. **Select a Lane**
   - Click on Lane 1, 2, 3, or 4 buttons
   - Each lane has its own color and MIDI channel

2. **Assign Markers to Lanes**
   - Select a lane before placing markers
   - Existing markers can be moved between lanes

3. **Mute and Solo**
   - Use M (mute) and S (solo) buttons to control playback
   - Solo a lane to hear it alone
   - Mute lanes to temporarily silence them

## Common First-Time Issues

### No Sound
- **Check MIDI device selection**: Ensure correct output device is selected
- **Verify connections**: Make sure your synthesizer/DAW is receiving MIDI
- **Test with simple software**: Try a basic software synth first

### Timing Issues
- **Enable quantization**: Use grid snap for precise timing
- **Check BPM**: Ensure tempo matches your expectations
- **Ableton sync**: If using Ableton Live, enable sync mode

### Performance Issues
- **Close other applications**: Free up system resources
- **Adjust buffer settings**: In your audio interface control panel
- **Update drivers**: Ensure MIDI and audio drivers are current

## Next Steps

Once you're comfortable with the basics:

1. **Explore Advanced Features**
   - Multi-touch gestures for selecting multiple markers
   - Pinch-to-zoom for detailed editing
   - Drag markers off the disk to delete them

2. **Save Your Work**
   - Use File > Save to preserve your patterns
   - Projects are saved as JSON files
   - Auto-save protects against crashes

3. **Integrate with Your DAW**
   - Set up Ableton Live sync for tempo matching
   - Use virtual MIDI cables for other DAWs
   - Record MIDI output into your sequencer

4. **Learn Advanced Techniques**
   - Read the [Advanced Features Guide](advanced-features.md)
   - Check out [MIDI Setup](midi-setup.md) for detailed configuration
   - Browse [Troubleshooting](troubleshooting.md) for solutions to common issues

## Getting Help

If you run into issues:

1. **Check the Documentation**
   - Browse the complete [documentation](README.md)
   - Look for your specific issue in [Troubleshooting](troubleshooting.md)

2. **Community Support**
   - Ask questions in [GitHub Discussions](https://github.com/user/circular-midi-generator/discussions)
   - Report bugs in [GitHub Issues](https://github.com/user/circular-midi-generator/issues)

3. **Contact Support**
   - Email: support@circularmidi.com
   - Include your system info and a description of the issue

Welcome to the world of circular music creation! 🎵