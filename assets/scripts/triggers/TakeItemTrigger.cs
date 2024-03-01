using Godot;

//если активен, следит за инвентарем и получаемыми предметами
//включает другие триггеры, если игрок получает нужный предмет
partial class TakeItemTrigger: ActivateOtherTrigger
{
    [Export] private string ItemToTake;
    [Export] private bool DeletedDisactiveTriggers;

    Player player => Global.Get().player;

    public override async void _Ready()
    {
        base._Ready();
        await ToSignal(GetTree(), "process_frame");
        
        if (IsActive)
        {
            player.TakeItem += OnPlayerTakeItem;
        }
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        if (IsActive)
        {
            player.TakeItem += OnPlayerTakeItem;
        }
        else
        {
            player.TakeItem -= OnPlayerTakeItem;
        }
    }

    public void OnPlayerTakeItem(string itemCode)
    {
        if (itemCode == ItemToTake)
        {
            OnActivateTrigger();

            if (DeletedDisactiveTriggers)
            {
                foreach (var triggerToDeactivate in triggersToDisactive)
                {
                    Global.AddDeletedObject(triggerToDeactivate.Name);
                    triggerToDeactivate.QueueFree();
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