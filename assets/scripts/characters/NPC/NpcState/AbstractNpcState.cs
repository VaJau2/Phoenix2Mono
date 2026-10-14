using Godot;

public abstract partial class AbstractNpcState : Node
{
    protected NPC tempNpc;

    public virtual void Enable(NPC npc)
    {
        tempNpc = npc;
    }

    public virtual void Disable()
    {
        if (IsInstanceValid(this))
        {
            SetProcess(false);
            QueueFree();
        }
    }
}
