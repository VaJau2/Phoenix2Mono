using Godot;


public partial class CloneFlaskTrigger : TriggerBase
{
    private PhoenixSystem phoenixSystem;
    private CloneFlask cloneFlask;
    private Canvas canvas;
    
    private int step;
    private double timer;
    
    public override async void _Ready()
    {
        SetProcess(false);

        await ToSignal(GetTree(), "idle_frame");
        canvas = GetNode<Canvas>("/root/Main/Scene/canvas");
    }

    public override void _Process(double delta)
    {
        if (timer > 0)
        {
            timer -= delta;
        }
        else
        {
            SetProcess(false);
            step++;
            OnActivateTrigger();
        }
    }

    public async void Resurrect(CloneFlask _cloneFlask, PhoenixSystem _phoenixSystem)
    {
        cloneFlask = _cloneFlask;
        phoenixSystem = _phoenixSystem;
        phoenixSystem.CloneWokeUp = false;

        if (Global.Get().player == null)
        {
            var playerSpawner = GetNode<PlayerSpawner>("/root/Main/Scene/PlayerSpawner");
            playerSpawner.InitSpawn();
            
            await playerSpawner.ToSignal(playerSpawner, nameof(PlayerSpawner.SpawnedEventHandler));
        }
        
        OnActivateTrigger();
    }
    
    public override void OnActivateTrigger()
    {
        var player = Global.Get().player;
        var audioEffectController = player.GetNode<AudioEffectsController>("audioEffectsController");

        switch (step)
        {
            case 0:
                player.HealHealth(player.HealthMax);
                player.Visible = false;
                player.RotationHelperThird.SetThirdView(false);
                player.RotationHelperThird.MayChange = false;
                player.SetMayMove(false);
                player.Sit(false);
                player.Camera3D.eyesClosed = true;
               
                var playerPosTransform = cloneFlask.playerPos.GlobalTransform;
                player.GlobalTransform = Global.SetNewOrigin
                (
                    player.GlobalTransform,
                    playerPosTransform.Origin
                );

                var flaskRotation = playerPosTransform.Basis.GetEuler();
                player.Rotation = new Vector3(0, flaskRotation.Y, 0);
                
                cloneFlask.camera.MakeCurrent(true);
                cloneFlask.PrepareFlaskForPlayer();
                canvas.FadeOut();

                player.GetAudi().Stream = cloneFlask.underwater;
                player.GetAudi().Play();

                audioEffectController.AddEffects("flaskWater");
                audioEffectController.AddEffects("flaskGlass");
                
                timer = 2f;
                SetProcess(true);
                break;

            case 1:
                player.Camera3D.eyesClosed = false;
                cloneFlask.anim.Play("wakeUp");
                
                timer = 1f;
                SetProcess(true);
                break;

            case 2:
                cloneFlask.PlayMessage();
                cloneFlask.AnimateWater();
                timer = 1.75f;
                SetProcess(true);
                break;

            case 3:
                audioEffectController.RemoveEffects("flaskWater");
                
                timer = 2.5f;
                SetProcess(true);
                break;
            
            case 4:
                player.Visible = true;
                player.SetMayMove(true);
                player.Camera3D.Current = true;
                player.RotationHelperThird.MayChange = true;
                cloneFlask.DeleteBody();
                cloneFlask.camera.MakeCurrent(false);
                
                timer = 0.6f;
                SetProcess(true);
                break;

            case 5:
                player.GetAudi().Stream = cloneFlask.flaskOpen;
                player.GetAudi().Play();
                cloneFlask.AnimateGlass();
                
                timer = 0.3f;
                SetProcess(true);
                break;
            
            case 6:
                audioEffectController.RemoveEffects("flaskGlass");
                cloneFlask = null;
                step = 0;
                base.OnActivateTrigger();
                
                phoenixSystem.CloneWokeUp = true;
                break;
        }
    }
}
