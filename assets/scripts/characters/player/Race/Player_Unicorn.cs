using Godot;
using Godot.Collections;
using System;

public class Player_Unicorn : Player
{
    private const float MANA_SPEED = 5f;
    private const float TELEPORT_COST = 50f;
    private const float TELEPORT_MIN_COST = 10f;
    private const int TELEPORT_DISTANCE = 150;
    public const float MANA_MAX = 100;
    public float Mana;
    public float ManaDelta = 1f;

    public AudioStreamPlayer audiHorn;
    private AudioStreamSample teleportSound;
    private Messages messages;
    private Particles hornMagic;
    public UnicornShield shield;

    private PackedScene teleportMark;
    private PackedScene teleportEffect;

    private TeleportMark tempTeleportMark;
    private bool notEnoughMana = false;
    private bool teleportPressed = false;
    private bool startTeleporting = false;
    public bool teleportInside = false;

    public bool ManaIsEnough(float cost) 
    {
        return Mana > cost;
    }

    public void DecreaseMana(float decrease) 
    {
        Mana -= decrease;
    }

    public void SetMagicEmit(bool emit) 
    {
        hornMagic.Emitting = emit;
    }

    public override void TakeDamage(Character damager, int damage, int shapeID = 0)
    {
        if (shield.shieldOn && (int)Mana > 0) {
            damage /= (int)Mana;
            if (damage <= 0) {
                return;
            }
        }

        base.TakeDamage(damager, damage, shapeID);
    }

    public override Spatial GetWeaponParent(bool isPistol) {
        return GetNode<Spatial>("levitation/weapons");
    }

    public override void SetWeaponOn(bool isPistol) {
        SetMagicEmit(true);
    }

    public override void SetWeaponOff()
    {
        SetMagicEmit(false);
    }

    public override void _Ready()
    {
        base._Ready();
        audiHorn = GetNode<AudioStreamPlayer>("sound/audi_horn");
        teleportSound = GD.Load<AudioStreamSample>("res://assets/audio/magic/teleporting.wav");

        shield = GetNode<UnicornShield>("shield");
        hornMagic = GetNode<Particles>("player_body/Armature/Skeleton/BoneAttachment/HeadPos/Particles");
        teleportMark = GD.Load<PackedScene>("res://objects/characters/Player/magic/TeleportMark.tscn");
        teleportEffect = GD.Load<PackedScene>("res://objects/characters/Player/magic/TeleportEffect.tscn");

        Mana = MANA_MAX;
        messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
    }
    

    public override void _Process(float delta)
    {
        base._Process(delta);
        
        if (Mana < MANA_MAX)
        {
            Mana += MANA_SPEED * delta * (1/ManaDelta) * (1 - GetDamageBlock());
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (Health <= 0) return;
        if (!(@event is InputEventKey keyEvent)) return;
        if (keyEvent.Pressed || Input.IsActionPressed("dash")) return;
        if (!teleportPressed) return;
        
        teleportPressed = false;
        var manaIsEnough = ManaIsEnough(GetTeleportCost());
        if (!manaIsEnough || !tempTeleportMark.MayTeleport)
        {
            Camera.ReturnRayBack();
            ClearTeleportMark();

            if (!manaIsEnough)
            {
                messages.ShowMessage("notEnoughMana");
            }

            return;
        }
            
        startTeleporting = true;
    }

    private void SpawnTeleportMark()
    {
        if (tempTeleportMark != null) return;
        tempTeleportMark = (TeleportMark)teleportMark.Instance();
        GetParent().AddChild(tempTeleportMark);
        tempTeleportMark.GlobalTransform = Global.setNewOrigin(
            tempTeleportMark.GlobalTransform,
            GlobalTransform.origin
        );
    }

    private void ClearTeleportMark()
    {
        if (tempTeleportMark == null) return;
        tempTeleportMark.QueueFree();
        tempTeleportMark = null;
    }

    private void SpawnTeleportEffect() 
    {
        var effect = (Spatial)teleportEffect.Instance();
        GetParent().AddChild(effect);

        effect.GlobalTransform = Global.setNewOrigin(
            effect.GlobalTransform, 
            GlobalTransform.origin
        );
    }

    private float GetTeleportCost()
    {
        var tempDistance = GlobalTransform.origin.DistanceTo(tempTeleportMark.GlobalTransform.origin);
        return TELEPORT_MIN_COST + TELEPORT_COST * ManaDelta * (tempDistance / TELEPORT_DISTANCE);
    }

    public override async void UpdateStand()
    {
        if (!startTeleporting) return;
        OnStairs = false;
        audiHorn.Stream = teleportSound;
        audiHorn.Play();
        DecreaseMana(GetTeleportCost());

        SpawnTeleportEffect();

        GlobalTransform = 
            Global.setNewOrigin(GlobalTransform, tempTeleportMark.GetTeleportPoint());

        ClearTeleportMark();
        SpawnTeleportEffect();

        Camera.ReturnRayBack();
        startTeleporting = false;

        SetMagicEmit(true);
        await Global.Get().ToTimer(0.5f);
        SetMagicEmit(false);
    }

    public override void Jump() 
    {
        base.Jump();

        if (!Input.IsActionPressed("dash") || JumpHint.Visible) return;
        if (Health > 0)
        {
            var tempRay = Camera.UseRay(TELEPORT_DISTANCE);
            if(!teleportPressed) 
            {
                teleportPressed = true;
                SpawnTeleportMark();
            }
            else if (tempRay.IsColliding())
            {
                Spatial collider = (Spatial)tempRay.GetCollider();
                if (collider.Name == "sky") return;
                //оно может внезапно стереться даже здесь
                if (tempTeleportMark != null) 
                {
                    var place = tempRay.GetCollisionPoint();
                    place += tempRay.GetCollisionNormal() * 2f;

                    tempTeleportMark.UpdatePosition(place);
                    var teleportManaEnough = ManaIsEnough(GetTeleportCost());
                    tempTeleportMark.UpdateSprite(teleportManaEnough);
                }
                else 
                {
                    SpawnTeleportMark();
                }
            }
        }
        else //Health <= 0
        {
            if (!teleportPressed) return;
            teleportPressed = false;
            ClearTeleportMark();
        }
    }
    
    public override Dictionary GetSaveData()
    {
        Dictionary saveData = base.GetSaveData();
        saveData.Add("mana", Mana);
        saveData.Add("inside", teleportInside);
        return saveData;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        Mana = Convert.ToSingle(data["mana"]);
        teleportInside = Convert.ToBoolean(data["inside"]);
    }
}