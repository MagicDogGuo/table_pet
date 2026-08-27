namespace TablePet.Pet;

public static class WalkBehavior
{
    public static double PickTargetX(double currentX, double minX, double maxX, Random random)
    {
        if (maxX <= minX)
        {
            return minX;
        }

        var span = maxX - minX;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var target = minX + (random.NextDouble() * span);
            if (span < 80 || Math.Abs(target - currentX) >= 40)
            {
                return target;
            }
        }

        return currentX < (minX + maxX) / 2 ? maxX : minX;
    }

    public static double StepTowards(double current, double target, double maxDelta)
    {
        var delta = target - current;
        if (Math.Abs(delta) <= maxDelta)
        {
            return target;
        }

        return current + (Math.Sign(delta) * maxDelta);
    }

    public static bool HasArrived(double currentX, double targetX)
    {
        return Math.Abs(currentX - targetX) < 0.5;
    }
}
