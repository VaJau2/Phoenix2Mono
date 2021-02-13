using Godot;
using Godot.Collections;
using System;

public class SoundSteps: RayCast {

    //ссылка на скрипт через player.Body.SoundSteps

    [Export]
    public NodePath parentPath;
    const float STEP_COOLDOWN = 0.4f;
    const float STEP_CROUCH_COOLDOWN = 0.8f;
    const float STEP_RUN_COOLDOWN = 0.6f;

    const int SOUNDS_COUNT = 3;
    const float SOUND_SPEED = 6;
    Global global;

    private bool isPlayer = true;
    
    Character parent {get => GetNode<Character>(parentPath);}

    float timer = 0;
    int stepI = 0;

    string landMaterial = "grass";

    AudioStreamPlayer audi;
    AudioStreamSample dash;
    AudioStreamSample jump;

    Dictionary<string, Array<AudioStreamSample>> steps;
    Dictionary<string, Array<AudioStreamSample>> stepsRun;
    Dictionary<string, Array<AudioStreamSample>> stepsCrouch;

    Random rand;

    private bool playerCrouching {
        get {
            var player = parent as Player;
            return player.IsCrouching;
        }
    }

    private bool parentRunning {
        get {
            //TODO
            //добавить сюда условие для бегающих противников
            var player = parent as Player_Earthpony;
            if (player != null) {
                return player.IsRunning;
            }
            return false;
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
        if (parent is Player) {
            var player = parent as Player;
            audi = player.GetAudi();
            isPlayer = true;
        } else {
            audi = parent.GetNode<AudioStreamPlayer>("audi");
            isPlayer = false;
        }
       
        dash = GD.Load<AudioStreamSample>("res://assets/audio/steps/dash.wav");
        jump = GD.Load<AudioStreamSample>("res://assets/audio/steps/jump.wav");

        steps = new Dictionary<string, Array<AudioStreamSample>>()
        {
            {"grass", LoadSounds("grass", "stepGrassFast")},
            {"dirt", LoadSounds("dirt", "stepDirtFast")},
            {"wood", LoadSounds("wood", "stepWoodFast")},
            {"stone", LoadSounds("stone", "StoneStep")}
        };

        stepsRun = new Dictionary<string, Array<AudioStreamSample>>()
        {
            {"grass", LoadSounds("grass", "stepGrassRun")},
            {"dirt", LoadSounds("dirt", "stepDirtRun")},
            {"wood", LoadSounds("dirt", "stepDirtRun")},
            {"stone", LoadSounds("stone", "StoneStepRun")}
        };

        stepsCrouch = new Dictionary<string, Array<AudioStreamSample>>()
        {
            {"grass", LoadSounds("grass", "stepGrass")},
            {"dirt", LoadSounds("dirt", "stepDirt")},
            {"wood", LoadSounds("wood", "stepWoodFast")},
            {"stone", LoadSounds("stone", "StoneStep")}
        };

        global = Global.Get();
        rand = new Random();
    }

    public void SoundDash()
    {
        audi.Stream = dash;
        audi.Play();
        timer = STEP_COOLDOWN;
    }

    public override void _Process(float delta)
    {
        if (isPlayer) {
            if (global.playerRace != Race.Unicorn && !playerCrouching) {
                var pegasus = parent as Player_Pegasus;
                if (Input.IsActionJustPressed("jump") && (pegasus == null || !pegasus.IsFlying)) {
                    audi.Stream = jump;
                    audi.Play();
                    timer = 0;
                }
            }
        }

        bool sounding = parent.Health > 0 && (parent.Velocity.Length() > SOUND_SPEED) && (!isPlayer || parent.IsOnFloor());

        if (sounding) {
            if (timer > 0) {
                timer -= delta;
            } else {
                if (isPlayer && playerCrouching) {
                    audi.Stream = stepsCrouch[landMaterial][stepI];
                    timer = STEP_CROUCH_COOLDOWN;
                } else {
                    if (parentRunning) {
                        audi.Stream = stepsRun[landMaterial][stepI];
                        timer = STEP_RUN_COOLDOWN;
                    } else {
                        audi.Stream = steps[landMaterial][stepI];
                        timer = STEP_COOLDOWN;
                    }
                }
                audi.Play();

                var oldI = stepI;
                stepI = rand.Next(0, SOUNDS_COUNT);
                while (oldI == stepI) {
                    stepI = rand.Next(0, SOUNDS_COUNT);
                }
            }
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        var player = parent as Player;
        if (player.Health <= 0)
            return;
        
        if (isPlayer) {
            player.OnStairs = false;
        }

        if (parent.Velocity.Length() > 2) {
            if (!isPlayer || parent.IsOnFloor()) {
                var collideObj = GetCollider();
                if (collideObj is StaticBody) {
                    var collideBody = collideObj as StaticBody;
                    var friction = collideBody.PhysicsMaterialOverride.Friction;
                    var materialName = MatNames.GetMatName(friction);

                    if (materialName == "stairs") {
                        if (isPlayer) {
                            player.OnStairs = true;
                        }
                        landMaterial = "stone";
                        return;
                    } 

                    if (steps.Keys.Contains(materialName)) {
                        landMaterial = materialName;
                    } else {
                        landMaterial = "wood";
                    }
                    
                }
            }
        }
    }
}