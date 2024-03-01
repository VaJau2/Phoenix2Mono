using Godot;
using Godot.Collections;

public partial class DummyCloth : StaticBody3D, IInteractable, ISavable
{
    [Export] private Mesh pegasCloth;
    [Export] private string clothCode;
    private bool hasCloth = true;

    private MeshInstance3D bodyMesh;
    private MeshInstance3D clothMesh;
    private CollisionShape3D clothShape;
    
    private static string currentCloth
        => Global.Get().player.Inventory.cloth;

    public bool MayInteract => hasCloth && (string.IsNullOrEmpty(currentCloth) || currentCloth == "empty");
    public string InteractionHintCode => "putOn";

    private static Global global => Global.Get();
    
    public override void _Ready()
    {
        bodyMesh = GetNode<MeshInstance3D>("Body");
        clothMesh = GetNode<MeshInstance3D>("Cloth");
        clothShape = GetNode<CollisionShape3D>("ClothShape");

        if (global.playerRace == Race.Pegasus && pegasCloth != null)
        {
            clothMesh.Mesh = pegasCloth;
        }
    }

    public void Interact(PlayerCamera interactor)
    {
        var inventory = global.player.Inventory;
        var wearButton = inventory.GetWearButton(ItemType.armor);
        
        hasCloth = false;
        UpdateMeshes();
        
        inventory.WearItem(clothCode);
        wearButton.SetItem(clothCode);
    }

    private void UpdateMeshes()
    {
        bodyMesh.Visible = !hasCloth;
        clothMesh.Visible = hasCloth;
        clothShape.Disabled = !hasCloth;
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            { "hasCloth", hasCloth}
        };
    }

    public void LoadData(Dictionary data)
    {
        hasCloth = (bool)data["hasCloth"];
        UpdateMeshes();
    }
}
