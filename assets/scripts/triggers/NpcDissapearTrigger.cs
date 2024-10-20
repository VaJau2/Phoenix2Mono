using Godot;

//удаляет непися, когда тот попадает в зону проверки
public class NpcDissapearTrigger : ActivateOtherTrigger
{
    [Export] public string npcPath;
    
    public override void SetActive(bool active)
    {
        if (active) _on_activate_trigger();
        base.SetActive(active);
    }
    
    public override void _on_activate_trigger()
    {
        if (!IsActive) return;
        var npc = GetNode<NPC>(npcPath);
       
        npc.QueueFree();
        Global.AddDeletedObject(npc.Name);
        base._on_activate_trigger();
    }
    
    public override void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        var npc = GetNodeOrNull<NPC>(npcPath);
        if (body != npc) return;

        if (npc.Health > 0)
        {
            _on_activate_trigger();
        }
    }
}
