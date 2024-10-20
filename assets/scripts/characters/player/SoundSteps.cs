using Godot;
using Godot.Collections;
using System;

public class SoundSteps : RayCast
{
    //ссылка на скрипт через player.Body.SoundSteps

    [Export] public NodePath ParentPath;
    [Export] private bool isActive = true;

    private const float STEP_COOLDOWN = 0.4f;
    private const float STEP_JUMP_COOLDOWN = 0.7f;
    private const float STEP_CROUCH_COOLDOWN = 0.8f;
    private const float STEP_RUN_COOLDOWN = 0.6f;

    private const int SOUNDS_COUNT = 3;
    private const float SOUND_SPEED = 3;

    private Character parent => GetNode<Character>(ParentPath);
    private float timer = 0;
    private int stepI = 0;

    public string landMaterial { get; private set; }

    private AudioStreamPlayer audi;
    private AudioStreamPlayer3D audi3D;
    private AudioStream dash;
    private AudioStream jump;

    private Dictionary<string, Array<AudioStreamSample>> steps;
    private Dictionary<string, Array<AudioStreamSample>> stepsRun;
    private Dictionary<string, Array<AudioStreamSample>> stepsCrouch;

    private Character Player => parent;
    private bool IsPlayer => parent is Player;
    private Random rand;

    private bool PlayerCrouching
    {
        get
        {
            var player = parent as Player;
            return player?.IsCrouching ?? false;
        }
    }

    private bool ParentRunning
    {
        get
        {
            return parent switch
            {
                Player_Earthpony player when IsInstanceValid(player) => player.IsRunning,
                NPC npc when IsInstanceValid(npc) => npc.MovingController is NavigationMovingController { IsRunning: true},
                _ => false
            };
        }
    }

    private void PlaySound(AudioStream sound)
    {
        if (IsInstanceValid(audi))
        {
            audi.Stream = sound;
            audi.Play();
        }
        else
        {
            audi3D.Stream = sound;
            audi3D.Play();
        }
    }

    private Array<AudioStreamSample> LoadSounds(string materialName, string fileName)
    {
        var tempArray = new Array<AudioStreamSample>();

        for (int i = 1; i < SOUNDS_COUNT + 1; i++)
        {
            var tempSound = GD.Load<AudioStreamSample>("res://assets/audio/steps/" + materialName + "/" + fileName +
                                                       i.ToString() + ".wav");
            tempArray.Add(tempSound);
        }

        return tempArray;
    }

    public override void _Ready()
    {
        if (IsPlayer)
        {
            var player = parent as Player;
            audi = player.GetAudi();
        }
        else
        {
            audi3D = parent.GetNode<AudioStreamPlayer3D>("audi");
        }

        dash = GD.Load<AudioStream>("res://assets/audio/steps/dash.wav");
        jump = GD.Load<AudioStream>("res://assets/audio/steps/jump.mp3");

        steps = new Dictionary<string, Array<AudioStreamSample>>()
        {
            { "grass", LoadSounds("grass", "stepGrassFast") },
            { "dirt", LoadSounds("dirt", "stepDirtFast") },
            { "wood", LoadSounds("wood", "stepWoodFast") },
            { "stone", LoadSounds("stone", "StoneStep") },
            { "water", LoadSounds("water", "stepWaterFast") },
            { "snow", LoadSounds("snow", "stepSnowFast") },
            { "metal", LoadSounds("metal", "stepMetalFast") }
        };

        stepsRun = new Dictionary<string, Array<AudioStreamSample>>()
        {
            { "grass", LoadSounds("grass", "stepGrassRun") },
            { "dirt", LoadSounds("dirt", "stepDirtRun") },
            { "wood", LoadSounds("dirt", "stepDirtRun") },
            { "stone", LoadSounds("stone", "StoneStepRun") },
            { "water", LoadSounds("water", "stepWaterFast") },
            { "snow", LoadSounds("snow", "stepSnowRun") },
            { "metal", LoadSounds("metal", "stepMetalFast") }
        };

        stepsCrouch = new Dictionary<string, Array<AudioStreamSample>>()
        {
            { "grass", LoadSounds("grass", "stepGrass") },
            { "dirt", LoadSounds("dirt", "stepDirt") },
            { "wood", LoadSounds("wood", "stepWoodFast") },
            { "stone", LoadSounds("stone", "StoneStep") },
            { "water", LoadSounds("water", "stepWaterFast") },
            { "snow", LoadSounds("snow", "stepSnow") },
            { "metal", LoadSounds("metal", "stepMetalFast") }
        };

        rand = new Random();
    }

    public void SoundDash()
    {
        PlaySound(dash);
        timer = STEP_COOLDOWN;
    }

    private void CheckFloor()
    {
        if (Player.Health <= 0)
            return;

        if (IsPlayer)
        {
            ((Player)Player).OnStairs = false;
        }

        var collideObj = GetCollider();
        if (collideObj is StaticBody collideBody && collideBody.PhysicsMaterialOverride != null)
        {
            var friction = collideBody.PhysicsMaterialOverride.Friction;
            var materialName = MatNames.GetMatName(friction);

            if (materialName is "stairs" or "grass_stairs")
            {
                if (IsPlayer)
                {
                    ((Player)Player).OnStairs = true;
                }

                landMaterial = materialName switch
                {
                    "stairs" => "stone",
                    "grass_stairs" => "grass",
                    _ => landMaterial
                };

                return;
            }

            landMaterial = steps.Keys.Contains(materialName) ? materialName : "wood";
        }
        else
        {
            landMaterial = null;
        }
    }

    public void PlayJumpSound()
    {
        PlaySound(jump);
        timer = STEP_JUMP_COOLDOWN;
    }

    public override void _Process(float delta)
    {
        if (!isActive) return;

        if (IsPlayer)
        {
            if (!((Player)parent).MayMove)
            {
                return;
            }
        }

        bool sounding = parent.Health > 0 && (parent.Velocity.Length() > SOUND_SPEED) &&
                        !string.IsNullOrEmpty(landMaterial);

        if (sounding)
        {
            if (timer > 0)
            {
                timer -= delta;
            }
            else
            {
                if (IsPlayer && PlayerCrouching)
                {
                    PlaySound(stepsCrouch[landMaterial][stepI]);
                    timer = STEP_CROUCH_COOLDOWN;
                }
                else
                {
                    if (ParentRunning)
                    {
                        PlaySound(stepsRun[landMaterial][stepI]);
                        timer = STEP_RUN_COOLDOWN;
                    }
                    else
                    {
                        PlaySound(steps[landMaterial][stepI]);
                        timer = STEP_COOLDOWN;
                    }
                }

                var oldI = stepI;
                stepI = rand.Next(0, SOUNDS_COUNT);
                while (oldI == stepI)
                {
                    stepI = rand.Next(0, SOUNDS_COUNT);
                }
            }
        }

        CheckFloor();
    }
}
