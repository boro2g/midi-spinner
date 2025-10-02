using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Controls;

/// <summary>
/// Custom circular canvas control for the MIDI generator interface
/// Handles marker placement, rendering, and user interactions on a spinning disk
/// </summary>
public class CircularCanvas : Control
{
    #region Styled Properties

    /// <summary>
    /// Collection of markers to display on the canvas
    /// </summary>
    public static readonly StyledProperty<ObservableCollection<Marker>?> MarkersProperty =
        AvaloniaProperty.Register<CircularCanvas, ObservableCollection<Marker>?>(nameof(Markers));

    /// <summary>
    /// Collection of lanes for visual grouping and color coding
    /// </summary>
    public static readonly StyledProperty<ObservableCollection<Lane>?> LanesProperty =
        AvaloniaProperty.Register<CircularCanvas, ObservableCollection<Lane>?>(nameof(Lanes));

    /// <summary>
    /// Currently selected lane ID for new marker placement
    /// </summary>
    public static readonly StyledProperty<int> SelectedLaneIdProperty =
        AvaloniaProperty.Register<CircularCanvas, int>(nameof(SelectedLaneId), 0);

    /// <summary>
    /// Current playhead angle in degrees (0-360)
    /// </summary>
    public static readonly StyledProperty<double> PlayheadAngleProperty =
        AvaloniaProperty.Register<CircularCanvas, double>(nameof(PlayheadAngle), 0.0);

    /// <summary>
    /// Whether quantization grid should be visible
    /// </summary>
    public static readonly StyledProperty<bool> IsQuantizationEnabledProperty =
        AvaloniaProperty.Register<CircularCanvas, bool>(nameof(IsQuantizationEnabled), false);

    /// <summary>
    /// Quantization grid lines (angles in degrees)
    /// </summary>
    public static readonly StyledProperty<IList<double>?> GridLinesProperty =
        AvaloniaProperty.Register<CircularCanvas, IList<double>?>(nameof(GridLines));

    /// <summary>
    /// Disk rotation angle in degrees
    /// </summary>
    public static readonly StyledProperty<double> DiskRotationProperty =
        AvaloniaProperty.Register<CircularCanvas, double>(nameof(DiskRotation), 0.0);

    /// <summary>
    /// Currently selected marker for manipulation
    /// </summary>
    public static readonly StyledProperty<Marker?> SelectedMarkerProperty =
        AvaloniaProperty.Register<CircularCanvas, Marker?>(nameof(SelectedMarker));

    /// <summary>
    /// Collection of currently selected markers for multi-selection
    /// </summary>
    public static readonly StyledProperty<ObservableCollection<Marker>?> SelectedMarkersProperty =
        AvaloniaProperty.Register<CircularCanvas, ObservableCollection<Marker>?>(nameof(SelectedMarkers));

    /// <summary>
    /// Enables multi-touch gesture support
    /// </summary>
    public static readonly StyledProperty<bool> IsMultiTouchEnabledProperty =
        AvaloniaProperty.Register<CircularCanvas, bool>(nameof(IsMultiTouchEnabled), true);

    /// <summary>
    /// Enables pinch-to-zoom functionality
    /// </summary>
    public static readonly StyledProperty<bool> IsPinchZoomEnabledProperty =
        AvaloniaProperty.Register<CircularCanvas, bool>(nameof(IsPinchZoomEnabled), true);

    /// <summary>
    /// Enables rotation gestures for disk control
    /// </summary>
    public static readonly StyledProperty<bool> IsRotationGestureEnabledProperty =
        AvaloniaProperty.Register<CircularCanvas, bool>(nameof(IsRotationGestureEnabled), true);

    /// <summary>
    /// Enables haptic feedback for touch interactions
    /// </summary>
    public static readonly StyledProperty<bool> IsHapticFeedbackEnabledProperty =
        AvaloniaProperty.Register<CircularCanvas, bool>(nameof(IsHapticFeedbackEnabled), true);

    /// <summary>
    /// Current zoom level for the canvas
    /// </summary>
    public static readonly StyledProperty<double> ZoomLevelProperty =
        AvaloniaProperty.Register<CircularCanvas, double>(nameof(ZoomLevel), 1.0);

    /// <summary>
    /// Markers that are currently being dragged outside the disk (for removal animation)
    /// </summary>
    public static readonly StyledProperty<ObservableCollection<Marker>?> MarkersBeingRemovedProperty =
        AvaloniaProperty.Register<CircularCanvas, ObservableCollection<Marker>?>(nameof(MarkersBeingRemoved));

    #endregion

    #region Properties

    public ObservableCollection<Marker>? Markers
    {
        get => GetValue(MarkersProperty);
        set => SetValue(MarkersProperty, value);
    }

    public ObservableCollection<Lane>? Lanes
    {
        get => GetValue(LanesProperty);
        set => SetValue(LanesProperty, value);
    }

    public int SelectedLaneId
    {
        get => GetValue(SelectedLaneIdProperty);
        set => SetValue(SelectedLaneIdProperty, value);
    }

    public double PlayheadAngle
    {
        get => GetValue(PlayheadAngleProperty);
        set => SetValue(PlayheadAngleProperty, value);
    }

    public bool IsQuantizationEnabled
    {
        get => GetValue(IsQuantizationEnabledProperty);
        set => SetValue(IsQuantizationEnabledProperty, value);
    }

    public IList<double>? GridLines
    {
        get => GetValue(GridLinesProperty);
        set => SetValue(GridLinesProperty, value);
    }

    public double DiskRotation
    {
        get => GetValue(DiskRotationProperty);
        set => SetValue(DiskRotationProperty, value);
    }

    public Marker? SelectedMarker
    {
        get => GetValue(SelectedMarkerProperty);
        set => SetValue(SelectedMarkerProperty, value);
    }

    public ObservableCollection<Marker>? SelectedMarkers
    {
        get => GetValue(SelectedMarkersProperty);
        set => SetValue(SelectedMarkersProperty, value);
    }

    public bool IsMultiTouchEnabled
    {
        get => GetValue(IsMultiTouchEnabledProperty);
        set => SetValue(IsMultiTouchEnabledProperty, value);
    }

    public bool IsPinchZoomEnabled
    {
        get => GetValue(IsPinchZoomEnabledProperty);
        set => SetValue(IsPinchZoomEnabledProperty, value);
    }

    public bool IsRotationGestureEnabled
    {
        get => GetValue(IsRotationGestureEnabledProperty);
        set => SetValue(IsRotationGestureEnabledProperty, value);
    }

    public bool IsHapticFeedbackEnabled
    {
        get => GetValue(IsHapticFeedbackEnabledProperty);
        set => SetValue(IsHapticFeedbackEnabledProperty, value);
    }

