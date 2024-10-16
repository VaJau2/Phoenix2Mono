using Godot;

public class UseItemTrigger : ActivateOtherTrigger
{
    [Export] public string ItemToUse;
    
    Player player => Global.Get().player;

    public override async void _Ready()
    {
        base._Ready();

        await Global.Get().ToTimer(0.1f, this);
        
        if (IsActive)
        {
            player.Connect(nameof(Player.UseItem), this, nameof(_on_player_use_item));
        }
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        if (IsActive)
        {
            player.Connect(nameof(Player.UseItem), this, nameof(_on_player_use_item));
        }
        else
        {
            player.Disconnect(nameof(Player.UseItem), this, nameof(_on_player_use_item));
        }
    }

    public void _on_player_use_item(string itemCode)
    {
        if (itemCode == ItemToUse)
        {
            _on_activate_trigger();

            if (DeleteAfterTrigger)
            {
                QueueFree();
            }
        }
    }
}
