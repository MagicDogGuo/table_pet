using TablePet.Config;
using TablePet.Pet;

namespace TablePet.Persistence;

public sealed class AppSettings
{
    public double Left { get; set; } = double.NaN;
    public double Top { get; set; } = double.NaN;
    public int IntervalMinutes { get; set; } = ReminderConfig.DefaultIntervalMinutes;
    public bool ReminderEnabled { get; set; } = true;
    public bool ClickThrough { get; set; }
    public string LastState { get; set; } = nameof(PetState.Idle);

    public int ClampIntervalMinutes()
    {
        if (IntervalMinutes < ReminderConfig.MinIntervalMinutes)
        {
            return ReminderConfig.MinIntervalMinutes;
        }

        if (IntervalMinutes > ReminderConfig.MaxIntervalMinutes)
        {
            return ReminderConfig.MaxIntervalMinutes;
        }

        return IntervalMinutes;
    }
}