    public double ZoomLevel
    {
        get => GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    public ObservableCollection<Marker>? MarkersBeingRemoved
    {
        get => GetValue(MarkersBeingRemovedProperty);
        set => SetValue(MarkersBeingRemovedProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when a marker is placed on the canvas
    /// </summary>
    public event EventHandler<MarkerPlacedEventArgs>? MarkerPlaced;

    /// <summary>
    /// Raised when a marker is selected
    /// </summary>
    public event EventHandler<MarkerSelectedEventArgs>? MarkerSelected;

    /// <summary>
    /// Raised when a marker is moved
    /// </summary>
    public event EventHandler<MarkerMovedEventArgs>? MarkerMoved;

    /// <summary>
    /// Raised when a marker is removed
    /// </summary>
    public event EventHandler<MarkerRemovedEventArgs>? MarkerRemoved;

    /// <summary>
    /// Raised when multiple markers are selected
    /// </summary>
    public event EventHandler<MultiMarkerSelectedEventArgs>? MultiMarkerSelected;

    /// <summary>
    /// Raised when multiple markers are moved together
    /// </summary>
    public event EventHandler<MultiMarkerMovedEventArgs>? MultiMarkerMoved;

    /// <summary>
    /// Raised when zoom level changes
    /// </summary>
    public event EventHandler<ZoomChangedEventArgs>? ZoomChanged;

    /// <summary>
    /// Raised when a marker starts being dragged outside the disk (for removal preview)
    /// </summary>
    public event EventHandler<MarkerRemovalPreviewEventArgs>? MarkerRemovalPreview;

    /// <summary>
    /// Raised when a rotation gesture is performed
    /// </summary>
    public event EventHandler<RotationGestureEventArgs>? RotationGesture;

    /// <summary>
    /// Raised when haptic feedback should be triggered
    /// </summary>
    public event EventHandler<HapticFeedbackEventArgs>? HapticFeedbackRequested;

    #endregion

    #region Private Fields

    private Point _center;
    private double _radius;
    private bool _isDragging;
    private Marker? _draggedMarker;
    private Point _lastPointerPosition;

    // Multi-touch and gesture support
    private readonly Dictionary<int, PointerInfo> _activePointers = new();
    private readonly Dictionary<Marker, Point> _multiDragStartPositions = new();
    private bool _isMultiDragging;
    private bool _isSelectionMode;
    private Point _selectionStartPoint;
    private Rect _selectionRect;

    // Pinch-to-zoom support
    private bool _isPinching;
    private double _initialPinchDistance;
    private double _initialZoomLevel;
    private Point _pinchCenter;

    // Rotation gesture support
    private bool _isRotating;
    private double _initialRotationAngle;
    private double _initialDiskRotation;
    private Point _rotationCenter;

    // Haptic feedback support
    private DateTime _lastHapticFeedback = DateTime.MinValue;
    private readonly TimeSpan _hapticFeedbackCooldown = TimeSpan.FromMilliseconds(50);

    // Marker removal animation support
    private readonly Dictionary<Marker, RemovalAnimationInfo> _removalAnimations = new();
    private DispatcherTimer? _animationTimer;

    // Right-click drag support for velocity and note length
    private bool _isRightClickDragging;
    private Marker? _rightClickDraggedMarker;
    private Point _rightClickStartPosition;
    private int _initialVelocity;
    private double _initialNoteLength;

    // Double-click support for marker removal
    private DateTime _lastClickTime = DateTime.MinValue;
    private Marker? _lastClickedMarker;
    private readonly TimeSpan _doubleClickThreshold = TimeSpan.FromMilliseconds(500);

    // Visual styling
    private readonly IBrush _diskBrush = new SolidColorBrush(Color.FromRgb(45, 45, 55));
    private readonly IPen _diskPen = new Pen(new SolidColorBrush(Color.FromRgb(80, 80, 90)), 2);
    private readonly IPen _gridPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), 1);
    private readonly IPen _playheadPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 100, 100)), 3);

    #endregion

    #region Constructor

    public CircularCanvas()
    {
        // Subscribe to property changes for invalidation
        this.GetObservable(MarkersProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(LanesProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(SelectedLaneIdProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(PlayheadAngleProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(IsQuantizationEnabledProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(GridLinesProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(DiskRotationProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(SelectedMarkerProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(SelectedMarkersProperty).Subscribe(_ => InvalidateVisual());
        this.GetObservable(ZoomLevelProperty).Subscribe(_ => InvalidateVisual());

        // Initialize selected markers collection
        SelectedMarkers = new ObservableCollection<Marker>();
        MarkersBeingRemoved = new ObservableCollection<Marker>();

        // Set up animation timer for smooth removal animations
        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };
        _animationTimer.Tick += OnAnimationTick;

        // Set up input handling - Control doesn't have Background property
        // Input events will be handled through pointer events
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// Information about an active pointer for multi-touch support
    /// </summary>
    private class PointerInfo
    {
        public Point Position { get; set; }
        public Point StartPosition { get; set; }
        public Marker? AssociatedMarker { get; set; }
        public DateTime StartTime { get; set; }
        public bool IsDragging { get; set; }

        public PointerInfo(Point position)
        {
            Position = position;
            StartPosition = position;
            StartTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Information about a marker removal animation
    /// </summary>
    private class RemovalAnimationInfo
    {
        public Point CurrentPosition { get; set; }
        public Point TargetPosition { get; set; }
        public double Opacity { get; set; } = 1.0;
        public double Scale { get; set; } = 1.0;
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(300);
        public bool IsOutsideDisk { get; set; }

        public RemovalAnimationInfo(Point currentPosition, Point targetPosition)
        {
            CurrentPosition = currentPosition;
            TargetPosition = targetPosition;
            StartTime = DateTime.Now;
        }

        public double Progress => Math.Min(1.0, (DateTime.Now - StartTime).TotalMilliseconds / Duration.TotalMilliseconds);
        public bool IsComplete => Progress >= 1.0;
    }

    #endregion

    #region Multi-Touch Helper Methods

    /// <summary>
    /// Calculates the distance between two points
    /// </summary>
    private double CalculateDistance(Point p1, Point p2)
    {
        var dx = p1.X - p2.X;
        var dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Calculates the center point between two points
    /// </summary>
    private Point CalculateCenter(Point p1, Point p2)
    {
        return new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
    }

    /// <summary>
    /// Finds all markers within a selection rectangle
    /// </summary>
    private List<Marker> FindMarkersInRect(Rect rect)
    {
        var markersInRect = new List<Marker>();
        if (Markers == null) return markersInRect;

        foreach (var marker in Markers)
        {
            var markerPosition = CalculateMarkerPosition(marker);
            if (rect.Contains(markerPosition))
            {
                markersInRect.Add(marker);
            }
        }

        return markersInRect;
    }

    /// <summary>
    /// Updates the selection rectangle based on start and current points
    /// </summary>
    private void UpdateSelectionRect(Point startPoint, Point currentPoint)
    {
        var left = Math.Min(startPoint.X, currentPoint.X);
        var top = Math.Min(startPoint.Y, currentPoint.Y);
        var width = Math.Abs(currentPoint.X - startPoint.X);
        var height = Math.Abs(currentPoint.Y - startPoint.Y);

        _selectionRect = new Rect(left, top, width, height);
    }

    /// <summary>
    /// Clears all current selections
    /// </summary>
    private void ClearSelection()
    {
        SelectedMarkers?.Clear();
        SelectedMarker = null;
        _isSelectionMode = false;
    }

    /// <summary>
    /// Adds a marker to the selection
    /// </summary>
    private void AddToSelection(Marker marker)
    {
        if (SelectedMarkers == null) return;

        if (!SelectedMarkers.Contains(marker))
        {
            SelectedMarkers.Add(marker);
        }

        // Update single selection for compatibility
        if (SelectedMarkers.Count == 1)
        {
            SelectedMarker = marker;
        }
        else if (SelectedMarkers.Count > 1)
        {
            SelectedMarker = null; // Clear single selection when multiple are selected
        }
    }

    /// <summary>
    /// Removes a marker from the selection
    /// </summary>
    private void RemoveFromSelection(Marker marker)
    {
        if (SelectedMarkers == null) return;

        SelectedMarkers.Remove(marker);

        // Update single selection for compatibility
        if (SelectedMarkers.Count == 1)
        {
            SelectedMarker = SelectedMarkers[0];
        }
        else if (SelectedMarkers.Count == 0)
        {
            SelectedMarker = null;
        }
    }

    /// <summary>
    /// Checks if a marker is currently selected
    /// </summary>
    private bool IsMarkerSelected(Marker marker)
    {
        return SelectedMarkers?.Contains(marker) == true;
    }

    /// <summary>
    /// Starts removal animation for a marker
    /// </summary>
    private void StartRemovalAnimation(Marker marker, Point currentPosition)
    {
        if (_removalAnimations.ContainsKey(marker)) return;

        // Calculate target position outside the disk (fade out direction)
        var direction = new Point(
            currentPosition.X - _center.X,
            currentPosition.Y - _center.Y
        );
        var length = Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
        if (length > 0)
        {
            direction = new Point(direction.X / length, direction.Y / length);
        }

        var targetPosition = new Point(
            _center.X + direction.X * (_radius * 1.5),
            _center.Y + direction.Y * (_radius * 1.5)
        );

        var animationInfo = new RemovalAnimationInfo(currentPosition, targetPosition)
        {
            IsOutsideDisk = true
        };

        _removalAnimations[marker] = animationInfo;
        MarkersBeingRemoved?.Add(marker);

        // Start animation timer if not already running
        if (!_animationTimer!.IsEnabled)
        {
            _animationTimer.Start();
        }

        // Notify about removal preview
        MarkerRemovalPreview?.Invoke(this, new MarkerRemovalPreviewEventArgs(marker, true));
    }

    /// <summary>
    /// Cancels removal animation for a marker (when dragged back into disk)
    /// </summary>
    private void CancelRemovalAnimation(Marker marker)
    {
        if (_removalAnimations.TryGetValue(marker, out var animationInfo))
        {
            _removalAnimations.Remove(marker);
            MarkersBeingRemoved?.Remove(marker);

            // Notify about removal preview cancellation
            MarkerRemovalPreview?.Invoke(this, new MarkerRemovalPreviewEventArgs(marker, false));
        }

        // Stop animation timer if no more animations
        if (_removalAnimations.Count == 0 && _animationTimer!.IsEnabled)
        {
            _animationTimer.Stop();
        }
    }

    /// <summary>
    /// Animation tick handler for smooth removal animations
    /// </summary>
    private void OnAnimationTick(object? sender, EventArgs e)
    {
        var completedAnimations = new List<Marker>();

        foreach (var kvp in _removalAnimations.ToList())
        {
            var marker = kvp.Key;
            var animation = kvp.Value;

            var progress = animation.Progress;
            var easedProgress = EaseOutCubic(progress);

            // Update animation properties
            animation.CurrentPosition = new Point(
                Lerp(animation.CurrentPosition.X, animation.TargetPosition.X, easedProgress * 0.1),
                Lerp(animation.CurrentPosition.Y, animation.TargetPosition.Y, easedProgress * 0.1)
            );

            animation.Opacity = 1.0 - easedProgress;
            animation.Scale = 1.0 + (easedProgress * 0.5); // Slight scale up during removal

            if (animation.IsComplete)
            {
                completedAnimations.Add(marker);
            }
        }

        // Clean up completed animations
        foreach (var marker in completedAnimations)
        {
            _removalAnimations.Remove(marker);
            MarkersBeingRemoved?.Remove(marker);
        }

        // Stop timer if no more animations
        if (_removalAnimations.Count == 0)
        {
            _animationTimer!.Stop();
        }

        // Trigger visual update
        InvalidateVisual();
    }

    /// <summary>
    /// Easing function for smooth animations
    /// </summary>
    private static double EaseOutCubic(double t)
    {
        return 1 - Math.Pow(1 - t, 3);
    }

    /// <summary>
    /// Linear interpolation between two values
    /// </summary>
    private static double Lerp(double a, double b, double t)
    {
        return a + (b - a) * t;
    }

    /// <summary>
    /// Enhanced boundary detection with visual feedback zones
    /// </summary>
    private bool IsPointNearDiskEdge(Point point, out double distanceFromEdge)
    {
        var distanceFromCenter = CalculateDistance(point);
        distanceFromEdge = Math.Abs(distanceFromCenter - _radius);
        
        // Consider "near edge" if within 20 pixels of the boundary
        return distanceFromEdge <= 20;
    }

    /// <summary>
    /// Gets the removal feedback intensity based on distance from disk edge
    /// </summary>
    private double GetRemovalFeedbackIntensity(Point point)
    {
        var distanceFromCenter = CalculateDistance(point);
        
        if (distanceFromCenter <= _radius)
        {
            return 0.0; // Inside disk, no removal feedback
        }
        
        var distanceOutside = distanceFromCenter - _radius;
        var maxFeedbackDistance = _radius * 0.3; // Feedback zone extends 30% of radius outside
        
        return Math.Min(1.0, distanceOutside / maxFeedbackDistance);
    }

    /// <summary>
    /// Calculates the angle between two vectors from a center point
    /// </summary>
    private double CalculateAngleBetweenVectors(Point center, Point p1, Point p2)
    {
        var v1 = new Point(p1.X - center.X, p1.Y - center.Y);
        var v2 = new Point(p2.X - center.X, p2.Y - center.Y);
        
        var dot = v1.X * v2.X + v1.Y * v2.Y;
        var cross = v1.X * v2.Y - v1.Y * v2.X;
        
        var angle = Math.Atan2(cross, dot) * 180.0 / Math.PI;
        return angle;
    }

    /// <summary>
    /// Triggers haptic feedback if enabled and not in cooldown
    /// </summary>
    private void TriggerHapticFeedback(HapticFeedbackType feedbackType, double intensity = 1.0)
    {
        if (!IsHapticFeedbackEnabled) return;
        
        var now = DateTime.Now;
        if (now - _lastHapticFeedback < _hapticFeedbackCooldown) return;
        
        _lastHapticFeedback = now;
        HapticFeedbackRequested?.Invoke(this, new HapticFeedbackEventArgs(feedbackType, intensity));
    }

    /// <summary>
    /// Detects if a gesture is a rotation based on pointer movements
    /// </summary>
    private bool IsRotationGesture(Point center, Point p1Start, Point p1Current, Point p2Start, Point p2Current)
    {
        // Calculate initial and current angles for both pointers
        var initialAngle1 = Math.Atan2(p1Start.Y - center.Y, p1Start.X - center.X);
        var currentAngle1 = Math.Atan2(p1Current.Y - center.Y, p1Current.X - center.X);
        var initialAngle2 = Math.Atan2(p2Start.Y - center.Y, p2Start.X - center.X);
        var currentAngle2 = Math.Atan2(p2Current.Y - center.Y, p2Current.X - center.X);
        
        var deltaAngle1 = currentAngle1 - initialAngle1;
        var deltaAngle2 = currentAngle2 - initialAngle2;
        
        // Normalize angles to [-π, π]
        deltaAngle1 = ((deltaAngle1 + Math.PI) % (2 * Math.PI)) - Math.PI;
        deltaAngle2 = ((deltaAngle2 + Math.PI) % (2 * Math.PI)) - Math.PI;
        
        // Check if both pointers are rotating in the same direction
        var rotationThreshold = Math.PI / 12; // 15 degrees
        return Math.Abs(deltaAngle1) > rotationThreshold && 
               Math.Abs(deltaAngle2) > rotationThreshold &&
               Math.Sign(deltaAngle1) == Math.Sign(deltaAngle2);
    }

    #endregion

    #region Coordinate System Methods

    /// <summary>
    /// Calculates the angle from center to a point in degrees (0° = 12 o'clock, clockwise)
    /// </summary>
    private double CalculateAngle(Point point)
    {
        var deltaX = point.X - _center.X;
        var deltaY = point.Y - _center.Y;
        
        // Calculate angle in radians, then convert to degrees
        var angleRadians = Math.Atan2(deltaY, deltaX);
        var angleDegrees = angleRadians * 180.0 / Math.PI;
        
        // Adjust so 0° is at 12 o'clock (top) and increases clockwise
        angleDegrees = (angleDegrees + 90) % 360;
        if (angleDegrees < 0) angleDegrees += 360;
        
        return angleDegrees;
    }

    /// <summary>
    /// Calculates what angle to store for a marker so it appears at the clicked position
    /// when rendered with the current disk rotation
    /// </summary>
    private double CalculateAngleAccountingForRotation(Point point)
    {
        // Goal: Find the angle θ such that when the marker is rendered with rotation R,
        // it appears at the clicked position P.
        //
        // During rendering: visual_position = rotate(marker_position(θ), R)
        // We want: visual_position = P
        // So: P = rotate(marker_position(θ), R)
        // Therefore: marker_position(θ) = rotate_inverse(P, R)
        // And: θ = angle_of(rotate_inverse(P, R))
        
        // Step 1: Apply inverse rotation to the click point
        var rotationRadians = -DiskRotation * Math.PI / 180.0; // Negative for inverse
        
        // Translate to origin
        var translatedX = point.X - _center.X;
        var translatedY = point.Y - _center.Y;
        
        // Apply inverse rotation
        var cos = Math.Cos(rotationRadians);
        var sin = Math.Sin(rotationRadians);
        var rotatedX = translatedX * cos - translatedY * sin;
        var rotatedY = translatedX * sin + translatedY * cos;
        
        // Translate back
        var unrotatedPoint = new Point(rotatedX + _center.X, rotatedY + _center.Y);
        
        // Step 2: Calculate angle of the unrotated point
        return CalculateAngle(unrotatedPoint);
    }

    /// <summary>
    /// Calculates the position on the circle for a given angle
    /// </summary>
    private Point CalculatePosition(double angle, double radius)
    {
        // Convert angle to radians and adjust for 12 o'clock start
        var angleRadians = (angle - 90) * Math.PI / 180.0;
        
        var x = _center.X + radius * Math.Cos(angleRadians);
        var y = _center.Y + radius * Math.Sin(angleRadians);
        
        return new Point(x, y);
    }

    /// <summary>
    /// Calculates the radius for a specific lane
    /// </summary>
    private double GetLaneRadius(int laneId)
    {
        var laneSpacing = 25;
        var startRadius = _radius - 40;
        var laneIndex = Lanes?.ToList().FindIndex(l => l.Id == laneId) ?? 0;
        
        return startRadius - (laneIndex * laneSpacing);
    }

    /// <summary>
    /// Calculates the position for a marker based on its lane
    /// </summary>
    private Point CalculateMarkerPosition(Marker marker)
    {
        var laneRadius = GetLaneRadius(marker.Lane);
        return CalculatePosition(marker.Angle, laneRadius);
    }

    /// <summary>
    /// Calculates the distance from center to a point
    /// </summary>
    private double CalculateDistance(Point point)
    {
        var deltaX = point.X - _center.X;
        var deltaY = point.Y - _center.Y;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    /// <summary>
    /// Checks if a point is within the disk area
    /// </summary>
    private bool IsPointInDisk(Point point)
    {
        return CalculateDistance(point) <= _radius;
    }

    /// <summary>
    /// Transforms a click position to account for disk rotation
    /// This converts unrotated click coordinates to rotated coordinate space for marker comparison
    /// </summary>
    private Point TransformClickPositionForRotation(Point clickPosition)
    {
        // Apply inverse rotation to convert click coordinates back to unrotated space
        var rotationRadians = -DiskRotation * Math.PI / 180.0; // Negative for inverse
        
        // Translate to origin
        var translatedX = clickPosition.X - _center.X;
        var translatedY = clickPosition.Y - _center.Y;
        
        // Apply inverse rotation
        var cos = Math.Cos(rotationRadians);
        var sin = Math.Sin(rotationRadians);
        var rotatedX = translatedX * cos - translatedY * sin;
        var rotatedY = translatedX * sin + translatedY * cos;
        
        // Translate back
        return new Point(rotatedX + _center.X, rotatedY + _center.Y);
    }

    /// <summary>
    /// Finds the marker at a given position (within tolerance)
    /// Accounts for disk rotation when comparing positions
    /// </summary>
    private Marker? FindMarkerAtPosition(Point position, double tolerance = 15.0)
    {
        if (Markers == null) return null;

        // Transform the click position to account for disk rotation
        var transformedPosition = TransformClickPositionForRotation(position);

        Marker? closestMarker = null;
        double closestDistance = double.MaxValue;

        foreach (var marker in Markers)
        {
            var markerPosition = CalculateMarkerPosition(marker);
            var distance = Math.Sqrt(
                Math.Pow(transformedPosition.X - markerPosition.X, 2) + 
                Math.Pow(transformedPosition.Y - markerPosition.Y, 2)
            );
            
            if (distance <= tolerance && distance < closestDistance)
            {
                closestMarker = marker;
                closestDistance = distance;
            }
        }
        
        return closestMarker;
    }

    /// <summary>
    /// Snaps an angle to the nearest grid line if quantization is enabled
    /// </summary>
    private double SnapToNearestGridLine(double angle)
    {
        if (GridLines == null || !GridLines.Any()) return angle;
        
        var nearestGridLine = GridLines
            .Select(gridAngle => new { GridAngle = gridAngle, Distance = Math.Abs(angle - gridAngle) })
            .OrderBy(x => x.Distance)
            .First();
            
        // Snap if within reasonable distance (e.g., 15 degrees)
        if (nearestGridLine.Distance <= 15)
        {
            // Trigger haptic feedback for grid snapping
            var snapIntensity = 1.0 - (nearestGridLine.Distance / 15.0);
            TriggerHapticFeedback(HapticFeedbackType.GridSnap, snapIntensity);
            
            return nearestGridLine.GridAngle;
        }
        
        return angle;
    }



    #endregion

    #region Input Handling

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        var position = e.GetPosition(this);
        var pointerId = e.Pointer.Id;
        
        // Store pointer information for multi-touch support
        var pointerInfo = new PointerInfo(position);
        _activePointers[pointerId] = pointerInfo;
        
        // Handle multi-touch gestures
        if (IsMultiTouchEnabled && _activePointers.Count > 1)
        {
            HandleMultiTouchPressed(e);
            return;
        }
        
        _lastPointerPosition = position;
        
        // Check for keyboard modifiers
        var keyModifiers = e.KeyModifiers;
        var isCtrlPressed = keyModifiers.HasFlag(KeyModifiers.Control);
        var isShiftPressed = keyModifiers.HasFlag(KeyModifiers.Shift);
        
        // Check if clicking on an existing marker
        var clickedMarker = FindMarkerAtPosition(position);
        
        if (clickedMarker != null)
        {
            pointerInfo.AssociatedMarker = clickedMarker;
            
            // Handle right-click for velocity and note length adjustment
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                StartRightClickDrag(clickedMarker, position);
                e.Pointer.Capture(this);
                return;
            }
            
            // Handle double-click for marker removal
            if (IsDoubleClick(clickedMarker))
            {
                RemoveMarker(clickedMarker);
                return;
            }
            
            // Handle multi-selection with Ctrl key
            if (isCtrlPressed)
            {
                if (IsMarkerSelected(clickedMarker))
                {
                    RemoveFromSelection(clickedMarker);
                }
                else
                {
                    AddToSelection(clickedMarker);
                }
            }
            else if (isShiftPressed && SelectedMarkers?.Count > 0)
            {
                // Add to selection without clearing existing selection
                AddToSelection(clickedMarker);
            }
            else
            {
                // Single selection (clear others unless already selected)
                if (!IsMarkerSelected(clickedMarker))
                {
                    ClearSelection();
                    AddToSelection(clickedMarker);
                }
                
                // Start dragging
                if (SelectedMarkers?.Count > 0)
                {
                    StartMultiMarkerDrag();
                }
            }
            
            // Visual feedback for selection
            Cursor = new Cursor(StandardCursorType.SizeAll);
            MarkerSelected?.Invoke(this, new MarkerSelectedEventArgs(clickedMarker));
            
            // Trigger haptic feedback for marker selection
            TriggerHapticFeedback(HapticFeedbackType.MarkerSelect);
            
            // Capture pointer for dragging
            e.Pointer.Capture(this);
            InvalidateVisual();
        }
        else if (IsPointInDisk(position))
        {
            // Start selection rectangle if Shift is pressed and no marker clicked
            if (isShiftPressed)
            {
                _isSelectionMode = true;
                _selectionStartPoint = position;
                _selectionRect = new Rect(position, new Size(0, 0));
            }
            else
            {
                // Clear existing selection
                ClearSelection();
                
                // Create new marker
                CreateNewMarker(position);
            }
        }
        else
        {
            // Clicked outside disk - clear selection
            ClearSelection();
            InvalidateVisual();
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        
        var position = e.GetPosition(this);
        var pointerId = e.Pointer.Id;
        
        // Update pointer information
        if (_activePointers.TryGetValue(pointerId, out var pointerInfo))
        {
            pointerInfo.Position = position;
        }
        
        // Handle multi-touch gestures
        if (IsMultiTouchEnabled && _activePointers.Count > 1)
        {
            HandleMultiTouchMoved(e);
            return;
        }
        
        // Handle selection rectangle
        if (_isSelectionMode)
        {
            UpdateSelectionRect(_selectionStartPoint, position);
            
            // Update selection based on rectangle
            var markersInRect = FindMarkersInRect(_selectionRect);
            ClearSelection();
            foreach (var marker in markersInRect)
            {
                AddToSelection(marker);
            }
            
            InvalidateVisual();
            return;
        }
        
        // Handle right-click dragging for velocity and note length
        if (_isRightClickDragging)
        {
            HandleRightClickDrag(position);
            return;
        }

        // Handle multi-marker dragging
        if (_isMultiDragging && SelectedMarkers?.Count > 0)
        {
            HandleMultiMarkerDrag(position);
            return;
        }
        
        // Handle single marker dragging (legacy support)
        if (_isDragging && _draggedMarker != null)
        {
            HandleSingleMarkerDrag(position, e.KeyModifiers);
            return;
        }
        
        // Hover effects for non-dragging state
        var hoveredMarker = FindMarkerAtPosition(position);
        if (hoveredMarker != null)
        {
            Cursor = new Cursor(StandardCursorType.Hand);
        }
        else
        {
            Cursor = new Cursor(StandardCursorType.Arrow);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        
        var position = e.GetPosition(this);
        var pointerId = e.Pointer.Id;
        
        // Remove pointer from active pointers
        _activePointers.Remove(pointerId);
        
        // Handle multi-touch gestures
        if (IsMultiTouchEnabled && _activePointers.Count >= 1)
        {
            HandleMultiTouchReleased(e);
            return;
        }
        
        // Handle right-click drag completion
        if (_isRightClickDragging)
        {
            CompleteRightClickDrag();
            e.Pointer.Capture(null);
            return;
        }

        // End selection mode
        if (_isSelectionMode)
        {
            _isSelectionMode = false;
            
            // Finalize selection
            var markersInRect = FindMarkersInRect(_selectionRect);
            if (markersInRect.Count > 0)
            {
                MultiMarkerSelected?.Invoke(this, new MultiMarkerSelectedEventArgs(markersInRect));
            }
            
            InvalidateVisual();
            e.Pointer.Capture(null);
            return;
        }
        
        // Handle multi-marker drag completion
        if (_isMultiDragging)
        {
            CompleteMultiMarkerDrag(position);
            e.Pointer.Capture(null);
            return;
        }
        
        // Handle single marker drag completion (legacy support)
        if (_isDragging && _draggedMarker != null)
        {
            CompleteSingleMarkerDrag(position);
            e.Pointer.Capture(null);
            return;
        }
        
        // Reset cursor
        Cursor = new Cursor(StandardCursorType.Arrow);
    }

    #endregion

    #region Right-Click Drag Handling

    private void StartRightClickDrag(Marker marker, Point startPosition)
    {
        _isRightClickDragging = true;
        _rightClickDraggedMarker = marker;
        _rightClickStartPosition = startPosition;
        _initialVelocity = marker.Velocity;
        _initialNoteLength = marker.NoteLength;
        
        // Select the marker for visual feedback
        ClearSelection();
        AddToSelection(marker);
        
        // Change cursor to indicate special drag mode
        Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
        
        // Trigger haptic feedback
        TriggerHapticFeedback(HapticFeedbackType.MarkerSelect);
    }

    private void HandleRightClickDrag(Point currentPosition)
    {
        if (!_isRightClickDragging || _rightClickDraggedMarker == null) return;

        var deltaX = currentPosition.X - _rightClickStartPosition.X;
        var deltaY = currentPosition.Y - _rightClickStartPosition.Y;

        // Vertical drag controls velocity (up = higher velocity, down = lower velocity)
        var velocityDelta = (int)(-deltaY * 0.5); // Negative because Y increases downward
        var newVelocity = Math.Max(1, Math.Min(127, _initialVelocity + velocityDelta));

        // Horizontal drag controls note length (right = longer, left = shorter)
        var noteLengthDelta = deltaX * 0.002; // Small multiplier for fine control
        var newNoteLength = Math.Max(0.0625, Math.Min(4.0, _initialNoteLength + noteLengthDelta)); // 1/16 note to whole note

        // Update marker properties
        _rightClickDraggedMarker.Velocity = newVelocity;
        _rightClickDraggedMarker.NoteLength = newNoteLength;

        // Update cursor based on primary drag direction
        if (Math.Abs(deltaY) > Math.Abs(deltaX))
        {
            Cursor = new Cursor(StandardCursorType.SizeNorthSouth); // Velocity mode
        }
        else
        {
            Cursor = new Cursor(StandardCursorType.SizeWestEast); // Note length mode
        }

        InvalidateVisual();
    }

    private void CompleteRightClickDrag()
    {
        if (!_isRightClickDragging) return;

        _isRightClickDragging = false;
        _rightClickDraggedMarker = null;
        Cursor = new Cursor(StandardCursorType.Arrow);

        // Trigger haptic feedback for completion
        TriggerHapticFeedback(HapticFeedbackType.MarkerPlace);
    }

    #endregion

    #region Double-Click Handling

    private bool IsDoubleClick(Marker marker)
    {
        var now = DateTime.Now;
        var timeSinceLastClick = now - _lastClickTime;
        
        var isDoubleClick = timeSinceLastClick <= _doubleClickThreshold && 
                           _lastClickedMarker == marker;
        
        // Update tracking for next click
        _lastClickTime = now;
        _lastClickedMarker = marker;
        
        return isDoubleClick;
    }

    private void RemoveMarker(Marker marker)
    {
        // Remove from selection if selected
        RemoveFromSelection(marker);
        
        // Trigger removal event
        MarkerRemoved?.Invoke(this, new MarkerRemovedEventArgs(marker));
        
        // Trigger haptic feedback
        TriggerHapticFeedback(HapticFeedbackType.MarkerRemove);
        
        // Visual feedback
        InvalidateVisual();
    }

    #endregion

    #region Multi-Touch Gesture Handling

    private void HandleMultiTouchPressed(PointerPressedEventArgs e)
    {
        if (_activePointers.Count == 2)
        {
            var pointers = _activePointers.Values.ToArray();
            var center = CalculateCenter(pointers[0].Position, pointers[1].Position);
            
            // Initialize pinch-to-zoom if enabled
            if (IsPinchZoomEnabled)
            {
                _initialPinchDistance = CalculateDistance(pointers[0].Position, pointers[1].Position);
                _pinchCenter = center;
                _initialZoomLevel = ZoomLevel;
                _isPinching = true;
            }
            
            // Initialize rotation gesture if enabled
            if (IsRotationGestureEnabled)
            {
                _rotationCenter = center;
                _initialRotationAngle = CalculateAngleBetweenVectors(
                    center, pointers[0].Position, pointers[1].Position);
                _initialDiskRotation = DiskRotation;
                _isRotating = false; // Will be set to true when rotation is detected
            }
            
            // Trigger haptic feedback for gesture start
            TriggerHapticFeedback(HapticFeedbackType.GestureStart);
            
            e.Pointer.Capture(this);
        }
    }

    private void HandleMultiTouchMoved(PointerEventArgs e)
    {
        if (_activePointers.Count == 2)
        {
            var pointers = _activePointers.Values.ToArray();
            var currentCenter = CalculateCenter(pointers[0].Position, pointers[1].Position);
            
            // Handle pinch-to-zoom
            if (_isPinching && IsPinchZoomEnabled)
            {
                var currentDistance = CalculateDistance(pointers[0].Position, pointers[1].Position);
                var scaleFactor = currentDistance / _initialPinchDistance;
                
                var newZoomLevel = Math.Max(0.5, Math.Min(3.0, _initialZoomLevel * scaleFactor));
                
                if (Math.Abs(newZoomLevel - ZoomLevel) > 0.01)
                {
                    ZoomLevel = newZoomLevel;
                    ZoomChanged?.Invoke(this, new ZoomChangedEventArgs(ZoomLevel, _pinchCenter));
                    
                    // Trigger haptic feedback for zoom changes
                    var zoomIntensity = Math.Abs(newZoomLevel - _initialZoomLevel) / 2.0;
                    TriggerHapticFeedback(HapticFeedbackType.ZoomChange, Math.Min(1.0, zoomIntensity));
                    
                    InvalidateVisual();
                }
            }
            
            // Handle rotation gesture
            if (IsRotationGestureEnabled)
            {
                // Check if this is a rotation gesture
                if (!_isRotating)
                {
                    _isRotating = IsRotationGesture(
                        _rotationCenter,
                        pointers[0].StartPosition, pointers[0].Position,
                        pointers[1].StartPosition, pointers[1].Position
                    );
                    
                    if (_isRotating)
                    {
                        TriggerHapticFeedback(HapticFeedbackType.RotationStart);
                    }
                }
                
                if (_isRotating)
                {
                    var currentAngle = CalculateAngleBetweenVectors(
                        currentCenter, pointers[0].Position, pointers[1].Position);
                    
                    var angleDelta = currentAngle - _initialRotationAngle;
                    
                    // Convert to degrees and apply to disk rotation
                    var rotationDelta = angleDelta * 180.0 / Math.PI;
                    var newDiskRotation = (_initialDiskRotation + rotationDelta) % 360.0;
                    if (newDiskRotation < 0) newDiskRotation += 360.0;
                    
                    if (Math.Abs(newDiskRotation - DiskRotation) > 1.0) // Threshold to avoid jitter
                    {
                        DiskRotation = newDiskRotation;
                        RotationGesture?.Invoke(this, new RotationGestureEventArgs(rotationDelta, newDiskRotation));
                        
                        // Trigger haptic feedback for rotation
                        var rotationIntensity = Math.Abs(rotationDelta) / 45.0; // Normalize to 45 degrees
                        TriggerHapticFeedback(HapticFeedbackType.Rotation, Math.Min(1.0, rotationIntensity));
                        
                        InvalidateVisual();
                    }
                }
            }
        }
    }

    private void HandleMultiTouchReleased(PointerReleasedEventArgs e)
    {
        if (_activePointers.Count < 2)
        {
            // End gestures when less than 2 pointers
            if (_isPinching)
            {
                _isPinching = false;
                TriggerHapticFeedback(HapticFeedbackType.GestureEnd);
            }
            
            if (_isRotating)
            {
                _isRotating = false;
                TriggerHapticFeedback(HapticFeedbackType.GestureEnd);
            }
        }
    }

    #endregion

    #region Multi-Marker Manipulation

    private void StartMultiMarkerDrag()
    {
        if (SelectedMarkers == null || SelectedMarkers.Count == 0) return;
        
        _isMultiDragging = true;
        _multiDragStartPositions.Clear();
        
        // Store initial positions for relative movement
        foreach (var marker in SelectedMarkers)
        {
            var markerPosition = CalculateMarkerPosition(marker);
            _multiDragStartPositions[marker] = markerPosition;
        }
    }

    private void HandleMultiMarkerDrag(Point currentPosition)
    {
        if (SelectedMarkers == null || SelectedMarkers.Count == 0) return;
        
        var deltaX = currentPosition.X - _lastPointerPosition.X;
        var deltaY = currentPosition.Y - _lastPointerPosition.Y;
        
        var movedMarkers = new List<MarkerMovedInfo>();
        
        foreach (var marker in SelectedMarkers.ToList()) // ToList to avoid modification during iteration
        {
            var oldAngle = marker.Angle;
            
            // Calculate new position based on relative movement
            if (_multiDragStartPositions.TryGetValue(marker, out var startPosition))
            {
                var newPosition = new Point(
                    startPosition.X + (currentPosition.X - _lastPointerPosition.X),
                    startPosition.Y + (currentPosition.Y - _lastPointerPosition.Y)
                );
                
                // Enhanced removal detection with visual feedback
                var isOutsideDisk = !IsPointInDisk(newPosition);
                var isNearEdge = IsPointNearDiskEdge(newPosition, out var distanceFromEdge);
                
                if (isOutsideDisk)
                {
                    // Start removal animation if not already started
                    if (!_removalAnimations.ContainsKey(marker))
                    {
                        StartRemovalAnimation(marker, newPosition);
                        
                        // Trigger haptic feedback for boundary hit
                        TriggerHapticFeedback(HapticFeedbackType.BoundaryHit);
                    }
                    else
                    {
                        // Update animation target position
                        _removalAnimations[marker].TargetPosition = newPosition;
                    }
                    continue;
                }
                else
                {
                    // Cancel removal animation if marker is dragged back into disk
                    if (_removalAnimations.ContainsKey(marker))
                    {
                        CancelRemovalAnimation(marker);
                    }
                }
                
                // Calculate new angle accounting for rotation
                var newAngle = CalculateAngleAccountingForRotation(newPosition);
                
                // Apply quantization if enabled
                if (IsQuantizationEnabled && GridLines != null)
                {
                    newAngle = SnapToNearestGridLine(newAngle);
                }
                
                marker.Angle = newAngle;
                movedMarkers.Add(new MarkerMovedInfo(marker, oldAngle, newAngle));
            }
        }
        
        if (movedMarkers.Count > 0)
        {
            MultiMarkerMoved?.Invoke(this, new MultiMarkerMovedEventArgs(movedMarkers));
        }
        
        _lastPointerPosition = currentPosition;
        InvalidateVisual();
    }

    private void CompleteMultiMarkerDrag(Point finalPosition)
    {
        if (SelectedMarkers == null || SelectedMarkers.Count == 0)
        {
            _isMultiDragging = false;
            return;
        }
        
        var markersToRemove = new List<Marker>();
        
        // Check for markers that should be removed (dragged outside disk)
        foreach (var marker in SelectedMarkers.ToList())
        {
            if (_multiDragStartPositions.TryGetValue(marker, out var startPosition))
            {
                var finalMarkerPosition = new Point(
                    startPosition.X + (finalPosition.X - _lastPointerPosition.X),
                    startPosition.Y + (finalPosition.Y - _lastPointerPosition.Y)
                );
                
                if (!IsPointInDisk(finalMarkerPosition))
                {
                    markersToRemove.Add(marker);
                }
                else
                {
                    // Final quantization snap if enabled
                    if (IsQuantizationEnabled && GridLines != null)
                    {
                        var snappedAngle = SnapToNearestGridLine(marker.Angle);
                        if (Math.Abs(snappedAngle - marker.Angle) > 0.1)
                        {
                            marker.Angle = snappedAngle;
                        }
                    }
                }
            }
        }
        
        // Remove markers that were dragged outside
        foreach (var marker in markersToRemove)
        {
            RemoveFromSelection(marker);
            MarkerRemoved?.Invoke(this, new MarkerRemovedEventArgs(marker));
            
            // Trigger haptic feedback for marker removal
            TriggerHapticFeedback(HapticFeedbackType.MarkerRemove);
        }
        
        // Reset drag state
        _isMultiDragging = false;
        _multiDragStartPositions.Clear();
        Cursor = new Cursor(StandardCursorType.Arrow);
        InvalidateVisual();
    }

    #endregion

    #region Legacy Single Marker Support

    private void HandleSingleMarkerDrag(Point currentPosition, KeyModifiers keyModifiers)
    {
        if (_draggedMarker == null) return;
        
        // Enhanced drag behavior with visual feedback
        var isOutsideDisk = !IsPointInDisk(currentPosition);
        var isNearEdge = IsPointNearDiskEdge(currentPosition, out var distanceFromEdge);
        
        if (isOutsideDisk)
        {
            // Start removal animation if not already started
            if (!_removalAnimations.ContainsKey(_draggedMarker))
            {
                StartRemovalAnimation(_draggedMarker, currentPosition);
                
                // Trigger haptic feedback for boundary hit
                TriggerHapticFeedback(HapticFeedbackType.BoundaryHit);
            }
            else
            {
                // Update animation target position
                _removalAnimations[_draggedMarker].TargetPosition = currentPosition;
            }
            InvalidateVisual();
            return;
        }
        else
        {
            // Cancel removal animation if marker is dragged back into disk
            if (_removalAnimations.ContainsKey(_draggedMarker))
            {
                CancelRemovalAnimation(_draggedMarker);
            }
        }
        
        // Update marker angle based on new position, accounting for rotation
        var newAngle = CalculateAngleAccountingForRotation(currentPosition);
        var oldAngle = _draggedMarker.Angle;
        
        // Enhanced velocity adjustment with keyboard modifier support
        var verticalDelta = currentPosition.Y - _lastPointerPosition.Y;
        var horizontalDelta = currentPosition.X - _lastPointerPosition.X;
        
        var isShiftPressed = keyModifiers.HasFlag(KeyModifiers.Shift);
        var isCtrlPressed = keyModifiers.HasFlag(KeyModifiers.Control);
        
        var newVelocity = _draggedMarker.Velocity;
        
        // Velocity adjustment mode (Shift key or vertical movement)
        if (isShiftPressed || Math.Abs(verticalDelta) > Math.Abs(horizontalDelta))
        {
            var velocityChange = (int)(-verticalDelta * 0.8);
            newVelocity = Math.Max(1, Math.Min(127, _draggedMarker.Velocity + velocityChange));
            
            if (!isCtrlPressed)
            {
                _draggedMarker.Velocity = newVelocity;
                InvalidateVisual();
                return;
            }
        }
        else
        {
            var radialDistance = CalculateDistance(currentPosition);
            var previousRadialDistance = CalculateDistance(_lastPointerPosition);
            var radialDelta = radialDistance - previousRadialDistance;
            
            var velocityChange = (int)(radialDelta * 0.1);
            newVelocity = Math.Max(1, Math.Min(127, _draggedMarker.Velocity + velocityChange));
        }
        
        // Apply quantization if enabled
        if (IsQuantizationEnabled && GridLines != null)
        {
            newAngle = SnapToNearestGridLine(newAngle);
        }
        
        _draggedMarker.Angle = newAngle;
        _draggedMarker.Velocity = newVelocity;
        
        MarkerMoved?.Invoke(this, new MarkerMovedEventArgs(_draggedMarker, oldAngle, newAngle));
        
        _lastPointerPosition = currentPosition;
        InvalidateVisual();
    }

    private void CompleteSingleMarkerDrag(Point finalPosition)
    {
        if (_draggedMarker == null) return;
        
        if (!IsPointInDisk(finalPosition))
        {
            MarkerRemoved?.Invoke(this, new MarkerRemovedEventArgs(_draggedMarker));
            
            // Trigger haptic feedback for marker removal
            TriggerHapticFeedback(HapticFeedbackType.MarkerRemove);
        }
        else
        {
            if (IsQuantizationEnabled && GridLines != null)
            {
                var snappedAngle = SnapToNearestGridLine(_draggedMarker.Angle);
                if (Math.Abs(snappedAngle - _draggedMarker.Angle) > 0.1)
                {
                    var oldAngle = _draggedMarker.Angle;
                    _draggedMarker.Angle = snappedAngle;
                    MarkerMoved?.Invoke(this, new MarkerMovedEventArgs(_draggedMarker, oldAngle, snappedAngle));
                }
            }
        }
        
        _isDragging = false;
        _draggedMarker = null;
        Cursor = new Cursor(StandardCursorType.Arrow);
        InvalidateVisual();
    }

    private void CreateNewMarker(Point position)
    {
        // Use the rotation-aware angle calculation
        var angle = CalculateAngleAccountingForRotation(position);
        
        // Apply quantization to new marker placement if enabled
        if (IsQuantizationEnabled && GridLines != null)
        {
            angle = SnapToNearestGridLine(angle);
        }
        
        // Determine which lane the user clicked in based on distance from center
        var distanceFromCenter = CalculateDistance(position);
        var targetLaneId = DetermineLaneFromDistance(distanceFromCenter);
        
        // Create new marker with color based on angle (chromatic mapping)
        var semitone = (int)(angle / 30) % 12;
        var midiNote = 60 + semitone;
        var color = Marker.GetColorFromMidiNote(midiNote);
        var newMarker = new Marker(angle, System.Drawing.Color.FromArgb(color.R, color.G, color.B));
        
        // Set lane assignment
        newMarker.Lane = targetLaneId;
        
        // Set default velocity based on distance from center
        var normalizedDistance = Math.Min(1.0, distanceFromCenter / _radius);
        var velocity = (int)(127 * (1.0 - normalizedDistance * 0.3));
        newMarker.Velocity = Math.Max(70, velocity);
        
        MarkerPlaced?.Invoke(this, new MarkerPlacedEventArgs(newMarker, position));
        
        // Trigger haptic feedback for marker placement
        TriggerHapticFeedback(HapticFeedbackType.MarkerPlace);
    }



    /// <summary>
    /// Determines which lane a click position corresponds to based on distance from center
    /// </summary>
    private int DetermineLaneFromDistance(double distanceFromCenter)
    {
        if (Lanes == null || !Lanes.Any()) return SelectedLaneId;

        var laneSpacing = 25;
        var startRadius = _radius - 40;
        
        // Find the closest lane based on distance
        var bestLaneId = SelectedLaneId;
        var bestDistance = double.MaxValue;
        
        for (int i = 0; i < Lanes.Count && i < 6; i++)
        {
            var lane = Lanes[i];
            var laneRadius = startRadius - (i * laneSpacing);
            
            // Skip lanes that would be too close to center
            if (laneRadius < 30) continue;
            
            var distance = Math.Abs(distanceFromCenter - laneRadius);
            
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestLaneId = lane.Id;
            }
        }
        
        return bestLaneId;
    }

    #endregion

    #region Rendering

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateDimensions();
    }

    private void UpdateDimensions()
    {
        _center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        _radius = Math.Min(Bounds.Width, Bounds.Height) / 2 - 20; // Leave margin
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;
        
        UpdateDimensions();
        
        // Apply zoom transform around center
        var zoomTransform = Matrix.CreateScale(ZoomLevel, ZoomLevel);
        var centerTransform = Matrix.CreateTranslation(_center.X, _center.Y);
        var negCenterTransform = Matrix.CreateTranslation(-_center.X, -_center.Y);
        
        // Apply disk rotation transform around center
        var rotationTransform = Matrix.CreateRotation(DiskRotation * Math.PI / 180.0);
        
        // Combine transforms: translate to origin, scale, rotate, translate back
        var combinedTransform = negCenterTransform * zoomTransform * rotationTransform * centerTransform;
        
        using (context.PushTransform(combinedTransform))
        {
            // Draw disk background
            DrawDisk(context);
            
            // Draw quantization grid if enabled
            if (IsQuantizationEnabled && GridLines != null)
            {
                DrawQuantizationGrid(context);
            }
            
            // Draw markers
            DrawMarkers(context);
        }
        
        // Draw markers being removed (with animation, outside the main transform)
        DrawRemovalAnimations(context);
        
        // Draw playhead (not rotated with disk, but affected by zoom)
        var zoomTransformForPlayhead = Matrix.CreateScale(ZoomLevel, ZoomLevel);
        var playheadCenterTransform = Matrix.CreateTranslation(_center.X, _center.Y);
        var playheadNegCenterTransform = Matrix.CreateTranslation(-_center.X, -_center.Y);
        var playheadCombinedTransform = playheadNegCenterTransform * zoomTransformForPlayhead * playheadCenterTransform;
        
        using (context.PushTransform(playheadCombinedTransform))
        {
            DrawPlayhead(context);
        }
        
        // Draw selection rectangle (not affected by transforms)
        if (_isSelectionMode)
        {
            DrawSelectionRectangle(context);
        }
        
        // Draw multi-selection indicators
        if (SelectedMarkers?.Count > 1)
        {
            DrawMultiSelectionIndicators(context);
        }
        
        // Draw velocity and note length indicators (outside rotation transform so text doesn't rotate)
        DrawVelocityAndNoteLengthIndicators(context);
    }

    private void DrawDisk(DrawingContext context)
    {
        // Draw main disk
        context.DrawEllipse(_diskBrush, _diskPen, _center, _radius, _radius);
        
        // Draw lane indicators
        DrawLaneIndicators(context);
        
        // Draw visual notches every 8th of the diameter (45 degrees apart)
        DrawDiskNotches(context);
        
        // Draw center dot
        var centerDotRadius = 5;
        context.DrawEllipse(
            new SolidColorBrush(Color.FromRgb(200, 200, 200)), 
            null, 
            _center, 
            centerDotRadius, 
            centerDotRadius
        );
    }

    private void DrawLaneIndicators(DrawingContext context)
    {
        if (Lanes == null || !Lanes.Any()) return;

        var laneSpacing = 25; // Distance between lane rings
        var startRadius = _radius - 40; // Start lanes inside the disk edge

        for (int i = 0; i < Math.Min(Lanes.Count, 6); i++) // Limit to 6 lanes for visual clarity
        {
            var lane = Lanes[i];
            var laneRadius = startRadius - (i * laneSpacing);
            
            if (laneRadius < 30) break; // Don't draw lanes too close to center

            // Convert System.Drawing.Color to Avalonia Color
            var laneColor = Color.FromArgb(lane.ThemeColor.A, lane.ThemeColor.R, lane.ThemeColor.G, lane.ThemeColor.B);
            
            // Draw lane ring with theme color
            var lanePen = new Pen(new SolidColorBrush(laneColor, 0.4), 2);
            context.DrawEllipse(null, lanePen, _center, laneRadius, laneRadius);

            // Highlight selected lane
            if (lane.Id == SelectedLaneId)
            {
                var selectedPen = new Pen(new SolidColorBrush(laneColor, 0.8), 3);
                context.DrawEllipse(null, selectedPen, _center, laneRadius, laneRadius);
            }

            // Draw lane label at 12 o'clock position
            DrawLaneLabel(context, lane, laneRadius, i);
        }
    }

    private void DrawLaneLabel(DrawingContext context, Lane lane, double laneRadius, int laneIndex)
    {
        // Position label at top of the lane ring
        var labelX = _center.X;
        var labelY = _center.Y - laneRadius - 15;
        var labelPosition = new Point(labelX, labelY);

        // Convert System.Drawing.Color to Avalonia Color
        var laneColor = Color.FromArgb(lane.ThemeColor.A, lane.ThemeColor.R, lane.ThemeColor.G, lane.ThemeColor.B);
        
        // Create label background
        var backgroundBrush = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0));
        var borderBrush = new SolidColorBrush(laneColor);
        
        // Lane name and channel info
        var labelText = $"{lane.Name} (Ch{lane.MidiChannel})";
        var textBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        var typeface = new Typeface("Arial", FontStyle.Normal, FontWeight.Bold);
        
        var formattedText = new FormattedText(
            labelText,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            10,
            textBrush
        );

        // Draw background rectangle
        var padding = 4;
        var backgroundRect = new Rect(
            labelPosition.X - formattedText.Width / 2 - padding,
            labelPosition.Y - formattedText.Height / 2 - padding,
            formattedText.Width + padding * 2,
            formattedText.Height + padding * 2
        );

        context.DrawRectangle(backgroundBrush, new Pen(borderBrush, 1), backgroundRect, 3, 3);

        // Draw text
        var textPosition = new Point(
            labelPosition.X - formattedText.Width / 2,
            labelPosition.Y - formattedText.Height / 2
        );
        
        context.DrawText(formattedText, textPosition);

        // Draw mute/solo indicators
        if (lane.IsMuted || lane.IsSoloed)
        {
            DrawLaneStateIndicators(context, lane, backgroundRect);
        }
    }

    private void DrawLaneStateIndicators(DrawingContext context, Lane lane, Rect labelRect)
    {
        var indicatorSize = 8;
        var indicatorY = labelRect.Bottom + 2;
        
        if (lane.IsMuted)
        {
            // Draw mute indicator (red X)
            var muteX = labelRect.Left + 5;
            var muteBrush = new SolidColorBrush(Color.FromRgb(255, 100, 100));
            var mutePen = new Pen(muteBrush, 2);
            
            context.DrawLine(mutePen, 
                new Point(muteX, indicatorY), 
                new Point(muteX + indicatorSize, indicatorY + indicatorSize));
            context.DrawLine(mutePen, 
                new Point(muteX + indicatorSize, indicatorY), 
                new Point(muteX, indicatorY + indicatorSize));
        }
        
        if (lane.IsSoloed)
        {
            // Draw solo indicator (yellow S)
            var soloX = labelRect.Right - 15;
            var soloBrush = new SolidColorBrush(Color.FromRgb(255, 255, 100));
            var typeface = new Typeface("Arial", FontStyle.Normal, FontWeight.Bold);
            
            var soloText = new FormattedText(
                "S",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                10,
                soloBrush
            );
            
            context.DrawText(soloText, new Point(soloX, indicatorY));
        }
    }

    private void DrawDiskNotches(DrawingContext context)
    {
        var notchPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), 1.5);
        var notchLength = 15;
        
        // Draw 8 notches (every 45 degrees)
        for (int i = 0; i < 8; i++)
        {
            var angle = i * 45.0; // 360 / 8 = 45 degrees
            var radians = (angle - 90) * Math.PI / 180.0; // -90 to start at top
            
            // Outer edge of notch
            var outerX = _center.X + Math.Cos(radians) * _radius;
            var outerY = _center.Y + Math.Sin(radians) * _radius;
            var outerPoint = new Point(outerX, outerY);
            
            // Inner edge of notch
            var innerX = _center.X + Math.Cos(radians) * (_radius - notchLength);
            var innerY = _center.Y + Math.Sin(radians) * (_radius - notchLength);
            var innerPoint = new Point(innerX, innerY);
            
            context.DrawLine(notchPen, outerPoint, innerPoint);
        }
    }

    private void DrawQuantizationGrid(DrawingContext context)
    {
        if (GridLines == null) return;
        
        foreach (var gridAngle in GridLines)
        {
            var startPoint = CalculatePosition(gridAngle, _radius * 0.3);
            var endPoint = CalculatePosition(gridAngle, _radius * 0.95);
            
            context.DrawLine(_gridPen, startPoint, endPoint);
        }
    }

    private void DrawMarkers(DrawingContext context)
    {
        if (Markers == null) return;
        
        foreach (var marker in Markers)
        {
            // Skip markers that are being animated for removal
            if (!_removalAnimations.ContainsKey(marker))
            {
                DrawMarker(context, marker);
            }
        }
    }

    private void DrawRemovalAnimations(DrawingContext context)
    {
        foreach (var kvp in _removalAnimations)
        {
            var marker = kvp.Key;
            var animation = kvp.Value;
            
            DrawMarkerWithAnimation(context, marker, animation);
        }
    }

    private void DrawMarker(DrawingContext context, Marker marker)
    {
        var position = CalculateMarkerPosition(marker);
        var baseRadius = 8 * ZoomLevel; // Apply zoom to marker size
        
        // Scale marker size based on note length (0.0625 = 1/16 note, 1.0 = whole note)
        var noteLengthMultiplier = Math.Max(0.5, Math.Min(2.0, marker.NoteLength * 4)); // Scale 0.25 (1/4) to 1.0, max 2.0
        var markerRadius = baseRadius * noteLengthMultiplier;
        
        // Convert System.Drawing.Color to Avalonia Color
        var avaloniaColor = Color.FromArgb(marker.Color.A, marker.Color.R, marker.Color.G, marker.Color.B);
        
        // Enhanced visual feedback based on state
        if (marker.IsActive)
        {
            // Active marker: larger size with pulsing glow effect
            markerRadius = 14 * ZoomLevel;
            
            // Draw outer glow for active markers
            var glowRadius = markerRadius + (6 * ZoomLevel);
            var glowBrush = new SolidColorBrush(avaloniaColor, 0.3);
            context.DrawEllipse(glowBrush, null, position, glowRadius, glowRadius);
            
            // Draw middle glow
            var midGlowRadius = markerRadius + (3 * ZoomLevel);
            var midGlowBrush = new SolidColorBrush(avaloniaColor, 0.6);
            context.DrawEllipse(midGlowBrush, null, position, midGlowRadius, midGlowRadius);
        }
        
        // Multi-selection ring (takes precedence over single selection)
        var isMultiSelected = IsMarkerSelected(marker);
        if (isMultiSelected && SelectedMarkers?.Count > 1)
        {
            var selectionRadius = markerRadius + (4 * ZoomLevel);
            var multiSelectionPen = new Pen(new SolidColorBrush(Color.FromRgb(100, 255, 100)), 2 * ZoomLevel);
            
            // Multi-selection ring with different color
            context.DrawEllipse(null, multiSelectionPen, position, selectionRadius, selectionRadius);
            
            // Inner selection highlight
            var innerSelectionPen = new Pen(new SolidColorBrush(Color.FromRgb(200, 255, 200)), 1 * ZoomLevel);
            context.DrawEllipse(null, innerSelectionPen, position, markerRadius + (1 * ZoomLevel), markerRadius + (1 * ZoomLevel));
        }
        // Single selection ring
        else if (marker == SelectedMarker || (isMultiSelected && SelectedMarkers?.Count == 1))
        {
            var selectionRadius = markerRadius + (4 * ZoomLevel);
            var selectionPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 255, 100)), 2 * ZoomLevel);
            
            // Animated selection ring (could be enhanced with actual animation)
            context.DrawEllipse(null, selectionPen, position, selectionRadius, selectionRadius);
            
            // Inner selection highlight
            var innerSelectionPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 255, 255)), 1 * ZoomLevel);
            context.DrawEllipse(null, innerSelectionPen, position, markerRadius + (1 * ZoomLevel), markerRadius + (1 * ZoomLevel));
        }
        
        // Main marker body with velocity-based visual feedback
        var velocity = marker.Velocity;
        var velocityNormalized = velocity / 127.0;
        
        // Velocity affects opacity (transparency shows velocity)
        var velocityOpacity = Math.Max(0.3, velocityNormalized * 0.8 + 0.2); // Range from 0.2 to 1.0
        var finalRadius = markerRadius; // Size is now controlled by note length, not velocity
        
        var markerBrush = new SolidColorBrush(avaloniaColor, velocityOpacity);
        
        // Enhanced border based on state
        IBrush borderBrush;
        double borderThickness;
        
        if (marker.IsActive)
        {
            borderBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            borderThickness = 2;
        }
        else if (marker == SelectedMarker)
        {
            borderBrush = new SolidColorBrush(Color.FromRgb(255, 255, 200));
            borderThickness = 1.5;
        }
        else
        {
            borderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));
            borderThickness = 1;
        }
        
        var markerPen = new Pen(borderBrush, borderThickness);
        context.DrawEllipse(markerBrush, markerPen, position, finalRadius, finalRadius);
        
        // Note: Velocity and note length indicators are now drawn outside the rotation transform
        
        // Lane indicator (small colored dot)
        if (marker.Lane > 0)
        {
            DrawLaneIndicator(context, position, marker.Lane, finalRadius);
        }
        
        // Grid lock indicator
        if (marker.IsLockedToGrid)
        {
            DrawGridLockIndicator(context, position, finalRadius);
        }
    }
    
    /// <summary>
    /// Draws velocity and note length indicators for all selected markers (outside rotation transform)
    /// </summary>
    private void DrawVelocityAndNoteLengthIndicators(DrawingContext context)
    {
        if (SelectedMarker != null)
        {
            // Calculate the marker's position in unrotated space
            var laneRadius = GetLaneRadius(SelectedMarker.Lane);
            var unrotatedPosition = CalculatePosition(SelectedMarker.Angle, laneRadius);
            
            // Apply the same transform that's used in rendering to get the visual position
            var rotationRadians = DiskRotation * Math.PI / 180.0;
            var translatedX = unrotatedPosition.X - _center.X;
            var translatedY = unrotatedPosition.Y - _center.Y;
            var cos = Math.Cos(rotationRadians);
            var sin = Math.Sin(rotationRadians);
            var rotatedX = translatedX * cos - translatedY * sin;
            var rotatedY = translatedX * sin + translatedY * cos;
            var visualPosition = new Point(rotatedX + _center.X, rotatedY + _center.Y);
            
            var baseRadius = 8 * ZoomLevel;
            var noteLengthMultiplier = Math.Max(0.5, Math.Min(2.0, SelectedMarker.NoteLength * 4));
            var markerRadius = baseRadius * noteLengthMultiplier;
            
            DrawVelocityAndNoteLengthIndicator(context, visualPosition, SelectedMarker, markerRadius);
        }
    }

    private void DrawVelocityAndNoteLengthIndicator(DrawingContext context, Point markerPosition, Marker marker, double markerRadius)
    {
        var velocity = marker.Velocity;
        var noteLength = marker.NoteLength;
        var velocityNormalized = velocity / 127.0;
        
        // Velocity indicator (vertical bar on the left)
        var velocityBarWidth = 3;
        var velocityBarHeight = 25;
        var velocityIndicatorHeight = velocityNormalized * velocityBarHeight;
        
        var velocityBarX = markerPosition.X - markerRadius - 8;
        var velocityBarY = markerPosition.Y - velocityBarHeight / 2;
        
        // Background bar for velocity
        var velocityBackgroundRect = new Rect(velocityBarX, velocityBarY, velocityBarWidth, velocityBarHeight);
        var velocityBackgroundBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100), 0.3);
        context.DrawRectangle(velocityBackgroundBrush, null, velocityBackgroundRect);
        
        // Velocity bar (actual velocity height)
        var velocityRect = new Rect(velocityBarX, velocityBarY + (velocityBarHeight - velocityIndicatorHeight), velocityBarWidth, velocityIndicatorHeight);
        
        // Color-code velocity: green (low) to yellow (mid) to red (high)
        Color velocityColor;
        if (velocityNormalized < 0.5)
        {
            var t = velocityNormalized * 2;
            velocityColor = Color.FromRgb((byte)(0 + t * 255), 255, 0);
        }
        else
        {
            var t = (velocityNormalized - 0.5) * 2;
            velocityColor = Color.FromRgb(255, (byte)(255 - t * 255), 0);
        }
        
        var velocityBrush = new SolidColorBrush(velocityColor);
        context.DrawRectangle(velocityBrush, null, velocityRect);
        
        // Note length indicator (horizontal bar on the right)
        var noteLengthBarWidth = 25;
        var noteLengthBarHeight = 3;
        var noteLengthNormalized = Math.Min(1.0, noteLength / 1.0); // Normalize to whole note (1.0)
        var noteLengthIndicatorWidth = noteLengthNormalized * noteLengthBarWidth;
        
        var noteLengthBarX = markerPosition.X + markerRadius + 5;
        var noteLengthBarY = markerPosition.Y - noteLengthBarHeight / 2;
        
        // Background bar for note length
        var noteLengthBackgroundRect = new Rect(noteLengthBarX, noteLengthBarY, noteLengthBarWidth, noteLengthBarHeight);
        var noteLengthBackgroundBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100), 0.3);
        context.DrawRectangle(noteLengthBackgroundBrush, null, noteLengthBackgroundRect);
        
        // Note length bar (actual length width)
        var noteLengthRect = new Rect(noteLengthBarX, noteLengthBarY, noteLengthIndicatorWidth, noteLengthBarHeight);
        
        // Color-code note length: blue (short) to purple (long)
        var noteLengthColor = Color.FromRgb(
            (byte)(100 + noteLengthNormalized * 155),
            (byte)(100 - noteLengthNormalized * 50),
            255
        );
        
        var noteLengthBrush = new SolidColorBrush(noteLengthColor);
        context.DrawRectangle(noteLengthBrush, null, noteLengthRect);
        
        // Text indicators
        var textBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        var typeface = new Typeface("Arial");
        
        // Velocity text
        var velocityText = velocity.ToString();
        var velocityFormattedText = new FormattedText(
            velocityText,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            9,
            textBrush
        );
        
        var velocityTextPosition = new Point(
            velocityBarX - velocityFormattedText.Width - 2,
            velocityBarY + velocityBarHeight / 2 - velocityFormattedText.Height / 2
        );
        
        context.DrawText(velocityFormattedText, velocityTextPosition);
        
        // Note length text (show as fraction)
        var noteLengthText = GetNoteLengthDisplayText(noteLength);
        var noteLengthFormattedText = new FormattedText(
            noteLengthText,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            9,
            textBrush
        );
        
        var noteLengthTextPosition = new Point(
            noteLengthBarX + noteLengthBarWidth + 2,
            noteLengthBarY - noteLengthFormattedText.Height / 2
        );
        
        context.DrawText(noteLengthFormattedText, noteLengthTextPosition);
    }

    private string GetNoteLengthDisplayText(double noteLength)
    {
        // Convert note length to common musical fractions
        if (Math.Abs(noteLength - 0.0625) < 0.01) return "1/16";
        if (Math.Abs(noteLength - 0.125) < 0.01) return "1/8";
        if (Math.Abs(noteLength - 0.25) < 0.01) return "1/4";
        if (Math.Abs(noteLength - 0.5) < 0.01) return "1/2";
        if (Math.Abs(noteLength - 1.0) < 0.01) return "1/1";
        if (Math.Abs(noteLength - 2.0) < 0.01) return "2/1";
        
        // For other values, show as decimal
        return noteLength.ToString("F2");
    }
    
    private void DrawLaneIndicator(DrawingContext context, Point markerPosition, int lane, double markerRadius)
    {
        // Small colored dot to indicate lane
        var laneColors = new[]
        {
            Color.FromRgb(255, 100, 100), // Lane 1 - Red
            Color.FromRgb(100, 255, 100), // Lane 2 - Green  
            Color.FromRgb(100, 100, 255), // Lane 3 - Blue
            Color.FromRgb(255, 255, 100), // Lane 4 - Yellow
            Color.FromRgb(255, 100, 255), // Lane 5 - Magenta
            Color.FromRgb(100, 255, 255), // Lane 6 - Cyan
        };
        
        var laneColor = laneColors[Math.Min(lane - 1, laneColors.Length - 1)];
        var laneBrush = new SolidColorBrush(laneColor);
        
        var indicatorRadius = 3;
        var indicatorPosition = new Point(
            markerPosition.X + markerRadius - indicatorRadius,
            markerPosition.Y - markerRadius + indicatorRadius
        );
        
        context.DrawEllipse(laneBrush, null, indicatorPosition, indicatorRadius, indicatorRadius);
    }
    
    private void DrawGridLockIndicator(DrawingContext context, Point markerPosition, double markerRadius)
    {
        // Small square to indicate grid lock
        var lockSize = 4;
        var lockPosition = new Point(
            markerPosition.X - markerRadius + 2,
            markerPosition.Y - markerRadius + 2
        );
        
        var lockRect = new Rect(lockPosition.X, lockPosition.Y, lockSize, lockSize);
        var lockBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        var lockPen = new Pen(new SolidColorBrush(Color.FromRgb(0, 0, 0)), 1);
        
        context.DrawRectangle(lockBrush, lockPen, lockRect);
    }

    private void DrawPlayhead(DrawingContext context)
    {
        // Enhanced playhead visualization at 12 o'clock (not affected by disk rotation)
        
        // Main playhead line - thicker and more prominent
        var playheadStart = new Point(_center.X, _center.Y - _radius * 0.15);
        var playheadEnd = new Point(_center.X, _center.Y - _radius * 1.15);
        
        // Draw shadow/glow effect for better visibility
        var shadowPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)), 5);
        var shadowStart = new Point(playheadStart.X + 1, playheadStart.Y + 1);
        var shadowEnd = new Point(playheadEnd.X + 1, playheadEnd.Y + 1);
        context.DrawLine(shadowPen, shadowStart, shadowEnd);
        
        // Main playhead line
        context.DrawLine(_playheadPen, playheadStart, playheadEnd);
        
        // Enhanced playhead triangle/arrow
        var triangleSize = 10;
        var triangleHeight = 12;
        var arrowTip = new Point(_center.X, _center.Y - _radius * 1.15);
        var trianglePoints = new[]
        {
            arrowTip,
            new Point(_center.X - triangleSize, arrowTip.Y - triangleHeight),
            new Point(_center.X + triangleSize, arrowTip.Y - triangleHeight)
        };
        
        // Draw triangle shadow
        var shadowTrianglePoints = trianglePoints.Select(p => new Point(p.X + 1, p.Y + 1)).ToArray();
        var shadowTriangleGeometry = new PolylineGeometry(shadowTrianglePoints, true);
        context.DrawGeometry(new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)), null, shadowTriangleGeometry);
        
        // Draw main triangle
        var triangleGeometry = new PolylineGeometry(trianglePoints, true);
        var triangleBrush = new SolidColorBrush(Color.FromRgb(255, 120, 120));
        var trianglePen = new Pen(new SolidColorBrush(Color.FromRgb(200, 50, 50)), 2);
        context.DrawGeometry(triangleBrush, trianglePen, triangleGeometry);
        
        // Add playhead center marker
        var centerMarkerRadius = 4;
        var centerBrush = new SolidColorBrush(Color.FromRgb(255, 150, 150));
        var centerPen = new Pen(new SolidColorBrush(Color.FromRgb(200, 50, 50)), 1);
        context.DrawEllipse(centerBrush, centerPen, _center, centerMarkerRadius, centerMarkerRadius);
        
        // Optional: Add "12" text indicator
        DrawPlayheadLabel(context);
    }
    
    private void DrawPlayheadLabel(DrawingContext context)
    {
        var labelText = "12";
        var textBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        var typeface = new Typeface("Arial", FontStyle.Normal, FontWeight.Bold);
        var formattedText = new FormattedText(
            labelText,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            14,
            textBrush
        );
        
        var labelPosition = new Point(
            _center.X - formattedText.Width / 2,
            _center.Y - _radius * 1.3
        );
        
        // Draw text shadow
        var shadowPosition = new Point(labelPosition.X + 1, labelPosition.Y + 1);
        var shadowText = new FormattedText(
            labelText,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            14,
            new SolidColorBrush(Color.FromArgb(150, 0, 0, 0))
        );
        context.DrawText(shadowText, shadowPosition);
        
        // Draw main text
        context.DrawText(formattedText, labelPosition);
    }
    
    private void DrawSelectionRectangle(DrawingContext context)
    {
        if (_selectionRect.Width <= 0 || _selectionRect.Height <= 0) return;
        
        // Draw selection rectangle
        var selectionBrush = new SolidColorBrush(Color.FromArgb(50, 100, 150, 255));
        var selectionPen = new Pen(new SolidColorBrush(Color.FromRgb(100, 150, 255)), 1);
        
        context.DrawRectangle(selectionBrush, selectionPen, _selectionRect);
        
        // Draw corner handles
        var handleSize = 6;
        var handleBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        var handlePen = new Pen(new SolidColorBrush(Color.FromRgb(100, 150, 255)), 1);
        
        var corners = new[]
        {
            new Point(_selectionRect.Left, _selectionRect.Top),
            new Point(_selectionRect.Right, _selectionRect.Top),
            new Point(_selectionRect.Left, _selectionRect.Bottom),
            new Point(_selectionRect.Right, _selectionRect.Bottom)
        };
        
        foreach (var corner in corners)
        {
            var handleRect = new Rect(
                corner.X - handleSize / 2,
                corner.Y - handleSize / 2,
                handleSize,
                handleSize
            );
            context.DrawRectangle(handleBrush, handlePen, handleRect);
        }
    }
    
    private void DrawMultiSelectionIndicators(DrawingContext context)
    {
        if (SelectedMarkers == null || SelectedMarkers.Count <= 1) return;
        
        // Draw connection lines between selected markers
        var connectionPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 100, 255, 100)), 1);
        
        var positions = SelectedMarkers
            .Select(m => CalculateMarkerPosition(m))
            .ToArray();
        
        // Draw lines connecting all selected markers
        for (int i = 0; i < positions.Length - 1; i++)
        {
            for (int j = i + 1; j < positions.Length; j++)
            {
                context.DrawLine(connectionPen, positions[i], positions[j]);
            }
        }
        
        // Draw selection count indicator
        if (positions.Length > 0)
        {
            var centerPoint = new Point(
                positions.Average(p => p.X),
                positions.Average(p => p.Y)
            );
            
            var countText = SelectedMarkers.Count.ToString();
            var textBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            var typeface = new Typeface("Arial", FontStyle.Normal, FontWeight.Bold);
            var formattedText = new FormattedText(
                countText,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                12,
                textBrush
            );
            
            // Draw background circle for count
            var backgroundRadius = Math.Max(formattedText.Width, formattedText.Height) / 2 + 4;
            var backgroundBrush = new SolidColorBrush(Color.FromArgb(200, 100, 150, 255));
            context.DrawEllipse(backgroundBrush, null, centerPoint, backgroundRadius, backgroundRadius);
            
            // Draw count text
            var textPosition = new Point(
                centerPoint.X - formattedText.Width / 2,
                centerPoint.Y - formattedText.Height / 2
            );
            context.DrawText(formattedText, textPosition);
        }
    }

    private void DrawMarkerWithAnimation(DrawingContext context, Marker marker, RemovalAnimationInfo animation)
    {
        var position = animation.CurrentPosition;
        var baseRadius = 8 * ZoomLevel * animation.Scale;
        var markerRadius = baseRadius;
        
        // Convert System.Drawing.Color to Avalonia Color with animation opacity
        var avaloniaColor = Color.FromArgb(
            (byte)(marker.Color.A * animation.Opacity),
            marker.Color.R, 
            marker.Color.G, 
            marker.Color.B
        );
        
        // Enhanced visual feedback for removal
        if (animation.IsOutsideDisk)
        {
            // Add red tint to indicate removal
            var removalIntensity = GetRemovalFeedbackIntensity(position);
            var redTint = (byte)(255 * removalIntensity * animation.Opacity);
            avaloniaColor = Color.FromArgb(
                (byte)(marker.Color.A * animation.Opacity),
                (byte)Math.Min(255, marker.Color.R + redTint),
                (byte)Math.Max(0, marker.Color.G - redTint / 2),
                (byte)Math.Max(0, marker.Color.B - redTint / 2)
            );
            
            // Draw removal warning ring
            var warningRadius = markerRadius + (10 * animation.Scale);
            var warningBrush = new SolidColorBrush(Color.FromArgb(
                (byte)(100 * animation.Opacity),
                255, 100, 100
            ));
            context.DrawEllipse(warningBrush, null, position, warningRadius, warningRadius);
        }
        
        // Main marker body with animation effects
        var markerBrush = new SolidColorBrush(avaloniaColor);
        var borderBrush = new SolidColorBrush(Color.FromArgb(
            (byte)(200 * animation.Opacity),
            255, 255, 255
        ));
        var markerPen = new Pen(borderBrush, 1 * animation.Scale);
        
        context.DrawEllipse(markerBrush, markerPen, position, markerRadius, markerRadius);
        
        // Draw removal direction arrow
        if (animation.IsOutsideDisk && animation.Opacity > 0.3)
        {
            DrawRemovalArrow(context, position, animation);
        }
    }

    private void DrawRemovalArrow(DrawingContext context, Point markerPosition, RemovalAnimationInfo animation)
    {
        var direction = new Point(
            animation.TargetPosition.X - markerPosition.X,
            animation.TargetPosition.Y - markerPosition.Y
        );
        var length = Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
        
        if (length > 0)
        {
            direction = new Point(direction.X / length, direction.Y / length);
            
            var arrowLength = 20 * animation.Scale;
            var arrowEnd = new Point(
                markerPosition.X + direction.X * arrowLength,
                markerPosition.Y + direction.Y * arrowLength
            );
            
            // Arrow line
            var arrowPen = new Pen(new SolidColorBrush(Color.FromArgb(
                (byte)(150 * animation.Opacity),
                255, 100, 100
            )), 2 * animation.Scale);
            
            context.DrawLine(arrowPen, markerPosition, arrowEnd);
            
            // Arrow head
            var arrowHeadSize = 6 * animation.Scale;
            var perpendicular = new Point(-direction.Y, direction.X);
            
            var arrowHead1 = new Point(
                arrowEnd.X - direction.X * arrowHeadSize + perpendicular.X * arrowHeadSize / 2,
                arrowEnd.Y - direction.Y * arrowHeadSize + perpendicular.Y * arrowHeadSize / 2
            );
            
            var arrowHead2 = new Point(
                arrowEnd.X - direction.X * arrowHeadSize - perpendicular.X * arrowHeadSize / 2,
                arrowEnd.Y - direction.Y * arrowHeadSize - perpendicular.Y * arrowHeadSize / 2
            );
            
            context.DrawLine(arrowPen, arrowEnd, arrowHead1);
            context.DrawLine(arrowPen, arrowEnd, arrowHead2);
        }
    }

    #endregion
}

