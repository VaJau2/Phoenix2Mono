using Godot;

public class RotateGunTrigger: TriggerBase
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

        Spatial gun = GetNode<Spatial>(gunPath);
        gun.Transform = Global.setNewOrigin(gun.Transform, newPos);
        gun.Rotation = newRot;
        
        base._on_activate_trigger();
    }
}