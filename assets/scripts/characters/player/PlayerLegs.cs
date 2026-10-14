using Godot;
using System.Collections.Generic;

public partial class PlayerLegs : Node
{
    const int BACK_INCREASE = 2;

    const float BACK_HIT_ANGLE = 60;
    
    Global global;

    AudioStreamPlayer audi;
    AudioStreamWAV tryHit;
    AudioStreamWAV hit;

    Dictionary<string, AudioStreamWAV> materalSounds;
    bool tempFront;

    List<PhysicsBody3D> frontObjects;
    List<PhysicsBody3D> backObjects;

    private Player Player => global.player;

    public override void _Ready()
    {
        global = Global.Get();
        audi = GetNode<AudioStreamPlayer>("../../sound/audi_hitted");

        tryHit = GD.Load<AudioStreamWAV>("res://assets/audio/enemies/SwordTryHit.wav");
        hit = GD.Load<AudioStreamWAV>("res://assets/audio/flying/PegasusHit.wav");

        string matPath = "res://assets/audio/guns/legHits/";
        materalSounds = new Dictionary<string, AudioStreamWAV>()
        {
            { "fence", GD.Load<AudioStreamWAV>(matPath + "fence_hit.wav") },
            { "stone", GD.Load<AudioStreamWAV>(matPath + "stone_hit.wav") },
            { "wood", GD.Load<AudioStreamWAV>(matPath + "wood_hit.wav") },
        };

        frontObjects = [];
        backObjects = [];
    }

    private int GetDamage()
    {
        int damage = Player.BaseDamage + Player.LegsDamage;
        if (!tempFront)
        {
            damage *= BACK_INCREASE;
        }

        return damage;
    }


    private void HandleVictim(PhysicsBody3D victim, int damage)
    {
        if (!IsInstanceValid(victim)) return;

        if (victim is Character character)
        {
            audi.Stream = hit;
            character.TakeDamage(Player, damage);
        }
        else
        {
            if (victim is StaticBody3D body && body.PhysicsMaterialOverride != null)
            {
                var friction = body.PhysicsMaterialOverride.Friction;
                var materialName = MatNames.GetMatName(friction);
                if (materalSounds.ContainsKey(materialName))
                {
                    audi.Stream = materalSounds[materialName];
                }
            }

            switch (victim)
            {
                case BreakableObject obj:
                    obj.Brake(damage);
                    break;
                
                case FurnDoor door:
                {
                    door.TrySmashOpen(damage);
                    break;
                }
            }
        }
    }

    private void StartHit()
    {
        Player.IsHitting = true;
        Player.SetMayMove(false);
        if (tempFront)
        {
            Player.BodyFollowsCamera = true;
        }

        Player.Body.AnimateHitting(tempFront);
    }

    private async void FinishHit()
    {
        await global.ToTimer(0.15f);

        if (tempFront && (Player.Weapons.isPistol || !Player.Weapons.GunOn || Global.Get().playerRace == Race.Unicorn))
        {
            Player.BodyFollowsCamera = false;
        }

        audi.Stream = tryHit;
        audi.Play();

        await global.ToTimer(0.15f);
        audi.Stream = null;
        var damage = GetDamage();

        if (tempFront)
        {
            foreach (PhysicsBody3D victim in frontObjects)
            {
                HandleVictim(victim, damage);
            }
        }
        else
        {
            foreach (PhysicsBody3D victim in backObjects)
            {
                HandleVictim(victim, damage);
            }
        }

        audi.Play();

        await global.ToTimer(0.5f);

        Player.IsHitting = false;
        Player.SetMayMove(true);
    }

    public override void _Process(float delta)
    {
        if (Player == null) return;

        bool playerRunningFlying = false;

        switch (global.playerRace)
        {
            case Race.Pegasus:
                var pegasus = Player as Player_Pegasus;
                playerRunningFlying = pegasus.IsFlying;
                break;
            case Race.Earthpony:
                var earthpony = Player as Player_Earthpony;
                playerRunningFlying = earthpony.IsRunning;
                break;
        }

        if (!Player.IsHitting && Player.MayMove && !playerRunningFlying)
        {
            if (Input.IsActionJustPressed("legsHit") && Player.IsOnFloor())
            {
                tempFront = Mathf.Abs(Player.Body.bodyRot) < BACK_HIT_ANGLE;
                StartHit();
                FinishHit();
            }
        }
    }

    public void FrontAreaBodyEntered(PhysicsBody3D body)
    {
        if (body is Player) return;

        if (body is StaticBody3D or Character)
        {
            frontObjects.Add(body);
        }
    }

    public void FrontAreaBodyExited(PhysicsBody3D body)
    {
        if (body is Player) return;

        if (frontObjects.Contains(body))
        {
            frontObjects.Remove(body);
        }
    }

    public void BackAreaBodyEntered(PhysicsBody3D body)
    {
        if (body is Player) return;

        if (body is StaticBody3D or Character)
        {
            backObjects.Add(body);
        }
    }

    public void BackAreaBodyExited(PhysicsBody3D body)
    {
        if (body is Player) return;

        if (backObjects.Contains(body))
        {
            backObjects.Remove(body);
        }
    }
}