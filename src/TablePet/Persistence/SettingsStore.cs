using System.IO;
using System.Text.Json;
using TablePet.Config;

namespace TablePet.Persistence;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _filePath;
    private AppSettings _current = new();

    public SettingsStore(string directory)
    {
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, WindowConfig.SettingsFileName);
    }

    public AppSettings Current => _current;

    public AppSettings Load()
    {
        if (!File.Exists(_filePath))
        {
            _current = new AppSettings();
            return _current;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            _current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (JsonException)
        {
            _current = new AppSettings();
        }
        catch (IOException)
        {
            _current = new AppSettings();
        }

        _current.IntervalMinutes = _current.ClampIntervalMinutes();
        return _current;
    }

    public void Save()
    {
        _current.IntervalMinutes = _current.ClampIntervalMinutes();
        var json = JsonSerializer.Serialize(_current, JsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
