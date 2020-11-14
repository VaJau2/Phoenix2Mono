using Godot;

public class Mirror: MeshInstance {
    [Export]
    public Vector2 size;

    [Export]
    public int pixelsPerUnit = 90;

    Viewport viewport;
    Position3D planeMark;

    QuadMesh mesh;

    Camera mainCam = null;
    Camera mirrorCam = null;

    public bool cameraSee = false;

    public MirrorArea mirrorArea;

    public override void _Ready()
    {
        viewport = GetNode<Viewport>("Viewport");
        planeMark = GetNode<Position3D>("PlaneTransform");
        mesh = Mesh as QuadMesh;
        mesh.Size = size;
    }

    public void SetMirrorSize(Vector2 newSize) {
        size = newSize;
        mesh.Size = size;
        initialize_camera();
    }

    public void MirrorOn() {
        if (Engine.EditorHint) return;

        initialize_camera();
    }

    public void MirrorOff() {
        if (mirrorCam != null) {
            var mat = (SpatialMaterial)GetSurfaceMaterial(0);
            mat.AlbedoTexture = null;
            viewport.Size = Vector2.Zero;
            mirrorCam = null;
        }
    }

    public override void _Process(float delta)
    {
        if (Engine.EditorHint) return;

        if (mirrorCam != null && mainCam != null) {
            var planeOrigin = planeMark.GlobalTransform.origin;
            var planeNormal = planeMark.GlobalTransform.basis.z.Normalized();
            var reflectionPlane = new Plane(planeNormal, planeOrigin.Dot(planeNormal));
            var reflectionTransform = planeMark.GlobalTransform;
            
            var camPos = mainCam.GlobalTransform.origin;

            var projPos = reflectionPlane.Project(camPos);

            var mirroredPos = camPos + (projPos - camPos) * 2f;

            var transf = new Transform(new Basis(), mirroredPos);
            transf = transf.LookingAt(projPos, reflectionTransform.basis.y.Normalized());
            mirrorCam.GlobalTransform = transf;

            var oldOffset = reflectionTransform.XformInv(camPos);
            var offset = new Vector2(oldOffset.x, oldOffset.y);

            mirrorCam.SetFrustum(
                mesh.Size.x,
                -offset,
                projPos.DistanceTo(camPos),
                1000f
            );
        }
    }

    private async void initialize_camera() {
        if (!IsInsideTree() || Engine.EditorHint) return;

        var rootViewport = GetTree().Root;
        mainCam = rootViewport.GetCamera();
        while(mainCam == null) {
            mainCam = rootViewport.GetCamera();
            await ToSignal(GetTree(),"idle_frame");
        }

        if (mirrorCam != null) {
            mirrorCam.QueueFree();
        }

        mirrorCam = new Camera();
        mirrorCam.CullMask = 7;
        viewport.AddChild(mirrorCam);
        mirrorCam.KeepAspect = Camera.KeepAspectEnum.Width;
        mirrorCam.Current = true;

        viewport.Size = mesh.Size * pixelsPerUnit;

        var mat = (SpatialMaterial)GetSurfaceMaterial(0);
        mat.AlbedoTexture = viewport.GetTexture();
    }

    public void _on_visible_screen_entered() {
        cameraSee = true;
    }

    public void _on_visible_screen_exited() {
        cameraSee = false;
    }
}