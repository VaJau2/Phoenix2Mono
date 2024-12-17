using Godot;

public class Mirror : Spatial
{
    private const float NEAR_RATIO = 1.2f;
    private const float MIN_NEAR = 0.05f;
    
    private Spatial dummyCamera;
    private Camera mirrorCamera;

    private Player player => Global.Get().player;
    
    public override void _Ready()
    {
        dummyCamera = GetNode<Spatial>("DummyCamera");
        mirrorCamera = GetNode<Camera>("Viewport/MirrorCamera");
        SetActive(false);
    }

    public override void _PhysicsProcess(float delta)
    {
        if (player == null) return;

        var playerCamera = player.RotationHelperThird.GetCamera();
        var distance = playerCamera.GlobalTranslation.DistanceTo(GlobalTranslation);
        mirrorCamera.Near = Mathf.Max(MIN_NEAR, distance * NEAR_RATIO);
        
        UpdateCamera(playerCamera);
    }

    private void UpdateCamera(Spatial playerCamera)
    {
        Scale = new Vector3(Scale.x, -Scale.y, Scale.z);
        dummyCamera.GlobalTransform = playerCamera.GlobalTransform;
        Scale = new Vector3(Scale.x, -Scale.y, Scale.z);

        mirrorCamera.GlobalTransform = dummyCamera.GlobalTransform;
        
        var mirrorTransform = mirrorCamera.GlobalTransform;
        mirrorTransform.basis.x *= -1;
        mirrorCamera.GlobalTransform = mirrorTransform;
    }

    private void SetActive(bool value)
    {
        mirrorCamera.Current = value;
        SetPhysicsProcess(value);
    }

    private void _on_body_entered(Node body)
    {
        if (body is Player)
        {
            SetActive(true);
        }
    }
    
    private void _on_body_exited(Node body)
    {
        if (body is Player)
        {
            SetActive(false);
        }
    }
}
