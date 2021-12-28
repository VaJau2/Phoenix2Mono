using Godot;
using Godot.Collections;

public class NuclearManagerTrigger : TriggerBase
{
    [Export] private NodePath afterAudiPath;
    [Export] public int NewLevelNum;

    private AudioStreamPlayer3D alarm;
    private AudioStreamPlayer afterAudi;
    private ColorRect blackScreen;
    private EnemiesManager enemiesManager;

    private Control noise;
    private TextureRect noiseTexture;
    private AnimationPlayer noiseAnim;
    
    private float increaseSpeed = 0.5f;
    private bool warStarted = false;
    private bool isNoising = false;
    private Array<NuclearBombTrigger> bombs;

    private Environment environment;
    private float ambientEnergy = 0.5f;
    private Color ambientColor = new Color("10142f");
    private Color fogColor = new Color("1b1b20");
    private float fogDepthBegin = 15f;
    private float fogSpeed = 0;

    private bool gameOver;

    private SavableTimers timers;

    public override void _Ready()
    {
        base._Ready();
        timers = GetNode<SavableTimers>("/root/Main/Scene/timers");
        enemiesManager = GetNode<EnemiesManager>("/root/Main/Scene/npc");
        alarm = GetNode<AudioStreamPlayer3D>("alarm");
        afterAudi = GetNode <AudioStreamPlayer>(afterAudiPath);

        noise = GetNode<Control>("/root/Main/Scene/canvas/noise");
        noiseTexture = noise.GetNode<TextureRect>("texture");
        noiseAnim = noise.GetNode<AnimationPlayer>("anim");

        bombs = new Array<NuclearBombTrigger>();
        foreach (var child in GetChildren())
        {
            if (child is NuclearBombTrigger bomb)
            {
                bombs.Add(bomb);
            }
        }

        blackScreen = GetNode<ColorRect>("/root/Main/Scene/canvas/black");
        environment = GetNode<WorldEnvironment>("/root/Main/Scene/WorldEnvironment").Environment;
        ambientColor = environment.AmbientLightColor;
        ambientEnergy = environment.AmbientLightEnergy;
        fogColor = environment.FogColor;
        fogDepthBegin = environment.FogDepthBegin;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        if (IsActive)
        {
            StartWar();
        }
    }

    public async void StartWar()
    {
        SetActive(true);
        enemiesManager.StopAlarm();
        enemiesManager.hasAlarm = false;
        
        while (timers.CheckTimer(Name + "_timer0", 6))
        {
            await ToSignal(GetTree(), "idle_frame");
        }
        
        alarm.Play();
        foreach (var bomb in bombs)
        {
            bomb.Explode();
        }
        
        while (timers.CheckTimer(Name + "_timer1", 10))
        {
            await ToSignal(GetTree(), "idle_frame");
        }

        warStarted = true;
        
        while (timers.CheckTimer(Name + "_timer2", 11))
        {
            await ToSignal(GetTree(), "idle_frame");
        }
        
        afterAudi.Play();
        
        while (timers.CheckTimer(Name + "_timer3", 2))
        {
            await ToSignal(GetTree(), "idle_frame");
        }

        noise.Visible = true;
        noiseAnim.Play("noise");
        isNoising = true;
        
        while (timers.CheckTimer(Name + "_timer4", 3.5f))
        {
            await ToSignal(GetTree(), "idle_frame");
        }

        gameOver = true;
    }

    private void IncreaseShaking(float delta)
    {
        if (!(increaseSpeed > 0)) return;
        Global.Get().player.shakingSpeed += delta * increaseSpeed;
        increaseSpeed -= delta * 0.08f;
    }

    private void IncreaseNoise()
    {
        var noiseTextureModulate = noiseTexture.Modulate;
        if (noiseTextureModulate.a < 0.4f)
        {
            noiseTextureModulate.a += 0.004f;
        }
        noiseTexture.Modulate = noiseTextureModulate;
    }

    private void MakeSkyRed(float delta)
    {
        if (ambientColor.r < 0.9f)
        {
            ambientColor.r += delta * 0.05f;
            ambientEnergy += delta * 0.05f;
        }

        environment.AmbientLightColor = ambientColor;
        environment.AmbientLightEnergy = ambientEnergy;

        if (!isNoising) return;
        
        if (fogColor.r < 0.9f)
        {
            fogColor.r += delta * fogSpeed;
            fogDepthBegin -= delta * fogSpeed;
            fogSpeed += 0.001f;
        }

        environment.FogColor = fogColor;
        environment.FogDepthBegin = fogDepthBegin;
    }

    public override void _Process(float delta)
    {
        if (warStarted)
        {
            IncreaseShaking(delta);
            MakeSkyRed(delta);
        }

        if (isNoising)
        {
            IncreaseNoise();
        }

        if (!gameOver) return;
        
        blackScreen.Visible = true;
        var blackScreenColor = blackScreen.Color;
        if (blackScreenColor.a < 1)
        {
            blackScreenColor.a += delta;
            blackScreen.Color = blackScreenColor;
        }
        else
        {
            var levelsLoader = GetNode<LevelsLoader>("/root/Main");
            levelsLoader.LoadLevel(NewLevelNum);
        }
    }
}
