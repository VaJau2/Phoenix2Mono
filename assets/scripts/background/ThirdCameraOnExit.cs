using Godot;

// Создает отдельную следящую за игроком камеру
// при выходе из области
public class ThirdCameraOnExit : Area
{
    private const float CAMERA_HEIGHT = 2.5f;
    private const float CAMERA_HEIGHT_DELTA = 0.05f;
    
    private Camera tempCamera;
    private Player playerHere;
    private bool wasThirdView;
    private float startPosY;
    private Vector3 startPlayerPos;
    
    public void OnAreaEntered(Node body)
    {
        if (playerHere != body) return;
        
        playerHere.RotationHelperThird.SetThirdView(wasThirdView);
        playerHere = null;
        DespawnCamera();
        SetProcess(false);
    }
    
    public void OnAreaExited(Node body)
    {
        if (!(body is Player player)) return;
        if (playerHere != null) return;
        
        wasThirdView = player.ThirdView;
        player.RotationHelperThird.SetThirdView(true);
        playerHere = player;
        startPlayerPos = PlayerPos;
        SpawnCamera();
        SetProcess(true);
    }

    public override void _Ready()
    {
        SetProcess(false);
    }

    public override void _Process(float delta)
    {
        var distance = PlayerPos.DistanceTo(startPlayerPos);
        if (distance > 1)
        {
            tempCamera.LookAt(PlayerPos, Vector3.Up);
        }
        
        tempCamera.GlobalTransform = Global.setNewOrigin(tempCamera.GlobalTransform, new Vector3(
            tempCamera.GlobalTransform.origin.x,
            startPosY + distance * CAMERA_HEIGHT_DELTA,
            tempCamera.GlobalTransform.origin.z
        ));
    }

    private void SpawnCamera()
    {
        tempCamera = new Camera();
        GetParent().AddChild(tempCamera);
        
        tempCamera.GlobalTransform =
            Global.setNewOrigin(
                tempCamera.GlobalTransform,
                PlayerPos
            );
        
        startPosY = tempCamera.GlobalTransform.origin.y;
        tempCamera.Current = true;
    }

    private void DespawnCamera()
    {
        tempCamera.QueueFree();
        tempCamera = null;
    }
    
    private Vector3 PlayerPos 
        => new Vector3(
            playerHere.GlobalTransform.origin.x,
            playerHere.GlobalTransform.origin.y + CAMERA_HEIGHT,
            playerHere.GlobalTransform.origin.z
            );
}
