namespace TablePet.Reminder;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