#region Event Args Classes

public class MarkerPlacedEventArgs : EventArgs
{
    public Marker Marker { get; }
    public Point Position { get; }
    
    public MarkerPlacedEventArgs(Marker marker, Point position)
    {
        Marker = marker;
        Position = position;
    }
}

public class MarkerSelectedEventArgs : EventArgs
{
    public Marker Marker { get; }
    
    public MarkerSelectedEventArgs(Marker marker)
    {
        Marker = marker;
    }
}

public class MarkerMovedEventArgs : EventArgs
{
    public Marker Marker { get; }
    public double OldAngle { get; }
    public double NewAngle { get; }
    
    public MarkerMovedEventArgs(Marker marker, double oldAngle, double newAngle)
    {
        Marker = marker;
        OldAngle = oldAngle;
        NewAngle = newAngle;
    }
}

public class MarkerRemovedEventArgs : EventArgs
{
    public Marker Marker { get; }
    
    public MarkerRemovedEventArgs(Marker marker)
    {
        Marker = marker;
    }
}

public class MultiMarkerSelectedEventArgs : EventArgs
{
    public IReadOnlyList<Marker> Markers { get; }
    
    public MultiMarkerSelectedEventArgs(IReadOnlyList<Marker> markers)
    {
        Markers = markers;
    }
}

