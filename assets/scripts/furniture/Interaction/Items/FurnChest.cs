using System;
using Godot;
using Godot.Collections;

public partial class FurnChest: FurnBase, ISavable, IChest 
{
    [Export] private bool isBag;
    [Export] private bool SpawnRandomItems;
    [Export] private string chestCode;

    [Export] 
    public Array<string> startItemCodes = new Array<string>();
    [Export]
    public Dictionary<string, int> startAmmoCount = new Dictionary<string, int>();
    
    public string ChestCode => chestCode;
    public ChestHandler ChestHandler { get; private set; }

    public override void _Ready()
    {
        base._Ready();

        ChestHandler = new ChestHandler(this)
            .SetCode(ChestCode)
            .SetIsBag(isBag)
            .LoadStartItems(startItemCodes, startAmmoCount);

        if (SpawnRandomItems)
        {
            ChestHandler.SpawnRandomItems();
        }
    }

    public bool MayOpen => (IsOpen == ChestHandler.Menu.isOpen);

    public override void ClickFurn(AudioStreamWav openSound = null, float timer = 0, string openAnim = null)
    {
        if (!MayOpen) return;

        if (IsOpen) return;
        base.ClickFurn();
        ChestHandler.Open();
        ChestHandler.Menu.Connect("MenuIsClosed", new Callable(this, nameof(CloseFurn)));
    }

    public void CloseFurn()
    {
        base.ClickFurn();
        ChestHandler.Menu.Disconnect("MenuIsClosed", new Callable(this, nameof(CloseFurn)));
    }

    public Dictionary GetSaveData()
    {
        Dictionary saveData = ChestHandler.GetSaveData();

        if (Name.ToString().StartsWith("Created_"))
        {
            saveData.Add("pos", Transform.Origin);
            saveData.Add("rot", Transform.Basis.GetEuler());
        }

        return saveData;
    }

    public void LoadData(Dictionary data)
    {
        if (data.Count == 0) return;
        
        SpawnRandomItems = false;
        startItemCodes.Clear();
        startAmmoCount.Clear();
        
        if (Name.ToString().StartsWith("Created_"))
        {
            Vector3 newPos = data["pos"].ToString().ParseToVector3();
            Vector3 newRot = data["rot"].ToString().ParseToVector3();

            Basis newBasis = Basis.FromEuler(newRot);
            Transform3D newTransform = new Transform3D(newBasis, newPos);
            Transform = newTransform;
        }

        ChestHandler.LoadData(data);
    }
}