using Godot;
using Godot.Collections;

public partial class NuclearManagerTrigger : TriggerBase
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
    
    private Environment clonedEnvironment;

    public override void _Ready()
    {
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
        fogColor = environment.FogLightColor;
        fogDepthBegin = environment.FogDensity;
        SetProcess(false);
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
        SetProcess(true);
        enemiesManager.StopAlarm();
        enemiesManager.hasAlarm = false;

        await Global.Get().ToTimer(6, this);

        alarm.Play();
        foreach (var bomb in bombs)
        {
            bomb.Explode();
        }
        
        await Global.Get().ToTimer(10, this);

        clonedEnvironment = (Environment)environment.Duplicate();
        warStarted = true;
        
        await Global.Get().ToTimer(11, this);
        
        afterAudi.Play();
        
        await Global.Get().ToTimer(2, this);

        noise.Visible = true;
        noiseAnim.Play("noise");
        isNoising = true;
        
        await Global.Get().ToTimer(3.5f, this);

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
        if (noiseTextureModulate.A < 0.4f)
        {
            noiseTextureModulate.A += 0.004f;
        }
        noiseTexture.Modulate = noiseTextureModulate;
    }

    private void MakeSkyRed(float delta)
    {
        if (ambientColor.R < 0.9f)
        {
            ambientColor.R += delta * 0.05f;
            ambientEnergy += delta * 0.05f;
        }

        environment.AmbientLightColor = ambientColor;
        environment.AmbientLightEnergy = ambientEnergy;

        if (!isNoising) return;
        
        if (fogColor.R < 0.9f)
        {
            fogColor.R += delta * fogSpeed;
            fogDepthBegin -= delta * fogSpeed;
            fogSpeed += 0.001f;
        }

        environment.FogLightColor = fogColor;
        environment.FogDensity = fogDepthBegin;
    }

    private void ResetEnvironment()
    {
        environment.AmbientLightColor = clonedEnvironment.AmbientLightColor;
        environment.AmbientLightEnergy = clonedEnvironment.AmbientLightEnergy;
        environment.FogLightColor = clonedEnvironment.FogLightColor;
        environment.FogDensity = clonedEnvironment.FogDensity;
    }

    public override void _Process(double delta)
    {
        if (warStarted)
        {
            IncreaseShaking((float)delta);
            MakeSkyRed((float)delta);
        }

        if (isNoising)
        {
            IncreaseNoise();
        }

        if (!gameOver) return;
        
        blackScreen.Visible = true;
        var blackScreenColor = blackScreen.Color;
        if (blackScreenColor.A < 1)
        {
            blackScreenColor.A += (float)delta;
            blackScreen.Color = blackScreenColor;
        }
        else
        {
            ResetEnvironment();
            var levelsLoader = GetNode<LevelsLoader>("/root/Main");
            levelsLoader.LoadLevel(NewLevelNum);
        }
    }
}
