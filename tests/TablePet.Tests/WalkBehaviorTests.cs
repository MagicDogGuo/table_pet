using TablePet.Pet;

namespace TablePet.Tests;

public class WalkBehaviorTests
{
    [Fact]
    public void PickTargetX_stays_inside_range()
    {
        var random = new Random(1);
        for (var i = 0; i < 40; i++)
        {
            var target = WalkBehavior.PickTargetX(100, 0, 400, random);
            Assert.InRange(target, 0, 400);
        }
    }

    [Fact]
    public void PickTargetX_returns_min_when_range_is_empty()
    {
        var target = WalkBehavior.PickTargetX(50, 80, 80, new Random(1));
        Assert.Equal(80, target);
    }

    [Fact]
    public void StepTowards_does_not_overshoot()
    {
        Assert.Equal(10, WalkBehavior.StepTowards(0, 10, 40));
        Assert.Equal(4, WalkBehavior.StepTowards(0, 10, 4));
    }

    [Fact]
    public void HasArrived_uses_half_pixel_tolerance()
    {
        Assert.True(WalkBehavior.HasArrived(10, 10.2));
        Assert.False(WalkBehavior.HasArrived(10, 12));
    }
}
