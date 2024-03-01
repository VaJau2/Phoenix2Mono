using Godot;
using System;

public partial class UseItemTrigger : ActivateOtherTrigger
{
    [Export] public string ItemToUse;

    Player player => Global.Get().player;

    public override async void _Ready()
    {
        base._Ready();
        await ToSignal(GetTree(), "idle_frame");
        
        if (IsActive)
        {
            player.UseItem += OnPlayerUseItem;
        }
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        if (IsActive)
        {
            player.UseItem += OnPlayerUseItem;
        }
        else
        {
            player.UseItem -= OnPlayerUseItem;
        }
    }

    public void OnPlayerUseItem(string itemCode)
    {
        if (itemCode == ItemToUse)
        {
            OnActivateTrigger();

            if (DeleteAfterTrigger)
            {
                QueueFree();
            }
        }
    }
}
