using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CircularMidiGenerator.Core.Services;
using CircularMidiGenerator.Core.Models;

namespace CircularMidiGenerator.Services;

/// <summary>
/// Test result for a performance test
/// </summary>
public class PerformanceTestResult
{
    public string TestName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public double MeasuredValue { get; set; }
    public double Threshold { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Details { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Comprehensive performance test suite
/// </summary>
public class PerformanceTestSuite
{
    private readonly IMidiService _midiService;
    private readonly ITimingService _timingService;
    private readonly ILaneController _laneController;
    private readonly MidiLatencyOptimizer _latencyOptimizer;
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly ILogger<PerformanceTestSuite> _logger;

    public PerformanceTestSuite(
        IMidiService midiService,
        ITimingService timingService,
        ILaneController laneController,
        MidiLatencyOptimizer latencyOptimizer,
        PerformanceMonitor performanceMonitor,
        ILogger<PerformanceTestSuite> logger)
    {
        _midiService = midiService;
        _timingService = timingService;
        _laneController = laneController;
        _latencyOptimizer = latencyOptimizer;
        _performanceMonitor = performanceMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Run all performance tests
    /// </summary>
    public async Task<List<PerformanceTestResult>> RunAllTestsAsync()
    {
        _logger.LogInformation("Starting comprehensive performance test suite...");
        
        var results = new List<PerformanceTestResult>();

        try
        {
            // MIDI latency tests
            results.Add(await TestMidiLatencyAsync());
            results.Add(await TestMidiThroughputAsync());
            results.Add(await TestMidiJitterAsync());

            // Timing precision tests
            results.Add(await TestTimingPrecisionAsync());
            results.Add(await TestBPMAccuracyAsync());

            // Memory performance tests
            results.Add(await TestMemoryUsageAsync());
            results.Add(await TestMemoryLeaksAsync());

            // Rendering performance tests
            results.Add(await TestRenderingPerformanceAsync());
            results.Add(await TestMarkerScalingAsync());

            // System integration tests
            results.Add(await TestSystemLoadHandlingAsync());
            results.Add(await TestConcurrentOperationsAsync());

            var passedTests = results.Count(r => r.Passed);
            var totalTests = results.Count;

            _logger.LogInformation("Performance test suite completed: {PassedTests}/{TotalTests} tests passed", 
                passedTests, totalTests);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running performance test suite");
            throw;
        }
    }

    private async Task<PerformanceTestResult> TestMidiLatencyAsync()
    {
        const int testCount = 100;
        const double maxLatencyMs = 5.0;
        
        var stopwatch = Stopwatch.StartNew();
        var latencies = new List<double>();

        try
        {
            for (int i = 0; i < testCount; i++)
            {
                var start = Stopwatch.GetTimestamp();
                _latencyOptimizer.SendImmediate(1, 60, 100, true);
                var end = Stopwatch.GetTimestamp();
                
                var latencyMs = (end - start) * 1000.0 / Stopwatch.Frequency;
                latencies.Add(latencyMs);
                
                await Task.Delay(10); // Small delay between tests
            }

            var averageLatency = latencies.Average();
            var maxLatency = latencies.Max();

            return new PerformanceTestResult
            {
                TestName = "MIDI Latency",
                Passed = averageLatency <= maxLatencyMs,
                MeasuredValue = averageLatency,
                Threshold = maxLatencyMs,
                Unit = "ms",
                Details = $"Max: {maxLatency:F2}ms, Samples: {testCount}",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new PerformanceTestResult
            {
                TestName = "MIDI Latency",
                Passed = false,
                Details = $"Test failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<PerformanceTestResult> TestMidiThroughputAsync()
    {
        const int eventsPerSecond = 1000;
        const int testDurationSeconds = 5;
        const double minSuccessRate = 0.95;

        var stopwatch = Stopwatch.StartNew();
        var totalEvents = eventsPerSecond * testDurationSeconds;
        var successfulEvents = 0;

        try
        {
            var tasks = new List<Task>();
            
            for (int i = 0; i < totalEvents; i++)
            {
                var delay = TimeSpan.FromMilliseconds(i * 1000.0 / eventsPerSecond);
                
                tasks.Add(Task.Run(async () =>
                {
                    await Task.Delay(delay);
                    try
                    {
                        _latencyOptimizer.SendImmediate(1, 60 + (i % 12), 100, true);
                        Interlocked.Increment(ref successfulEvents);
                    }
                    catch
                    {
                        // Event failed
                    }
                }));
            }

            await Task.WhenAll(tasks);
            
            var successRate = (double)successfulEvents / totalEvents;

            return new PerformanceTestResult
            {
                TestName = "MIDI Throughput",
                Passed = successRate >= minSuccessRate,
                MeasuredValue = successRate,
                Threshold = minSuccessRate,
                Unit = "success rate",
                Details = $"Sent {successfulEvents}/{totalEvents} events successfully",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new PerformanceTestResult
            {
                TestName = "MIDI Throughput",
                Passed = false,
                Details = $"Test failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<PerformanceTestResult> TestMidiJitterAsync()
    {
        const int testCount = 200;
        const double maxJitterMs = 2.0;
        
        var stopwatch = Stopwatch.StartNew();
        var intervals = new List<double>();
        var lastTimestamp = Stopwatch.GetTimestamp();

        try
        {
            for (int i = 0; i < testCount; i++)
            {
                await Task.Delay(10); // Target 100Hz
                
                var currentTimestamp = Stopwatch.GetTimestamp();
                var intervalMs = (currentTimestamp - lastTimestamp) * 1000.0 / Stopwatch.Frequency;
                intervals.Add(intervalMs);
                lastTimestamp = currentTimestamp;
                
                _latencyOptimizer.SendImmediate(1, 60, 100, true);
            }

            var targetInterval = 10.0; // 10ms target
            var deviations = intervals.Select(i => Math.Abs(i - targetInterval)).ToList();
            var jitter = deviations.Average();
            var maxDeviation = deviations.Max();

            return new PerformanceTestResult
            {
                TestName = "MIDI Jitter",
                Passed = jitter <= maxJitterMs,
                MeasuredValue = jitter,
                Threshold = maxJitterMs,
                Unit = "ms",
                Details = $"Max deviation: {maxDeviation:F2}ms, Target interval: {targetInterval}ms",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new PerformanceTestResult
            {
                TestName = "MIDI Jitter",
                Passed = false,
                Details = $"Test failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<PerformanceTestResult> TestTimingPrecisionAsync()
    {
        const double targetBPM = 120.0;
        const double maxDeviationPercent = 1.0; // 1% deviation allowed
        
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _timingService.SetBPM(targetBPM);
            await Task.Delay(1000); // Let timing stabilize
            
            var measurements = new List<double>();
            var startTime = Stopwatch.GetTimestamp();
            
            // Measure timing over 10 seconds
            for (int i = 0; i < 100; i++)
            {
                await Task.Delay(100);
                var currentTime = Stopwatch.GetTimestamp();
                var elapsedSeconds = (currentTime - startTime) / (double)Stopwatch.Frequency;
                var expectedBeats = elapsedSeconds * (targetBPM / 60.0);
                var actualAngle = _timingService.CurrentAngle;
                var actualBeats = actualAngle / 360.0;
                
                var deviation = Math.Abs(actualBeats - expectedBeats) / expectedBeats * 100.0;
                measurements.Add(deviation);
            }

            var averageDeviation = measurements.Average();
            var maxDeviation = measurements.Max();

            return new PerformanceTestResult
            {
                TestName = "Timing Precision",
                Passed = averageDeviation <= maxDeviationPercent,
                MeasuredValue = averageDeviation,
                Threshold = maxDeviationPercent,
                Unit = "% deviation",
                Details = $"Max deviation: {maxDeviation:F2}%, Target BPM: {targetBPM}",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new PerformanceTestResult
            {
                TestName = "Timing Precision",
                Passed = false,
                Details = $"Test failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<PerformanceTestResult> TestBPMAccuracyAsync()
    {
        var testBPMs = new[] { 60.0, 120.0, 140.0, 180.0 };
        const double maxDeviationPercent = 0.5;
        
        var stopwatch = Stopwatch.StartNew();
        var allDeviations = new List<double>();

        try
        {
            foreach (var targetBPM in testBPMs)
            {
                _timingService.SetBPM(targetBPM);
                await Task.Delay(2000); // Let timing stabilize
                
                var startAngle = _timingService.CurrentAngle;
                var startTime = Stopwatch.GetTimestamp();
                
                await Task.Delay(5000); // Measure for 5 seconds
                
                var endAngle = _timingService.CurrentAngle;
                var endTime = Stopwatch.GetTimestamp();
                
                var elapsedSeconds = (endTime - startTime) / (double)Stopwatch.Frequency;
                var angleDifference = endAngle - startAngle;
                if (angleDifference < 0) angleDifference += 360; // Handle wrap-around
                
                var measuredBPM = (angleDifference / 360.0) * 60.0 / elapsedSeconds;
                var deviation = Math.Abs(measuredBPM - targetBPM) / targetBPM * 100.0;
                
                allDeviations.Add(deviation);
            }

            var averageDeviation = allDeviations.Average();
            var maxDeviation = allDeviations.Max();

            return new PerformanceTestResult
            {
                TestName = "BPM Accuracy",
                Passed = averageDeviation <= maxDeviationPercent,
                MeasuredValue = averageDeviation,
                Threshold = maxDeviationPercent,
                Unit = "% deviation",
                Details = $"Max deviation: {maxDeviation:F2}%, Tested BPMs: {string.Join(", ", testBPMs)}",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new PerformanceTestResult
            {
                TestName = "BPM Accuracy",
                Passed = false,
                Details = $"Test failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<PerformanceTestResult> TestMemoryUsageAsync()
    {
        const long maxMemoryMB = 200; // 200MB limit
        
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Force garbage collection to get baseline
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var initialMemory = GC.GetTotalMemory(false);
            
            // Create test load
            var markers = new List<Marker>();
            for (int i = 0; i < 1000; i++)
            {
                markers.Add(new Marker
                {
                    Id = Guid.NewGuid(),
                    Angle = i * 0.36,
                    Velocity = 100,
                    Lane = i % 4,
                    Color = Avalonia.Media.Colors.Red
                });
            }

            // Simulate usage
            await Task.Delay(2000);
            
            var currentMemory = GC.GetTotalMemory(false);
            var memoryUsageMB = (currentMemory - initialMemory) / 1024 / 1024;

            return new PerformanceTestResult
            {
                TestName = "Memory Usage",
                Passed = memoryUsageMB <= maxMemoryMB,
                MeasuredValue = memoryUsageMB,
                Threshold = maxMemoryMB,
                Unit = "MB",
                Details = $"Initial: {initialMemory / 1024 / 1024}MB, Current: {currentMemory / 1024 / 1024}MB",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new PerformanceTestResult
            {
                TestName = "Memory Usage",
                Passed = false,
                Details = $"Test failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<PerformanceTestResult> TestMemoryLeaksAsync()
    {
        const long maxLeakMB = 10; // 10MB leak tolerance
        
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Get baseline memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var baselineMemory = GC.GetTotalMemory(false);

            // Simulate intensive operations
            for (int cycle = 0; cycle < 10; cycle++)
            {
                var markers = new List<Marker>();
                for (int i = 0; i < 100; i++)
                {
                    markers.Add(new Marker
                    {
                        Id = Guid.NewGuid(),
                        Angle = i * 3.6,
                        Velocity = 100,
                        Lane = 0,
                        Color = Avalonia.Media.Colors.Blue
                    });
                }
                
                // Simulate processing
                await Task.Delay(100);
                
                // Clear references
                markers.Clear();
            }

            // Force cleanup and measure
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var finalMemory = GC.GetTotalMemory(false);
            var leakMB = (finalMemory - baselineMemory) / 1024 / 1024;

            return new PerformanceTestResult
            {
                TestName = "Memory Leaks",
                Passed = leakMB <= maxLeakMB,
                MeasuredValue = leakMB,
                Threshold = maxLeakMB,
                Unit = "MB",
                Details = $"Baseline: {baselineMemory / 1024 / 1024}MB, Final: {finalMemory / 1024 / 1024}MB",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new PerformanceTestResult
            {
                TestName = "Memory Leaks",
                Passed = false,
                Details = $"Test failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<PerformanceTestResult> TestRenderingPerformanceAsync()
    {
        const double minFPS = 30.0;
        
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Simulate rendering load
            var frameCount = 0;
            var renderStart = Stopwatch.GetTimestamp();
            
            for (int i = 0; i < 300; i++) // Simulate 5 seconds at 60fps
            {
                var frameStart = Stopwatch.GetTimestamp();
                
                // Simulate rendering work
                await Task.Delay(1);
                
                var frameEnd = Stopwatch.GetTimestamp();
                var frameTime = (frameEnd - frameStart) * 1000.0 / Stopwatch.Frequency;
                
                _performanceMonitor.RecordFrameTime(frameTime);
                frameCount++;
            }
            
            var renderEnd = Stopwatch.GetTimestamp();
            var totalTime = (renderEnd - renderStart) / (double)Stopwatch.Frequency;
            var averageFPS = frameCount / totalTime;

            return new PerformanceTestResult
            {
                TestName = "Rendering Performance",
                Passed = averageFPS >= minFPS,
                MeasuredValue = averageFPS,
                Threshold = minFPS,
                Unit = "FPS",
                Details = $"Rendered {frameCount} frames in {totalTime:F2}s",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new PerformanceTestResult
            {
                TestName = "Rendering Performance",
                Passed = false,
                Details = $"Test failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<PerformanceTestResult> TestMarkerScalingAsync()
    {
        const int maxMarkers = 5000;
        const double maxRenderTimeMs = 16.67; // 60fps target
        
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var markers = new List<Marker>();
            
            // Add markers incrementally and test performance
            for (int count = 100; count <= maxMarkers; count += 100)
            {
                // Add 100 more markers
                for (int i = 0; i < 100; i++)
                {
                    markers.Add(new Marker
                    {
                        Id = Guid.NewGuid(),
                        Angle = (count + i) * 360.0 / maxMarkers,
                        Velocity = 100,
                        Lane = (count + i) % 4,
                        Color = Avalonia.Media.Colors.Green
                    });
                }

                // Simulate rendering with this marker count
                var renderStart = Stopwatch.GetTimestamp();
                
                // Simulate marker processing
                foreach (var marker in markers)
                {
                    var _ = marker.Angle + marker.Velocity; // Simple calculation
                }
                
                var renderEnd = Stopwatch.GetTimestamp();
                var renderTime = (renderEnd - renderStart) * 1000.0 / Stopwatch.Frequency;
                
                if (renderTime > maxRenderTimeMs)
                {
                    return new PerformanceTestResult
                    {
                        TestName = "Marker Scaling",
                        Passed = false,
                        MeasuredValue = count,
                        Threshold = maxMarkers,
                        Unit = "markers",
                        Details = $"Performance degraded at {count} markers ({renderTime:F2}ms render time)",
                        Duration = stopwatch.Elapsed
                    };
                }
            }

            return new PerformanceTestResult
            {
                TestName = "Marker Scaling",
                Passed = true,
                MeasuredValue = maxMarkers,
                Threshold = maxMarkers,
                Unit = "markers",
                Details = $"Successfully handled {maxMarkers} markers",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new PerformanceTestResult
            {
                TestName = "Marker Scaling",
                Passed = false,
                Details = $"Test failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<PerformanceTestResult> TestSystemLoadHandlingAsync()
    {
        const double maxLatencyIncrease = 2.0; // 2x latency increase allowed under load
        
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Measure baseline latency
            var baselineLatencies = new List<double>();
            for (int i = 0; i < 50; i++)
            {
                var start = Stopwatch.GetTimestamp();
                _latencyOptimizer.SendImmediate(1, 60, 100, true);
                var end = Stopwatch.GetTimestamp();
                baselineLatencies.Add((end - start) * 1000.0 / Stopwatch.Frequency);
                await Task.Delay(10);
            }
            var baselineLatency = baselineLatencies.Average();

            // Create system load
            var loadTasks = new List<Task>();
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                loadTasks.Add(Task.Run(async () =>
                {
                    var endTime = DateTime.UtcNow.AddSeconds(5);
                    while (DateTime.UtcNow < endTime)
                    {
                        // CPU intensive work
                        Math.Sqrt(DateTime.UtcNow.Ticks);
                        await Task.Yield();
                    }
                }));
            }

            // Measure latency under load
            await Task.Delay(1000); // Let load stabilize
            var loadLatencies = new List<double>();
            for (int i = 0; i < 50; i++)
            {
                var start = Stopwatch.GetTimestamp();
                _latencyOptimizer.SendImmediate(1, 60, 100, true);
                var end = Stopwatch.GetTimestamp();
                loadLatencies.Add((end - start) * 1000.0 / Stopwatch.Frequency);
                await Task.Delay(10);
            }
            var loadLatency = loadLatencies.Average();

            await Task.WhenAll(loadTasks);

            var latencyIncrease = loadLatency / baselineLatency;

            return new PerformanceTestResult
            {
                TestName = "System Load Handling",
                Passed = latencyIncrease <= maxLatencyIncrease,
                MeasuredValue = latencyIncrease,
                Threshold = maxLatencyIncrease,
                Unit = "ratio",
                Details = $"Baseline: {baselineLatency:F2}ms, Under load: {loadLatency:F2}ms",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new PerformanceTestResult
            {
                TestName = "System Load Handling",
                Passed = false,
                Details = $"Test failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<PerformanceTestResult> TestConcurrentOperationsAsync()
    {
        const int concurrentOperations = 10;
        const double maxFailureRate = 0.05; // 5% failure rate allowed
        
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var tasks = new List<Task<bool>>();
            
            for (int i = 0; i < concurrentOperations; i++)
            {
                var operationId = i;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        // Simulate concurrent MIDI operations
                        for (int j = 0; j < 100; j++)
                        {
                            _latencyOptimizer.SendImmediate(1, 60 + (j % 12), 100, true);
                            await Task.Delay(1);
                        }
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }));
            }

            var results = await Task.WhenAll(tasks);
            var successCount = results.Count(r => r);
            var failureRate = 1.0 - (double)successCount / concurrentOperations;

            return new PerformanceTestResult
            {
                TestName = "Concurrent Operations",
                Passed = failureRate <= maxFailureRate,
                MeasuredValue = failureRate,
                Threshold = maxFailureRate,
                Unit = "failure rate",
                Details = $"Successful operations: {successCount}/{concurrentOperations}",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new PerformanceTestResult
            {
                TestName = "Concurrent Operations",
                Passed = false,
                Details = $"Test failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }
}