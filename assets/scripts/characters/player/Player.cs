using Godot;
using Godot.Collections;

public class Player : Character
{
    protected Global global = Global.Get();
    public float MouseSensivity = 0.1f;
    const int CAMERA_MIN_Y = -65;
    const int CAMERA_MAX_Y = 70;
    protected const int GRAVITY = -50;
    protected const float JUMP_SPEED = 18f;
    const float ACCEL = 5.5f;
    protected const float DEACCCEL = 12f;
    const float SHAKE_TIME = 0.1f;

    //Переменные состояния
    public bool MayMove = true;
    public bool MayRotateHead = true;
    public bool IsCrouching;
    public bool IsHitting;
    public bool IsLying;
    public int LegsDamage = 0;
    public bool FoodCanHeal = true;
    public float PriceDelta = 1;
    //Ссылки на классы игрока
    public PlayerCamera Camera {get; private set;}
    public Spatial RotationHelper {get; private set;}
    private Spatial headShape;
    public PlayerThirdPerson RotationHelperThird;
    private Spatial CameraHeadPos;
    public PlayerBody Body;
    //сслыки внутри body:
    // - Head
    // - Legs
    // - SoundSteps
    public PlayerStealth Stealth;
    public PlayerWeapons Weapons;
    public PlayerInventory inventory;

    private DamageEffects damageEffects;
    private ColorRect blackScreen;
    protected SoundSteps soundSteps;
    public Control JumpHint;
    private AudioStreamPlayer audi;
    private AudioStreamPlayer audiHitted;

    //Переменные для передвижения
    private Vector3 dir;
    public Vector3 impulse;

    private CollisionShape sphereCollider;
    private CollisionShape bodyCollider;

    public bool ThirdView = false;
    public bool BodyFollowsCamera;
    public bool OnStairs = false;
    private bool IsWalking = false;
    protected float crouchCooldown;
    private float bodyColliderSize = 1;

    float shakingSpeed = 0;
    bool shakeUp = false;
    float shakeTimer = 0;

    [Signal]
    public delegate void TakenDamage();


    public float GetVerticalLook() { return RotationHelper.RotationDegrees.x; }
    private bool OnFloor() {return soundSteps.landMaterial != null; }

    // интерфейс для вытаскивания audi (иногда он пустой)
    public AudioStreamPlayer GetAudi(bool hitted = false) {
        if (hitted) {
            if (audiHitted == null) {
                audiHitted = GetNode<AudioStreamPlayer>("sound/audi_hitted");
            }
            return audiHitted;
        } else {
            if (audi == null) {
                audi = GetNode<AudioStreamPlayer>("sound/audi");
            }
            return audi;
        }
    }

    public override int GetDamage()
    {
        int tempDamage = base.GetDamage();
        if (inventory.weapon != "") {
            tempDamage += Weapons.GetStatsInt("damage");
        }
        return tempDamage;
    }

    public override float GetDamageBlock() 
    {
        Dictionary armorProps = inventory.GetArmorProps();
        if(armorProps.Contains("damageBlock")) {
            float armorBlock = Global.ParseFloat(armorProps["damageBlock"].ToString());
            return base.GetDamageBlock() + armorBlock;
        } else {
            return base.GetDamageBlock();
        }
    }

    public virtual Spatial GetWeaponParent(bool isPistol)
    {
        if (isPistol) {
            if (ThirdView) {
                return GetNode<Spatial>("player_body/Armature/Skeleton/BoneAttachment/weapons");
            } else {
                return GetNode<Spatial>("rotation_helper/camera/weapons");
            }
        } else {
            return GetNode<Spatial>("player_body/Armature/Skeleton/BoneAttachment 2/weapons");
        }
    }

    public virtual void SetWeaponOn(bool isPistol)
    {
        var bug = GetNode<Spatial>("player_body/Armature/Skeleton/BoneAttachment 2/shotgunBag");
        bug.Visible = !isPistol;
        BodyFollowsCamera = !isPistol;
    }

