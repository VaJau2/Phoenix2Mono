using System;
using Godot;
using Godot.Collections;

/**
 * Предмет, который можно подобрать через взаимодействие
 * Играет звук ударов из audi
 * Замораживается через некоторое время, если не движется
 */
public class PickupItem : RigidBody, IInteractable, ISavable
{
    const float AUDI_COOLDOWN = 2;
    private const float MIN_VELOCITY_TO_SOUND = 1;
    const float STATIC_TIME = 1f;
    const float MIN_STATIC_SPEED = 0.02f;
    
    [Export] private string itemCode;
    
    public bool MayInteract => inventoryMenu.HasEmptyButton;
    public string InteractionHintCode => "pick";

    private AudioStreamPlayer3D audi;
    private InventoryMenu inventoryMenu;
    private Messages messages;
    
    private float audiCooldown;
    private float staticTimer = STATIC_TIME;
    private float currentSpeed;
    
    public void Interact(PlayerCamera interactor)
    {
        inventoryMenu.AddOrDropItem(itemCode);
        
        Dictionary itemData = ItemJSON.GetItemData(itemCode);
        messages.ShowMessage("itemTaken", itemData["name"].ToString(), "items");
        
        QueueFree();
    }

    public override void _Ready()
    {
        inventoryMenu = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
        audi = GetNodeOrNull<AudioStreamPlayer3D>("audi");
    }

    public override void _Process(float delta)
    {
        if (audiCooldown > 0)
        {
            audiCooldown -= delta;
        }

        currentSpeed = LinearVelocity.Length();
        
        if (currentSpeed < MIN_STATIC_SPEED)
        {
            if (staticTimer > 0)
            {
                staticTimer -= delta;
            }
            else
            {
                Mode = ModeEnum.Static;
            }
        }
        else
        {
            staticTimer = STATIC_TIME;
        }
    }

    public void _on_body_entered(Node body)
    {
        if (audi == null || audiCooldown > 0) return;
        if (currentSpeed < MIN_VELOCITY_TO_SOUND) return;
        
        if (!(body is StaticBody collideBody) || collideBody.PhysicsMaterialOverride == null) return;
        var friction = collideBody.PhysicsMaterialOverride.Friction;
        var materialName = MatNames.GetMatName(friction);

        if (string.IsNullOrEmpty(materialName)) return;
        audi.Play();
        audiCooldown = AUDI_COOLDOWN;
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            { "isStatic", Mode == ModeEnum.Static },
            { "pos_x", GlobalTransform.origin.x },
            { "pos_y", GlobalTransform.origin.y },
            { "pos_z", GlobalTransform.origin.z },
            { "rot_x", GlobalTransform.basis.GetEuler().x },
            { "rot_y", GlobalTransform.basis.GetEuler().y },
            { "rot_z", GlobalTransform.basis.GetEuler().z },
        };
    }

    public void LoadData(Dictionary data)
    {
        if ((bool)data["isStatic"])
        {
            Mode = ModeEnum.Static;
        }
        
        Vector3 newPos = new Vector3(Convert.ToSingle(data["pos_x"]), Convert.ToSingle(data["pos_y"]), Convert.ToSingle(data["pos_z"]));
        Vector3 newRot = new Vector3(Convert.ToSingle(data["rot_x"]), Convert.ToSingle(data["rot_y"]), Convert.ToSingle(data["rot_z"]));
        Vector3 oldScale = Scale;

        Basis newBasis = new Basis(newRot);
        Transform newTransform = new Transform(newBasis, newPos);
        GlobalTransform = newTransform;
        Scale = oldScale;
    }
}
