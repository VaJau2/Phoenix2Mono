using Godot;
using Godot.Collections;

public class NPCWeapons : Node
{
    NPC npc;
    Dictionary tempWeaponStats;
    Spatial tempWeapon;
    AnimationPlayer gunAnim;
    Spatial gunLight;
    Spatial gunFire;
    Particles gunSmoke;
    PackedScene gunParticlesPrefab;
    WeaponShellSpawner shellSpawner;

    //--звуки-------------
    AudioStreamPlayer3D audiShoot;
    AudioStreamSample shootSound;
    AudioStreamSample missSound;

    public bool GunOn;
    public bool isPistol;

    private RandomNumberGenerator rand = new RandomNumberGenerator();

    public void LoadWeapon(NpcWithWeapons npc, string weaponCode)
    {
        this.npc = npc;

        if (string.IsNullOrEmpty(weaponCode))
        {
            tempWeapon?.QueueFree();
            tempWeapon = null;
            SetWeapon(false);
            return;
        }

        //грузим префаб оружия
        string path = "res://objects/guns/prefabs/" + weaponCode + ".tscn";
        PackedScene weaponPrefab = GD.Load<PackedScene>(path);

        //грузим статистику оружия
        Dictionary weaponData = ItemJSON.GetItemData(weaponCode);
        tempWeaponStats = weaponData;
        isPistol = weaponData.Contains("isPistol");
        shootSound = GD.Load<AudioStreamSample>("res://assets/audio/guns/shoot/" + weaponCode + ".wav");
        npc.BaseDamage = GetStatsInt("damage");

        //вытаскиваем родительский нод из непися
        Spatial tempParent = npc.GetWeaponParent(isPistol);

        //отправляем префаб туда
        var weapon = weaponPrefab.Instance();
        tempParent.AddChild(weapon);

        tempWeapon = (Spatial) weapon;
        LoadGunEffects();

        SetWeapon(false);
    }

    public void SetWeapon(bool on)
    {
        GunOn = on;
        if (IsInstanceValid(tempWeapon))
        {
            tempWeapon.Visible = on;
        }

        if (!(npc is Pony tempPony)) return;
        if (tempPony.IsUnicorn)
        {
            tempPony.MagicParticles.Emitting = on;
        }
    }

    public float MakeShoot(float victimDistance)
    {
        if (tempWeapon == null)
        {
            return 1f;
        }

        audiShoot.Stream = shootSound;
        audiShoot.Play();
        gunAnim?.Play("shoot");

        if (npc is Pony pony)
        {
            var eyes = pony.GetNodeOrNull<NPCFace>("Armature/Skeleton/Body");
            eyes?.CloseEyes();
        }

        var statsDistance = GetStatsInt("shootDistance");

        if (tempWeaponStats.Contains("bullet"))
        {
            SpawnBullet();
        }
        else
        {
            var victim = npc.tempVictim;

            float shootChance = 1.0f - (victimDistance / statsDistance * 0.5f);
            shootChance /= (victim.Velocity.Length() / 5);
            shootChance *= Global.Get().Settings.npcAccuracy;

            AnimGunEffects();

            if (rand.Randf() < shootChance)
            {
                Vector3 shootPos = victim.GlobalTransform.origin + Vector3.Up;
                var gunParticles = (Spatial) gunParticlesPrefab.Instance();
                GetNode("/root/Main/Scene").AddChild(gunParticles);
                gunParticles.GlobalTransform = Global.setNewOrigin(
                    gunParticles.GlobalTransform,
                    shootPos
                );
                gunParticles.Call(
                    "_startEmitting",
                    npc.GlobalTransform.basis.z,
                    "blood"
                );

                npc.MakeDamage(victim);
            }
            else
            {
                if (victim is Player)
                {
                    var playerAudi = (victim as Player).GetAudi(true);
                    playerAudi.Stream = missSound;
                    playerAudi.Play();
                }
            }
        }

        if (tempWeaponStats.Contains("isSilence")) return GetStatsFloat("cooldown");
        
        EnemiesManager enemiesManager = npc.GetParent() as EnemiesManager;
        enemiesManager?.LoudShoot(GetStatsInt("shootDistance") * 0.75f, npc.GlobalTransform.origin);

        return GetStatsFloat("cooldown");
    }

    public int GetStatsInt(string statsName) => int.Parse(tempWeaponStats[statsName].ToString());
    public float GetStatsFloat(string statsName) => Global.ParseFloat(tempWeaponStats[statsName].ToString());

    private void SpawnBullet()
    {
        string bullet = tempWeaponStats["bullet"].ToString();
        var bulletPrefab = GD.Load<PackedScene>("res://objects/guns/bullets/" + bullet + ".tscn");
        Bullet newBullet = (Bullet) bulletPrefab.Instance();
        newBullet.Damage = npc.GetDamage();
        newBullet.Shooter = npc;
        newBullet.Timer = GetStatsFloat("shootDistance");

        GetNode("/root/Main/Scene").AddChild(newBullet);
        newBullet.GlobalTransform = gunFire.GlobalTransform;

        if (rand.Randf() < 0.4f)
        {
            var rotXDelta = (rand.Randf() - 0.5f) / 10f;
            var rotYDelta = (rand.Randf() - 0.5f) / 10f;
            newBullet.Rotation = new Vector3(
                newBullet.Rotation.x + rotXDelta,
                newBullet.Rotation.y + rotYDelta,
                newBullet.Rotation.z
            );
        }
    }

    private void LoadGunEffects()
    {
        if (tempWeapon.HasNode("anim"))
        {
            gunAnim = tempWeapon.GetNode<AnimationPlayer>("anim");
        }
        else
        {
            gunAnim = null;
        }

        gunLight = tempWeapon.GetNode<Spatial>("light");
        gunFire = tempWeapon.GetNode<Spatial>("fire");
        gunSmoke = tempWeapon.GetNode<Particles>("smoke");
        shellSpawner = tempWeapon.GetNodeOrNull<WeaponShellSpawner>("shells");
    }

    private async void AnimGunEffects()
    {
        gunLight.Visible = true;
        gunSmoke.Restart();
        gunFire.Visible = true;
        shellSpawner?.StartSpawning();
        await Global.Get().ToTimer(0.06f);
        gunFire.Visible = false;
        gunLight.Visible = false;
    }

    public override void _Ready()
    {
        gunParticlesPrefab = GD.Load<PackedScene>("res://objects/guns/gunParticles.tscn");
        missSound = GD.Load<AudioStreamSample>("res://assets/audio/guns/ShotBeside.wav");
        audiShoot = GetNode<AudioStreamPlayer3D>("../audiShoot");
        rand.Randomize();
    }
}