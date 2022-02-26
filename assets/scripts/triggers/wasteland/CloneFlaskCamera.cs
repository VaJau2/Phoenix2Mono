using Godot;

public class CloneFlaskCamera : Camera
{
    Spatial head;

    public override void _Ready()
    {
        head = GetNode<Spatial>("../Armature/Skeleton/BoneAttachment/head_pos");
        SetProcess(false);
        SetProcessInput(false);
    }

    public void MakeCurrent(bool on)
    {
        Current = on;
        SetProcess(on);
        SetProcessInput(on);
    }

    public override void _Process(float delta)
    {
        GlobalTransform = Global.setNewOrigin(
            GlobalTransform,
            head.GlobalTransform.origin
        );
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.GetMouseMode() == Input.MouseMode.Captured)
        {
            RotateZ(Mathf.Deg2Rad(motion.Relative.y * Global.Get().Settings.mouseSensivity));
            RotateY(Mathf.Deg2Rad(-motion.Relative.x * Global.Get().Settings.mouseSensivity));

            RotationDegrees = new Vector3(
                Mathf.Clamp(RotationDegrees.x, -50, 60),
                Mathf.Clamp(RotationDegrees.y, -70 + 90, 70 + 90),
                0
            );
        }
    }
}
