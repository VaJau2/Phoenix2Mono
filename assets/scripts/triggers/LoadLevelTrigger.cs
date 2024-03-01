using Godot;

//загружает уровень
//сохраняет инвентарь в отдельный нод в корне
public partial class LoadLevelTrigger: TriggerBase
{
    [Export] public int NewLevelNum;
    [Export] public bool SaveInventory = true;
    [Export] public bool SaveOldInventorySave = false;
    [Export] public string HintCode = "exitLocation";
    
    private static Player player => Global.Get().player;

    public override void _Ready()
    {
        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        player.Camera3D.ShowHint(HintCode, false);
        
        if (!Input.IsActionJustPressed("use")) return;
        var levelsLoader = GetNode<LevelsLoader>("/root/Main");
        var saveNode = GetNode<SaveNode>("/root/Main/SaveNode");
        
        if (SaveOldInventorySave)
        {
           saveNode.CloneSaveData(levelsLoader);
        }
        
        if (SaveInventory)
        {
            saveNode.InventoryData = player.Inventory.GetSaveData(false);
        }

        levelsLoader.LoadLevel(NewLevelNum);
    }

    public override void OnActivateTrigger()
    {
        if (!IsActive)
        {
            SetActive(true);
            return;
        }
        
        base.OnActivateTrigger();
    }

    public void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        if (!(body is Player)) return;
        SetProcess(true);
    }

    public void _on_body_exited(Node body)
    {
        if (!IsActive) return;
        if (!(body is Player)) return;
        player?.Camera3D.HideHint();
        SetProcess(false);
    }
}