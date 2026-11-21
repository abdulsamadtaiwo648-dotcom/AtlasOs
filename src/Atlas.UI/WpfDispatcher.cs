using System.Windows;
using System.Windows.Threading;
using Atlas.App.Mvvm;

namespace AtlasUI;

/// <summary>Runs view-model updates on the WPF UI thread.</summary>
public sealed class WpfDispatcher : IUiDispatcher
{
    public void Post(Action action)
    {
        Dispatcher dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        if (dispatcher.CheckAccess())
            action();
        else
            dispatcher.BeginInvoke(action);
    }
}
