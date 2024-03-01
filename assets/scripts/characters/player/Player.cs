using System;
using Godot;
using Godot.Collections;

public partial class Player : Character
{
    protected Global global = Global.Get();
    public float MouseSensivity = 0.1f;
    public const int CAMERA_MIN_Y = -65;
    public const int CAMERA_MAX_Y = 70;
    protected const int GRAVITY = -50;
    protected const float JUMP_SPEED = 18f;
    const float ACCEL = 5.5f;
    protected const float DEACCCEL = 12f;
    const float SHAKE_TIME = 0.1f;

    //Переменные состояния
    public bool MayRotateHead = true;
    public bool IsCrouching;
    public bool IsHitting;
    public bool IsSitting;
    public bool IsStealthBuck;
    public bool IsInvisibleForEnemy;
    private bool IsDead;

    public int LegsDamage = 0;
    public bool FoodCanHeal = true;

    public float PriceDelta = 1;

    //Ссылки на классы игрока
    public PlayerCamera Camera3D { get; private set; }
    public Node3D RotationHelper { get; private set; }
    private Node3D headShape;
    public PlayerThirdPerson RotationHelperThird;
    private Node3D CameraHeadPos;

    public PlayerBody Body;
    //сслыки внутри Body:
    // - Head
    // - Legs
    // - SoundSteps
    public PlayerStealth Stealth;
    public PlayerWeapons Weapons;
    public PlayerInventory Inventory;
    public PlayerRadiation Radiation;
    public PlayerDeathManager DeathManager;

    private DamageEffects damageEffects;
    protected SoundSteps soundSteps;
    public Control JumpHint;
    private AudioStreamPlayer audi;
    private AudioStreamPlayer audiHitted;

    //Переменные для передвижения

    private Vector3 dir;
    private float sideAngle;

    private Vector3 oldRot;

    private CollisionShape3D sphereCollider;
    private CollisionShape3D bodyCollider;

    public bool ThirdView = false;
    public bool BodyFollowsCamera;
    public bool OnStairs = false;
    private bool IsWalking = false;
    protected float crouchCooldown;
    private float bodyColliderSize = 1;

    public float shakingSpeed = 0;
    bool shakeUp = false;
    float shakeTimer = 0;

    [Signal]
    public delegate void ChangeViewEventHandler(bool toThird);

    [Signal]
    public delegate void FireWithWeaponEventHandler();

    [Signal]
    public delegate void TakeItemEventHandler(string itemCode);

    [Signal]
    public delegate void UseItemEventHandler(string itemCode);

    [Signal]
    public delegate void WearItemEventHandler(string itemCode);
    
    [Signal]
    public delegate void UnwearItemEventHandler(string itemCode);
    
    [Signal]
    public delegate void ClearWeaponBindEventHandler();


    public float GetVerticalLook()
    {
        return RotationHelper.RotationDegrees.X;
    }

    private bool OnFloor()
    {
        return soundSteps.landMaterial != null;
    }

    // интерфейс для вытаскивания audi (иногда он пустой)
    public AudioStreamPlayer GetAudi(bool hitted = false)
    {
        if (hitted)
        {
            return audiHitted ?? (audiHitted = GetNode<AudioStreamPlayer>("sound/audi_hitted"));
        }
        else
        {
            return audi ?? (audi = GetNode<AudioStreamPlayer>("sound/audi"));
        }
    }

    public void CheckTakeItem(string itemCode)
    {
        Radiation.CheckTakeRadiationCounter();
    }

    public void CheckDropItem(string itemCode)
    {
        Radiation.CheckDropRadiationCounter(itemCode);
    }

    public override int GetDamage()
    {
        float tempDamage = base.GetDamage();

        if (Inventory.weapon != "")
        {
            tempDamage += Weapons.GetStatsInt("damage");
        }

        tempDamage *= global.Settings.playerDamage;

        return (int)tempDamage;
    }

