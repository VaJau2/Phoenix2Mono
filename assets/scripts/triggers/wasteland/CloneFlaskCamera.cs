using Godot;

public partial class CloneFlaskCamera : Camera3D
{
    Node3D head;

    public override void _Ready()
    {
        head = GetNodeOrNull<Node3D>("../Armature/Skeleton3D/BoneAttachment3D/head_pos");
        SetProcess(false);
        SetProcessInput(false);
    }

    public void MakeCurrent(bool on)
    {
        if (head == null) return;
        
        Current = on;
        SetProcess(on);
        SetProcessInput(on);
    }

    public override void _Process(double delta)
    {
        GlobalTransform = Global.SetNewOrigin(
            GlobalTransform,
            head.GlobalTransform.Origin
        );
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            RotateZ(Mathf.DegToRad(motion.Relative.Y * Global.Get().Settings.mouseSensivity));
            RotateY(Mathf.DegToRad(-motion.Relative.X * Global.Get().Settings.mouseSensivity));

            RotationDegrees = new Vector3(
                Mathf.Clamp(RotationDegrees.X, -50, 60),
                Mathf.Clamp(RotationDegrees.Y, -70 + 90, 70 + 90),
                0
            );
        }
    }
}
