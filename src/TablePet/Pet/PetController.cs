using TablePet.Config;

namespace TablePet.Pet;

public enum PetAiCommand
{
    None,
    StartWalk
}

public sealed class PetController
{
    private readonly Random _random;
    private double _walkTargetX;
    private int _poseMsRemaining;

    public PetController(Random? random = null)
    {
        _random = random ?? Random.Shared;
        RollIdleTimer();
    }

    public PetState State { get; private set; } = PetState.Idle;

    public PetFacing Facing { get; private set; } = PetFacing.Right;

    public double WalkTargetX
    {
        get
        {
            if (State != PetState.Walk)
            {
                throw new InvalidOperationException("WalkTargetX is only valid while State is Walk.");
            }

            return _walkTargetX;
        }
    }

    public event Action? Changed;

    public void BeginDrag()
    {
        SetState(PetState.Dragged);
    }

    public void EndDrag()
    {
        if (State != PetState.Dragged)
        {
            return;
        }

        SetState(PetState.Idle);
        RollIdleTimer();
    }

    public void RequestSit()
    {
        if (State == PetState.Dragged)
        {
            return;
        }

        SetState(PetState.Sit);
        _poseMsRemaining = PetConfig.SitDurationMs;
    }

    public void RequestLie()
    {
        if (State == PetState.Dragged)
        {
            return;
        }

        SetState(PetState.Lie);
        _poseMsRemaining = PetConfig.LieDurationMs;
    }

    public void RequestWalk(double currentX, double minX, double maxX)
    {
        if (State == PetState.Dragged)
        {
            return;
        }

        _walkTargetX = WalkBehavior.PickTargetX(currentX, minX, maxX, _random);
        Facing = _walkTargetX >= currentX ? PetFacing.Right : PetFacing.Left;
        SetState(PetState.Walk);
    }

    public void RequestIdle()
    {
        if (State == PetState.Dragged)
        {
            return;
        }

        SetState(PetState.Idle);
        RollIdleTimer();
    }

    public void NotifyWalkArrived()
    {
        if (State != PetState.Walk)
        {
            return;
        }

        SetState(PetState.Idle);
        RollIdleTimer();
    }

    public PetAiCommand TickAi(int elapsedMs)
    {
        if (State is PetState.Dragged or PetState.Walk)
        {
            return PetAiCommand.None;
        }

        _poseMsRemaining -= elapsedMs;
        if (_poseMsRemaining > 0)
        {
            return PetAiCommand.None;
        }

        if (State is PetState.Sit or PetState.Lie)
        {
            SetState(PetState.Idle);
            RollIdleTimer();
            return PetAiCommand.None;
        }

        if (State != PetState.Idle)
        {
            return PetAiCommand.None;
        }

        var next = PickNextAfterIdle();
        switch (next)
        {
            case PetState.Sit:
                RequestSit();
                return PetAiCommand.None;
            case PetState.Lie:
                RequestLie();
                return PetAiCommand.None;
            case PetState.Walk:
                return PetAiCommand.StartWalk;
            default:
                RollIdleTimer();
                return PetAiCommand.None;
        }
    }

    public PetState PickNextAfterIdle()
    {
        var walk = PetConfig.WalkWeight;
        var sit = PetConfig.SitWeight;
        var lie = PetConfig.LieWeight;
        var total = walk + sit + lie;
        var roll = _random.Next(total);
        if (roll < walk)
        {
            return PetState.Walk;
        }

        if (roll < walk + sit)
        {
            return PetState.Sit;
        }

        return PetState.Lie;
    }

    private void RollIdleTimer()
    {
        _poseMsRemaining = _random.Next(PetConfig.IdleMinMs, PetConfig.IdleMaxMs + 1);
    }

    private void SetState(PetState state)
    {
        if (State == state)
        {
            return;
        }

        State = state;
        Changed?.Invoke();
    }
}
