using System.ComponentModel;
using System.Windows;
using Atlas.App;

namespace AtlasUI;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _shell;

    public MainWindow(ShellViewModel shell)
    {
        InitializeComponent();

        _shell = shell;
        DataContext = shell;

        Loaded += (_, _) => _shell.OnStarted();
        Closing += OnClosing;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        // Stop listening before the window goes away.
        _shell.Voice.Stop();
    }
}
