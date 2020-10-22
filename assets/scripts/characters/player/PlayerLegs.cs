using Godot;
using System.Collections.Generic;

public class PlayerLegs 
{
    const int EARTHPONY_FRONT_DAMAGE = 40;
    const int EARTHPONY_BACK_DAMAGE = 80;

    const int FRONT_DAMAGE = 10;
    const int BACK_DAMAGE = 25;

    const float BACK_HIT_ANGLE = 60;

    Player player;
    Global global;

    AudioStreamPlayer audi;
    AudioStreamSample tryHit;
    AudioStreamSample hit;

    Dictionary<string, AudioStreamSample> materaiSounds;

    bool tempFront = false;
    bool stoppingHit = false;
    float hittingTimer = 0;
    
    List<PhysicsBody> frontObjects;
    List<PhysicsBody> backObjects;

    public bool getTempFront { get { return tempFront;} }

    public PlayerLegs(Player player) 
    {
        this.player = player;
        global = Global.Get();
        audi = player.GetNode<AudioStreamPlayer>("sound/audi_hitted");

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

    private void handleVictim(PhysicsBody victim, int damage)
    {
        if (victim != null) {
            if (victim is Character) {
                audi.Stream = hit;
                Character character = victim as Character;
                character.TakeDamage(damage);
            } else {
                GD.Print("Tried to brake breakable object, but it didn't coded yet");
                GD.Print("Go to PlayerLegs.cs and code this c:");
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
        player.Body.AnimateHitting(tempFront, '1');
    }

    private async void finishHit()
    {
        stoppingHit = true;
        if (tempFront && player.Weapons.IsNotRifle) {
            player.BodyFollowsCamera = false;
        }
        player.Body.AnimateHitting(tempFront, '2');

        await global.ToTimer(0.15f);

        audi.Stream = tryHit;
        audi.Play();

        await global.ToTimer(0.1f);
        audi.Stream = null;

        if (tempFront) {
            var damage = FRONT_DAMAGE;
            if (global.playerRace == Race.Earthpony) {
                damage = EARTHPONY_FRONT_DAMAGE;
            }
            damage *= (int)hittingTimer;
            foreach (PhysicsBody victim in frontObjects) {
                handleVictim(victim, damage);
            }
        } else {
            var damage = BACK_DAMAGE;
            if (global.playerRace == Race.Earthpony) {
                damage = EARTHPONY_BACK_DAMAGE;
            }
            damage *= (int)hittingTimer;
            foreach (PhysicsBody victim in frontObjects) {
                handleVictim(victim, damage);
            }
        }
        audi.Play();

        await global.ToTimer(0.5f);

        player.IsHitting = false;
        player.MayMove = true;
        stoppingHit = false;
    }

    public void Update(float delta) {
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
                hittingTimer = 0;
                tempFront = Mathf.Abs(player.Body.bodyRot) < BACK_HIT_ANGLE;
                startHit();
            }
        }
        if (player.IsHitting) {
            if (hittingTimer < 5) {
                hittingTimer += delta;
            }

            if (!stoppingHit) {
                if (Input.IsActionJustReleased("legsHit")) {
                    finishHit();
                }
            }
        }
    }

    public void FrontAreaBodyEntered(PhysicsBody body) {
        if (body is StaticBody || body is Character) {
            frontObjects.Add(body);
        }
    }

    public void FrontAreaBodyExited(PhysicsBody body) {
        if (frontObjects.Contains(body)) {
            frontObjects.Remove(body);
        }
    }

    public void BackAreaBodyEntered(PhysicsBody body) {
        if (body is StaticBody || body is Character) {
            backObjects.Add(body);
        }
    }

    public void BackAreaBodyExited(PhysicsBody body) {
        if (backObjects.Contains(body)) {
            backObjects.Remove(body);
        }
    }
}