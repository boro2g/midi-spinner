# Circular MIDI Generator

A real-time software MIDI generator built with C# and Avalonia UI that reimagines the traditional piano roll interface. Instead of a linear timeline, users interact with a spinning circular disk where they can place colored markers that trigger MIDI notes when they pass the 12 o'clock position.

## Features

- **Circular Interface**: Intuitive spinning disk interface for pattern creation
- **Multi-Lane Support**: Multiple lanes for different instruments and MIDI channels
- **Real-Time Synchronization**: Sync with Ableton Live or use custom BPM settings
- **Quantization Grid**: Visual grid overlay with multiple note divisions (1/4, 1/8, 1/16, 1/32)
- **Color-Coded Pitches**: Chromatic color wheel mapping for intuitive pitch selection
- **Cross-Platform**: Runs on Windows, macOS, and Linux

## Quick Start

### Prerequisites

- .NET 8.0 SDK or later
- A MIDI-capable device or software synthesizer
- Optional: Ableton Live for tempo synchronization

### Installation

```bash
# Clone the repository
git clone https://github.com/user/circular-midi-generator.git
cd circular-midi-generator

# Restore dependencies
dotnet restore

# Build the project
dotnet build
```

### Running the Application

```bash
# Run the application
dotnet run --project src/CircularMidiGenerator

# Run tests (when available)
dotnet test
```

### First Time Setup

1. Install .NET 8.0 SDK from [dotnet.microsoft.com](https://dotnet.microsoft.com)
2. Clone this repository
3. Run `dotnet restore` to install dependencies
4. Run `dotnet build` to compile the project
5. Run `dotnet run --project src/CircularMidiGenerator` to start the application

## Project Structure

```
├── src/
│   ├── CircularMidiGenerator/          # Main Avalonia UI application
│   └── CircularMidiGenerator.Core/     # Core business logic and models
├── docs/                               # Documentation
├── .kiro/                             # Kiro configuration and specs
│   └── specs/circular-midi-generator/  # Feature specifications
├── README.md                          # This file
└── CircularMidiGenerator.sln          # Visual Studio solution
```

## Development Status

This project is currently under development using a spec-driven approach. The current implementation includes:

- ✅ Project structure and core interfaces
- ⏳ Domain models (Marker, Lane, ProjectConfiguration)
- ⏳ Service interfaces (MIDI, Timing, Quantization)
- ⏳ Basic MVVM infrastructure with ReactiveUI
- ⏳ Dependency injection setup

## Architecture

The application follows MVVM (Model-View-ViewModel) architecture with:

- **Models**: Core domain objects (Marker, Lane, ProjectConfiguration)
- **Services**: Business logic interfaces (IMidiService, ITimingService, IQuantizationService)
- **ViewModels**: Reactive UI state management using ReactiveUI
- **Views**: Avalonia UI controls and windows

## Technology Stack

- **UI Framework**: Avalonia UI 11.0 (cross-platform)
- **MIDI Library**: DryWetMIDI 7.1 (professional MIDI handling)
- **Reactive Programming**: ReactiveUI 19.5 (MVVM and reactive extensions)
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Logging**: Microsoft.Extensions.Logging
- **Serialization**: System.Text.Json

## Contributing

This project uses Kiro specs for feature development. See the `.kiro/specs/circular-midi-generator/` directory for detailed requirements, design, and implementation tasks.

To contribute:
1. Review the current spec documents
2. Pick up tasks from `tasks.md`
3. Follow the established coding standards
4. Submit pull requests with clear descriptions

## License

[License information to be added]

## Contact

[Contact information to be added]