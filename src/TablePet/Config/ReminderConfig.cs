namespace TablePet.Config;

public static class ReminderConfig
{
    public static int DefaultIntervalMinutes { get; } = 45;
    public static int MinIntervalMinutes { get; } = 1;
    public static int MaxIntervalMinutes { get; } = 180;
    public static string Message { get; } = "起來喝水！🥛";
}
