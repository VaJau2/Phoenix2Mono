using Godot;

public class RotateCinematic : PathBase
{
    private bool isFinished;
    private Spatial target;
    private CutsceneManager cutsceneManager;
    
    public override void _Ready()
    {
        base._Ready();
        target = GetNode<Spatial>("PathFollow/Target");
        cutsceneManager = GetNode<CutsceneManager>("../../..");
    }

    public override void Enable()
    {
        isFinished = false;
        base.Enable();
    }

    public override void _PhysicsProcess(float delta)
    {
        if (!isFinished) base._PhysicsProcess(delta);
        cutsceneManager.GetCamera().LookAt(target.GlobalTranslation, Vector3.Up);
    }

    public void OnFinished()
    {
        base.Disable();
    }
    
    protected override void Disable()
    {
        isFinished = true;
        EmitSignal(nameof(Finished));
    }
}
