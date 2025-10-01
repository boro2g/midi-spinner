# Requirements Document

## Introduction

The Circular MIDI Generator is a real-time software MIDI generator built with C# and Avalonia UI that reimagines the traditional piano roll interface. Instead of a linear timeline, users interact with a spinning circular disk where they can place colored markers. When markers pass the 12 o'clock position, they trigger MIDI notes with pitch determined by marker color. The application features multi-lane support for different instruments, tempo synchronization with external DAWs like Ableton Live, quantization options, and a vibrant, playful aesthetic designed to make music creation fun and intuitive.

## Requirements

### Requirement 1

**User Story:** As a music producer, I want to place colored markers on a spinning disk interface, so that I can create rhythmic MIDI patterns in an intuitive visual way.

#### Acceptance Criteria

1. WHEN the user clicks anywhere on the circular disk THEN the system SHALL place a marker at that position
2. WHEN a marker is placed THEN the system SHALL assign it a color that determines the MIDI note pitch
3. WHEN the disk rotates and a marker passes 12 o'clock THEN the system SHALL trigger the corresponding MIDI note
4. WHEN markers are placed THEN they SHALL rotate with the background disk continuously
5. IF the user drags a marker off the disk THEN the system SHALL remove that marker

### Requirement 2

**User Story:** As a musician, I want the application to sync with my DAW's tempo or use custom BPM settings, so that my patterns stay in time with my music production workflow.

#### Acceptance Criteria

1. WHEN the user enables Ableton Live sync THEN the system SHALL synchronize the disk rotation speed with Ableton's tempo
2. WHEN Ableton Live sync is disabled THEN the system SHALL allow manual BPM input
3. WHEN the BPM is changed THEN the disk rotation speed SHALL update immediately to match
4. WHEN connected to Ableton Live THEN the system SHALL maintain tempo sync during playback and tempo changes

### Requirement 3

**User Story:** As a user creating precise rhythmic patterns, I want quantization options with visual grid overlay, so that I can snap markers to specific note divisions for tight timing.

#### Acceptance Criteria

1. WHEN quantization is enabled THEN the system SHALL provide options for 1/16, 1/8, 1/4, and other note divisions
2. WHEN quantization mode is active THEN the system SHALL display a visual grid overlay showing the divisions on the circle
3. WHEN the disk rotates THEN the quantization grid SHALL spin in sync with the playhead
4. WHEN quantization is enabled and user places a marker THEN the marker SHALL snap to the nearest grid line
5. WHEN markers are quantized THEN they SHALL visually lock onto the spinning grid lines
6. WHEN quantization is disabled THEN markers SHALL be placed at exact click positions without snapping

### Requirement 4

**User Story:** As a performer, I want visual feedback showing playback position and active markers, so that I can see exactly when notes are being triggered.

#### Acceptance Criteria

1. WHEN the disk is rotating THEN the system SHALL display a clear playhead line at 12 o'clock position
2. WHEN a marker passes the playhead THEN the system SHALL visually highlight that marker
3. WHEN a marker is triggered THEN the highlight SHALL be clearly visible and distinct from inactive markers
4. WHEN multiple markers trigger simultaneously THEN all active markers SHALL be highlighted

### Requirement 5

**User Story:** As a music creator, I want to control the velocity of each marker individually, so that I can create dynamic and expressive MIDI patterns.

#### Acceptance Criteria

1. WHEN a marker is placed THEN the system SHALL assign it a default velocity value
2. WHEN the user interacts with a marker's velocity control THEN the system SHALL allow adjustment from 1 to 127
3. WHEN a marker is triggered THEN the system SHALL send the MIDI note with the marker's assigned velocity
4. WHEN the user drags a marker after placement THEN the system SHALL provide a drag-and-drop interface to adjust velocity
5. WHEN velocity is changed THEN the marker SHALL provide visual feedback indicating the new velocity level

### Requirement 6

**User Story:** As a musician working with multiple instruments, I want multiple lanes with independent settings, so that I can create complex multi-channel MIDI arrangements.

#### Acceptance Criteria

1. WHEN the application starts THEN the system SHALL support at least 4 different lanes
2. WHEN a lane is selected THEN markers placed SHALL belong to that specific lane and MIDI channel
3. WHEN markers are triggered THEN the system SHALL send MIDI output on the appropriate channel for each lane
4. WHEN quantization settings are changed THEN each lane SHALL maintain its own independent quantization settings
5. WHEN lanes are displayed THEN the system SHALL provide visual grouping and highlighting to distinguish markers by lane

### Requirement 7

**User Story:** As a live performer, I want lane mute and solo controls, so that I can dynamically control which instruments are active during performance.

#### Acceptance Criteria

1. WHEN a lane is muted THEN markers in that lane SHALL not trigger MIDI notes when passing the playhead
2. WHEN a lane is soloed THEN only that lane SHALL trigger MIDI notes while other lanes are silenced
3. WHEN multiple lanes are soloed THEN all soloed lanes SHALL remain active
4. WHEN all solo states are cleared THEN the system SHALL return to normal mute/unmute behavior
5. WHEN mute/solo states change THEN the system SHALL provide immediate visual feedback

### Requirement 8

**User Story:** As a user, I want to save and load my marker configurations, so that I can preserve my work and share patterns with others.

#### Acceptance Criteria

1. WHEN the user saves a configuration THEN the system SHALL export all markers with their angles, velocity, lane, and color to a JSON file
2. WHEN the user loads a configuration THEN the system SHALL restore all markers to their exact saved positions and properties
3. WHEN saving THEN the system SHALL include lane settings, quantization preferences, and BPM information
4. WHEN loading THEN the system SHALL validate the JSON format and handle corrupted files gracefully
5. WHEN a configuration is loaded THEN the current pattern SHALL be replaced with the loaded pattern

### Requirement 9

**User Story:** As a user, I want multi-touch and multi-marker interaction capabilities, so that I can efficiently manipulate multiple elements simultaneously.

#### Acceptance Criteria

1. WHEN the user performs multi-touch gestures THEN the system SHALL support simultaneous interaction with multiple markers
2. WHEN multiple markers are selected THEN the user SHALL be able to drag them together
3. WHEN multi-marker dragging occurs THEN all selected markers SHALL maintain their relative positions
4. WHEN multi-touch input is available THEN the system SHALL utilize it for enhanced interaction

### Requirement 10

**User Story:** As a user, I want the application to have a vibrant, playful aesthetic, so that music creation feels fun and engaging like playing with a child's toy.

#### Acceptance Criteria

1. WHEN the application launches THEN the system SHALL display a bright, vibrant color palette
2. WHEN markers are displayed THEN they SHALL use fun, saturated colors that are visually appealing
3. WHEN UI elements are rendered THEN they SHALL maintain a playful, toy-like aesthetic throughout
4. WHEN animations occur THEN they SHALL be smooth and delightful to enhance the playful experience
5. WHEN colors are assigned to pitches THEN the system SHALL use an intuitive and visually pleasing color-to-pitch mapping

### Requirement 11

**User Story:** As a music producer, I want reliable MIDI output using DryWetMIDI library, so that the application integrates seamlessly with professional music software.

#### Acceptance Criteria

1. WHEN the application starts THEN the system SHALL initialize MIDI output using the DryWetMIDI library
2. WHEN markers trigger THEN the system SHALL send properly formatted MIDI note messages
3. WHEN connected to Ableton Live THEN the system SHALL maintain stable MIDI communication
4. WHEN MIDI devices are available THEN the system SHALL allow selection of output devices
5. WHEN MIDI errors occur THEN the system SHALL handle them gracefully without crashing