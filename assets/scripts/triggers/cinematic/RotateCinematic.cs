using Godot;

public class RotateCinematic : PathBase
{
    private bool isFinished;
    private Spatial target;
    private Cutscene cutscene;
    
    public override void _Ready()
    {
        base._Ready();
        target = GetNode<Spatial>("PathFollow/Target");
        cutscene = GetNode<Cutscene>("../../");
    }

    public override void Enable()
    {
        isFinished = false;
        base.Enable();
    }

    public override void _PhysicsProcess(float delta)
    {
        if (!isFinished) base._PhysicsProcess(delta);
        cutscene.GetCamera().LookAt(target.GlobalTranslation, Vector3.Up);
    }

    public void OnFinished(PathBase pathBase = null)
    {
        base.Disable();
    }
    
    public override void Disable()
    {
        isFinished = true;
    }
}
