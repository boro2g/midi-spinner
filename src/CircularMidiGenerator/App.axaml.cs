using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CircularMidiGenerator.ViewModels;
using CircularMidiGenerator.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CircularMidiGenerator.Core.Services;

namespace CircularMidiGenerator;

public partial class App : Application
{
    public static ServiceProvider? ServiceProvider { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ConfigureServices();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainViewModel = ServiceProvider?.GetRequiredService<MainViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Core services (interfaces will be implemented in later tasks)
        // services.AddSingleton<IMidiService, MidiService>();
        // services.AddSingleton<ITimingService, TimingService>();
        // services.AddSingleton<IQuantizationService, QuantizationService>();

        // ViewModels
        services.AddTransient<MainViewModel>();

        ServiceProvider = services.BuildServiceProvider();
    }
}