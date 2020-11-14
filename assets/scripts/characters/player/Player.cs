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
    const float MAX_SLOPE_ANGLE = 50;
    const float SHAKE_TIME = 0.1f;

    //Player state booleans
    [Export]
    public bool HaveCoat;
    [Export]
    public Array<WeaponTypes> StartWeapons = new Array<WeaponTypes>();
    public bool MayMove = true;
    public bool IsCrouching;
    public bool IsHitting;
    public bool IsLying;
    public bool IsRoped;

    public Array<string> HaveKeys = new Array<string>();

    private float unropingTime = 0;

    private AudioStreamSample unropingSound = 
        GD.Load<AudioStreamSample>("res://assets/audio/enemies/ropes/unroping.wav");

    //Player parts links
    public Camera Camera {get; private set;}
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

    public Control JumpHint;

    private AudioStreamPlayer audi;
    private AudioStreamPlayer audiHitted;

    //Moving  variables
    protected float MaxSpeed = 17f;
    private Vector3 dir;
    public Vector3 impulse;

    private CollisionShape sphereCollider;
    private CollisionShape bodyCollider;

    public bool ThirdView;
    public bool BodyFollowsCamera;
    public bool BlockJump;
    public bool OnStairs = false;

    private float sideAngle;
    private float stairGravity;
    protected float crouchCooldown;
    private float bodyColliderSize = 1;

    float shakingSpeed = 0;
    bool shakeUp = false;
    float shakeTimer = 0;

    private static string socksPath = "res://assets/materials/player/";
    private Material bodyMaterial = GD.Load<Material>(socksPath + "player_body.material");
    private Material socksMaterial = GD.Load<Material>(socksPath + "player_body_socks.material");

    public Dictionary<string, bool> equipment = new Dictionary<string, bool>() 
    {
        {"have_armor", false},
        {"have_socks", false},
        {"have_bandage", false},
        {"have_headrope", false}
    };

    public float GetVerticalLook() { return RotationHelper.RotationDegrees.x; }

    /// <summary>
    /// интерфейс для вытаскивания audi (иногда он пустой)
    /// </summary>
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

    public void LoadPlayer()
    {
        global.player = this;

        LoadHeadBody(global.playerRace);
        Stealth = GetNode<PlayerStealth>("stealth");
        Weapons = GetNode<PlayerWeapons>("gun_shape");
        JumpHint = GetNode<Control>("/root/Main/Scene/canvas/jumpHint");

        Camera = GetNode<Camera>("rotation_helper/camera");
        RotationHelper = GetNode<Spatial>("rotation_helper");
        headShape = GetNode<Spatial>("headShape");
        RotationHelperThird = GetNode<PlayerThirdPerson>("rotation_helper_third");
        CameraHeadPos = GetNode<Spatial>("player_body/Armature/Skeleton/BoneAttachment/Head/cameraPos");

        audi = GetAudi();
        audiHitted = GetAudi(true);

        sphereCollider = GetNode<CollisionShape>("shape");
        bodyCollider = GetNode<CollisionShape>("body_shape");

        MouseSensivity = global.Settings.mouseSensivity;
        Input.SetMouseMode(Input.MouseMode.Captured);
    }

    private void LoadMesh(string meshName, MeshInstance meshInstance, int[] parameters) 
    {
        string fileName = "";
        for(int i = 0; i < parameters.Length; i++) {
            fileName += parameters[i].ToString();
        }

        string path = "res://assets/models/player_variants/" + meshName + "/" + fileName + ".res";
        Mesh loadedMesh = GD.Load<Mesh>(path);
        meshInstance.Mesh = loadedMesh;
    }

    public void LoadHeadMesh(Race playerRace) 
    {
        int[] headParams = new int[4];

        if (playerRace == Race.Unicorn) {
            headParams[0] = 1;
        }
        if (HaveCoat) {
            headParams[1] = 1;
        }
        if(equipment["have_bandage"]) {
            headParams[2] = 1;
        }
        if(equipment["have_headrope"]) {
            headParams[3] = 1;
        }

        MeshInstance headMesh = GetNode<MeshInstance>("player_body/Armature/Skeleton/BoneAttachment/Head");
        LoadMesh("head", headMesh, headParams);
    }

    public void LoadBodyMesh(Race playerRace) 
    {
        int[] bodyParams = new int[3];

        if(HaveCoat) {
            bodyParams[0] = 1;
        }
        if(equipment["have_armor"]) {
            bodyParams[1] = 1;
        }
        if (playerRace == Race.Pegasus) {
            bodyParams[2] = 1;
        }

        MeshInstance bodyMesh = GetNode<MeshInstance>("player_body/Armature/Skeleton/Body");
        LoadMesh("body", bodyMesh, bodyParams);
    }

    private void LoadHeadBody(Race playerRace) 
    {
        LoadHeadMesh(playerRace);
        LoadBodyMesh(playerRace);

        Body = GetNode<PlayerBody>("player_body");
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
        if(MakeRoped) {
            Body.Head.ShyOn();
            IsRoped = true;
            Body.MakeRoped(true);
        } else {
            Body.MakeLying(true);
        }
    }

    private void Unrope() 
    {
        unropingTime = 3f;
        Body.MakeRoped(false);
        IsRoped = false;
    }

    async private void GetUp() 
    {
        MayMove = false;
        Body.MakeLying(false);
        await ToSignal(GetTree().CreateTimer(1.6f), "timeout");
        if(IsLying && !IsRoped) {
            IsLying = false;
            MayMove = true;
        }
    }

    protected void Sit(bool sitOn) 
    {
        IsCrouching = sitOn;
        Stealth.SetLabelVisible(sitOn);
        if (sitOn) {
            bodyColliderSize = 0.56f;
            Body.Translate(new Vector3(0, 0.51f, 0));
            MaxSpeed = 8;
            crouchCooldown = 0.5f;
        } else {
            bodyColliderSize = 1;
            Body.Translate(new Vector3(0, -0.51f, 0));
            MaxSpeed = 17;
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
        if (Input.IsActionJustPressed("jump") && !BlockJump) {
            if (IsCrouching) {
                Sit(false);
            }
        }
    }

    public virtual void Fly() {}

    protected void ProcessInput(float delta) 
    {
        dir = new Vector3();

        Vector2 inputMovementVector = new Vector2();
        bool goSide = false;

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
            sideAngle += delta;
            goSide = true;
        }
        if (Input.IsActionPressed("ui_right")) {
            inputMovementVector.x += 1;
            sideAngle -= delta;
            goSide = true;
        }

        if (ThirdView) {
            Vector3 thirdRot = RotationHelperThird.Rotation;
            thirdRot.x = RotationHelper.Rotation.x;
            RotationHelperThird.Rotation = thirdRot;
        }

        Transform cameraTransf = RotationHelper.GlobalTransform;
        cameraTransf.origin = CameraHeadPos.GlobalTransform.origin;
        RotationHelper.GlobalTransform = cameraTransf;
        headShape.GlobalTransform = cameraTransf;

        if (IsRoped) {
            if (inputMovementVector.Length() > 0) {
                if (unropingTime < 5) {
                    Body.Head.ShyOn();
                    unropingTime += delta;
                    Body.AnimateUnroping(true);
                    if (!audiHitted.Playing) {
                        audiHitted.Stream = unropingSound;
                        audiHitted.Play();
                    }
                    return;
                } else {
                    Unrope();
                    return;
                }
            } else {
                Body.AnimateUnroping(false);
                if (unropingTime > 0){
                    unropingTime -= delta;
                }
                return;
            }
        }

        if (goSide) {
            sideAngle = Mathf.Clamp(sideAngle, -2, 2);
        } else {
            sideAngle = Mathf.MoveToward(sideAngle, 0, 4 * delta);
        }

        Vector3 cameraRot = Camera.RotationDegrees;
        cameraRot.z = sideAngle;
        Camera.RotationDegrees = cameraRot;

        if (IsLying && inputMovementVector.Length() > 0) {
            GetUp();
        }

        if (!OnStairs || inputMovementVector.Length() > 0) {
            stairGravity = 1;
        } else {
            stairGravity = 0;
        }

        inputMovementVector = inputMovementVector.Normalized();

        Transform camXForm = Camera.GlobalTransform;
        dir += -camXForm.basis.z * inputMovementVector.y;
	    dir += camXForm.basis.x * inputMovementVector.x;

        if (IsOnFloor()) {
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

    public virtual float GetWalkSpeed(float delta) 
    {
        return MaxSpeed;
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

        if (OnStairs && stairGravity == 0) {
            Velocity.y = 0;
        } else {
            Velocity.y = GetGravitySpeed(tempShake, delta);
        }

        var hvel = Velocity;
        hvel.y = 0;

        var target = dir;
        target *= GetWalkSpeed(delta);

        float acceleration;
        if (dir.Dot(hvel) > 0) {
            acceleration = ACCEL;
        } else {
            acceleration = GetDeacceleration();
        }

        hvel = hvel.LinearInterpolate(target, acceleration * delta);
        Velocity.x = hvel.x;
        Velocity.z = hvel.z;
        Velocity = MoveAndSlide(Velocity, new Vector3(0, 1, 0), false, 4, Mathf.Deg2Rad(MAX_SLOPE_ANGLE));
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
        if (@event is InputEventMouseMotion && 
            Input.GetMouseMode() == Input.MouseMode.Captured) {

            var mouseEvent = @event as InputEventMouseMotion;
            RotationHelper.RotateX(Mathf.Deg2Rad(mouseEvent.Relative.y * -MouseSensivity));
            RotateBodyClumped(mouseEvent.Relative.x * -MouseSensivity);

            Vector3 cameraRot = RotationHelper.RotationDegrees;
            cameraRot.x = Mathf.Clamp(cameraRot.x, CAMERA_MIN_Y, CAMERA_MAX_Y);
            cameraRot.y = 0;
            cameraRot.z = sideAngle;
            RotationHelper.RotationDegrees = cameraRot;

            OnCameraRotatingX(mouseEvent.Relative.x);
        }
    }

    public override void _Ready()
    {
        LoadPlayer();
    }

    public override void _Process(float delta)
    {
        bodyCollider.Rotation = Body.Rotation;
    }

    public override void _PhysicsProcess(float delta)
    {
        ProcessInput(delta);
        if (Health > 0 && MayMove) {
            HandleImpulse();
            ProcessMovement(delta);
        } else {
            Velocity = new Vector3(0, 0, 0);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (Health > 0) {
            RotateCamera(@event);
        }
    }
}
