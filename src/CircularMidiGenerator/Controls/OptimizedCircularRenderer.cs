using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using CircularMidiGenerator.Core.Models;
using CircularMidiGenerator.Services;

namespace CircularMidiGenerator.Controls;

/// <summary>
/// High-performance renderer for the circular canvas using Skia
/// </summary>
public class OptimizedCircularRenderer : ICustomDrawOperation
{
    private readonly CircularCanvasRenderData _renderData;
    private readonly PerformanceMonitor? _performanceMonitor;
    private static readonly Dictionary<string, SKPaint> _paintCache = new();
    private static readonly Dictionary<int, SKPath> _pathCache = new();

    public OptimizedCircularRenderer(CircularCanvasRenderData renderData, PerformanceMonitor? performanceMonitor = null)
    {
        _renderData = renderData;
        _performanceMonitor = performanceMonitor;
    }

    public Rect Bounds => _renderData.Bounds;

    public bool HitTest(Point p) => _renderData.Bounds.Contains(p);

    public bool Equals(ICustomDrawOperation? other) => false; // Always redraw for animations

    public void Dispose()
    {
        // Cleanup if needed
    }

    public void Render(ImmediateDrawingContext context)
    {
        var renderStart = Environment.TickCount64;

        if (context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is ISkiaSharpApiLeaseFeature leaseFeature)
        {
            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            
            RenderOptimized(canvas);
        }

        var renderTime = Environment.TickCount64 - renderStart;
        _performanceMonitor?.RecordFrameTime(renderTime);
    }

    private void RenderOptimized(SKCanvas canvas)
    {
        canvas.Save();

        try
        {
            // Set up coordinate system
            var center = new SKPoint((float)_renderData.Center.X, (float)_renderData.Center.Y);
            var radius = (float)_renderData.Radius;

            // Draw disk background with caching
            DrawDiskBackground(canvas, center, radius);

            // Draw quantization grid if enabled
            if (_renderData.IsQuantizationEnabled && _renderData.GridLines?.Any() == true)
            {
                DrawQuantizationGrid(canvas, center, radius);
            }

            // Draw markers with optimized batching
            if (_renderData.Markers?.Any() == true)
            {
                DrawMarkersOptimized(canvas, center, radius);
            }

            // Draw playhead
            DrawPlayhead(canvas, center, radius);

            // Draw selection indicators
            if (_renderData.SelectedMarkers?.Any() == true)
            {
                DrawSelectionIndicators(canvas, center, radius);
            }
        }
        finally
        {
            canvas.Restore();
        }
    }

    private void DrawDiskBackground(SKCanvas canvas, SKPoint center, float radius)
    {
        var diskPaint = GetOrCreatePaint("disk_background", () => new SKPaint
        {
            Color = SKColor.Parse("#2A2A2A"),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        });

        var borderPaint = GetOrCreatePaint("disk_border", () => new SKPaint
        {
            Color = SKColor.Parse("#404040"),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        });

        // Draw main disk
        canvas.DrawCircle(center, radius, diskPaint);
        canvas.DrawCircle(center, radius, borderPaint);

        // Draw center dot
        var centerPaint = GetOrCreatePaint("center_dot", () => new SKPaint
        {
            Color = SKColor.Parse("#666666"),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        });

        canvas.DrawCircle(center, 4, centerPaint);
    }

    private void DrawQuantizationGrid(SKCanvas canvas, SKPoint center, float radius)
    {
        var gridPaint = GetOrCreatePaint("grid_lines", () => new SKPaint
        {
            Color = SKColor.Parse("#404040"),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0)
        });

