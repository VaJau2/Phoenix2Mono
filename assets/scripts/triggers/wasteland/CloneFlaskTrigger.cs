using Godot;


public class CloneFlaskTrigger : TriggerBase
{
    private CloneFlask cloneFlask;
    private int step;
    private float timer;

    private AudioEffectsController audioEffectController;
    private Canvas canvas;

    private PhoenixSystem phoenixSystem;
    
    public override async void _Ready()
    {
        SetProcess(false);

        await ToSignal(GetTree(), "idle_frame");

        audioEffectController = GetNode<AudioEffectsController>("/root/Main/Scene/Player/audioEffectsController");
        canvas = GetNode<Canvas>("/root/Main/Scene/canvas");
    }

    public override void _Process(float delta)
    {
        if (timer > 0)
        {
            timer -= delta;
        }
        else
        {
            SetProcess(false);
            step++;
            _on_activate_trigger();
        }
    }

    public void Resurrect(CloneFlask _cloneFlask, PhoenixSystem _phoenixSystem)
    {
        cloneFlask = _cloneFlask;
        phoenixSystem = _phoenixSystem;
        phoenixSystem.CloneWokeUp = false;
        
        _on_activate_trigger();
    }
    
    public override void _on_activate_trigger()
    {
        var global = Global.Get();
        var player = global.player;

        switch (step)
        {
            case 0:
                player.Resurrect();
                cloneFlask.SetRace(global.playerRace);
                player.Visible = false;
                player.RotationHelperThird.SetThirdView(false);
                player.RotationHelperThird.MayChange = false;
                player.MayMove = false;
                player.Sit(false);
                player.Camera.eyesClosed = true;
               
                var playerPosTransform = cloneFlask.playerPos.GlobalTransform;
                player.GlobalTransform = Global.SetNewOrigin
                (
                    player.GlobalTransform,
                    playerPosTransform.origin
                );

                var flaskRotation = playerPosTransform.basis.GetEuler();
                player.Rotation = new Vector3(0, flaskRotation.y, 0);
                
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
                player.Camera.eyesClosed = false;
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
                player.MayMove = true;
                player.Camera.Current = true;
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
                base._on_activate_trigger();
                
                phoenixSystem.CloneWokeUp = true;
                break;
        }
    }
}
