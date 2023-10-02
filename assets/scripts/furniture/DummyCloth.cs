using Godot;
using Godot.Collections;

public class DummyCloth : StaticBody, IInteractable, ISavable
{
    [Export] private Mesh pegasCloth;
    [Export] private string clothCode;
    private bool hasCloth = true;

    private MeshInstance bodyMesh;
    private MeshInstance clothMesh;
    private CollisionShape clothShape;
    
    private InventoryMenu inventoryMenu;

    public bool MayInteract => hasCloth && inventoryMenu.HasEmptyButton;
    public string InteractionHintCode => "putOn";

    private static Global global => Global.Get();
    
    public override void _Ready()
    {
        inventoryMenu = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");

        bodyMesh = GetNode<MeshInstance>("Body");
        clothMesh = GetNode<MeshInstance>("Cloth");
        clothShape = GetNode<CollisionShape>("ClothShape");

        if (global.playerRace == Race.Pegasus && pegasCloth != null)
        {
            clothMesh.Mesh = pegasCloth;
        }
    }

    public void Interact(PlayerCamera interactor)
    {
        var inventory = global.player.inventory;
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
