using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace CircularMidiGenerator.Logging;

/// <summary>
/// Extensions for configuring application logging
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Configure comprehensive logging for the application
    /// </summary>
    public static IServiceCollection AddApplicationLogging(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            
            // Console logging for development
            builder.AddConsole(options =>
            {
                options.IncludeScopes = true;
                options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] ";
            });

            // Debug output for Visual Studio
            builder.AddDebug();

            // Set minimum log levels
#if DEBUG
            builder.SetMinimumLevel(LogLevel.Debug);
            
            // More verbose logging for specific categories in debug mode
            builder.AddFilter("CircularMidiGenerator.Core.Services.MidiService", LogLevel.Trace);
            builder.AddFilter("CircularMidiGenerator.Core.Services.TimingService", LogLevel.Trace);
            builder.AddFilter("CircularMidiGenerator.Core.Services.MarkerTriggerService", LogLevel.Debug);
#else
            builder.SetMinimumLevel(LogLevel.Information);
            
            // Reduce noise from framework components in release
            builder.AddFilter("Microsoft", LogLevel.Warning);
            builder.AddFilter("System", LogLevel.Warning);
            builder.AddFilter("Avalonia", LogLevel.Warning);
#endif
        });

        return services;
    }
}