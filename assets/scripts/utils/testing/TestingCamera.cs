using Godot;

/// <summary>
/// Тестовая камера включается при нажатии на F12 и позволяет летать по миру
/// Повторное нажатие F12 выключает камеру
/// </summary>
public class TestingCamera : Camera
{
    private const float PLAYER_HEIGHT = 1.5f;
    private Global global;
    private Spatial pivot;
    
    public override void _Ready()
    {
        if (!OS.IsDebugBuild())
        {
            QueueFree();
            return;
        }
        
        pivot = GetParent<Spatial>();
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

    public override void _Process(float delta)
    {
        if (!Current) return;
        UpdateMovingCamera(delta);
    }

    private void UpdateSettingCamera(InputEvent @event)
    {
        if (@event is not InputEventKey eventKey) return;
        if (!eventKey.IsPressed()) return;
        if (eventKey.Scancode != (uint)KeyList.F12) return;
        
        Current = !Current;
        SetProcess(Current);

        var player = Global.Get().player;
        if (player == null) return;

        var playerPos = player.GlobalTransform.origin;
        playerPos.y += PLAYER_HEIGHT;
        pivot.GlobalTransform = Global.SetNewOrigin(pivot.GlobalTransform, playerPos);

        if (!Current)
        {
            player.Camera.Current = true;
            player.ThirdView = false;
        }
        
        player.SetMayMove(!Current);
        player.MayRotateHead = !Current;
        
        var messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
        messages.ShowMessage("freeCamera" + (Current ? "On" : "Off"), "testing", Messages.HINT_TIMER);
    }

    private void UpdateRotatingCamera(InputEvent @event)
    {
        if (@event is not InputEventMouseMotion mouseEvent || Input.MouseMode != Input.MouseModeEnum.Captured) 
            return;
        
        var mouseSensitivity = global.Settings.mouseSensivity;
        RotateX(Mathf.Deg2Rad(mouseEvent.Relative.y * -mouseSensitivity));
        pivot.RotateY(Mathf.Deg2Rad(mouseEvent.Relative.x * -mouseSensitivity));

        var cameraRot = RotationDegrees;
        cameraRot.x = Mathf.Clamp(cameraRot.x, Player.CAMERA_MIN_Y, Player.CAMERA_MAX_Y);
        cameraRot.y = cameraRot.z = 0;
        RotationDegrees = cameraRot;
    }

    private void UpdateMovingCamera(float delta)
    {
        var dir = new Vector3();
        
        if (Input.IsActionPressed("ui_up"))
        {
            dir -= Transform.basis.z;
        }
        if (Input.IsActionPressed("ui_down"))
        {
            dir += Transform.basis.z;
        }
        if (Input.IsActionPressed("ui_left"))
        {
            dir.x -= 1;
        }
        if (Input.IsActionPressed("ui_right"))
        {
            dir.x += 1;
        }
        if (Input.IsActionPressed("test_camera_up"))
        {
            dir.y += 1;
        }
        if (Input.IsActionPressed("test_camera_down"))
        {
            dir.y -= 1;
        }
        
        var speed = Input.IsActionPressed("ui_shift") ? 20 : 6;
        pivot.Translate(dir * speed * delta);
    }
}
