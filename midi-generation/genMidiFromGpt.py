from struct import pack

def write_varlen(value):
    """Encode a MIDI variable-length quantity."""
    bytes_out = []
    buffer = value & 0x7F
    while value >> 7:
        value >>= 7
        buffer <<= 8
        buffer |= ((value & 0x7F) | 0x80)
    out = []
    while True:
        out.append(buffer & 0xFF)
        if buffer & 0x80:
            buffer >>= 8
        else:
            break
    return bytes(out[::-1])

def create_midi(filename, notes, tempo=155):
    microseconds_per_beat = int(60000000 / tempo)
    track_data = bytearray()

    # Set tempo meta event
    track_data += b'\x00\xFF\x51\x03' + pack('>L', microseconds_per_beat)[1:]

    last_time = 0
    for start, pitch, dur, vel in notes:
        delta_start = int((start - last_time) * 480)
        delta_dur = int(dur * 480)
        # Note on
        track_data += write_varlen(delta_start) + b'\x90' + bytes([pitch, vel])
        # Note off
        track_data += write_varlen(delta_dur) + b'\x80' + bytes([pitch, 0])
        last_time = start + dur

    # End of track
    track_data += b'\x00\xFF\x2F\x00'

    # Build file
    header = b'MThd' + pack('>LHHH', 6, 1, 1, 480)
    track = b'MTrk' + pack('>L', len(track_data)) + track_data
    with open(filename, 'wb') as f:
        f.write(header + track)
    print(f"MIDI file written: {filename}")

# Complex rolling jump-up 808 bassline (F minor, 155 BPM)
notes = [
    (0.0, 41, 0.75, 100),  # F1
    (0.75, 46, 0.25, 95),  # Bb1
    (1.0, 43, 0.5, 100),   # G1
    (1.5, 41, 0.25, 100),  # F1
    (1.75, 48, 0.25, 100), # C2
    (2.0, 45, 0.5, 100),   # A1
    (2.5, 41, 0.5, 100),   # F1
    (3.0, 43, 0.5, 100),   # G1
    (3.5, 46, 0.25, 100),  # Bb1
    (3.75, 41, 0.25, 100), # F1
    (4.0, 48, 0.5, 100),   # C2
    (4.5, 45, 0.25, 100),  # A1
    (4.75, 43, 0.25, 100), # G1
    (5.0, 41, 1.0, 100),   # F1
    (6.0, 46, 0.5, 100),   # Bb1
    (6.5, 48, 0.5, 100),   # C2
    (7.0, 41, 1.0, 100)    # F1
]
print ("Running script")
create_midi("jumpup_complex_808_bass_155bpm.mid", notes, tempo=155)
