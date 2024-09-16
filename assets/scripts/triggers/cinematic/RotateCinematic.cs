using Godot;

public class RotateCinematic : PathBase
{
    private Spatial target;
    private CutsceneManager cutsceneManager;
    
    public override void _Ready()
    {
        base._Ready();
        target = GetNode<Spatial>("PathFollow/Target");
        cutsceneManager = GetNode<CutsceneManager>("../../..");
    }

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);
        cutsceneManager.GetCamera().LookAt(target.GlobalTranslation, Vector3.Up);
    }
}
