using Godot;

public class ManaBar : Control
{
    private Player player;
    private Control mask;
    
    public override void _Ready()
    {
        mask = GetNode<Control>("/root/Main/Scene/canvas/mana/mask");
        MenuBase.LoadColorForChildren(this);
    }
    
    public override void _Process(float delta)
    {
        if (player == null)
        {
            player = Global.Get().player;
        }

        if (player is Player_Unicorn unicorn)
        {
            if (unicorn.Mana >= Player_Unicorn.MANA_MAX) return;
            
            var ratio = unicorn.Mana / Player_Unicorn.MANA_MAX;
            mask.RectSize = new Vector2(128, ratio * 128);
        }
    }
    
}
