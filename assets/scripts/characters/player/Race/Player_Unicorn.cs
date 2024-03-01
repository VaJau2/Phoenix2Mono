using Godot;
using Godot.Collections;
using System;

public partial class Player_Unicorn : Player
{
    private const float MANA_SPEED = 5f;
    private const float TELEPORT_COST = 50f;
    private const float TELEPORT_MIN_COST = 10f;
    private const int TELEPORT_DISTANCE = 150;
    public const float MANA_MAX = 100;
    public float Mana;
    public float ManaDelta = 1f;

    public AudioStreamPlayer audiHorn;
    private AudioStreamWav teleportSound;
    private Messages messages;
    private GpuParticles3D hornMagic;
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

    public override Node3D GetWeaponParent(bool isPistol) 
    {
        return GetNode<Node3D>("levitation/weapons");
    }

    public override void SetWeaponOn(bool isPistol) 
    {
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
        teleportSound = GD.Load<AudioStreamWav>("res://assets/audio/magic/teleporting.wav");

        shield = GetNode<UnicornShield>("shield");
        hornMagic = GetNode<GpuParticles3D>("player_body/Armature/Skeleton3D/BoneAttachment3D/HeadPos/Particles");
        teleportMark = GD.Load<PackedScene>("res://objects/characters/Player/magic/TeleportMark.tscn");
        teleportEffect = GD.Load<PackedScene>("res://objects/characters/Player/magic/TeleportEffect.tscn");

        Mana = MANA_MAX;
        messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
    }
    

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        if (Mana < MANA_MAX)
        {
            Mana += MANA_SPEED * (float)delta * (1/ManaDelta) * (1 - GetDamageBlock());
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
            Camera3D.ReturnRayBack();
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
        tempTeleportMark = teleportMark.Instantiate<TeleportMark>();
        GetParent().AddChild(tempTeleportMark);
        tempTeleportMark.GlobalTransform = Global.SetNewOrigin(
            tempTeleportMark.GlobalTransform,
            GlobalTransform.Origin
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
        var effect = teleportEffect.Instantiate<Node3D>();
        GetParent().AddChild(effect);

        effect.GlobalTransform = Global.SetNewOrigin(
            effect.GlobalTransform, 
            GlobalTransform.Origin
        );
    }

    private float GetTeleportCost()
    {
        var tempDistance = GlobalTransform.Origin.DistanceTo(tempTeleportMark.GlobalTransform.Origin);
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
            Global.SetNewOrigin(GlobalTransform, tempTeleportMark.GetTeleportPoint());

        ClearTeleportMark();
        SpawnTeleportEffect();

        Camera3D.ReturnRayBack();
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
            var tempRay = Camera3D.UseRay(TELEPORT_DISTANCE);
            if(!teleportPressed) 
            {
                teleportPressed = true;
                SpawnTeleportMark();
            }
            else if (tempRay.IsColliding())
            {
                Node3D collider = (Node3D)tempRay.GetCollider();
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
        
        if (data.TryGetValue("mana", out var mana))
        {
            Mana = Convert.ToSingle(mana);
        }
        
        if (data.TryGetValue("inside", out var isInside))
        {
            teleportInside = Convert.ToBoolean(isInside);
        }
    }
}