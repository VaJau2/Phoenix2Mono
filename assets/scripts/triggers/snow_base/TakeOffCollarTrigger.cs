using Godot;

public class TakeOffCollarTrigger : TriggerBase
{
    [Export] private NodePath npcPath;
    [Export] private string collarPath;
    [Export] private AudioStream deactivatingSound;
    
    public override void _on_activate_trigger()
    {
        if (!IsActive) return;

        var npc = GetNode(npcPath);
        var collar = npc.GetNode<Spatial>(collarPath);
        collar.Visible = false;

        var player = Global.Get().player;
        var playerAudi = player.GetAudi(true);

        playerAudi.Stream = deactivatingSound;
        playerAudi.Play();
        
        base._on_activate_trigger();
    }
}