    public virtual void SetWeaponOff()
    {
        var bug = GetNode<Spatial>("player_body/Armature/Skeleton/BoneAttachment 2/shotgunBag");
        bug.Visible = false;
        BodyFollowsCamera = false;
    }

    public void LoadBodyMesh() 
    {
        var bodyMesh = GetNode<MeshInstance>("player_body/Armature/Skeleton/Body");
        LoadClothMesh(inventory.cloth, "first", bodyMesh);
        var bodyThirdMesh = GetNode<MeshInstance>("player_body/Armature/Skeleton/Body_third");
        LoadClothMesh(inventory.cloth, "third", bodyThirdMesh);
        
        PlayerHead head = bodyThirdMesh as PlayerHead;
        head.FindFaceMaterial();

        Body = GetNode<PlayerBody>("player_body");
        Body.SetHead(head);
    }

    private void LoadClothMesh(string clothName, string viewName, MeshInstance meshInstance) 
    {
        //определяем название расы по текущей Race
        //(у единорогов от первого лица нет своей модельки)
        Race playerRace = global.playerRace;

        string raceName = "earthpony";
        if (viewName == "first") {
            if (playerRace == Race.Pegasus) {
                raceName = "pegasus";
            }
        } else {
            if (playerRace == Race.Unicorn) {
                raceName = "unicorn";
            }
            if (playerRace == Race.Pegasus) {
                raceName = "pegasus";
            }
        }
        

        string path = "res://assets/models/player_variants/" + clothName + "/" + viewName + "/" + raceName + ".res";
        Mesh loadedMesh = GD.Load<Mesh>(path);
        meshInstance.Mesh = loadedMesh;
    }

    public void LoadArtifactMesh(string artifactName = null)
    {
        var artifact = GetNode<MeshInstance>("player_body/Armature/Skeleton/artifact");
        artifact.Visible = artifactName != null;

        if (artifactName != null) {
            string path = "res://assets/models/player_variants/artifacts/" + artifactName + "/";
            Mesh loadedMesh = GD.Load<Mesh>(path + "mesh.res");
            Skin loadedSkin = GD.Load<Skin>(path + "skin.res");
            artifact.Mesh = loadedMesh;
            artifact.Skin = loadedSkin;
        }
    }

    private void HandleImpulse() 
    {
        if(impulse.Length() > 0) {
            MoveAndCollide(impulse);
            Vector3 newImpulse = impulse;
            newImpulse /= 1.5f;
            impulse = newImpulse;
        }
    }

    public void Faint(bool MakeRoped = false) 
    {
        IsLying = true;
        MayMove = false;
        Body.MakeLying(true);
    }

    async private void GetUp() 
    {
        MayMove = false;
        Body.MakeLying(false);
        await ToSignal(GetTree().CreateTimer(1.6f), "timeout");
        if(IsLying) {
            IsLying = false;
            MayMove = true;
        }
    }

