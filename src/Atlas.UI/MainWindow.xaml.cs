using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using Atlas.Core.Interfaces;
using Atlas.Speech.Interfaces;
using Atlas.Speech.SherpaSTT;
using Atlas.Speech.WakeWord;

namespace AtlasUI;

public partial class MainWindow : Window
{
    // ── Services ────────────────────────────────────────────────────────
    private readonly IAtlasKernel      _kernel;
    private readonly ISpeechService    _speech;
    private readonly SherpaRecognizer  _recognizer;
    private readonly WakeWordDetector  _wakeWord;

    // ── State ────────────────────────────────────────────────────────────
    private enum VuiState { Idle, Listening, Transcribing, Thinking, Speaking }
    private VuiState _state = VuiState.Idle;

    // ── Waveform animation ───────────────────────────────────────────────
    private DispatcherTimer? _waveTimer;
    private readonly Random  _rng = new();

    // ── Colors ───────────────────────────────────────────────────────────
    private static readonly Color IdleColor     = Color.FromRgb(0x44, 0x66, 0xFF);
    private static readonly Color ListenColor   = Color.FromRgb(0xFF, 0x44, 0x66);
    private static readonly Color ThinkColor    = Color.FromRgb(0xFF, 0xAA, 0x22);
    private static readonly Color SpeakColor    = Color.FromRgb(0x44, 0xFF, 0xBB);

    public MainWindow(IAtlasKernel kernel, ISpeechService speech,
                       SherpaRecognizer recognizer, WakeWordDetector wakeWord)
    {
        InitializeComponent();
        _kernel     = kernel;
        _speech     = speech;
        _recognizer = recognizer;
        _wakeWord   = wakeWord;

        // Hook VUI events from recognizer
        _recognizer.ListeningStarted    += () => Dispatch(() => SetState(VuiState.Listening));
        _recognizer.SpeechDetected      += () => Dispatch(() => PulseOrbOnce());
        _recognizer.TranscribingStarted += () => Dispatch(() => SetState(VuiState.Transcribing));
        _recognizer.ListeningEnded      += () => Dispatch(() => { /* handled after await */ });

        // Wake word → trigger mic session automatically
        _wakeWord.WakeWordDetected += keyword =>
        {
            Dispatch(() =>
            {
                AddSystemMessage($"🎙 Wake word detected: '{keyword}'");
                if (_state == VuiState.Idle)
                    _ = HandleVoiceAsync();
            });
        };

        // Start idle pulse + wake word listener
        SetState(VuiState.Idle);
        _wakeWord.Start();
        AddSystemMessage("Atlas online. Say \"Hey Atlas\" or click the orb to speak.");
    }

    // ══════════════════════════════════════════════════════════════════════
    // VUI State Machine
    // ══════════════════════════════════════════════════════════════════════
    private void SetState(VuiState state)
    {
        _state = state;

        // Stop all storyboards
        StopAllStoryboards();

        // Control wake word listener state to avoid mic hardware contention
        if (state == VuiState.Idle)
            _wakeWord.Start();
        else
            _wakeWord.Stop();

        switch (state)
        {
            case VuiState.Idle:
                SetOrbColor(IdleColor);
                OrbStatusText.Text = "Click orb or press Enter";
                StartWaveform(false);
                ((Storyboard)Resources["PulseIdle"]).Begin(this, true);
                StatusLabel.Text = " — Ready";
                break;

            case VuiState.Listening:
                SetOrbColor(ListenColor);
                OrbStatusText.Text = "Listening...";
                StartWaveform(true, ListenColor);
                ((Storyboard)Resources["PulseListen"]).Begin(this, true);
                StatusLabel.Text = " — Listening";
                break;

            case VuiState.Transcribing:
                SetOrbColor(ThinkColor);
                OrbStatusText.Text = "Transcribing...";
                StartWaveform(true, ThinkColor);
                StatusLabel.Text = " — Processing";
                break;

            case VuiState.Thinking:
                SetOrbColor(ThinkColor);
                OrbStatusText.Text = "Thinking...";
                StartWaveform(false);
                ((Storyboard)Resources["PulseIdle"]).Begin(this, true);
                StatusLabel.Text = " — Thinking";
                break;

            case VuiState.Speaking:
                SetOrbColor(SpeakColor);
                OrbStatusText.Text = "Speaking...";
                StartWaveform(true, SpeakColor);
                ((Storyboard)Resources["PulseSpeak"]).Begin(this, true);
                StatusLabel.Text = " — Speaking";
                break;
        }
    }

