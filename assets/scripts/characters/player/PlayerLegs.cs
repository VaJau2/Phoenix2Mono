using Godot;
using System.Collections.Generic;

public class PlayerLegs: Node
{
    const int BACK_INCREASE = 2;
    const int DOOR_OPEN_DAMAGE = 110;

    const float BACK_HIT_ANGLE = 60;

    //Player player;
    Global global;

    AudioStreamPlayer audi;
    AudioStreamSample tryHit;
    AudioStreamSample hit;

    Dictionary<string, AudioStreamSample> materaiSounds;
    bool tempFront = false;
    
    List<PhysicsBody> frontObjects;
    List<PhysicsBody> backObjects;

    public bool getTempFront { get { return tempFront;} }

    private Player player { get => global.player; }

    public override void _Ready()
    {
        global = Global.Get();
        audi = GetNode<AudioStreamPlayer>("../../sound/audi_hitted");

        tryHit = GD.Load<AudioStreamSample>("res://assets/audio/enemies/SwordTryHit.wav");
        hit= GD.Load<AudioStreamSample>("res://assets/audio/flying/PegasusHit.wav");

        string matPath = "res://assets/audio/guns/legHits/";
        materaiSounds = new Dictionary<string, AudioStreamSample>()
        {
            {"door", GD.Load<AudioStreamSample>(matPath + "door_slam.wav")},
            {"door_open", GD.Load<AudioStreamSample>(matPath + "door_slam_open.wav")},
            {"fence", GD.Load<AudioStreamSample>(matPath + "fence_hit.wav")},
            {"stone", GD.Load<AudioStreamSample>(matPath + "stone_hit.wav")},
            {"wood", GD.Load<AudioStreamSample>(matPath + "wood_hit.wav")},
        };

        frontObjects = new List<PhysicsBody>();
        backObjects = new List<PhysicsBody>();
    }

    private int GetDamage()
    {
        int damage = player.BaseDamage + player.LegsDamage;
        if (!tempFront) {
            damage *= BACK_INCREASE;
        } 
        return damage;
    }


    private void handleVictim(PhysicsBody victim, int damage)
    {
        if (victim != null) {
            if (victim is Character) {
                audi.Stream = hit;
                Character character = victim as Character;
                character.TakeDamage(damage);
            } else {
                if (victim is StaticBody) {
                    var body = victim as StaticBody;
                    var friction = body.PhysicsMaterialOverride.Friction;
                    var materialName = MatNames.GetMatName(friction);
                    if (materaiSounds.ContainsKey(materialName)) {
                        audi.Stream = materaiSounds[materialName];
                    }
                }

                if (victim is BreakableObject) {
                    var obj = victim as BreakableObject;
                    obj.Brake(damage);
                }
                else if (victim is FurnDoor) {
                    var door = victim as FurnDoor;
                    if (!door.IsOpen) {
                        if (!door.ForceOpening) {
                            door.audi.Stream = materaiSounds["stone"];
                            door.audi.Play();
                        } else if(door.myKey != "" && 
                            damage < DOOR_OPEN_DAMAGE) {
                                door.audi.Stream = materaiSounds["door"];
                                door.audi.Play();
                        } else {
                            door.setOpen(materaiSounds["door_open"], 0, true);
                        }
                    }
                }
            }
        }
    }

    private void startHit()
    {
        player.IsHitting = true;
        player.MayMove = false;
        if (tempFront) {
            player.BodyFollowsCamera = true;
        }
        player.Body.AnimateHitting(tempFront);
    }

    private async void finishHit()
    {
        if (tempFront && (player.Weapons.isPistol || !player.Weapons.GunOn)) {
            player.BodyFollowsCamera = false;
        }

        await global.ToTimer(0.15f);

        audi.Stream = tryHit;
        audi.Play();

        await global.ToTimer(0.15f);
        audi.Stream = null;
        var damage = GetDamage();

        if (tempFront) {
            foreach (PhysicsBody victim in frontObjects) {
                handleVictim(victim, damage);
            }
        } else {
            foreach (PhysicsBody victim in backObjects){
                handleVictim(victim, damage);
            }
        }
        audi.Play();

        await global.ToTimer(0.5f);

        player.IsHitting = false;
        player.MayMove = true;
    }

    public override void _Process(float delta)
    {
        if (player == null) return;
        
        bool playerRunningFlying = false;

        switch(global.playerRace) {
            case Race.Pegasus:
                var pegasus = player as Player_Pegasus;
                playerRunningFlying = pegasus.IsFlying;
                break;
            case Race.Earthpony:
                var earthpony = player as Player_Earthpony;
                playerRunningFlying = earthpony.IsRunning;
                break;
        }

        if (!player.IsHitting && player.MayMove && !playerRunningFlying) {
            if (Input.IsActionJustPressed("legsHit") && player.IsOnFloor()) {
                tempFront = Mathf.Abs(player.Body.bodyRot) < BACK_HIT_ANGLE;
                startHit();
                finishHit();
            }
        }
    }

    public void FrontAreaBodyEntered(PhysicsBody body) {
        if (body is Player) return;

        if (body is StaticBody || body is Character) {
            frontObjects.Add(body);
        }
    }

    public void FrontAreaBodyExited(PhysicsBody body) {
        if (body is Player) return;

        if (frontObjects.Contains(body)) {
            frontObjects.Remove(body);
        }
    }

    public void BackAreaBodyEntered(PhysicsBody body) {
        if (body is Player) return;

        if (body is StaticBody || body is Character) {
            backObjects.Add(body);
        }
    }

    public void BackAreaBodyExited(PhysicsBody body) {
        if (body is Player) return;

        if (backObjects.Contains(body)) {
            backObjects.Remove(body);
        }
    }
}