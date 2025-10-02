using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CircularMidiGenerator.ViewModels;

namespace CircularMidiGenerator.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}