    private void StopAllStoryboards()
    {
        foreach (string key in new[] { "PulseIdle", "PulseListen", "PulseSpeak" })
        {
            try { ((Storyboard)Resources[key]).Stop(this); } catch { }
        }
    }

    private void SetOrbColor(Color c)
    {
        // Update orb fill gradient
        if (OrbElement.Fill is RadialGradientBrush brush)
        {
            brush = brush.Clone();
            brush.GradientStops[1].Color = Color.FromArgb(0xFF, (byte)(c.R / 2), (byte)(c.G / 2), (byte)(c.B / 2));
            OrbElement.Fill = brush;
        }

        // Update ring stroke and glow
        OrbRing.Stroke = new SolidColorBrush(c);
        if (OrbRing.Effect is DropShadowEffect glow)
            glow.Color = c;

        // Update glow on orb
        OrbElement.Effect = new DropShadowEffect
        {
            Color       = c,
            BlurRadius  = 40,
            ShadowDepth = 0,
            Opacity     = 0.85
        };
    }

    private void PulseOrbOnce()
    {
        var anim = new DoubleAnimation(1.3, 1.0, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        OrbScale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
        OrbScale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
    }

    // ── Animated waveform bars ─────────────────────────────────────────────
    private void StartWaveform(bool animate, Color? color = null)
    {
        _waveTimer?.Stop();
        var bars = new[] { Bar1, Bar2, Bar3, Bar4, Bar5, Bar6, Bar7 };
        double[] baseH = { 8, 16, 32, 48, 32, 16, 8 };
        var fill = color.HasValue ? new SolidColorBrush(color.Value) : new SolidColorBrush(IdleColor);

        foreach (var b in bars)
        {
            b.Fill = fill.Clone();
        }

        if (!animate)
        {
            for (int i = 0; i < bars.Length; i++)
                bars[i].Height = baseH[i];
            return;
        }

        _waveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _waveTimer.Tick += (_, _) =>
        {
            for (int i = 0; i < bars.Length; i++)
            {
                double h = baseH[i] + _rng.NextDouble() * 36 - 8;
                h = Math.Max(4, Math.Min(64, h));
                var ha = new DoubleAnimation(h, TimeSpan.FromMilliseconds(100))
                {
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };
                bars[i].BeginAnimation(FrameworkElement.HeightProperty, ha);
            }
        };
        _waveTimer.Start();
    }

    private void Dispatch(Action action) => Dispatcher.Invoke(action);

    // ══════════════════════════════════════════════════════════════════════
    // Orb click = voice mode
    // ══════════════════════════════════════════════════════════════════════
    private async void OrbButton_Click(object sender, RoutedEventArgs e)
    {
        await HandleVoiceAsync();
    }

    private async System.Threading.Tasks.Task HandleVoiceAsync()
    {
        if (_state is VuiState.Listening or VuiState.Transcribing or VuiState.Thinking) return;

        // Force stop wake word during active voice recognition session
        _wakeWord.Stop();

        try
        {
            // ListenAsync fires VUI events internally through recognizer
            string input = await _speech.ListenAsync();

            if (string.IsNullOrWhiteSpace(input))
            {
                SetState(VuiState.Idle);
                AddSystemMessage("I didn't catch that — please try again.");
                return;
            }

            AddUserMessage(input);
            await ProcessInput(input);
        }
        catch (Exception ex)
        {
            SetState(VuiState.Idle);
            AddSystemMessage($"Voice error: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Text input
    // ══════════════════════════════════════════════════════════════════════
    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        string text = InputBox.Text.Trim();
        if (string.IsNullOrEmpty(text) || _state is VuiState.Thinking) return;
        InputBox.Clear();
        PlaceholderText.Visibility = Visibility.Visible;
        AddUserMessage(text);
        await ProcessInput(text);
    }

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) SendButton_Click(sender, new RoutedEventArgs());
    }

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        PlaceholderText.Visibility = string.IsNullOrEmpty(InputBox.Text)
            ? Visibility.Visible : Visibility.Collapsed;
    }

