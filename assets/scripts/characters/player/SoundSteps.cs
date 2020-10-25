using Godot;
using Godot.Collections;
using System;

public class SoundSteps {

    //ссылка на скрипт через player.Body.SoundSteps

    const float STEP_COOLDOWN = 0.4f;
    const float STEP_CROUCH_COOLDOWN = 0.8f;
    const float STEP_RUN_COOLDOWN = 0.6f;

    const int SOUNDS_COUNT = 3;
    const float SOUND_SPEED = 6;
    Global global;

    private bool isPlayer = true;
    
    RayCast ray;
    Character parent;

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

    public SoundSteps(Character parent, RayCast ray)
    {
        this.parent = parent;
        this.ray = ray;

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

    public void Update(float delta)
    {
        if (isPlayer) {
            if (global.playerRace != Race.Unicorn) {
                var pegasus = parent as Player_Pegasus;
                if (Input.IsActionJustPressed("jump") && (pegasus == null || !pegasus.IsFlying)) {
                    audi.Stream = jump;
                    audi.Play();
                    timer = 0;
                }
            }

            if (global.playerRace == Race.Earthpony) {
                var earthpony = parent as Player_Earthpony;
                if (Input.IsActionJustPressed("dash") && earthpony.IsDashing) {
                    audi.Stream = dash;
                    audi.Play();
                    timer = STEP_COOLDOWN;
                }
            }
        }

        bool sounding = (parent.GetSpeed() > SOUND_SPEED) && (!isPlayer || parent.IsOnFloor());

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

    public void PhysicsUpdate(float delta)
    {
        var player = parent as Player;
        if (isPlayer) {
            player.OnStairs = false;
        }

        if (parent.GetSpeed() > 2) {
            if (!isPlayer || parent.IsOnFloor()) {
                var collideObj = ray.GetCollider();
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