        foreach (var gridAngle in _renderData.GridLines!)
        {
            var adjustedAngle = gridAngle + _renderData.DiskRotation;
            var radians = adjustedAngle * Math.PI / 180.0;
            
            var startX = center.X + (float)(Math.Cos(radians) * 20);
            var startY = center.Y + (float)(Math.Sin(radians) * 20);
            var endX = center.X + (float)(Math.Cos(radians) * radius);
            var endY = center.Y + (float)(Math.Sin(radians) * radius);

            canvas.DrawLine(startX, startY, endX, endY, gridPaint);
        }
    }

    private void DrawMarkersOptimized(SKCanvas canvas, SKPoint center, float radius)
    {
        // Group markers by lane for batch rendering
        var markersByLane = _renderData.Markers!
            .Where(m => _renderData.Lanes?.Any(l => l.Id == m.Lane) == true)
            .GroupBy(m => m.Lane)
            .ToList();

        foreach (var laneGroup in markersByLane)
        {
            var lane = _renderData.Lanes!.First(l => l.Id == laneGroup.Key);
            DrawLaneMarkers(canvas, center, radius, laneGroup, lane);
        }
    }

    private void DrawLaneMarkers(SKCanvas canvas, SKPoint center, float radius, 
        IGrouping<int, Marker> markers, Lane lane)
    {
        var laneRadius = radius - (lane.Id * 15) - 30; // Offset by lane
        if (laneRadius < 20) return; // Skip if too small

        foreach (var marker in markers)
        {
            DrawSingleMarker(canvas, center, laneRadius, marker, lane);
        }
    }

    private void DrawSingleMarker(SKCanvas canvas, SKPoint center, float radius, Marker marker, Lane lane)
    {
        var adjustedAngle = marker.Angle + _renderData.DiskRotation;
        var radians = adjustedAngle * Math.PI / 180.0;
        
        var markerX = center.X + (float)(Math.Cos(radians) * radius);
        var markerY = center.Y + (float)(Math.Sin(radians) * radius);
        var markerCenter = new SKPoint(markerX, markerY);

        // Get marker size based on velocity
        var markerSize = 4 + (marker.Velocity / 127.0f * 8); // 4-12 pixels

        // Create marker paint with color and velocity-based alpha
        var alpha = (byte)(180 + (marker.Velocity / 127.0f * 75)); // 180-255
        var markerColor = SKColor.Parse(marker.Color.ToString()).WithAlpha(alpha);
        
        var markerPaint = new SKPaint
        {
            Color = markerColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        // Draw marker with glow effect for active markers
        if (marker.IsActive)
        {
            var glowPaint = new SKPaint
            {
                Color = markerColor.WithAlpha(100),
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 3)
            };
            canvas.DrawCircle(markerCenter, markerSize + 2, glowPaint);
            glowPaint.Dispose();
        }

        canvas.DrawCircle(markerCenter, markerSize, markerPaint);
        markerPaint.Dispose();

        // Draw lane indicator (small colored ring)
        if (lane.ThemeColor != default)
        {
            var ringPaint = new SKPaint
            {
                Color = SKColor.Parse(lane.ThemeColor.ToString()),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1
            };
            canvas.DrawCircle(markerCenter, markerSize + 2, ringPaint);
            ringPaint.Dispose();
        }
    }

    private void DrawPlayhead(SKCanvas canvas, SKPoint center, float radius)
    {
        var playheadAngle = _renderData.PlayheadAngle;
        var radians = (playheadAngle - 90) * Math.PI / 180.0; // -90 to start at top

        var playheadPaint = GetOrCreatePaint("playhead", () => new SKPaint
        {
            Color = SKColor.Parse("#FF4444"),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3
        });

        var startX = center.X + (float)(Math.Cos(radians) * 15);
        var startY = center.Y + (float)(Math.Sin(radians) * 15);
        var endX = center.X + (float)(Math.Cos(radians) * (radius + 10));
        var endY = center.Y + (float)(Math.Sin(radians) * (radius + 10));

        canvas.DrawLine(startX, startY, endX, endY, playheadPaint);

        // Draw playhead triangle
        var trianglePath = GetOrCreatePath(0, () =>
        {
            var path = new SKPath();
            path.MoveTo(endX, endY);
            path.LineTo(endX - 8, endY - 4);
            path.LineTo(endX - 8, endY + 4);
            path.Close();
            return path;
        });

        var trianglePaint = GetOrCreatePaint("playhead_triangle", () => new SKPaint
        {
            Color = SKColor.Parse("#FF4444"),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        });

        canvas.Save();
        canvas.Translate(center.X, center.Y);
        canvas.RotateRadians((float)radians);
        canvas.Translate(-center.X, -center.Y);
        canvas.DrawPath(trianglePath, trianglePaint);
        canvas.Restore();
    }

    private void DrawSelectionIndicators(SKCanvas canvas, SKPoint center, float radius)
    {
        var selectionPaint = GetOrCreatePaint("selection", () => new SKPaint
        {
            Color = SKColor.Parse("#00BFFF"),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            PathEffect = SKPathEffect.CreateDash(new float[] { 3, 3 }, 0)
        });

        foreach (var marker in _renderData.SelectedMarkers!)
        {
            var lane = _renderData.Lanes?.FirstOrDefault(l => l.Id == marker.Lane);
            if (lane == null) continue;

            var laneRadius = radius - (lane.Id * 15) - 30;
            var adjustedAngle = marker.Angle + _renderData.DiskRotation;
            var radians = adjustedAngle * Math.PI / 180.0;
            
            var markerX = center.X + (float)(Math.Cos(radians) * laneRadius);
            var markerY = center.Y + (float)(Math.Sin(radians) * laneRadius);
            
            var selectionRadius = 8 + (marker.Velocity / 127.0f * 8);
            canvas.DrawCircle(markerX, markerY, selectionRadius, selectionPaint);
        }
    }

    private static SKPaint GetOrCreatePaint(string key, Func<SKPaint> factory)
    {
        if (!_paintCache.TryGetValue(key, out var paint))
        {
            paint = factory();
            _paintCache[key] = paint;
        }
        return paint;
    }

    private static SKPath GetOrCreatePath(int key, Func<SKPath> factory)
    {
        if (!_pathCache.TryGetValue(key, out var path))
        {
            path = factory();
            _pathCache[key] = path;
        }
        return path;
    }

    /// <summary>
    /// Clear cached resources (call when disposing or when significant changes occur)
    /// </summary>
    public static void ClearCache()
    {
        foreach (var paint in _paintCache.Values)
        {
            paint.Dispose();
        }
        _paintCache.Clear();

        foreach (var path in _pathCache.Values)
        {
            path.Dispose();
        }
        _pathCache.Clear();
    }
}

/// <summary>
/// Data structure for optimized rendering
/// </summary>
public class CircularCanvasRenderData
{
    public Rect Bounds { get; set; }
    public Point Center { get; set; }
    public double Radius { get; set; }
    public double PlayheadAngle { get; set; }
    public double DiskRotation { get; set; }
    public bool IsQuantizationEnabled { get; set; }
    public IList<double>? GridLines { get; set; }
    public IList<Marker>? Markers { get; set; }
    public IList<Lane>? Lanes { get; set; }
    public IList<Marker>? SelectedMarkers { get; set; }
}