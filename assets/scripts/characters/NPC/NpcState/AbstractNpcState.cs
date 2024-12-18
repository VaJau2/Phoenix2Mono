using Godot;

public abstract class AbstractNpcState : Node
{
    protected NPC tempNpc;

    public virtual void Enable(NPC npc)
    {
        tempNpc = npc;
    }

    public virtual void Disable()
    {
        QueueFree();
    }
}
