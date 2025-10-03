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

## macOS + Ableton Live Quick Setup

If you're using macOS with Ableton Live, follow these steps for the fastest setup:

### 1. Create Virtual MIDI Connection
1. **Open Audio MIDI Setup**:
   - Press `Cmd + Space` and type "Audio MIDI Setup"
   - Or go to **Applications > Utilities > Audio MIDI Setup**

2. **Enable IAC Driver**:
   - Go to **Window > Show MIDI Studio** (or press `Cmd + 2`)
   - Double-click the **IAC Driver** icon
   - Check **"Device is online"**
   - You should see **"IAC Bus 1"** listed - this is your virtual MIDI port

### 2. Configure Ableton Live
1. **Open MIDI Preferences**:
   - In Ableton Live, go to **Live > Preferences** (or press `Cmd + ,`)
   - Click on **Link/Tempo/MIDI** tab

2. **Enable IAC Bus**:
   - Find **"IAC Bus 1"** in the MIDI Ports section
   - Set both **Track** and **Remote** to **"On"**

3. **Create MIDI Track**:
   - Create a new MIDI track (`Cmd + Shift + T`)
   - Set the track's **MIDI From** to **"IAC Bus 1"**
   - Load an instrument (like **Wavetable**, **Operator**, or **Drum Rack**)
   - **Arm the track** for recording (click the record button on the track)

### 3. Configure Circular MIDI Generator
1. **Launch the App** and wait for it to fully load
2. **Select MIDI Device**:
   - In the control panel, find the **"MIDI Out:"** dropdown
   - Select **"IAC Bus 1"** from the list
   - If you don't see it, click the **🔄** refresh button

3. **Test the Connection**:
   - Click anywhere on the circular disk to place a marker
   - Press the **Play** button (▶️)
   - You should hear the note play in Ableton when the marker passes 12 o'clock!

### 4. Create Your First Pattern
- Place markers at 12, 3, 6, and 9 o'clock positions for a basic 4/4 pattern
- Adjust BPM to **120** for a comfortable tempo
- Try dragging markers up and down to change velocity (volume)

---

## General Setup (All Platforms)

### 1. MIDI Setup

Before creating music, you need to set up MIDI output:

1. **Connect Your MIDI Device**
   - Hardware synthesizer via USB or MIDI interface
   - Software synthesizer or DAW running on your computer
   - Virtual MIDI cable (loopMIDI on Windows, IAC Driver on macOS)

2. **Select MIDI Output**
   - In Circular MIDI Generator, find the **"MIDI Out:"** dropdown in the control panel
   - Select your desired output device (e.g., "IAC Bus 1" for Ableton Live on macOS)
   - Click the **🔄** refresh button if your device doesn't appear
   - The dropdown will show "No device selected" until you choose one

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
- **Control Panel**: Two-row layout with playback and file controls
  - **Top Row**: Play button, BPM control, MIDI device selection, Quantize checkbox
  - **Bottom Row**: Save, Load, Reset, and Stop All Notes buttons
- **Lane Panel**: Right sidebar for managing different instrument tracks

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
   - Click on Lane 1, 2, 3, or 4 buttons in the right panel
   - Each lane has its own color and MIDI channel

2. **Assign Markers to Lanes**
   - Select a lane before placing markers
   - New markers will be assigned to the currently selected lane
   - Each lane can have independent quantization settings

3. **Lane Organization**
   - Use different lanes for different instruments (drums, bass, melody, etc.)
   - Each lane sends to a different MIDI channel
   - Visual color coding helps organize complex arrangements

## Common First-Time Issues

### No Sound
- **Check MIDI device selection**: Ensure correct output device is selected in the "MIDI Out:" dropdown
- **Verify connections**: Make sure your synthesizer/DAW is receiving MIDI
- **Test with simple software**: Try a basic software synth first
- **macOS**: Ensure IAC Driver is enabled and Ableton track is armed

### macOS-Specific Issues
- **"App can't be opened"**: Go to System Preferences > Security & Privacy, click "Open Anyway"
- **IAC Bus not visible**: Open Audio MIDI Setup, enable IAC Driver, restart both applications
- **No sound in Ableton**: Check that the MIDI track is armed and has an instrument loaded
- **Permission issues**: Grant audio/microphone permissions when prompted

### Timing Issues
- **Enable quantization**: Use grid snap for precise timing
- **Check BPM**: Ensure tempo matches your expectations (try 120 BPM first)
- **Ableton sync**: If using Ableton Live, enable sync mode for tempo matching

### Performance Issues
- **Close other applications**: Free up system resources
- **Adjust buffer settings**: In your audio interface control panel
- **Update drivers**: Ensure MIDI and audio drivers are current
- **macOS**: Close unnecessary background apps, check Activity Monitor for CPU usage

## Next Steps

Once you're comfortable with the basics:

1. **Explore Advanced Features**
   - **Multi-selection**: Hold Ctrl/Cmd and click to select multiple markers
   - **Drag to remove**: Drag markers outside the disk edge to delete them
   - **Velocity control**: Drag markers up/down to adjust volume
   - **Lane switching**: Try different lanes for different instruments

2. **Save and Organize Your Work**
   - Use the **💾 Save** button to preserve your patterns as JSON files
   - Use the **📁 Load** button to restore saved projects
   - Use the **🗑️ Reset** button to clear all markers and start fresh
   - Auto-save protects against crashes and unexpected shutdowns

3. **Advanced DAW Integration**
   - **Ableton Live**: Follow the [setup guide](#macos--ableton-live-quick-setup) above
   - **Other DAWs**: Use virtual MIDI cables (IAC on macOS, loopMIDI on Windows)
   - **Recording**: Capture MIDI output in your DAW for further editing
   - **Multiple tracks**: Use different lanes to control different instruments

4. **Creative Techniques**
   - **Polyrhythms**: Use different quantization settings on different lanes
   - **Velocity patterns**: Create dynamic patterns by varying marker heights
   - **Tempo changes**: Experiment with different BPM settings for different feels
   - **Live performance**: Use the interface for real-time pattern creation

## Getting Help

If you run into issues:

1. **Check the Documentation**
   - Browse the complete [project README](../README.md) for detailed features and setup
   - Review the [macOS + Ableton setup section](#macos--ableton-live-quick-setup) above
   - Check the [Common Issues section](#common-first-time-issues) in this guide

2. **Community Support**
   - Ask questions in [GitHub Discussions](https://github.com/user/circular-midi-generator/discussions)
   - Report bugs in [GitHub Issues](https://github.com/user/circular-midi-generator/issues)

3. **Contact Support**
   - Email: support@circularmidi.com
   - Include your system info and a description of the issue

Welcome to the world of circular music creation! 🎵