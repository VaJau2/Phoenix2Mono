using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

/**
 * Предмет, который можно подобрать через взаимодействие
 * Играет звук ударов из audi
 * Замораживается через некоторое время, если не движется
 */
public class PickupItem : RigidBody, IInteractable, ISavable
{
    private const float AUDI_COOLDOWN = 0.3f;
    private const float MIN_VELOCITY_TO_SOUND = 0.3f;
    
    [Export] private string itemCode;
    [Export] private List<AudioStreamSample> sounds;
    
    public bool MayInteract => inventoryMenu.HasEmptyButton;
    public string InteractionHintCode => "pick";

    private AudioStreamPlayer3D audi;
    private InventoryMenu inventoryMenu;
    private Messages messages;
    
    private float audiCooldown;
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
    }

    public void _on_body_entered(Node body)
    {
        if (audiCooldown > 0) return;
        if (currentSpeed < MIN_VELOCITY_TO_SOUND) return;
        
        if (body is StaticBody { PhysicsMaterialOverride: not null } collideBody)
        {
            PlaySound();
            audiCooldown = AUDI_COOLDOWN;
        }
    }
    
    private void PlaySound()
    {
        var rand = new Random();
        var randI = rand.Next(0, sounds.Count);
        audi.Stream = sounds[randI];
        audi.Play();
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            { "isStatic", Mode == ModeEnum.Static },
            { "pos", GlobalTransform.origin },
            { "rot", GlobalTransform.basis.GetEuler() },
        };
    }

    public void LoadData(Dictionary data)
    {
        if ((bool)data["isStatic"])
        {
            Mode = ModeEnum.Static;
        }
        
        Vector3 newPos = data["pos"].ToString().ParseToVector3();
        Vector3 newRot = data["rot"].ToString().ParseToVector3();
        Vector3 oldScale = Scale;

        Basis newBasis = new Basis(newRot);
        Transform newTransform = new Transform(newBasis, newPos);
        GlobalTransform = newTransform;
        Scale = oldScale;
    }
}
