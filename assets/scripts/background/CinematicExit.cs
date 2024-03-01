using System;
using Godot;
using Godot.Collections;


// Создает отдельную следящую за игроком камеру
// при выходе из области
public partial class CinematicExit : Area3D, ISavable
{
    // насколько сильно зенитное замедление камеры в начале и ускорение в конце
    private const float MAX_DISTANCE = 400f;
    
    // дистанция на которой спавнится кинематографическая камера от игрока
    private const float CAMERA_SPAWN_DISTANCE = 3f;
    
    // дальность прорисовки
    private const float CAMERA_FAR = 1000f;
    
    // скорость интерполяции кинематографической камеры
    private const float CAMERA_SPEED = 4f;
    
    // минимальная высота камеры
    private const float CAMERA_HEIGHT = 2.5f;
    
    // соотношение высоты камеры к пройденной игроком дистанции от границы
    private const float CAMERA_HEIGHT_RATIO = 0.05f;

    private Camera3D cinematicCamera;
    
    private Player player;
    private bool wasThirdView;
    private float startPosY;
    private Vector3 exitPoint;

    private float distance;
    
    private float distanceRatio => distance / MAX_DISTANCE;
    
    private Vector3 PlayerPos
        => new(
            player.GlobalTransform.Origin.X,
            player.GlobalTransform.Origin.Y + CAMERA_HEIGHT + distance * distanceRatio,
            player.GlobalTransform.Origin.Z
        );
    
    public override void _Ready()
    {
        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        distance = player.GlobalTransform.Origin.DistanceTo(exitPoint);
        
        if (distance > 1 && cinematicCamera.GlobalTransform.Origin != PlayerPos)
        {
            cinematicCamera.LookAt(PlayerPos, Vector3.Up);
        }

        var target = new Vector3(
            cinematicCamera.GlobalTransform.Origin.X,
            startPosY + distance * CAMERA_HEIGHT_RATIO,
            cinematicCamera.GlobalTransform.Origin.Z
        );
        var transform = cinematicCamera.GlobalTransform;
        transform.Origin = transform.Origin.Lerp(target, (float)delta * CAMERA_SPEED);
        cinematicCamera.GlobalTransform = transform;
    }

    public void OnAreaEntered(Node body = null)
    {
        if (player == null) return;
        
        exitPoint = Vector3.Zero;
        player.RotationHelperThird.MayChange = true;
        player.RotationHelperThird.SetThirdView(wasThirdView);

        DespawnCamera();
        SetProcess(false);
    }
    
    public void OnAreaExited(Node body)
    {
        if (Global.Get().player == null) return;
        if (body.Name != "exitCheck") return;
        
        player = body.GetParent<Player>();
        wasThirdView = player.ThirdView;
        player.RotationHelperThird.SetThirdView(true);
        player.RotationHelperThird.MayChange = false;
        
        SpawnCamera();
    }

    private async void SpawnCamera()
    {
        cinematicCamera = new Camera3D();
        cinematicCamera.Name = "Created_CinematicCamera";
        GetNode("../../").CallDeferred("add_child", cinematicCamera);

        await ToSignal(GetTree(), "process_frame");

        var locationCenter = new Vector3(
            GlobalTransform.Origin.X, 
            player.GlobalTransform.Origin.Y,
            GlobalTransform.Origin.Z
            );
        
        var vec = (locationCenter - player.GlobalTransform.Origin).Normalized();

        cinematicCamera.GlobalTransform =
            Global.SetNewOrigin(
                cinematicCamera.GlobalTransform,
                player.GlobalTransform.Origin + vec * CAMERA_SPAWN_DISTANCE + new Vector3(0,CAMERA_HEIGHT,0)
            );
        
        startPosY = cinematicCamera.GlobalTransform.Origin.Y;
        cinematicCamera.Far = CAMERA_FAR;
        cinematicCamera.Current = true;
        
        exitPoint = cinematicCamera.GlobalTransform.Origin;

        SetProcess(true);
    }
    
    private void SpawnCamera(Vector3 pos)
    {
        cinematicCamera = new Camera3D();
        cinematicCamera.Name = "Created_CinematicCamera";
        GetNode("../../").CallDeferred("add_child", cinematicCamera);
        
        cinematicCamera.GlobalTransform =
            Global.SetNewOrigin(
                cinematicCamera.GlobalTransform,
                pos
            );
        
        startPosY = cinematicCamera.GlobalTransform.Origin.Y;
        cinematicCamera.Far = CAMERA_FAR;
        cinematicCamera.Current = true;
        
        exitPoint = cinematicCamera.GlobalTransform.Origin;

        SetProcess(true);
    }

    private void DespawnCamera()
    {
        cinematicCamera.QueueFree();
        cinematicCamera = null;
    }

    public Dictionary GetSaveData()
    {
        if (exitPoint == Vector3.Zero) return new Dictionary();
        
        return new Dictionary
        {
            {"exitPointX", exitPoint.X},
            {"exitPointY", exitPoint.Y},
            {"exitPointZ", exitPoint.Z},
            {"cameraPosX", cinematicCamera.GlobalTransform.Origin.X},
            {"cameraPosY", cinematicCamera.GlobalTransform.Origin.Y},
            {"cameraPosZ", cinematicCamera.GlobalTransform.Origin.Z}
        };
    }

    public void LoadData(Dictionary data)
    {
        if (!data.ContainsKey("exitPointX")) return;

        player = GetNode<Player>("/root/Main/Scene/Player");
        player.RotationHelperThird.SetThirdView(true);
        player.RotationHelperThird.MayChange = false;
        
        exitPoint = new Vector3(
            Convert.ToSingle(data["exitPointX"]), 
            Convert.ToSingle(data["exitPointY"]), 
            Convert.ToSingle(data["exitPointZ"])
        );

        var cameraPos = new Vector3(
            Convert.ToSingle(data["cameraPosX"]), 
            Convert.ToSingle(data["cameraPosY"]), 
            Convert.ToSingle(data["cameraPosZ"])
        );
        
        LoadCamera(cameraPos);
    }

    private async void LoadCamera(Vector3 pos)
    {
        await ToSignal(GetTree(), "process_frame");
        
        SpawnCamera(pos);
    }
}
