using Godot;

public partial class RotateGunTrigger: TriggerBase
{
    [Export] public string gunPath;
    [Export] public Vector3 newPos;
    [Export] public Vector3 newRot;

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        OnActivateTrigger();
    }

    public override void OnActivateTrigger()
    {
        if (!IsActive)
        {
            base.OnActivateTrigger();
            return;
        }

        Node3D gun = GetNode<Node3D>(gunPath);
        gun.Transform = Global.SetNewOrigin(gun.Transform, newPos);
        gun.Rotation = newRot;
        
        base.OnActivateTrigger();
    }
}