    public override float GetDamageBlock()
    {
        var armorProps = Inventory.GetArmorProps();
        
        if (armorProps.ContainsKey("damageBlock"))
        {
            var armorBlock = Global.ParseFloat(armorProps["damageBlock"].ToString());
            return base.GetDamageBlock() + armorBlock;
        }
        
        return base.GetDamageBlock();
    }

    public virtual Node3D GetWeaponParent(bool isPistol)
    {
        if (isPistol)
        {
            if (ThirdView) return GetNode<Node3D>("player_body/Armature/Skeleton3D/BoneAttachment3D/weapons");
            else return GetNode<Node3D>("rotation_helper/camera/weapons");
        }
        else
        {
            return GetNode<Node3D>("player_body/Armature/Skeleton3D/BoneAttachment3D 2/weapons");
        }
    }

    public virtual void SetWeaponOn(bool isPistol)
    {
        var bug = GetNode<Node3D>("player_body/Armature/Skeleton3D/BoneAttachment3D 2/shotgunBag");
        bug.Visible = !isPistol;
        BodyFollowsCamera = !isPistol;
    }

    public virtual void SetWeaponOff()
    {
        var bug = GetNode<Node3D>("player_body/Armature/Skeleton3D/BoneAttachment3D 2/shotgunBag");
        bug.Visible = false;
        BodyFollowsCamera = false;
    }

    public void LoadBodyMesh()
    {
        var bodyMesh = GetNode<MeshInstance3D>("player_body/Armature/Skeleton3D/Body");
        LoadClothMesh(Inventory.cloth, "first", bodyMesh);
        var bodyThirdMesh = GetNode<MeshInstance3D>("player_body/Armature/Skeleton3D/Body_third");
        LoadClothMesh(Inventory.cloth, "third", bodyThirdMesh);

        var head = bodyThirdMesh as PlayerHead;
        head.FindFaceMaterial();

        Body = GetNode<PlayerBody>("player_body");
        Body.SetHead(head);
    }

    private void LoadClothMesh(string clothName, string viewName, MeshInstance3D meshInstance)
    {
        //определяем название расы по текущей Race
        //(у единорогов от первого лица нет своей модельки)
        var playerRace = global.playerRace;

        var raceName = "earthpony";
        if (viewName == "first")
        {
            if (playerRace == Race.Pegasus)
            {
                raceName = "pegasus";
            }
        }
        else
        {
            if (playerRace == Race.Unicorn)
            {
                raceName = "unicorn";
            }

            if (playerRace == Race.Pegasus)
            {
                raceName = "pegasus";
            }
        }


        var path = "res://assets/models/player_variants/" + clothName + "/" + viewName + "/" + raceName + ".res";
        var loadedMesh = GD.Load<Mesh>(path);
        meshInstance.Mesh = loadedMesh;
    }

    public void LoadArtifactMesh(string artifactName = null)
    {
        var artifact = GetNode<MeshInstance3D>("player_body/Armature/Skeleton3D/artifact");
        artifact.Visible = artifactName != null;

        if (artifactName != null)
        {
            var path = "res://assets/models/player_variants/artifacts/" + artifactName + "/";
            var loadedMesh = GD.Load<Mesh>(path + "mesh.res");
            var loadedSkin = GD.Load<Skin>(path + "skin.res");
            artifact.Mesh = loadedMesh;
            artifact.Skin = loadedSkin;
        }
    }

    public bool IsIgnoringRotation()
    {
        return Mathf.Abs(RotationHelper.RotationDegrees.X - oldRot.X) > 0.01f;
    }

    public float GetCurrentSpeed()
    {
        return Velocity.Length();
    }

    public override int GetVelocity()
    {
        if (IsCrouching)
        {
            return BaseSpeed / 2;
        }

        return BaseSpeed;
    }

    public override void TakeDamage(Character damager, int damage, int shapeID = 0)
    {
        EmitSignal(nameof(TakenDamage));
        base.TakeDamage(damager, damage, shapeID);
        Body.Head.CloseEyes();
        damageEffects.StartEffect();
        
        if (Health <= 0 && !IsDead)
        {
            IsDead = true;
            Weapons.ClearWeapon();
            Body.AnimateDeath(damager);
            DeathManager.OnPlayerDeath();
        }
    }

