using TablePet.Pet;

namespace TablePet.Tests;

public class PetControllerTests
{
    [Fact]
    public void BeginDrag_preempts_walk()
    {
        var pet = new PetController(new Random(2));
        pet.RequestWalk(0, 0, 400);
        Assert.Equal(PetState.Walk, pet.State);

        pet.BeginDrag();
        Assert.Equal(PetState.Dragged, pet.State);
        Assert.Throws<InvalidOperationException>(() => _ = pet.WalkTargetX);
    }

    [Fact]
    public void EndDrag_returns_to_idle()
    {
        var pet = new PetController(new Random(2));
        pet.BeginDrag();
        pet.EndDrag();
        Assert.Equal(PetState.Idle, pet.State);
    }

    [Fact]
    public void RequestSit_is_ignored_while_dragging()
    {
        var pet = new PetController(new Random(2));
        pet.BeginDrag();
        pet.RequestSit();
        Assert.Equal(PetState.Dragged, pet.State);
    }

    [Fact]
    public void RequestWalk_sets_facing_toward_target()
    {
        var pet = new PetController(new Random(3));
        pet.RequestWalk(0, 200, 200);
        Assert.Equal(PetState.Walk, pet.State);
        Assert.Equal(PetFacing.Right, pet.Facing);
        Assert.Equal(200, pet.WalkTargetX);
    }

    [Fact]
    public void NotifyWalkArrived_returns_to_idle()
    {
        var pet = new PetController(new Random(3));
        pet.RequestWalk(0, 0, 10);
        pet.NotifyWalkArrived();
        Assert.Equal(PetState.Idle, pet.State);
    }

    [Fact]
    public void TickAi_does_not_leave_walk_on_its_own()
    {
        var pet = new PetController(new Random(4));
        pet.RequestWalk(0, 0, 100);
        var command = pet.TickAi(10_000);
        Assert.Equal(PetAiCommand.None, command);
        Assert.Equal(PetState.Walk, pet.State);
    }

    [Fact]
    public void PickNextAfterIdle_only_returns_known_states()
    {
        var pet = new PetController(new Random(5));
        for (var i = 0; i < 30; i++)
        {
            var next = pet.PickNextAfterIdle();
            Assert.True(next is PetState.Walk or PetState.Sit or PetState.Lie);
        }
    }
}
