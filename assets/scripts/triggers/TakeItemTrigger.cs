using Godot;

//если активен, следит за инвентарем и получаемыми предметами
//включает другие триггеры, если игрок получает нужный предмет
class TakeItemTrigger: ActivateOtherTrigger
{
    [Export] private string ItemToTake;
    [Export] private bool DeletedDisactiveTriggers;

    Player player => Global.Get().player;

    public override async void _Ready()
    {
        base._Ready();
        await ToSignal(GetTree(), "idle_frame");
        
        if (IsActive)
        {
            player.Connect(nameof(Player.TakeItem), this, nameof(_on_player_take_item));
        }
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        if (IsActive)
        {
            player.Connect(nameof(Player.TakeItem), this, nameof(_on_player_take_item));
        }
        else
        {
            player.Disconnect(nameof(Player.TakeItem), this, nameof(_on_player_take_item));
        }
    }

    public void _on_player_take_item(string itemCode)
    {
        if (itemCode == ItemToTake)
        {
            _on_activate_trigger();

            if (DeletedDisactiveTriggers)
            {
                foreach (var triggerToDisactive in triggersToDisactive)
                {
                    Global.AddDeletedObject(triggerToDisactive.Name);
                    triggerToDisactive.QueueFree();
                }
            }
            
            if (DeleteAfterTrigger)
            {
                Global.AddDeletedObject(Name);
                QueueFree();
            }
        }
    }
}