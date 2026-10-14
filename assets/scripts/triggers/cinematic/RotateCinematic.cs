using Godot;

public partial class RotateCinematic : PathBase
{
    private bool isFinished;
    private Node3D target;
    private PathBase waitingCinematic;
    private Cutscene cutscene;
    
    public override void _Ready()
    {
        base._Ready();
        target = GetNode<Node3D>("PathFollow3D/Target");
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
        cutscene.GetCamera3d().LookAt(target.GlobalTranslation, Vector3.Up);
    }

    public void SetWaiting(PathBase pathBase)
    {
        waitingCinematic = pathBase;
        waitingCinematic.Connect(nameof(Finished), new Callable(this, nameof(OnFinished)));
    }

    public void OnFinished(PathBase pathBase = null)
    {
        waitingCinematic = null;
        base.Disable();
    }
    
    public override void Disable()
    {
        isFinished = true;
        
        if (waitingCinematic != null) return;
        
        OnFinished();
    }
}