    // ══════════════════════════════════════════════════════════════════════
    // Core processing pipeline
    // ══════════════════════════════════════════════════════════════════════
    private async System.Threading.Tasks.Task ProcessInput(string input)
    {
        SetState(VuiState.Thinking);
        InputBox.IsEnabled = false;
        SendButton.IsEnabled = false;

        try
        {
            string response = await _kernel.ProcessAsync(input);
            AddAtlasMessage(response);

            SetState(VuiState.Speaking);
            await _speech.SpeakAsync(response);
        }
        catch (Exception ex)
        {
            AddSystemMessage($"Error: {ex.Message}");
        }
        finally
        {
            SetState(VuiState.Idle);
            InputBox.IsEnabled  = true;
            SendButton.IsEnabled = true;
            InputBox.Focus();
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Message bubbles
    // ══════════════════════════════════════════════════════════════════════
    private void AddUserMessage(string text)
    {
        var bubble = MakeBubble(text, isUser: true);
        MessagePanel.Children.Add(bubble);
        ChatScroll.ScrollToBottom();
    }

    private void AddAtlasMessage(string text)
    {
        var bubble = MakeBubble(text, isUser: false);
        MessagePanel.Children.Add(bubble);
        ChatScroll.ScrollToBottom();
    }

    private void AddSystemMessage(string text)
    {
        var tb = new TextBlock
        {
            Text                = text,
            Foreground          = new SolidColorBrush(Color.FromRgb(0x44, 0x55, 0x77)),
            FontSize            = 12,
            FontFamily          = new FontFamily("Segoe UI"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin              = new Thickness(0, 8, 0, 8),
            Opacity             = 0
        };
        MessagePanel.Children.Add(tb);
        ChatScroll.ScrollToBottom();
        FadeIn(tb);
    }

    private UIElement MakeBubble(string text, bool isUser)
    {
        var outerBorder = new Border
        {
            HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            Margin              = new Thickness(isUser ? 80 : 0, 4, isUser ? 0 : 80, 4),
            MaxWidth            = 680,
            Opacity             = 0
        };

        // Gradient border line on the left for Atlas
        var innerBorder = new Border
        {
            CornerRadius    = new CornerRadius(isUser ? 18 : 4, 18, 18, isUser ? 4 : 18),
            Padding         = new Thickness(16, 12, 16, 12),
            BorderThickness = new Thickness(isUser ? 0 : 2, 0, 0, 0),
            BorderBrush     = isUser ? null : new SolidColorBrush(Color.FromRgb(0x33, 0x66, 0xFF)),
        };

        innerBorder.Background = isUser
            ? new LinearGradientBrush(
                Color.FromRgb(0x1A, 0x33, 0x88),
                Color.FromRgb(0x0D, 0x1A, 0x55),
                new Point(0, 0), new Point(1, 1))
            : new SolidColorBrush(Color.FromRgb(0x0E, 0x11, 0x28));

        if (isUser)
        {
            innerBorder.Effect = new DropShadowEffect
            {
                Color       = Color.FromRgb(0x33, 0x55, 0xFF),
                BlurRadius  = 12,
                ShadowDepth = 0,
                Opacity     = 0.3
            };
        }

        var stack = new StackPanel();

        if (!isUser)
        {
            var senderLabel = new TextBlock
            {
                Text       = "ATLAS",
                Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x88, 0xFF)),
                FontSize   = 10,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI"),
                Margin     = new Thickness(0, 0, 0, 6)
            };
            stack.Children.Add(senderLabel);
        }

        var messageText = new TextBlock
        {
            Text        = text,
            Foreground  = new SolidColorBrush(Color.FromRgb(0xBB, 0xCC, 0xEE)),
            FontSize    = 14,
            FontFamily  = new FontFamily("Segoe UI"),
            TextWrapping= TextWrapping.Wrap,
            LineHeight  = 22
        };
        stack.Children.Add(messageText);

        // Timestamp
        var ts = new TextBlock
        {
            Text       = DateTime.Now.ToString("HH:mm"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x44, 0x66)),
            FontSize   = 10,
            FontFamily = new FontFamily("Segoe UI"),
            Margin     = new Thickness(0, 6, 0, 0),
            HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left
        };
        stack.Children.Add(ts);

        innerBorder.Child = stack;
        outerBorder.Child = innerBorder;

        FadeIn(outerBorder);
        return outerBorder;
    }

    private static void FadeIn(UIElement el)
    {
        var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(350))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        el.BeginAnimation(OpacityProperty, anim);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Window chrome
    // ══════════════════════════════════════════════════════════════════════
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void MinBtn_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();
}