using Godot;

//поддержка патронов не добавлена
//если необходима, нужно будет добавить сюда
public partial class AddItemInChestTrigger : TriggerBase
{
    [Export] private string[] itemsCode;
    [Export] private NodePath chestPath;
    private IChest chest;
    
    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        OnActivateTrigger();
    }

    public override void OnActivateTrigger()
    {
        if (!IsActive) return;
        
        chest = GetNode<FurnChest>(chestPath);
        if (chest != null)
        {
            foreach(string itemCode in itemsCode)
            {
                chest.ChestHandler.AddNewItem(itemCode);
            }
        }
        base.OnActivateTrigger();
    }
}
