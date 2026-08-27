using TablePet.Persistence;

namespace TablePet.Tests;

public class SettingsStoreTests
{
    [Fact]
    public void Save_then_load_round_trips_values()
    {
        var dir = NewDir();
        var store = new SettingsStore(dir);
        store.Load();
        store.Current.Left = 32;
        store.Current.Top = 64;
        store.Current.IntervalMinutes = 20;
        store.Current.ReminderEnabled = false;
        store.Current.ClickThrough = true;
        store.Save();

        var loaded = new SettingsStore(dir).Load();
        Assert.Equal(32, loaded.Left);
        Assert.Equal(64, loaded.Top);
        Assert.Equal(20, loaded.IntervalMinutes);
        Assert.False(loaded.ReminderEnabled);
        Assert.True(loaded.ClickThrough);
    }

    [Fact]
    public void Corrupt_file_falls_back_to_defaults()
    {
        var dir = NewDir();
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "settings.json"), "{ not json");

        var settings = new SettingsStore(dir).Load();
        Assert.True(double.IsNaN(settings.Left));
        Assert.True(settings.ReminderEnabled);
        Assert.Equal(45, settings.IntervalMinutes);
    }

    [Fact]
    public void Interval_is_clamped_on_load()
    {
        var dir = NewDir();
        Directory.CreateDirectory(dir);
        File.WriteAllText(
            Path.Combine(dir, "settings.json"),
            """{"intervalMinutes": 999, "reminderEnabled": true}""");

        var settings = new SettingsStore(dir).Load();
        Assert.Equal(180, settings.IntervalMinutes);
    }

    private static string NewDir()
    {
        return Path.Combine(Path.GetTempPath(), "TablePetTests", Guid.NewGuid().ToString("N"));
    }
}