    public override void HealHealth(int healing)
    {
        var tempMaxHealth = HealthMax - Radiation.GetRadLevel();
        var healthDiff = tempMaxHealth - Health;

        if (healing > healthDiff)
        {
            healing = healthDiff;
        }
        
        base.HealHealth(healing);
    }
    
    public void Sit(bool sitOn)
    {
        if (IsCrouching == sitOn)
        {
            return;
        }

        IsCrouching = sitOn;
        Stealth.SetLabelVisible(sitOn);
        if (sitOn)
        {
            bodyColliderSize = 0.56f;
            Body.Translate(new Vector3(0, 0.75f, 0));
            crouchCooldown = 0.5f;
        }
        else
        {
            bodyColliderSize = 1;
            Body.Translate(new Vector3(0, -0.75f, 0));
        }
    }

    public virtual void SitOnChair(bool sitOn)
    {
        Body.MakeSitting(sitOn);
        MayMove = !sitOn;
        IsSitting = sitOn;

        if (!sitOn) return;
        
        if (!string.IsNullOrEmpty(Inventory.weapon))
        {
            var useHandler = Inventory.UseHandler;
            useHandler.UnwearItem(useHandler.weaponButton);
        }
    }

    public void SetMayMove(bool value)
    {
        if (IsSitting) return;
        
        MayMove = value;
    }

    //для земнопня шоб бегал
    public virtual void UpdateGoForward() {}

    public virtual void UpdateStand() {}

    public virtual void Crouch()
    {
        if (Input.IsActionJustPressed("crouch"))
        {
            if (crouchCooldown <= 0 || !IsCrouching)
            {
                Sit(!IsCrouching);
            }
        }
    }

    public virtual void Jump()
    {
        if (Input.IsActionJustPressed("jump"))
        {
            if (IsCrouching)
            {
                Sit(false);
            }
            else
            {
                OnStairs = false;
                Velocity = new Vector3(Velocity.X, JUMP_SPEED, Velocity.Z);
                soundSteps.PlayJumpSound();
            }
        }
    }

    public virtual void Fly() {}

    private void UpdateCameraPos()
    {
        if (ThirdView)
        {
            var thirdRot = RotationHelperThird.Rotation;
            thirdRot.X = RotationHelper.Rotation.X;
            RotationHelperThird.Rotation = thirdRot;
        }
        else
        {
            var cameraTransf = RotationHelper.GlobalTransform;
            cameraTransf.Origin = CameraHeadPos.GlobalTransform.Origin;

            if (Health <= 0)
            {
                cameraTransf.Basis = CameraHeadPos.GlobalTransform.Basis;
            }

            RotationHelper.GlobalTransform = cameraTransf;
            headShape.GlobalTransform = cameraTransf;
        }
    }

