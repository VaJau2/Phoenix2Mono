using System;
using Godot;
using Godot.Collections;

public partial class FurnChest: FurnBase, ISavable, IChest 
{
    [Export] private bool isBag;
    [Export] private bool SpawnRandomItems;
    [Export] private string chestCode;

    [Export] 
    public Array<string> startItemCodes = new();
    [Export]
    public Dictionary<string, int> startAmmoCount = new();
    
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
        ChestHandler.Menu.MenuIsClosed += CloseFurn;
    }

    public void CloseFurn()
    {
        base.ClickFurn();
        ChestHandler.Menu.MenuIsClosed -= CloseFurn;
    }

    public Dictionary GetSaveData()
    {
        Dictionary saveData = ChestHandler.GetSaveData();

        if (Name.ToString().StartsWith("Created_"))
        {
            saveData.Add("pos_x", Transform.Origin.X);
            saveData.Add("pos_y", Transform.Origin.Y);
            saveData.Add("pos_z", Transform.Origin.Z);
            saveData.Add("rot_x", Transform.Basis.GetEuler().X);
            saveData.Add("rot_y", Transform.Basis.GetEuler().Y);
            saveData.Add("rot_z", Transform.Basis.GetEuler().Z);
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
            Vector3 newPos = new Vector3(Convert.ToSingle(data["pos_x"]), Convert.ToSingle(data["pos_y"]),
                Convert.ToSingle(data["pos_z"]));
            Vector3 newRot = new Vector3(Convert.ToSingle(data["rot_x"]), Convert.ToSingle(data["rot_y"]),
                Convert.ToSingle(data["rot_z"]));

            Basis newBasis = Basis.FromEuler(newRot);
            Transform3D newTransform = new Transform3D(newBasis, newPos);
            Transform = newTransform;
        }

        ChestHandler.LoadData(data);
    }
}