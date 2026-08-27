namespace TablePet.Config;

public static class ReminderConfig
{
    public static int DefaultIntervalMinutes { get; } = 45;
    public static int MinIntervalMinutes { get; } = 5;
    public static int MaxIntervalMinutes { get; } = 180;
    public static int SnoozeMinutes { get; } = 5;
    public static int BubbleSeconds { get; } = 20;
    public static string Message { get; } = "Time to drink water.";
}