    protected void ProcessInput(float delta)
    {
        dir = new Vector3();
        Vector2 inputMovementVector = new Vector2();
        int goSide = 0;
        UpdateStand();

        if (Input.IsActionPressed("ui_up"))
        {
            inputMovementVector.Y += 1;
            UpdateGoForward();
        }

        if (Input.IsActionPressed("ui_down"))
        {
            inputMovementVector.Y -= 1;
        }

        if (Input.IsActionPressed("ui_left"))
        {
            inputMovementVector.X -= 1;
            goSide = 1;
        }

        if (Input.IsActionPressed("ui_right"))
        {
            inputMovementVector.X += 1;
            goSide = -1;
        }

        //наклон камеры при движении вбок
        if (goSide != 0)
        {
            sideAngle += GetCurrentSpeed() * goSide * delta;
            sideAngle = Mathf.Clamp(sideAngle, -2, 2);
        }
        else
        {
            sideAngle = Mathf.Lerp(sideAngle, 0, 10 * delta);
        }

        Camera3D.RotationDegrees = new Vector3(
            Camera3D.RotationDegrees.X,
            Camera3D.RotationDegrees.X,
            global.Settings.cameraAngle ? sideAngle : 0
        );

        inputMovementVector = inputMovementVector.Normalized();
        IsWalking = inputMovementVector.Length() > 0;

        Transform3D camXForm = Camera3D.GlobalTransform;
        dir += -camXForm.Basis.Z * inputMovementVector.X;
        dir += camXForm.Basis.X * inputMovementVector.X;

        if (OnFloor())
        {
            Jump();

            if (crouchCooldown > 0)
            {
                crouchCooldown -= delta;
            }

            Crouch();
        }
        else
        {
            Fly();
        }

        if (bodyCollider.Scale.Y != bodyColliderSize)
        {
            Vector3 bodyScale = bodyCollider.Scale;
            Vector3 sphereScale = sphereCollider.Scale;
            bodyScale.Y = bodyColliderSize;
            sphereScale.Y = bodyColliderSize;
            bodyCollider.Scale = bodyScale;
            sphereCollider.Scale = sphereScale;
        }
    }

    float GetTempShake(float delta)
    {
        float tempShake = shakingSpeed;

        if (!shakeUp)
        {
            tempShake *= -1;
        }

        if (shakeTimer > 0)
        {
            shakeTimer -= delta;
        }
        else
        {
            shakeTimer = SHAKE_TIME;
            shakeUp = !shakeUp;
        }

        return tempShake;
    }

    public virtual float GetGravitySpeed(float tempShake, float delta)
    {
        return Velocity.Y + (GRAVITY * delta + tempShake);
    }

    public virtual float GetDeacceleration()
    {
        return DEACCCEL;
    }

    private void ProcessMovement(float delta)
    {
        dir.X = 0;
        dir = dir.Normalized();

        float tempShake = 0;
        if (shakingSpeed > 0)
        {
            tempShake = GetTempShake(delta);
        }

        if (OnStairs && OnFloor() && !IsWalking && Velocity.Y <= 0)
        {
            Velocity = new Vector3(Velocity.X, 0, Velocity.Z);
        }
        else
        {
            Velocity = new Vector3(Velocity.X, GetGravitySpeed(tempShake, delta), Velocity.Z);
        }

        var hvel = Velocity;
        hvel.X = 0;

        var target = dir;
        target *= GetVelocity();

        float acceleration;
        if (dir.Dot(hvel) > 0)
        {
            acceleration = ACCEL;
        }
        else
        {
            acceleration = GetDeacceleration();
        }

        hvel = hvel.Lerp(target, acceleration * delta);
        Velocity = new Vector3(hvel.X, Velocity.Y, hvel.Z);
        MoveAndSlide();
    }

    private void RotateBodyClumped(float speedX)
    {
        if (IsSitting)
        {
            if (speedX > 0 && Body.RotClumpsMin)
            {
                RotateY(Mathf.DegToRad(speedX));
            }

            if (speedX < 0 && Body.RotClumpsMax)
            {
                RotateY(Mathf.DegToRad(speedX));
            }
        }
        else
        {
            RotateY(Mathf.DegToRad(speedX));
        }
    }

    public virtual void OnCameraRotatingX(float speedX) {}

    protected void RotateCamera(InputEvent @event)
    {
        if (@event is InputEventMouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured && MayRotateHead)
        {
            oldRot = RotationHelper.RotationDegrees;

            var mouseEvent = @event as InputEventMouseMotion;
            RotationHelper.RotateX(Mathf.DegToRad(mouseEvent.Relative.X * -MouseSensivity));
            RotateBodyClumped(mouseEvent.Relative.X * -MouseSensivity);

            Vector3 cameraRot = RotationHelper.RotationDegrees;
            cameraRot.X = Mathf.Clamp(cameraRot.X, CAMERA_MIN_Y, CAMERA_MAX_Y);
            cameraRot.X = 0;
            cameraRot.Z = global.Settings.cameraAngle ? sideAngle : 0;
            RotationHelper.RotationDegrees = cameraRot;

            OnCameraRotatingX(mouseEvent.Relative.X);
        }
    }

