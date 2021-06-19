using Godot;

public class NpcPoinitingGun: TriggerBase
{
    [Export] public string npcPath;
    [Export] public float delayTimer;
    private NpcWithWeapons npc;

    public override void _Ready()
    {
        if (IsActive)
        {
            _on_activate_trigger();
        }
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        _on_activate_trigger();
    }

    public override async void _on_activate_trigger()
    {
        npc = GetNode<NpcWithWeapons>(npcPath);
        
        if (delayTimer > 0)
        {
            await Global.Get().ToTimer(delayTimer);
        }
        npc?.weapons.SetWeapon(true);
        base._on_activate_trigger();
    }
}