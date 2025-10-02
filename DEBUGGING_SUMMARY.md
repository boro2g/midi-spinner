# Debugging Summary - Rotation and Lane Issues

## 🔍 **Current Understanding**

### **How the System Works**
1. **Markers are stored** with a base angle (0-360°)
2. **During rendering**, `CalculateMarkerPosition()` adds `DiskRotation` to the stored angle
3. **Visual result**: `marker.Angle + DiskRotation` determines where marker appears
4. **Lane rings** are drawn inside the rotation transform, so they rotate with the disk

### **The Rotation Problem**
- **User clicks** at a visual position on the rotated disk
- **Click coordinates** are in screen space (unrotated coordinate system)
- **Question**: Should we store the raw angle or compensate for rotation?

### **Current Approach (Simplified)**
```csharp
private void CreateNewMarker(Point position)
{
    var angle = CalculateAngle(position);
    // Store raw angle - no rotation compensation
    // Marker will appear at clicked position when DiskRotation = 0
}
```

## 🎯 **Lane Detection Logic**

### **Current Implementation**
```csharp
private int DetermineLaneFromDistance(double distanceFromCenter)
{
    var laneSpacing = 25;
    var startRadius = _radius - 40;
    
    // Lane 0 (Drums): radius = startRadius - 0 = _radius - 40
    // Lane 1 (Bass):  radius = startRadius - 25 = _radius - 65  
    // Lane 2 (Lead):  radius = startRadius - 50 = _radius - 90
    // Lane 3 (Pad):   radius = startRadius - 75 = _radius - 115
}
```

### **Expected Lane Radii** (assuming _radius = 300)
- **Drums (Lane 0)**: 260px from center
- **Bass (Lane 1)**: 235px from center  
- **Lead (Lane 2)**: 210px from center
- **Pad (Lane 3)**: 185px from center

## 🚨 **Potential Issues**

### **Rotation Issue Theories**
1. **Theory A**: Need to compensate for rotation during marker creation
2. **Theory B**: Raw angle storage is correct, issue is elsewhere
3. **Theory C**: Problem is in the rendering/positioning logic

### **Lane Selection Issue Theories**
1. **Lane radii calculations** might be wrong
2. **Distance tolerance** might be too strict
3. **Lane ordering** might be incorrect (index vs ID confusion)
4. **Coordinate system** mismatch between click and lane positions

## 🔧 **Next Steps to Debug**

### **Test Rotation Without Disk Movement**
1. Place marker at 12 o'clock position (DiskRotation = 0)
2. Manually set DiskRotation = 90° 
3. Check if marker appears at 3 o'clock position
4. This tests if the rendering logic is correct

### **Test Lane Detection**
1. Calculate actual canvas dimensions and expected lane radii
2. Click at known distances from center
3. Verify which lane gets selected
4. Check if lane IDs match expectations

### **Simplify and Isolate**
1. Remove all rotation compensation temporarily
2. Test lane selection with DiskRotation = 0
3. Once lanes work, then tackle rotation separately

## 🎵 **Expected Behavior**

### **Correct Rotation Behavior**
- User clicks at visual 12 o'clock on rotated disk
- Marker should appear at that visual position
- When disk rotates back to 0°, marker should stay in same relative position

### **Correct Lane Behavior**  
- Click near outer ring → Drums (Lane 0)
- Click near second ring → Bass (Lane 1)
- Click near third ring → Lead (Lane 2)  
- Click near inner ring → Pad (Lane 3)

The key is to test these behaviors independently to isolate the root cause.