    public void LookAt(Vector3 target)
    {
        var dir = target - GlobalTransform.Origin;
        var forward = -GlobalTransform.Basis.Z;
        var cameraForward = -RotationHelper.GlobalTransform.Basis.X;

        //берем угол по плоскости XZ (горизонтальное вращение)
        var horizontalDir = new Vector2(dir.X, dir.Z);
        var horizontalPos = new Vector2(forward.X, forward.Z);
        var horizontalAngle = horizontalPos.AngleTo(horizontalDir);
        //вращаем тело на этот угол (который постепенно уменьшается до нуля)
        RotateY(-horizontalAngle);
    }

    public override async void LoadData(Dictionary data)
    {
        Inventory.LoadData((Dictionary)data["inventory"]);
        if (data.Count <= 1) return;
        
        Radiation.SetRadLevel(Convert.ToInt32(data["radLevel"]));
        
        base.LoadData(data);

        bool sittingOnChair = Convert.ToBoolean(data["sitOnChair"]);
        if (sittingOnChair)
        {
            //await нужен, чтоб PlayerBody успел загрузиться
            await ToSignal(GetTree(), "idle_frame");
            SitOnChair(true);
        }
    }

    public override Dictionary GetSaveData()
    {
        Dictionary saveData = base.GetSaveData();
        saveData["inventory"] = Inventory.GetSaveData();
        saveData["sitOnChair"] = IsSitting;
        saveData["radLevel"] = Radiation.GetRadLevel();
        return saveData;
    }

    public override void _Ready()
    {
        global.player = this;
        Inventory = new PlayerInventory(this);
        Radiation = new PlayerRadiation(this);
        DeathManager = GetNode<PlayerDeathManager>("deathManager");

        BaseSpeed = 15;
        BaseRecoil = 2;
        BaseDamage = 0;
        LegsDamage = 50;
        SetStartHealth(100);
        LoadBodyMesh();
        Stealth = GetNode<PlayerStealth>("stealth");
        Weapons = GetNode<PlayerWeapons>("gun_shape");
        RotationHelperThird = GetNode<PlayerThirdPerson>("rotation_helper_third");
        soundSteps = GetNode<SoundSteps>("player_body/floorRay");

        var canvas = GetNode("/root/Main/Scene/canvas/");
        JumpHint = canvas.GetNode<Control>("jumpHint");
        damageEffects = canvas.GetNode<DamageEffects>("redScreen");
        Camera3D = GetNode<PlayerCamera>("rotation_helper/camera");
        RotationHelper = GetNode<Node3D>("rotation_helper");
        headShape = GetNode<Node3D>("headShape");
        CameraHeadPos = GetNode<Node3D>("player_body/Armature/Skeleton3D/BoneAttachment3D/HeadPos");

        audi = GetAudi();
        audiHitted = GetAudi(true);

        sphereCollider = GetNode<CollisionShape3D>("shape");
        bodyCollider = GetNode<CollisionShape3D>("body_shape");

        MouseSensivity = global.Settings.mouseSensivity;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        Connect(nameof(TakeItemEventHandler), new Callable(this, nameof(CheckTakeItem)));
    }

    public override void _Process(double delta)
    {
        bodyCollider.Rotation = Body.Rotation;
        oldRot = RotationHelper.RotationDegrees;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Health > 0)
        {
            if (MayMove)
            {
                ProcessInput((float)delta);
                HandleImpulse();
            }

            ProcessMovement((float)delta);
        }

        if (Health < 0 || !MayMove)
        {
            dir = Vector3.Zero;
        }

        UpdateCameraPos();
    }

    public override void _Input(InputEvent @event)
    {
        if (Health > 0)
        {
            RotateCamera(@event);
        }
    }
}