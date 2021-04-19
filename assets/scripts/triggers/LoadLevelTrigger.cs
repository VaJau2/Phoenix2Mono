using Godot;

//загружает уровень
//сохраняет инвентарь в отдельный нод в корне
public class LoadLevelTrigger: TriggerBase
{
    [Export] public int NewLevelNum;
    [Export] public bool SaveInventory = true;
    
    private static Player player => Global.Get().player;

    public override void _Ready()
    {
        SetProcess(false);
    }

    public override void _Process(float delta)
    {
        player.Camera.ShowHint("exitLocation", false);
        
        if (!Input.IsActionJustPressed("use")) return;
        var levelsLoader = GetNode<LevelsLoader>("/root/Main");
        if (SaveInventory)
        {
            var newSaveNode = new SaveNode();
            levelsLoader.AddChild(newSaveNode);
            newSaveNode.Name = "SavedData";
            newSaveNode.InventoryData = player.inventory.GetSaveData(false);
        }

        levelsLoader.LoadLevel(NewLevelNum);
    }

    public override void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        if (!(body is Player)) return;
        SetProcess(true);
    }

    public void _on_body_exited(Node body)
    {
        if (!IsActive) return;
        if (!(body is Player)) return;
        player?.Camera.HideHint();
        SetProcess(false);
    }
}