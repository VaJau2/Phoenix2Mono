using System;
using Godot;
using Godot.Collections;

public class FurnChest: FurnBase, ISavable, IChest 
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

    public override void ClickFurn(AudioStreamSample openSound = null, float timer = 0, string openAnim = null)
    {
        if (!MayOpen) return;

        if (IsOpen) return;
        base.ClickFurn();
        ChestHandler.Open();
        ChestHandler.Menu.Connect("MenuIsClosed", this, nameof(CloseFurn));
    }

    public void CloseFurn()
    {
        base.ClickFurn();
        ChestHandler.Menu.Disconnect("MenuIsClosed", this, nameof(CloseFurn));
    }

    public Dictionary GetSaveData()
    {
        Dictionary saveData = ChestHandler.GetSaveData();

        if (Name.BeginsWith("Created_"))
        {
            saveData.Add("pos_x", Transform.origin.x);
            saveData.Add("pos_y", Transform.origin.y);
            saveData.Add("pos_z", Transform.origin.z);
            saveData.Add("rot_x", Transform.basis.GetEuler().x);
            saveData.Add("rot_y", Transform.basis.GetEuler().y);
            saveData.Add("rot_z", Transform.basis.GetEuler().z);
        }

        return saveData;
    }

    public void LoadData(Dictionary data)
    {
        if (data.Count == 0) return;
        
        SpawnRandomItems = false;
        startItemCodes.Clear();
        startAmmoCount.Clear();
        
        if (Name.BeginsWith("Created_"))
        {
            Vector3 newPos = new Vector3(Convert.ToSingle(data["pos_x"]), Convert.ToSingle(data["pos_y"]),
                Convert.ToSingle(data["pos_z"]));
            Vector3 newRot = new Vector3(Convert.ToSingle(data["rot_x"]), Convert.ToSingle(data["rot_y"]),
                Convert.ToSingle(data["rot_z"]));

            Basis newBasis = new Basis(newRot);
            Transform newTransform = new Transform(newBasis, newPos);
            Transform = newTransform;
        }

        ChestHandler.LoadData(data);
    }
}