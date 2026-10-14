using Godot;

public partial class RotateGunTrigger: TriggerBase
{
    [Export] public string gunPath;
    [Export] public Vector3 newPos;
    [Export] public Vector3 newRot;

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        _on_activate_trigger();
    }

    public override void _on_activate_trigger()
    {
        if (!IsActive)
        {
            base._on_activate_trigger();
            return;
        }

        Node3D gun = GetNode<Node3D>(gunPath);
        gun.Transform3D = Global.SetNewOrigin(gun.Transform3D, newPos);
        gun.Rotation = newRot;
        
        base._on_activate_trigger();
    }
}