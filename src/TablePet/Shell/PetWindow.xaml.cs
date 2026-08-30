using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using TablePet.Config;
using TablePet.Pet;
using TablePet.Persistence;
using TablePet.Reminder;
using TablePet.Ui;

namespace TablePet.Shell;

public partial class PetWindow : Window
{
    private readonly PetController _pet;
    private readonly ReminderService _reminder;
    private readonly SettingsStore _settingsStore;
    private readonly SpriteAnimator _animator;
    private readonly DispatcherTimer _loop = new();
    private readonly TimeSpan _frameTime = TimeSpan.FromMilliseconds(16);
    private TimeSpan _reminderTick;
    private DateTime _lastTickUtc = DateTime.UtcNow;
    private bool _placed;

    public PetWindow(PetController pet, ReminderService reminder, SettingsStore settingsStore)
    {
        _pet = pet;
        _reminder = reminder;
        _settingsStore = settingsStore;
        _animator = new SpriteAnimator(SpriteAtlas.LoadDefault());

        InitializeComponent();

        _pet.Changed += ApplyPresentation;
        _reminder.ReminderDue += ShowReminderBubble;

        _loop.Interval = _frameTime;
        _loop.Tick += OnLoopTick;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    public void ApplyClickThrough()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var enable = _settingsStore.Current.ClickThrough
            && _pet.State != PetState.Dragged
            && !_reminder.IsBubbleVisible;
        ClickThroughService.SetClickThrough(hwnd, enable);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PlaceWindow();
        ApplyPresentation();
        ApplyClickThrough();
        _loop.Start();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _loop.Stop();
        _pet.Changed -= ApplyPresentation;
        _reminder.ReminderDue -= ShowReminderBubble;
        SavePosition();
    }

    private void PlaceWindow()
    {
        if (_placed)
        {
            return;
        }

        _placed = true;
        var settings = _settingsStore.Current;
        if (!double.IsNaN(settings.Left) && !double.IsNaN(settings.Top))
        {
            Left = settings.Left;
            Top = settings.Top;
        }
        else
        {
            var work = SystemParameters.WorkArea;
            var width = Width > 0 ? Width : PetConfig.FrameWidth;
            var height = Height > 0 ? Height : PetConfig.FrameHeight;
            Left = work.Left + ((work.Width - width) / 2);
            Top = work.Top + ((work.Height - height) / 2);
        }

        ScreenBounds.ClampToWorkArea(this);
    }

    private void OnLoopTick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        var delta = now - _lastTickUtc;
        _lastTickUtc = now;
        if (delta <= TimeSpan.Zero || delta > TimeSpan.FromMilliseconds(250))
        {
            delta = _frameTime;
        }

        var elapsedMs = (int)Math.Max(1, delta.TotalMilliseconds);

        if (!_reminder.IsBubbleVisible)
        {
            var ai = _pet.TickAi(elapsedMs);
            if (ai == PetAiCommand.StartWalk)
            {
                StartWalk();
            }
        }

        if (_pet.State == PetState.Walk)
        {
            StepWalk(delta);
        }

        _reminderTick += delta;
        if (_reminderTick >= TimeSpan.FromSeconds(1))
        {
            _reminderTick = TimeSpan.Zero;
            _reminder.Tick();
        }

        ApplyAnimatorClip();
        _animator.Advance(delta);
        if (_animator.HasFrames && _animator.CurrentFrame is not null)
        {
            PetImage.Source = _animator.CurrentFrame;
        }

        WindowActivation.KeepTopmostWithoutActivate(this);
    }

    private void StartWalk()
    {
        var work = ScreenBounds.GetWorkArea(this);
        var minX = work.Left;
        var maxX = work.Right - ActualWidth;
        _pet.RequestWalk(Left, minX, maxX);
    }

    private void StepWalk(TimeSpan delta)
    {
        double targetX;
        switch (_pet.State)
        {
            case PetState.Walk:
                targetX = _pet.WalkTargetX;
                break;
            default:
                return;
        }

        var step = PetConfig.WalkSpeedPixelsPerSecond * delta.TotalSeconds;
        Left = WalkBehavior.StepTowards(Left, targetX, step);
        ScreenBounds.ClampToWorkArea(this);
        if (WalkBehavior.HasArrived(Left, targetX))
        {
            _pet.NotifyWalkArrived();
        }
    }

    private void ApplyPresentation()
    {
        var useBitmap = _animator.HasFrames;
        PetImage.Visibility = useBitmap ? Visibility.Visible : Visibility.Collapsed;
        Placeholder.Visibility = useBitmap ? Visibility.Collapsed : Visibility.Visible;

        var scaleY = _pet.State switch
        {
            PetState.Sit => 0.78,
            PetState.Lie => 0.45,
            _ => 1.0
        };
        Placeholder.RenderTransformOrigin = new Point(0.5, 1);
        Placeholder.RenderTransform = new ScaleTransform(1, scaleY);

        ApplyAnimatorClip();
        ApplyClickThrough();
    }

    private void ApplyAnimatorClip()
    {
        if (_reminder.IsBubbleVisible)
        {
            _animator.SetClip("drink", PetFacing.Right);
            return;
        }

        _animator.SetClip(_pet.State, _pet.Facing);
    }

    private void ShowReminderBubble()
    {
        if (_pet.State != PetState.Dragged)
        {
            _pet.RequestSit();
        }

        BubbleText.Text = ReminderConfig.Message;
        Bubble.Visibility = Visibility.Visible;
        ApplyClickThrough();
        Dispatcher.BeginInvoke(() => ScreenBounds.ClampToWorkArea(this));
    }

    private void HideReminderBubble()
    {
        Bubble.Visibility = Visibility.Collapsed;
        _reminder.Acknowledge();
        ApplyClickThrough();
    }

    private void Pet_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2)
        {
            return;
        }

        _pet.BeginDrag();
        ApplyClickThrough();
        try
        {
            DragMove();
        }
        finally
        {
            ScreenBounds.ClampToWorkArea(this);
            _pet.EndDrag();
            SavePosition();
            ApplyClickThrough();
        }
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        HideReminderBubble();
    }

    private void IdleMenu_Click(object sender, RoutedEventArgs e) => _pet.RequestIdle();

    private void WalkMenu_Click(object sender, RoutedEventArgs e) => StartWalk();

    private void SitMenu_Click(object sender, RoutedEventArgs e) => _pet.RequestSit();

    private void LieMenu_Click(object sender, RoutedEventArgs e) => _pet.RequestLie();

    private void HideMenu_Click(object sender, RoutedEventArgs e) => Hide();

    private void SettingsMenu_Click(object sender, RoutedEventArgs e)
    {
        OpenSettings();
    }

    public void OpenSettings()
    {
        _reminder.IsPaused = true;
        var window = new SettingsWindow(_settingsStore)
        {
            Owner = this
        };
        window.ShowDialog();
        _reminder.IsPaused = false;
        _reminder.ScheduleFromNow();
        ApplyClickThrough();
    }

    private void SavePosition()
    {
        _settingsStore.Current.Left = Left;
        _settingsStore.Current.Top = Top;
        _settingsStore.Current.LastState = _pet.State.ToString();
        _settingsStore.Save();
    }
}
