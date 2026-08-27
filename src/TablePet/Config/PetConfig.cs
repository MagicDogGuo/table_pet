namespace TablePet.Config;

public static class PetConfig
{
    public static int FrameWidth { get; } = 128;
    public static int FrameHeight { get; } = 128;
    public static double WalkSpeedPixelsPerSecond { get; } = 80;
    public static int IdleMinMs { get; } = 3000;
    public static int IdleMaxMs { get; } = 8000;
    public static int SitDurationMs { get; } = 8000;
    public static int LieDurationMs { get; } = 12000;
    public static int WalkWeight { get; } = 4;
    public static int SitWeight { get; } = 2;
    public static int LieWeight { get; } = 1;
    public static string DefaultPetId { get; } = "default";
}
