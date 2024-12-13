using System;
using Godot;
using Godot.Collections;


// Создает отдельную следящую за игроком камеру
// при выходе из области
public class CinematicExit : Area, ISavable
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

    private Camera cinematicCamera;
    
    private Player player;
    private bool wasThirdView;
    private float startPosY;
    private Vector3 exitPoint;

    private float distance;
    
    private float distanceRatio => distance / MAX_DISTANCE;
    
    private Vector3 PlayerPos
        => new(
            player.GlobalTransform.origin.x,
            player.GlobalTransform.origin.y + CAMERA_HEIGHT + distance * distanceRatio,
            player.GlobalTransform.origin.z
        );
    
    public override void _Ready()
    {
        SetProcess(false);
    }

    public override void _Process(float delta)
    {
        distance = player.GlobalTransform.origin.DistanceTo(exitPoint);
        
        if (distance > 1 && cinematicCamera.GlobalTransform.origin != PlayerPos)
        {
            cinematicCamera.LookAt(PlayerPos, Vector3.Up);
        }

        var target = new Vector3(
            cinematicCamera.GlobalTransform.origin.x,
            startPosY + distance * CAMERA_HEIGHT_RATIO,
            cinematicCamera.GlobalTransform.origin.z
        );
        var transform = cinematicCamera.GlobalTransform;
        transform.origin = transform.origin.LinearInterpolate(target, delta * CAMERA_SPEED);
        cinematicCamera.GlobalTransform = transform;
    }

    public void OnAreaEntered(Node body = null)
    {
        if (player == null) return;
        
        player.RotationHelperThird.MayChange = true;
        player.RotationHelperThird.SetThirdView(wasThirdView);
        player.DeathManager.Disconnect(nameof(PlayerDeathManager.PlayerDie), this, nameof(OnPlayerDeath));
        player = null;
        
        exitPoint = Vector3.Zero;
        
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

        player.DeathManager.Connect(nameof(PlayerDeathManager.PlayerDie), this, nameof(OnPlayerDeath));
    }

    private async void SpawnCamera()
    {
        cinematicCamera = new Camera();
        cinematicCamera.Name = "Created_CinematicCamera";
        GetNode("../../").CallDeferred("add_child", cinematicCamera);

        await ToSignal(GetTree(), "idle_frame");

        var locationCenter = new Vector3(
            GlobalTransform.origin.x, 
            player.GlobalTransform.origin.y,
            GlobalTransform.origin.z
            );
        
        var vec = (locationCenter - player.GlobalTransform.origin).Normalized();

        cinematicCamera.GlobalTransform =
            Global.SetNewOrigin(
                cinematicCamera.GlobalTransform,
                player.GlobalTransform.origin + vec * CAMERA_SPAWN_DISTANCE + new Vector3(0,CAMERA_HEIGHT,0)
            );
        
        startPosY = cinematicCamera.GlobalTransform.origin.y;
        cinematicCamera.Far = CAMERA_FAR;
        cinematicCamera.Current = true;
        
        exitPoint = cinematicCamera.GlobalTransform.origin;

        SetProcess(true);
    }
    
    private async void SpawnCamera(Vector3 pos)
    {
        cinematicCamera = new Camera();
        cinematicCamera.Name = "Created_CinematicCamera";
        GetNode("../../").CallDeferred("add_child", cinematicCamera);
        
        cinematicCamera.GlobalTransform =
            Global.SetNewOrigin(
                cinematicCamera.GlobalTransform,
                pos
            );
        
        startPosY = cinematicCamera.GlobalTransform.origin.y;
        cinematicCamera.Far = CAMERA_FAR;
        cinematicCamera.Current = true;
        
        exitPoint = cinematicCamera.GlobalTransform.origin;

        SetProcess(true);
    }

    private void DespawnCamera()
    {
        cinematicCamera.QueueFree();
        cinematicCamera = null;
    }

    private void OnPlayerDeath()
    {
        player = null;
        SetProcess(false);
    }

    public Dictionary GetSaveData()
    {
        if (exitPoint == Vector3.Zero) return new Dictionary();
        
        return new Dictionary
        {
            {"exitPoint", exitPoint},
            {"cameraPos", cinematicCamera.GlobalTranslation}
        };
    }

    public void LoadData(Dictionary data)
    {
        if (!data.Contains("exitPointX")) return;

        player = GetNode<Player>("/root/Main/Scene/Player");
        player.RotationHelperThird.SetThirdView(true);
        player.RotationHelperThird.MayChange = false;

        exitPoint = data["exitPoint"].ToString().ParseToVector3();

        var cameraPos = data["cameraPos"].ToString().ParseToVector3();
        LoadCamera(cameraPos);
    }

    private async void LoadCamera(Vector3 pos)
    {
        await ToSignal(GetTree(), "idle_frame");
        
        SpawnCamera(pos);
    }
}
