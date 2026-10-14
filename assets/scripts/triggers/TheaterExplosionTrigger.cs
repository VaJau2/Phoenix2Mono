using Godot;

public partial class TheaterExplosionTrigger: TriggerBase
{
    [Export] public NodePath explosionPath;
    [Export] public NodePath chairsParentPath;
    private Node3D chairsParent;
    private Explosion explosion;

    public override void _Ready()
    {
        base._Ready();
        explosion = GetNode<Explosion>(explosionPath);
        chairsParent = GetNode<Node3D>(chairsParentPath);
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        _on_activate_trigger();
    }

    public override void _on_activate_trigger()
    {
        if (!IsActive) return;
        
        explosion.Explode();
        Global.Get().player.SitOnChair(false);
        foreach (TheaterChair chair in chairsParent.GetChildren())
        {
            chair.isActive = false;
        }
        
        base._on_activate_trigger();
    }
}
