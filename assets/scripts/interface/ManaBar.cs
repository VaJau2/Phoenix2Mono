using Godot;

public class ManaBar : Control
{
    private Player player;
    private Control mask;
    private PackedScene prefab;
    
    public override void _Ready()
    {
        mask = GetNode<Control>("/root/Main/Scene/canvas/mana/mask");
        prefab = GD.Load<PackedScene>("res://objects/interface/manaBar.tscn");
        
        MenuBase.LoadColorForChildren(this);

        var phoenixSystem = GetNodeOrNull<PhoenixSystem>("/root/Main/Scene/triggers/Phoenix System");
        phoenixSystem?.Connect(nameof(PhoenixSystem.CloneAwake), this, nameof(Reset));
    }
    
    public override void _Process(float delta)
    {
        player = Global.Get().player;

        if (player is not Player_Unicorn unicorn) return;
        if (unicorn.Mana >= Player_Unicorn.MANA_MAX) return;
        
        if (float.IsNaN(mask.RectSize.y))
        {
            mask.QueueFree();
            mask = prefab.Instance<Control>();
            mask.Modulate = Global.Get().Settings.interfaceColor;
            AddChild(mask);
        }
        
        var ratio = unicorn.Mana / Player_Unicorn.MANA_MAX;
        mask.RectSize = new Vector2(128, ratio * 128);
    }

    private void Reset()
    {
        mask.RectSize = new Vector2(128, 128);
    }
}
