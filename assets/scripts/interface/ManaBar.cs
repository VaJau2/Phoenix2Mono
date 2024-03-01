using Godot;

public partial class ManaBar : Control
{
    private Player player;
    private Control mask;
    
    public override void _Ready()
    {
        mask = GetNode<Control>("/root/Main/Scene/canvas/mana/mask");
        MenuBase.LoadColorForChildren(this);
    }
    
    public override void _Process(double delta)
    {
        player = Global.Get().player;

        if (player is not Player_Unicorn unicorn) return;
        if (unicorn.Mana >= Player_Unicorn.MANA_MAX) return;
            
        var ratio = unicorn.Mana / Player_Unicorn.MANA_MAX;
        mask.Size = new Vector2(128, ratio * 128);
    }
}
