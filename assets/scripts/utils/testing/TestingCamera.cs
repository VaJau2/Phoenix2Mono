using Godot;

/// <summary>
/// Тестовая камера включается при нажатии на F12 и позволяет летать по миру
/// Повторное нажатие F12 выключает камеру
/// </summary>
public partial class TestingCamera : Camera3D
{
    private const float PLAYER_HEIGHT = 1.5f;
    private Global global;
    private Node3D pivot;
    
    public override void _Ready()
    {
        if (!OS.IsDebugBuild())
        {
            QueueFree();
            return;
        }
        
        pivot = GetParent<Node3D>();
        global = Global.Get();
        Current = false;
        SetProcess(false);
    }
    
    public override void _Input(InputEvent @event)
    {
        UpdateSettingCamera(@event);
        if (!Current) return;
        
        UpdateRotatingCamera(@event);
    }

    public override void _Process(double delta)
    {
        if (!Current) return;
        UpdateMovingCamera((float)delta);
    }

    private void UpdateSettingCamera(InputEvent @event)
    {
        if (@event is not InputEventKey eventKey) return;
        if (!eventKey.IsPressed()) return;
        if (eventKey.Keycode != Key.F12) return;
        
        Current = !Current;
        SetProcess(Current);

        var player = Global.Get().player;
        if (player == null) return;

        var playerPos = player.GlobalTransform.Origin;
        playerPos.Y += PLAYER_HEIGHT;
        pivot.GlobalTransform = Global.SetNewOrigin(pivot.GlobalTransform, playerPos);

        if (!Current)
        {
            player.Camera3D.Current = true;
            player.ThirdView = false;
        }
        
        player.MayMove = !Current;
        player.MayRotateHead = !Current;
        
        var messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
        messages.ShowMessage("freeCamera" + (Current ? "On" : "Off"), "testing", Messages.HINT_TIMER);
    }

    private void UpdateRotatingCamera(InputEvent @event)
    {
        if (@event is not InputEventMouseMotion mouseEvent || Input.MouseMode != Input.MouseModeEnum.Captured) 
            return;
        
        var mouseSensitivity = global.Settings.mouseSensivity;
        RotateX(Mathf.DegToRad(mouseEvent.Relative.X * -mouseSensitivity));
        pivot.RotateY(Mathf.DegToRad(mouseEvent.Relative.X * -mouseSensitivity));

        var cameraRot = RotationDegrees;
        cameraRot.X = Mathf.Clamp(cameraRot.X, Player.CAMERA_MIN_Y, Player.CAMERA_MAX_Y);
        cameraRot.X = cameraRot.Z = 0;
        RotationDegrees = cameraRot;
    }

    private void UpdateMovingCamera(float delta)
    {
        var dir = new Vector3();
        
        if (Input.IsActionPressed("ui_up"))
        {
            dir -= Transform.Basis.Z;
        }
        if (Input.IsActionPressed("ui_down"))
        {
            dir += Transform.Basis.Z;
        }
        if (Input.IsActionPressed("ui_left"))
        {
            dir.X -= 1;
        }
        if (Input.IsActionPressed("ui_right"))
        {
            dir.X += 1;
        }
        
        var speed = Input.IsActionPressed("ui_shift") ? 20 : 6;
        pivot.Translate(dir * speed * delta);
    }
}
