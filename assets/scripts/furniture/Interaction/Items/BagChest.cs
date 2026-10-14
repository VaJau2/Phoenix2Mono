using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public class BagChest : RigidBody, IChest, IInteractable, ISavable
{
    private const float AUDI_COOLDOWN = 0.3f;
    
    [Export] private bool SpawnRandomItems;
    [Export] private List<AudioStreamSample> openSounds;
    [Export] private List<AudioStreamSample> dropSounds;
    [Export] private AudioStreamSample bottleSound;
    
    private AudioStreamPlayer3D audi;
    private float audiCooldown;
    
    public string ChestCode => "bag";
    public ChestHandler ChestHandler { get; private set; }

    public bool MayInteract => true;
    public string InteractionHintCode => "open";
    public void Interact(PlayerCamera interactor)
    {
        interactor.HideInteractionSquare();
        OpenBag();
    }

    public override void _Ready()
    {
        audi = GetNode<AudioStreamPlayer3D>("audi");

        ChestHandler = new ChestHandler(this)
            .SetCode(ChestCode)
            .SetIsBag(true);
        
        if (SpawnRandomItems)
        {
            ChestHandler.SpawnRandomItems();
        }
    }

    public override void _Process(float delta)
    {
        if (audiCooldown > 0)
        {
            audiCooldown -= delta;
        }
    }

    private void OpenBag(float timer = 0, string openAnim = null)
    {
        PlaySound(openSounds);
        ChestHandler.Open();

        if (!ChestHandler.Menu.IsConnected("MenuIsClosed", this, nameof(CloseBag)))
        {
            ChestHandler.Menu.Connect("MenuIsClosed", this, nameof(CloseBag));
        }
    }

    public void CloseBag()
    {
        PlaySound(openSounds);
        
        if (ChestHandler.Menu.IsConnected("MenuIsClosed", this, nameof(CloseBag)))
        {
            ChestHandler.Menu.Disconnect("MenuIsClosed", this, nameof(CloseBag));
        }
    }
    
    private void PlaySound(List<AudioStreamSample> soundsArray)
    {
        var rand = new Random();
        var randI = rand.Next(0, soundsArray.Count);
        audi.Stream = soundsArray[randI];
        audi.Play();
    }

    private void PlayDropSound()
    {
        if (ChestHandler.ContainBottleItems())
        {
            if (!dropSounds.Contains(bottleSound))
            {
                dropSounds.Add(bottleSound);
            }
        }
        else if (dropSounds.Contains(bottleSound))
        {
            dropSounds.Remove(bottleSound);
        }
        
        PlaySound(dropSounds);
    }
    
    public Dictionary GetSaveData()
    {
        Dictionary saveData = ChestHandler.GetSaveData();

        if (Name.BeginsWith("Created_"))
        {
            saveData.Add("pos", Transform.origin);
            saveData.Add("rot", Transform.basis.GetEuler());
        }

        return saveData;
    }

    public void OnBodyEntered(Node body)
    {
        if (audiCooldown > 0) return;
        
        if (body is StaticBody { PhysicsMaterialOverride: not null } collideBody)
        {
            PlayDropSound();
            audiCooldown = AUDI_COOLDOWN;
        }
    }
    
    public void LoadData(Dictionary data)
    {
        if (data.Count == 0) return;
        
        if (Name.BeginsWith("Created_"))
        {
            Vector3 newPos = data["pos"].ToString().ParseToVector3();
            Vector3 newRot = data["rot"].ToString().ParseToVector3();

            Basis newBasis = new Basis(newRot);
            Transform newTransform = new Transform(newBasis, newPos);
            Transform = newTransform;
        }

        ChestHandler.LoadData(data);
    }
}