public class MultiMarkerMovedEventArgs : EventArgs
{
    public IReadOnlyList<MarkerMovedInfo> MovedMarkers { get; }
    
    public MultiMarkerMovedEventArgs(IReadOnlyList<MarkerMovedInfo> movedMarkers)
    {
        MovedMarkers = movedMarkers;
    }
}

public class MarkerMovedInfo
{
    public Marker Marker { get; }
    public double OldAngle { get; }
    public double NewAngle { get; }
    
    public MarkerMovedInfo(Marker marker, double oldAngle, double newAngle)
    {
        Marker = marker;
        OldAngle = oldAngle;
        NewAngle = newAngle;
    }
}

public class ZoomChangedEventArgs : EventArgs
{
    public double ZoomLevel { get; }
    public Point ZoomCenter { get; }
    
    public ZoomChangedEventArgs(double zoomLevel, Point zoomCenter)
    {
        ZoomLevel = zoomLevel;
        ZoomCenter = zoomCenter;
    }
}

public class MarkerRemovalPreviewEventArgs : EventArgs
{
    public Marker Marker { get; }
    public bool IsBeingRemoved { get; }
    
    public MarkerRemovalPreviewEventArgs(Marker marker, bool isBeingRemoved)
    {
        Marker = marker;
        IsBeingRemoved = isBeingRemoved;
    }
}

public class RotationGestureEventArgs : EventArgs
{
    public double RotationDelta { get; }
    public double NewRotation { get; }
    
    public RotationGestureEventArgs(double rotationDelta, double newRotation)
    {
        RotationDelta = rotationDelta;
        NewRotation = newRotation;
    }
}

public class HapticFeedbackEventArgs : EventArgs
{
    public HapticFeedbackType FeedbackType { get; }
    public double Intensity { get; }
    
    public HapticFeedbackEventArgs(HapticFeedbackType feedbackType, double intensity = 1.0)
    {
        FeedbackType = feedbackType;
        Intensity = Math.Max(0.0, Math.Min(1.0, intensity));
    }
}

public enum HapticFeedbackType
{
    MarkerPlace,
    MarkerSelect,
    MarkerRemove,
    GestureStart,
    GestureEnd,
    ZoomChange,
    Rotation,
    RotationStart,
    GridSnap,
    BoundaryHit
}

#endregion