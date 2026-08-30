using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TablePet.Config;

namespace TablePet.Pet;

public sealed class SpriteAtlas
{
    private readonly Dictionary<string, List<BitmapSource>> _clips = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _fpsOverrides = new(StringComparer.OrdinalIgnoreCase);
    private int _defaultFps = 8;

    public bool HasFrames => _clips.Count > 0;

    public static SpriteAtlas LoadDefault()
    {
        var atlas = new SpriteAtlas();
        var root = Path.Combine(AppContext.BaseDirectory, "Assets", "Pet", PetConfig.DefaultPetId);
        atlas.LoadFromFolder(root);
        return atlas;
    }

    public void LoadFromFolder(string root)
    {
        _clips.Clear();
        _fpsOverrides.Clear();
        if (!Directory.Exists(root))
        {
            return;
        }

        var manifestPath = Path.Combine(root, "manifest.json");
        if (File.Exists(manifestPath))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
                if (doc.RootElement.TryGetProperty("fps", out var fpsEl))
                {
                    _defaultFps = fpsEl.GetInt32();
                }

                if (doc.RootElement.TryGetProperty("clips", out var clipsEl))
                {
                    foreach (var clip in clipsEl.EnumerateObject())
                    {
                        if (clip.Value.TryGetProperty("fps", out var clipFps))
                        {
                            _fpsOverrides[clip.Name] = clipFps.GetInt32();
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Keep defaults when the manifest is corrupt.
            }
        }

        foreach (var dir in Directory.GetDirectories(root))
        {
            var clipId = Path.GetFileName(dir);
            var frames = Directory.GetFiles(dir, "*.png")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(LoadPng)
                .Where(frame => frame is not null)
                .Cast<BitmapSource>()
                .ToList();
            if (frames.Count > 0)
            {
                _clips[clipId] = frames;
            }
        }
    }

    public int FpsFor(string clipId)
    {
        return _fpsOverrides.TryGetValue(clipId, out var fps) ? fps : _defaultFps;
    }

    public int FrameCount(string clipId)
    {
        var frames = ResolveClip(clipId);
        return frames?.Count ?? 0;
    }

    public BitmapSource? GetFrame(string clipId, int index, PetFacing facing)
    {
        var frames = ResolveClip(clipId);
        if (frames is null || frames.Count == 0)
        {
            return null;
        }

        var frame = frames[Math.Abs(index) % frames.Count];
        if (facing != PetFacing.Left)
        {
            return frame;
        }

        var transform = new TransformGroup();
        transform.Children.Add(new ScaleTransform(-1, 1));
        transform.Children.Add(new TranslateTransform(frame.PixelWidth, 0));
        var flipped = new TransformedBitmap(frame, transform);
        flipped.Freeze();
        return flipped;
    }

    private List<BitmapSource>? ResolveClip(string clipId)
    {
        if (_clips.TryGetValue(clipId, out var exact))
        {
            return exact;
        }

        if (clipId == "drag" && _clips.TryGetValue("idle", out var idle))
        {
            return idle;
        }

        if (clipId == "drink" && _clips.TryGetValue("sit", out var sit))
        {
            return sit;
        }

        return null;
    }

    private static BitmapSource? LoadPng(string path)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (IOException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }
}
