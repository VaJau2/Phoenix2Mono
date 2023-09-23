using Godot;

public class PlayerThirdPerson : Spatial
{
    const int BUTTON_WHEEL_UP = 4;
    const int BUTTON_WHEEL_DOWN = 5;

    public Camera thirdCamera;
    public Camera firstCamera;
    public RayCast TempRay;
    public bool MayChange = true;

    private Player player;
    private Control eyePartsInterface;

    private Vector3 third = new Vector3(2.5f, 0.4f, 4.2f);
    private Vector3 thirdMin = new Vector3(1.75f, 0.35f, 3.5f);
    private Vector3 thirdMax = new Vector3(2.75f, 0.45f, 5.2f);
    private Vector3 tempThird = new Vector3(2.5f, 0.4f, 4.2f);

    private RayCast RayToPlayer;
    private RayCast RayFirst;
    private RayCast RayThird;
    private bool seePlayer = false;
    private Vector3 oldThird;

    private Spatial Body;
    private GeometryInstance Body_third;


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
            Body_third.CastShadow = GeometryInstance.ShadowCastingSetting.ShadowsOnly;
            TempRay = RayFirst;
        }
        else
        {
            Body_third.CastShadow = GeometryInstance.ShadowCastingSetting.On;
            TempRay = RayThird;
        }

        //возвращаем игроку вращение при переходе от 3 лица
        //если он повернулся на больше 180 градусов
        if (!on)
        {
            if (!player.Body.RotClumpsMin || !player.Body.RotClumpsMax)
            {
                var bodyRotY = player.Body.GlobalTransform.basis.GetEuler().y;
                Vector3 playerRot = player.GlobalTransform.basis.GetEuler();
                playerRot.y = bodyRotY;

                Basis basis = new Basis(playerRot);
                Vector3 origin = player.GlobalTransform.origin;
                Transform playerTransform = new Transform(basis, origin);
                player.GlobalTransform = playerTransform;

                player.Body.SetRotZero();
            }
        }
    }

    private float updateSide(float delta, float side, string keyUp,
        string keyDown, float maxValue, float minValue, float usualValue)
    {
        if (player.MayMove && Input.IsActionPressed(keyUp))
        {
            side = Mathf.MoveToward(side, maxValue, delta * 1.5f);
        }
        else if (player.MayMove && Input.IsActionPressed(keyDown))
        {
            side = Mathf.MoveToward(side, minValue, delta * 1.5f);
        }
        else
        {
            side = Mathf.MoveToward(side, usualValue, delta * 1.5f);
        }

        return side;
    }

    private void UpdateThirdCameraPos(float delta)
    {
        tempThird.x = updateSide(delta, tempThird.x, "ui_left", "ui_right",
            third.x + 0.7f, third.x - 0.7f, third.x);
        tempThird.z = updateSide(delta, tempThird.z, "ui_up", "ui_down",
            third.z + 0.5f, third.z - 0.5f, third.z);

        if (player is Player_Pegasus)
        {
            var pegasus = player as Player_Pegasus;

            if (pegasus.IsFlying)
            {
                tempThird.y = updateSide(delta, tempThird.y, "jump", "ui_shift",
                    third.y + 0.7f, third.y - 0.7f, third.y);
            }
            else
            {
                tempThird.y = third.y;
            }
        }
        else
        {
            tempThird.y = third.y;
        }

        thirdCamera.Translation = tempThird;
    }

    private void checkCameraSee()
    {
        RayToPlayer.Enabled = true;
        var dir = player.GlobalTransform.origin - RayToPlayer.GlobalTransform.origin;
        RayToPlayer.CastTo = dir;

        Transform rayTransf = RayToPlayer.GlobalTransform;
        rayTransf.basis = new Basis(Vector3.Zero);
        RayToPlayer.GlobalTransform = rayTransf;

        if (RayToPlayer.IsColliding())
        {
            var collider = RayToPlayer.GetCollider() as Node;
            if (collider.Name == "Player")
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

    private void closeCamera()
    {
        if (third.x > thirdMin.x)
        {
            third.x -= 0.05f;
        }

        if (third.y > thirdMin.y)
        {
            third.y -= 0.01f;
        }

        if (third.z > thirdMin.z)
        {
            third.z -= 0.1f;
        }

        if (third.x <= thirdMin.x &&
            third.y <= thirdMin.y &&
            third.z <= thirdMin.z)
        {
            if (player.ThirdView)
            {
                SetThirdView(false);
            }
        }
    }

    private void farCamera()
    {
        if (!player.ThirdView)
        {
            SetThirdView(true);
            third = thirdMin;
            tempThird = third;
        }

        if (third.x < thirdMax.x)
        {
            third.x += 0.05f;
        }

        if (third.y < thirdMax.y)
        {
            third.y += 0.01f;
        }

        if (third.z < thirdMax.z)
        {
            third.z += 0.1f;
        }
    }

    public override void _Ready()
    {
        player = GetParent<Player>();

        eyePartsInterface = GetNode<Control>("/root/Main/Scene/canvas/eyesParts");
        firstCamera = GetNode<Camera>("../rotation_helper/camera");
        thirdCamera = GetNode<Camera>("camera");
        RayToPlayer = GetNode<RayCast>("camera/RayToPlayer");

        RayFirst = GetNode<RayCast>("../rotation_helper/camera/rayFirst");
        RayThird = GetNode<RayCast>("camera/rayThird");
        RayFirst.AddException(player);
        RayThird.AddException(player);
        TempRay = RayFirst;

        Body = GetNode<Spatial>("../player_body/Armature/Skeleton/Body");
        Body_third = GetNode<GeometryInstance>("../player_body/Armature/Skeleton/Body_third");
    }

    public override void _Process(float delta)
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
            UpdateThirdCameraPos(delta);
            checkCameraSee();

            if (!seePlayer)
            {
                closeCamera();
            }
            else
            {
                if (oldThird != Vector3.Zero)
                {
                    if (oldThird.x > third.x)
                    {
                        farCamera();
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
                checkCameraSee();
                if (seePlayer)
                {
                    farCamera();
                    oldThird = Vector3.Zero;
                }
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton
            && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            var mouseEvent = @event as InputEventMouseButton;

            if (mouseEvent.IsPressed())
            {
                if (mouseEvent.ButtonIndex == BUTTON_WHEEL_UP)
                {
                    closeCamera();
                    oldThird = Vector3.Zero;
                }

                if (mouseEvent.ButtonIndex == BUTTON_WHEEL_DOWN)
                {
                    farCamera();
                }
            }
        }
    }
}