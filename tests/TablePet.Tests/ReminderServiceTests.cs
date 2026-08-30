using TablePet.Persistence;
using TablePet.Reminder;

namespace TablePet.Tests;

public class ReminderServiceTests
{
    [Fact]
    public void Tick_does_not_fire_before_interval()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var store = NewStore();
        var reminder = new ReminderService(clock, store);
        var fired = 0;
        reminder.ReminderDue += () => fired++;

        clock.Advance(TimeSpan.FromMinutes(store.Current.IntervalMinutes - 1));
        reminder.Tick();

        Assert.Equal(0, fired);
        Assert.False(reminder.IsBubbleVisible);
    }

    [Fact]
    public void Tick_fires_after_interval()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var store = NewStore();
        var reminder = new ReminderService(clock, store);
        var fired = 0;
        reminder.ReminderDue += () => fired++;

        clock.Advance(TimeSpan.FromMinutes(store.Current.IntervalMinutes));
        reminder.Tick();

        Assert.Equal(1, fired);
        Assert.True(reminder.IsBubbleVisible);
    }

    [Fact]
    public void Tick_does_not_stack_while_bubble_is_visible()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var store = NewStore();
        var reminder = new ReminderService(clock, store);
        var fired = 0;
        reminder.ReminderDue += () => fired++;

        clock.Advance(TimeSpan.FromMinutes(store.Current.IntervalMinutes));
        reminder.Tick();
        clock.Advance(TimeSpan.FromMinutes(store.Current.IntervalMinutes));
        reminder.Tick();

        Assert.Equal(1, fired);
    }

    [Fact]
    public void Disabled_reminder_does_not_fire()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var store = NewStore();
        store.Current.ReminderEnabled = false;
        var reminder = new ReminderService(clock, store);
        var fired = 0;
        reminder.ReminderDue += () => fired++;

        clock.Advance(TimeSpan.FromMinutes(store.Current.IntervalMinutes));
        reminder.Tick();

        Assert.Equal(0, fired);
    }

    private static SettingsStore NewStore()
    {
        var dir = Path.Combine(Path.GetTempPath(), "TablePetTests", Guid.NewGuid().ToString("N"));
        var store = new SettingsStore(dir);
        store.Load();
        return store;
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; private set; }

        public void Advance(TimeSpan span)
        {
            UtcNow += span;
        }
    }
}
