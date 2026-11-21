using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Atlas.App;

namespace AtlasUI.Views;

public partial class ChatView : UserControl
{
    // True while the user is reading the bottom of the chat; we only follow new text then.
    private bool _followBottom = true;

    public ChatView()
    {
        InitializeComponent();
    }

    private void Scroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.ExtentHeightChange == 0)
        {
            // The user scrolled: follow new text only if they are near the bottom.
            _followBottom = Scroller.VerticalOffset >= Scroller.ScrollableHeight - 24;
        }
        else if (_followBottom)
        {
            // New text arrived while following: stay at the bottom.
            Scroller.ScrollToEnd();
        }
    }

    // Enter sends, Shift+Enter adds a new line.
    private void Input_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            return;

        if (DataContext is ShellViewModel shell && shell.Chat.SendCommand.CanExecute(null))
        {
            shell.Chat.SendCommand.Execute(null);
            _followBottom = true;
        }

        e.Handled = true;
    }
}
