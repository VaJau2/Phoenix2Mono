using Godot;

//удаляет непися, когда тот попадает в зону проверки
public partial class NpcDissapearTrigger : ActivateOtherTrigger
{
    [Export] public string npcPath;
    
    public override void SetActive(bool active)
    {
        OnActivateTrigger();
        base.SetActive(active);
    }
    
    public override void OnActivateTrigger()
    {
        if (!IsActive) return;
        Pony npc = GetNode<Pony>(npcPath);
       
        npc.QueueFree();
        Global.AddDeletedObject(npc.Name);
        base.OnActivateTrigger();
    }
    
    public override void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        Pony npc = GetNodeOrNull<Pony>(npcPath);
        if (body != npc) return;

        if (npc.Health > 0)
        {
            OnActivateTrigger();
        }
    }
}
