using System.Windows.Media.Imaging;

namespace TablePet.Pet;

public sealed class SpriteAnimator
{
    private readonly SpriteAtlas _atlas;
    private string _clipId = "idle";
    private PetFacing _facing = PetFacing.Right;
    private TimeSpan _elapsed;
    private int _frameIndex;

    public SpriteAnimator(SpriteAtlas atlas)
    {
        _atlas = atlas;
    }

    public bool HasFrames => _atlas.HasFrames;

    public BitmapSource? CurrentFrame { get; private set; }

    public void SetClip(PetState state, PetFacing facing)
    {
        var clipId = ClipId(state);
        var clipChanged = clipId != _clipId;
        _clipId = clipId;
        _facing = facing;
        if (clipChanged)
        {
            _elapsed = TimeSpan.Zero;
            _frameIndex = 0;
        }

        CurrentFrame = _atlas.GetFrame(_clipId, _frameIndex, _facing);
    }

    public void Advance(TimeSpan delta)
    {
        if (!_atlas.HasFrames)
        {
            CurrentFrame = null;
            return;
        }

        var fps = _atlas.FpsFor(_clipId);
        _elapsed += delta;
        var frameTime = TimeSpan.FromSeconds(1d / Math.Max(1, fps));
        while (_elapsed >= frameTime)
        {
            _elapsed -= frameTime;
            var count = _atlas.FrameCount(_clipId);
            if (count <= 0)
            {
                break;
            }

            _frameIndex = (_frameIndex + 1) % count;
        }

        CurrentFrame = _atlas.GetFrame(_clipId, _frameIndex, _facing);
    }

    public static string ClipId(PetState state)
    {
        return state switch
        {
            PetState.Walk => "walk",
            PetState.Sit => "sit",
            PetState.Lie => "lie",
            PetState.Dragged => "drag",
            _ => "idle"
        };
    }
}
