using Godot;

//удаляет непися, когда тот попадает в зону проверки
public class NpcDissapearTrigger : ActivateOtherTrigger
{
    [Export] public string npcPath;
    
    public override void SetActive(bool active)
    {
        _on_activate_trigger();
        base.SetActive(active);
    }
    
    public override void _on_activate_trigger()
    {
        if (!IsActive) return;
        Pony npc = GetNode<Pony>(npcPath);
       
        npc.QueueFree();
        Global.AddDeletedObject(npc.Name);
        base._on_activate_trigger();
    }
    
    public void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        Pony npc = GetNode<Pony>(npcPath);
        if (body != npc) return;

        if (npc.Health > 0)
        {
            _on_activate_trigger();
        }
    }
}
