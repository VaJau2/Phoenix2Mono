using Godot;

public partial class PlayerThirdPerson : Node3D
{
    public Camera3D thirdCamera;
    public Camera3D firstCamera;
    public RayCast3D TempRay;
    public bool MayChange = true;

    private Player player;
    private Control eyePartsInterface;

    private Vector3 third = new Vector3(2.5f, 0.4f, 4.2f);
    private Vector3 thirdMin = new Vector3(1.75f, 0.35f, 3.5f);
    private Vector3 thirdMax = new Vector3(2.75f, 0.45f, 5.2f);
    private Vector3 tempThird = new Vector3(2.5f, 0.4f, 4.2f);

    private RayCast3D RayToPlayer;
    private RayCast3D RayFirst;
    private RayCast3D RayThird;
    private bool seePlayer = false;
    private Vector3 oldThird;

    private Node3D Body;
    private GeometryInstance3D Body_third;


    public void SetThirdView(bool on)
    {
        if (!MayChange)
        {
            return;
        }

        firstCamera.Current = !on;
        thirdCamera.Current = on;
        RayFirst.Enabled = !on;
        RayThird.Enabled = on;
        player.ThirdView = on;
        RayToPlayer.Enabled = on;
        eyePartsInterface.Visible = !on;
        player.Weapons.СheckThirdView();

        Body.Visible = !on;
        if (!on)
        {
            Body_third.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly;
            TempRay = RayFirst;
        }
        else
        {
            Body_third.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
            TempRay = RayThird;
        }

        //возвращаем игроку вращение при переходе от 3 лица
        //если он повернулся на больше 180 градусов
        if (!on)
        {
            if (!player.Body.RotClumpsMin || !player.Body.RotClumpsMax)
            {
                var bodyRotY = player.Body.GlobalTransform.Basis.GetEuler().Y;
                Vector3 playerRot = player.GlobalTransform.Basis.GetEuler();
                playerRot.Y = bodyRotY;

                Basis basis = Basis.FromEuler(playerRot);
                Vector3 origin = player.GlobalTransform.Origin;
                Transform3D playerTransform = new Transform3D(basis, origin);
                player.GlobalTransform = playerTransform;

                player.Body.SetRotZero();
            }
        }
        
        player.EmitSignal(Player.SignalName.ChangeView, on);
    }

    private float UpdateSide(float delta, float side, string keyUp,
        string keyDown, float maxValue, float minValue, float usualValue)
    {
        switch (player.MayMove)
        {
            case true when Input.IsActionPressed(keyUp):
                side = Mathf.MoveToward(side, maxValue, delta * 1.5f);
                break;
            case true when Input.IsActionPressed(keyDown):
                side = Mathf.MoveToward(side, minValue, delta * 1.5f);
                break;
            default:
                side = Mathf.MoveToward(side, usualValue, delta * 1.5f);
                break;
        }

        return side;
    }

    private void UpdateThirdCameraPos(float delta)
    {
        tempThird.X = UpdateSide(delta, tempThird.X, "ui_left", "ui_right",
            third.X + 0.7f, third.X - 0.7f, third.X);
        tempThird.Z = UpdateSide(delta, tempThird.Z, "ui_up", "ui_down",
            third.Z + 0.5f, third.Z - 0.5f, third.Z);

        if (player is Player_Pegasus)
        {
            var pegasus = player as Player_Pegasus;

            if (pegasus.IsFlying)
            {
                tempThird.X = UpdateSide(delta, tempThird.X, "jump", "ui_shift",
                    third.X + 0.7f, third.X - 0.7f, third.X);
            }
            else
            {
                tempThird.X = third.X;
            }
        }
        else
        {
            tempThird.X = third.X;
        }

        thirdCamera.Position = tempThird;
    }

    private void CheckCameraSee()
    {
        RayToPlayer.Enabled = true;
        var dir = player.GlobalTransform.Origin - RayToPlayer.GlobalTransform.Origin;
        RayToPlayer.TargetPosition = dir;

        Transform3D rayTransf = RayToPlayer.GlobalTransform;
        rayTransf.Basis = Basis.FromEuler(Vector3.Zero);
        RayToPlayer.GlobalTransform = rayTransf;

        if (RayToPlayer.IsColliding())
        {
            if (RayToPlayer.GetCollider() is Player)
            {
                seePlayer = true;
            }
            else
            {
                oldThird = third;
                seePlayer = false;
            }
        }
    }

    private void CloseCamera()
    {
        if (third.X > thirdMin.X)
        {
            third.X -= 0.05f;
        }

        if (third.X > thirdMin.X)
        {
            third.X -= 0.01f;
        }

        if (third.Z > thirdMin.Z)
        {
            third.Z -= 0.1f;
        }

        if (third.X <= thirdMin.X &&
            third.X <= thirdMin.X &&
            third.Z <= thirdMin.Z)
        {
            if (player.ThirdView)
            {
                SetThirdView(false);
            }
        }
    }

    private void FarCamera()
    {
        if (!player.ThirdView)
        {
            SetThirdView(true);
            third = thirdMin;
            tempThird = third;
        }

        if (third.X < thirdMax.X)
        {
            third.X += 0.05f;
        }

        if (third.X < thirdMax.X)
        {
            third.X += 0.01f;
        }

        if (third.Z < thirdMax.Z)
        {
            third.Z += 0.1f;
        }
    }

    public override void _Ready()
    {
        player = GetParent<Player>();

        eyePartsInterface = GetNode<Control>("/root/Main/Scene/canvas/eyesParts");
        firstCamera = GetNode<Camera3D>("../rotation_helper/camera");
        thirdCamera = GetNode<Camera3D>("camera");
        RayToPlayer = GetNode<RayCast3D>("camera/RayToPlayer");

        RayFirst = GetNode<RayCast3D>("../rotation_helper/camera/rayFirst");
        RayThird = GetNode<RayCast3D>("camera/rayThird");
        RayFirst.AddException(player);
        RayThird.AddException(player);
        TempRay = RayFirst;

        Body = GetNode<Node3D>("../player_body/Armature/Skeleton3D/Body");
        Body_third = GetNode<GeometryInstance3D>("../player_body/Armature/Skeleton3D/Body_third");
    }

    public override void _Process(double delta)
    {
        if (!player.MayRotateHead)
        {
            return;
        }

        if (Input.IsActionJustPressed("changeView"))
        {
            if (oldThird != Vector3.Zero)
            {
                oldThird = Vector3.Zero;
            }
            else
            {
                SetThirdView(!player.ThirdView);
            }
        }

        if (player.ThirdView)
        {
            UpdateThirdCameraPos((float)delta);
            CheckCameraSee();

            if (!seePlayer)
            {
                CloseCamera();
            }
            else
            {
                if (oldThird != Vector3.Zero)
                {
                    if (oldThird.X > third.X)
                    {
                        FarCamera();
                    }
                    else
                    {
                        oldThird = Vector3.Zero;
                    }
                }
            }
        }
        else
        {
            if (oldThird != Vector3.Zero)
            {
                CheckCameraSee();
                if (seePlayer)
                {
                    FarCamera();
                    oldThird = Vector3.Zero;
                }
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseEvent
            || Input.MouseMode != Input.MouseModeEnum.Captured) return;
        
        if (!mouseEvent.IsPressed()) return;
        
        switch (mouseEvent.ButtonIndex)
        {
            case MouseButton.WheelUp:
                CloseCamera();
                oldThird = Vector3.Zero;
                break;
            case MouseButton.WheelDown:
                FarCamera();
                break;
        }
    }
}