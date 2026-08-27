using TablePet.Config;
using TablePet.Persistence;

namespace TablePet.Reminder;

public sealed class ReminderService
{
    private readonly IClock _clock;
    private readonly SettingsStore _settingsStore;
    private DateTimeOffset _nextDue;
    private bool _bubbleVisible;

    public ReminderService(IClock clock, SettingsStore settingsStore)
    {
        _clock = clock;
        _settingsStore = settingsStore;
        ScheduleFromNow();
    }

    public bool IsPaused { get; set; }

    public bool IsBubbleVisible => _bubbleVisible;

    public event Action? ReminderDue;

    public void Tick()
    {
        if (IsPaused || _bubbleVisible)
        {
            return;
        }

        var settings = _settingsStore.Current;
        if (!settings.ReminderEnabled)
        {
            return;
        }

        if (_clock.UtcNow < _nextDue)
        {
            return;
        }

        _bubbleVisible = true;
        ReminderDue?.Invoke();
    }

    public void Acknowledge()
    {
        _bubbleVisible = false;
        ScheduleFromNow();
    }

    public void Snooze()
    {
        _bubbleVisible = false;
        _nextDue = _clock.UtcNow.AddMinutes(ReminderConfig.SnoozeMinutes);
    }

    public void ScheduleFromNow()
    {
        var minutes = _settingsStore.Current.ClampIntervalMinutes();
        _nextDue = _clock.UtcNow.AddMinutes(minutes);
    }
}
