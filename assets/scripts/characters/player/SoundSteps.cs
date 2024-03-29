using Godot;
using Godot.Collections;
using System;

public class SoundSteps: RayCast {

    //ссылка на скрипт через player.Body.SoundSteps

    [Export] public NodePath parentPath;
    [Export] private bool isActive = true;
    
    const float STEP_COOLDOWN = 0.4f;
    const float STEP_JUMP_COOLDOWN = 0.7f;
    const float STEP_CROUCH_COOLDOWN = 0.8f;
    const float STEP_RUN_COOLDOWN = 0.6f;

    const int SOUNDS_COUNT = 3;
    const float SOUND_SPEED = 6;
    Global global;
    
    Character parent => GetNode<Character>(parentPath);
    float timer = 0;
    int stepI = 0;

    public string landMaterial {get; private set;} = null;

    AudioStreamPlayer audi;
    AudioStreamPlayer3D audi3D;
    AudioStream dash;
    AudioStream jump;

    Dictionary<string, Array<AudioStreamSample>> steps;
    Dictionary<string, Array<AudioStreamSample>> stepsRun;
    Dictionary<string, Array<AudioStreamSample>> stepsCrouch;

    Character player => parent as Character;
    bool isPlayer => parent is Player;
    Random rand;

    private bool playerCrouching {
        get {
            var player = parent as Player;
            return player.IsCrouching;
        }
    }

    private bool parentRunning {
        get {
            var player = parent as Player_Earthpony;
            if (IsInstanceValid(player)) {
                return player.IsRunning;
            } 

            var npc = parent as Pony;
            if (IsInstanceValid(npc)) {
                return npc.IsRunning;
            }
            return false;
        }
    }

    private void PlaySound(AudioStream sound)
    {
        if (IsInstanceValid(audi)) {
            audi.Stream = sound;
            audi.Play();
        } else {
            audi3D.Stream = sound;
            audi3D.Play();
        }
    }

    private Array<AudioStreamSample> LoadSounds(string materialName, string fileName)
    {
        var tempArray = new Array<AudioStreamSample>();

        for(int i = 1; i < SOUNDS_COUNT + 1; i++) {
            var tempSound = GD.Load<AudioStreamSample>("res://assets/audio/steps/" + materialName + "/" + fileName + i.ToString() + ".wav");
            tempArray.Add(tempSound);
        }
        return tempArray;
    }

    public override void _Ready()
    {
        if (isPlayer) {
            var player = parent as Player;
            audi = player.GetAudi();
        } else {
            audi3D = parent.GetNode<AudioStreamPlayer3D>("audi");
        }
       
        dash = GD.Load<AudioStream>("res://assets/audio/steps/dash.wav");
        jump = GD.Load<AudioStream>("res://assets/audio/steps/jump.mp3");

        steps = new Dictionary<string, Array<AudioStreamSample>>()
        {
            {"grass", LoadSounds("grass", "stepGrassFast")},
            {"dirt", LoadSounds("dirt", "stepDirtFast")},
            {"wood", LoadSounds("wood", "stepWoodFast")},
            {"stone", LoadSounds("stone", "StoneStep")},
            {"water", LoadSounds("water", "stepWaterFast")},
            {"snow", LoadSounds("snow", "stepSnowFast")},
            {"metal", LoadSounds("metal", "stepMetalFast")}
        };

        stepsRun = new Dictionary<string, Array<AudioStreamSample>>()
        {
            {"grass", LoadSounds("grass", "stepGrassRun")},
            {"dirt", LoadSounds("dirt", "stepDirtRun")},
            {"wood", LoadSounds("dirt", "stepDirtRun")},
            {"stone", LoadSounds("stone", "StoneStepRun")},
            {"water", LoadSounds("water", "stepWaterFast")},
            {"snow", LoadSounds("snow", "stepSnowRun")},
            {"metal", LoadSounds("metal", "stepMetalFast")}
        };

        stepsCrouch = new Dictionary<string, Array<AudioStreamSample>>()
        {
            {"grass", LoadSounds("grass", "stepGrass")},
            {"dirt", LoadSounds("dirt", "stepDirt")},
            {"wood", LoadSounds("wood", "stepWoodFast")},
            {"stone", LoadSounds("stone", "StoneStep")},
            {"water", LoadSounds("water", "stepWaterFast")},
            {"snow", LoadSounds("snow", "stepSnow")},
            {"metal", LoadSounds("metal", "stepMetalFast")}
        };

        global = Global.Get();
        rand = new Random();
    }

    public void SoundDash()
    {
        PlaySound(dash);
        timer = STEP_COOLDOWN;
    }

    private void CheckFloor()
    {
        if (player.Health <= 0)
            return;

        if (isPlayer) {
            (player as Player).OnStairs = false;
        }

        var collideObj = GetCollider();
        if (collideObj is StaticBody collideBody && collideBody.PhysicsMaterialOverride != null) {
            var friction = collideBody.PhysicsMaterialOverride.Friction;
            var materialName = MatNames.GetMatName(friction);
            
            if (materialName == "stairs" || materialName == "grass_stairs") {
                if (isPlayer) {
                    (player as Player).OnStairs = true;
                }
                switch (materialName) {
                    case "stairs":
                        landMaterial = "stone";
                        break;
                    case "grass_stairs":
                        landMaterial = "grass";
                        break;
                }
                
                return;
            } 

            if (steps.Keys.Contains(materialName)) {
                landMaterial = materialName;
            } else {
                landMaterial = "wood";
            }
            
        } else {
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
        
        if (isPlayer) {
            if (!(parent as Player).MayMove) {
                return;
            }
        }

        bool sounding = parent.Health > 0 && (parent.Velocity.Length() > SOUND_SPEED) && !string.IsNullOrEmpty(landMaterial);

        if (sounding) {
            if (timer > 0) {
                timer -= delta;
            } else {
                if (isPlayer && playerCrouching) {
                    PlaySound(stepsCrouch[landMaterial][stepI]);
                    timer = STEP_CROUCH_COOLDOWN;
                } else {
                    if (parentRunning) {
                        PlaySound(stepsRun[landMaterial][stepI]);
                        timer = STEP_RUN_COOLDOWN;
                    } else {
                        PlaySound(steps[landMaterial][stepI]);
                        timer = STEP_COOLDOWN;
                    }
                }

                var oldI = stepI;
                stepI = rand.Next(0, SOUNDS_COUNT);
                while (oldI == stepI) {
                    stepI = rand.Next(0, SOUNDS_COUNT);
                }
            }
        }

        CheckFloor();
    }
}