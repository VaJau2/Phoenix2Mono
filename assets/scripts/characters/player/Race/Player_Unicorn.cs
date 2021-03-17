using Godot;

public class Player_Unicorn : Player
{
    private const float MANA_SPEED = 3f;
    private const float TELEPORT_COST = 50f;
    private const int TELEPORT_DISTANCE = 150;
    public const float MANA_MAX = 100;
    public float Mana;
    public float ManaDelta = 1f;

    public AudioStreamPlayer audiHorn;
    private AudioStreamSample teleportSound;
    private Messages messages;
    private Particles hornMagic;
    private UnicornShield shield;

    private PackedScene teleportMark;
    private PackedScene teleportEffect;

    private Spatial tempTeleportMark;
    private bool notEnoughMana = false;
    private bool teleportPressed = false;
    private bool startTeleporting = false;
    private bool teleportInside = false;

    private ProgressBar manaBar;

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
        if (shield.shieldOn) {
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
        manaBar = GetNode<ProgressBar>("/root/Main/Scene/canvas/manaBar");
        messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
    }
    

    public override void _Process(float delta)
    {
        base._Process(delta);
        if (Mana < MANA_MAX)
        {
            Mana += MANA_SPEED * delta * (1/ManaDelta) * (1 - GetDamageBlock());
            manaBar.Visible = true;
            manaBar.Value = Mana;
        }
        else 
        {
            if(manaBar.Visible) 
            {
                manaBar.Visible = false;
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (Health > 0)
        {
            if (@event is InputEventKey)
            {
                var keyEvent = @event as InputEventKey;
                if (!keyEvent.Pressed && !Input.IsActionPressed("jump"))
                {
                    if (teleportPressed) {
                        teleportPressed = false;
                        startTeleporting = true;
                    }
                    if (notEnoughMana) {
                        notEnoughMana = false;
                    }
                };
            }
        }
    }

    private void SpawnTeleportEffect() 
    {
        var effect = (Spatial)teleportEffect.Instance();
        GetParent().AddChild(effect);

        effect.GlobalTransform = 
            Global.setNewOrigin(effect.GlobalTransform, GlobalTransform.origin);
    }

    public override async void UpdateStand()
    {
        if (startTeleporting) 
        {
            OnStairs = false;
            audiHorn.Stream = teleportSound;
            audiHorn.Play();

            SpawnTeleportEffect();

            GlobalTransform = 
                Global.setNewOrigin(GlobalTransform, tempTeleportMark.GlobalTransform.origin);

            tempTeleportMark.QueueFree();
            tempTeleportMark = null;
            DecreaseMana(TELEPORT_COST * ManaDelta);

            SpawnTeleportEffect();

            Camera.ReturnRayBack();
            startTeleporting = false;

            SetMagicEmit(true);
            await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
            SetMagicEmit(false);
        }
    }

    public override void Jump() 
    {
        if (Input.IsActionPressed("jump") && !JumpHint.Visible) 
        {
            if (ManaIsEnough(TELEPORT_COST * ManaDelta)) {
                if (Health > 0)
                {
                    var tempRay = Camera.UseRay(TELEPORT_DISTANCE);
                    if(!teleportPressed) 
                    {
                        teleportPressed = true;
                        if (tempTeleportMark == null)
                        {
                            tempTeleportMark = (Spatial)teleportMark.Instance();
                            GetParent().AddChild(tempTeleportMark);

                            tempTeleportMark.GlobalTransform = Global.setNewOrigin(
                                tempTeleportMark.GlobalTransform,
                                GlobalTransform.origin
                            );
                        }
                    } 
                    else if (tempRay.IsColliding())
                    {
                        Spatial collider = (Spatial)tempRay.GetCollider();
                        if (collider.Name != "sky") 
                        {
                            //оно может внезапно стереться даже здесь
                            if (tempTeleportMark != null) 
                            {
                                var place = tempRay.GetCollisionPoint();

                                tempTeleportMark.GlobalTransform = Global.setNewOrigin(
                                    tempTeleportMark.GlobalTransform,
                                    place
                                );

                                //чтоб не перемещаться через стенки бункера наружу
                                if (teleportInside && 
                                    tempTeleportMark.Translation.y > Translation.y + 3)
                                    {
                                        Vector3 newPos = tempTeleportMark.Translation;
                                        newPos.y -= 3;
                                        tempTeleportMark.Translation = newPos;
                                    }
                            }
                            else 
                            {
                                tempTeleportMark = (Spatial)teleportMark.Instance();
                                GetParent().AddChild(tempTeleportMark);
                            }
                        }
                    }
                }
                else //Health <= 0
                {
                    if (teleportPressed)
                    {
                        teleportPressed = false;
                        if (tempTeleportMark != null)
                        {
                            tempTeleportMark.QueueFree();
                            tempTeleportMark = null;
                        }
                    }
                }
            } else {
                if (!notEnoughMana) {
                    notEnoughMana = true;
                    messages.ShowMessage("notEnoughMana");
                }
            }
        }
    }
}