    public override int GetSpeed()
    {
        if (IsCrouching) {
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
        if (Health <= 0) {
            Weapons.ClearWeapon();
            AnimateDealth();
            Body.AnimateDealth(damager);
        }
    }

    private async void AnimateDealth()
    {
        while (blackScreen.Color.a < 1) {
            Color temp = blackScreen.Color;
            temp.a += 0.01f;
            blackScreen.Color = temp;
            await global.ToTimer(0.04f);
        }
        GetNode<LevelsLoader>("/root/Main").ShowDealthMenu();
    }

    protected void Sit(bool sitOn) 
    {
        IsCrouching = sitOn;
        Stealth.SetLabelVisible(sitOn);
        if (sitOn) {
            bodyColliderSize = 0.56f;
            Body.Translate(new Vector3(0, 0.75f, 0));
            crouchCooldown = 0.5f;
        } else {
            bodyColliderSize = 1;
            Body.Translate(new Vector3(0, -0.75f, 0));
        }
    }

    //для земнопня шоб бегал
    public virtual void UpdateGoForward() {}

    public virtual void UpdateStand() {}

    public virtual void Crouch() 
    {
        if (Input.IsActionJustPressed("crouch")) {
            if (crouchCooldown <= 0 || !IsCrouching) {
                Sit(!IsCrouching);
            }
        }
    }

    public virtual void Jump() 
    {
        if (Input.IsActionJustPressed("jump")) {
            if (IsCrouching) {
                Sit(false);
            }
        }
    }

    public virtual void Fly() {}

    private void UpdateCameraPos() 
    {
        if (ThirdView) {
            Vector3 thirdRot = RotationHelperThird.Rotation;
            thirdRot.x = RotationHelper.Rotation.x;
            RotationHelperThird.Rotation = thirdRot;
        } else {
            Transform cameraTransf = RotationHelper.GlobalTransform;
            cameraTransf.origin = CameraHeadPos.GlobalTransform.origin;
            if (Health <= 0) {
                cameraTransf.basis = CameraHeadPos.GlobalTransform.basis;
            }
            RotationHelper.GlobalTransform = cameraTransf;
            headShape.GlobalTransform = cameraTransf;
        }
    }

    protected void ProcessInput(float delta) 
    {
        dir = new Vector3();

        Vector2 inputMovementVector = new Vector2();
        UpdateStand();
        
        if (Input.IsActionPressed("ui_up")) {
            inputMovementVector.y += 1;
            UpdateGoForward();
        }
        if (Input.IsActionPressed("ui_down")) {
            inputMovementVector.y -= 1;
        }
        if (Input.IsActionPressed("ui_left")) {
            inputMovementVector.x -= 1;
        }
        if (Input.IsActionPressed("ui_right")) {
            inputMovementVector.x += 1;
        }

        if (IsLying && inputMovementVector.Length() > 0) {
            GetUp();
        }

        inputMovementVector = inputMovementVector.Normalized();
        IsWalking = inputMovementVector.Length() > 0;

        Transform camXForm = Camera.GlobalTransform;
        dir += -camXForm.basis.z * inputMovementVector.y;
	    dir += camXForm.basis.x * inputMovementVector.x;

        if (OnFloor()) {
            Jump();

            if (crouchCooldown > 0) {
                crouchCooldown -= delta;
            }

            Crouch();
        } else {
            Fly();
        }

        if (bodyCollider.Scale.y != bodyColliderSize) {
            Vector3 bodyScale = bodyCollider.Scale;
            Vector3 sphereScale = sphereCollider.Scale;
            bodyScale.y = bodyColliderSize;
            sphereScale.y = bodyColliderSize;
            bodyCollider.Scale = bodyScale;
            sphereCollider.Scale = sphereScale;
        }
    }

    float GetTempShake(float delta) {
        float tempShake = shakingSpeed;

        if (!shakeUp) {
            tempShake *= -1;
        }

        if (shakeTimer > 0) {
            shakeTimer -= delta;
        } else {
            shakeTimer = SHAKE_TIME;
            shakeUp = !shakeUp;
        }

        return tempShake;
    }

    public virtual float GetGravitySpeed(float tempShake, float delta) 
    {
        return Velocity.y + (GRAVITY * delta + tempShake);
    }

    public virtual float GetDeacceleration() 
    {
        return DEACCCEL;
    }

    private void ProcessMovement(float delta) 
    {
        dir.y = 0;
        dir = dir.Normalized();

        float tempShake = 0;
        if (shakingSpeed > 0) {
            tempShake = GetTempShake(delta);
        }

        if (OnStairs && OnFloor() && !IsWalking && Velocity.y <= 0) {
            Velocity.y = 0;
        } else {
            Velocity.y = GetGravitySpeed(tempShake, delta);
        }

        var hvel = Velocity;
        hvel.y = 0;

        var target = dir;
        target *= GetSpeed();

        float acceleration;
        if (dir.Dot(hvel) > 0) {
            acceleration = ACCEL;
        } else {
            acceleration = GetDeacceleration();
        }

        hvel = hvel.LinearInterpolate(target, acceleration * delta);
        Velocity.x = hvel.x;
        Velocity.z = hvel.z;
        Velocity = MoveAndSlide(Velocity, new Vector3(0, 1, 0), true, 4);
    }

    private void RotateBodyClumped(float speedX)
    {
        if (IsLying) 
        {
            if(speedX > 0 && Body.RotClumpsMin) {
                RotateY(Mathf.Deg2Rad(speedX));
            }
            if(speedX < 0 && Body.RotClumpsMax) {
                RotateY(Mathf.Deg2Rad(speedX));
            }
        }
        else {
            RotateY(Mathf.Deg2Rad(speedX));
        }
    }

    public virtual void OnCameraRotatingX(float speedX) {}

    protected void RotateCamera(InputEvent @event) 
    {
        if (@event is InputEventMouseMotion
            && Input.GetMouseMode() == Input.MouseMode.Captured
            && MayRotateHead) {

            var mouseEvent = @event as InputEventMouseMotion;
            RotationHelper.RotateX(Mathf.Deg2Rad(mouseEvent.Relative.y * -MouseSensivity));
            RotateBodyClumped(mouseEvent.Relative.x * -MouseSensivity);

            Vector3 cameraRot = RotationHelper.RotationDegrees;
            cameraRot.x = Mathf.Clamp(cameraRot.x, CAMERA_MIN_Y, CAMERA_MAX_Y);
            cameraRot.y = 0;
            cameraRot.z = 0;
            RotationHelper.RotationDegrees = cameraRot;

            OnCameraRotatingX(mouseEvent.Relative.x);
        }
    }

    public void LookAt(Vector3 target)
    {
        var dir = target - GlobalTransform.origin;
        var forward = -GlobalTransform.basis.z;
        var cameraForward = -RotationHelper.GlobalTransform.basis.y;

        //берем угол по плоскости XZ (горизонтальное вращение)
        var horizontalDir = new Vector2(dir.x, dir.z);
        var horizontalPos = new Vector2(forward.x, forward.z);
        var horizontalAngle = horizontalPos.AngleTo(horizontalDir);
        //вращаем тело на этот угол (который постепенно уменьшается до нуля)
        RotateY(-horizontalAngle);
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
    }

    public override Dictionary GetSaveData()
    {
        return base.GetSaveData();
    }

    public override void _Ready()
    {
        global.player = this;
        inventory = new PlayerInventory(this);

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
        blackScreen = canvas.GetNode<ColorRect>("black");
        Camera = GetNode<PlayerCamera>("rotation_helper/camera");
        RotationHelper = GetNode<Spatial>("rotation_helper");
        headShape = GetNode<Spatial>("headShape");
        CameraHeadPos = GetNode<Spatial>("player_body/Armature/Skeleton/BoneAttachment/HeadPos");

        audi = GetAudi();
        audiHitted = GetAudi(true);

        sphereCollider = GetNode<CollisionShape>("shape");
        bodyCollider = GetNode<CollisionShape>("body_shape");

        MouseSensivity = global.Settings.mouseSensivity;
        Input.SetMouseMode(Input.MouseMode.Captured);
    }

    public override void _Process(float delta)
    {
        bodyCollider.Rotation = Body.Rotation;
    }

    public override void _PhysicsProcess(float delta)
    {
        if (Health > 0) {
            if (MayMove) {
                ProcessInput(delta);
                HandleImpulse();
            }
            
            ProcessMovement(delta);
        }
        if (Health < 0 || !MayMove) {
            dir = Vector3.Zero;
        }

        UpdateCameraPos();
    }

    public override void _Input(InputEvent @event)
    {
        if (Health > 0) {
            RotateCamera(@event);
        }
    }
}
