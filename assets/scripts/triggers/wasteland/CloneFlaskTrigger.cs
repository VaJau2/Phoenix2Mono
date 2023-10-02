using Godot;


public class CloneFlaskTrigger : TriggerBase
{
    [Export] private NodePath flaskPath;
    [Export] AudioStreamSample underwater;
    [Export] AudioStreamSample flaskOpen;
    private CloneFlask cloneFlask;
    private ColorRect blackScreen;
    private int step;
    private float timer;
    private bool changeBlackScreen;

    private AudioEffectsController audioEffectController;

    public override async void _Ready()
    {
        SetProcess(false);
        cloneFlask = GetNode<CloneFlask>(flaskPath);
        blackScreen = GetNode<ColorRect>("/root/Main/Scene/canvas/black");

        await ToSignal(GetTree(), "idle_frame");

        audioEffectController = GetNode<AudioEffectsController>("/root/Main/Scene/Player/audioEffectsController");
        
        if (IsActive)
        {
            _on_activate_trigger();
        }
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

    public override void _on_activate_trigger()
    {
        var global = Global.Get();
        var player = global.player;

        switch (step)
        {
            case 0:
                cloneFlask.SetRace(global.playerRace);
                player.Visible = false;
                player.RotationHelperThird.SetThirdView(false);
                player.RotationHelperThird.MayChange = false;
                player.MayMove = false;
                player.Sit(false);
                player.Camera.eyesClosed = true;
               
                cloneFlask.camera.MakeCurrent(true);
                cloneFlask.PrepareFlaskForPlayer();
                blackScreen.Color = new Color(0, 0, 0, 1);

                player.GetAudi(true).Stream = underwater;
                player.GetAudi(true).Play();

                audioEffectController.AddEffects("flaskWater");
                audioEffectController.AddEffects("flaskGlass");
                
                timer = 2f;
                SetProcess(true);
                break;

            case 1:
                player.Camera.eyesClosed = false;
                changeBlackScreen = true;
                
                cloneFlask.anim.CurrentAnimation = "wakeUp";
                
                timer = 1f;
                SetProcess(true);
                break;

            case 2:
                cloneFlask.audi.Play();
                cloneFlask.AnimateWater();
                timer = 1.5f;
                SetProcess(true);
                break;

            case 3:
                audioEffectController.RemoveEffects("flaskWater");
                
                timer = 2.5f;
                SetProcess(true);
                break;
            
            case 4:
                var playerPosTransform = cloneFlask.playerPos.GlobalTransform;
                player.GlobalTransform = Global.setNewOrigin
                (
                    player.GlobalTransform,
                    playerPosTransform.origin
                );
                var flaskRotation = playerPosTransform.basis.GetEuler();
                player.Rotation = new Vector3(0, flaskRotation.y, 0);
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
                player.GetAudi(true).Stream = flaskOpen;
                player.GetAudi(true).Play();
                cloneFlask.AnimateGlass();
                
                timer = 0.3f;
                SetProcess(true);
                break;
            
            case 6:
                audioEffectController.RemoveEffects("flaskGlass");
                base._on_activate_trigger();
                break;
        }
    }
}
