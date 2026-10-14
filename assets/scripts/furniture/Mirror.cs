using Godot;

public class Mirror : MeshInstance
{
    [Export] private Vector2 size = new(5,7);
   
    private int pixelPerUnit;
    private Viewport viewport;
    private Spatial planeMark;
    private Camera mirrorCamera;
    
    private bool mayOn => isPlayerSee && isPlayerAround && Global.Get().Settings.reflection != 0;
    private bool isOn;

    private Player player => Global.Get().player;
    private bool isPlayerSee;
    private bool isPlayerAround;
    
    public override void _Ready()
    {
        viewport = GetNode<Viewport>("Viewport");
        planeMark = GetNode<Spatial>("PlaneTransform");
        
        if (Mesh is PlaneMesh planeMesh)
        {
            planeMesh.Size = size;
        }
        
        var levelsLoader = GetNode<LevelsLoader>("/root/Main");
        levelsLoader.Connect(nameof(LevelsLoader.SaveDataLoaded), this, nameof(OnSaveDataLoaded));
    }
    
    private async void OnSaveDataLoaded()
    {
        await ToSignal(GetTree(), "idle_frame");
        
        pixelPerUnit = Global.Get().Settings.reflection;
        On();
        
        await ToSignal(GetTree(), "idle_frame");
        
        Off();
    }

    public override void _PhysicsProcess(float delta)
    {
        if (!isOn) return;
        if (mirrorCamera == null) return;
        if (player == null) return;
        
        UpdateSize();
        
        // Compute reflection plane and its global transform  (origin in the middle, 
        //  X and Y axis properly aligned with the viewport, -Z is the mirror's forward direction)
        var planeOrigin = planeMark.GlobalTransform.origin;
        var planeNormal = planeMark.GlobalTransform.basis.z.Normalized();
        var reflectionPlane = new Plane(planeNormal, planeOrigin.Dot(planeNormal));
        var reflectionTransform = planeMark.GlobalTransform;
        
        // Player camera position
        var playerCameraPos = player.RotationHelperThird.GetCamera().GlobalTransform.origin;
        
        // The projected point of main camera's position onto the reflection plane
        var projectPos = reflectionPlane.Project(playerCameraPos);
        
        // Main camera position reflected over the mirror's plane
        var mirroredPos = playerCameraPos + (projectPos - playerCameraPos) * 2;

        // Compute mirror camera transform
        // - translation at the mirrored position
        // - looking perpendicularly into the reflection plane (this way the near clip plane will be 
        //   parallel to the reflection plane)
        var t = new Transform(new Basis(), mirroredPos);
        t = t.LookingAt(projectPos, reflectionTransform.basis.y.Normalized());
        mirrorCamera.GlobalTransform = t;
        
        // Compute the tilting offset for the frustum (the X and Y coordinates of the mirrored camera position
        // when expressed in the reflection plane coordinate system)
        var rawOffset = reflectionTransform.XformInv(playerCameraPos);
        var offset = new Vector2(rawOffset.x, rawOffset.y);
        
        // Set mirror camera frustum
        // - size   -> mirror's width (camera is set to KEEP_WIDTH)
        // - offset -> previously computed tilting offset
        // - z_near -> distance between the mirror camera and the reflection plane (this ensures we won't
        //               be reflecting anything behind the mirror)
        // - z_far	-> large arbitrary value (render distance limit form th mirror camera position)
        mirrorCamera.SetFrustum(size.x, -offset, projectPos.DistanceTo(playerCameraPos), 150);
    }

    private void On()
    {
        isOn = true;
        InitializeCamera();
    }

    private void Off()
    {
        isOn = false;
        SetTexture(null);
        viewport.Size = Vector2.Zero;
        mirrorCamera.QueueFree();
        mirrorCamera = null;
    }
    
    private void InitializeCamera()
    {
        mirrorCamera?.QueueFree();

        mirrorCamera = new Camera();
        viewport.AddChild(mirrorCamera);
        mirrorCamera.CullMask = 5;
        mirrorCamera.KeepAspect = Camera.KeepAspectEnum.Width;
        mirrorCamera.Current = true;

        viewport.Size = size * pixelPerUnit;
        SetTexture(viewport.GetTexture());
    }

    private void SetTexture(Texture texture)
    {
        if (GetSurfaceMaterial(0) is SpatialMaterial material)
        {
            material.AlbedoTexture = texture;
        }
    }

    private void UpdateSize()
    {
        if (pixelPerUnit == Global.Get().Settings.reflection) return;
        
        pixelPerUnit = Global.Get().Settings.reflection;
        viewport.Size = size * pixelPerUnit;

        switch (mayOn)
        {
            case true:
                On();
                break;
            
            case false when isOn:
                Off();
                break;
        }
    }
    
    private void _on_visible_screen_entered()
    {
        isPlayerSee = true;
        if (mayOn) On();
    }

    private void _on_visible_screen_exited()
    {
        isPlayerSee = false;
        if (isOn) Off();
    }

    private void _on_body_entered(Node body)
    {
        if (body.Name != "Player") return;
        
        isPlayerAround = true;
        if (mayOn) On();
    }
    
    private void _on_body_exited(Node body)
    {
        if (body.Name != "Player") return;
        isPlayerAround = false;
        if (isOn) Off();
    }
}
