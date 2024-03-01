using Godot;
using Godot.Collections;

public partial class NPCWeapons : Node
{
    NPC npc;
    Dictionary tempWeaponStats;
    Node3D tempWeapon;
    AnimationPlayer gunAnim;
    Node3D gunLight;
    Node3D gunFire;
    GpuParticles3D gunSmoke;
    PackedScene gunParticlesPrefab;
    WeaponShellSpawner shellSpawner;

    //--звуки-------------
    AudioStreamPlayer3D audiShoot;
    AudioStreamWav shootSound;
    AudioStreamWav missSound;

    public bool GunOn;
    public bool isPistol;

    private RandomNumberGenerator rand = new();

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
        isPistol = weaponData.ContainsKey("isPistol");
        shootSound = GD.Load<AudioStreamWav>("res://assets/audio/guns/shoot/" + weaponCode + ".wav");
        npc.BaseDamage = GetStatsInt("damage");

        //вытаскиваем родительский нод из непися
        Node3D tempParent = npc.GetWeaponParent(isPistol);

        //отправляем префаб туда
        var weapon = weaponPrefab.Instantiate();
        tempParent.AddChild(weapon);

        tempWeapon = (Node3D) weapon;
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

    public void SpawnPickableItem(string weaponCode)
    {
        if (string.IsNullOrEmpty(weaponCode)) return;
        if (!(npc is NpcWithWeapons npcWithWeapons)) return;
        
        string path = "res://objects/guns/items/" + weaponCode + ".tscn";
        if (!ResourceLoader.Exists(path)) return;
        
        Node3D tempParent = npcWithWeapons.GetWeaponParent(isPistol);
        PackedScene itemPrefab = GD.Load<PackedScene>(path);
        var item = itemPrefab.Instantiate<Node3D>();
        item.Name = "Created_" + npc.Name + "s_" + item.Name;
        var itemsParent = GetNode<Node>("/root/Main/Scene");
        itemsParent.AddChild(item);
        item.GlobalTransform = Global.SetNewOrigin(item.GlobalTransform, tempParent.GlobalTransform.Origin);
        item.GlobalRotation = tempParent.GlobalRotation;
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
            var eyes = pony.GetNodeOrNull<NPCFace>("Armature/Skeleton3D/Body");
            eyes?.CloseEyes();
        }

        var statsDistance = GetStatsInt("shootDistance");

        if (tempWeaponStats.ContainsKey("bullet"))
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
                Vector3 shootPos = victim.GlobalTransform.Origin + Vector3.Up;
                var gunParticles = gunParticlesPrefab.Instantiate<Node3D>();
                GetNode("/root/Main/Scene").AddChild(gunParticles);
                gunParticles.GlobalTransform = Global.SetNewOrigin(
                    gunParticles.GlobalTransform,
                    shootPos
                );
                gunParticles.Call(
                    "_startEmitting",
                    npc.GlobalTransform.Basis.Z,
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

        if (tempWeaponStats.ContainsKey("isSilence")) return GetStatsFloat("cooldown");
        
        EnemiesManager enemiesManager = npc.GetParent() as EnemiesManager;
        enemiesManager?.LoudShoot(GetStatsInt("shootDistance") * 0.75f, npc.GlobalTransform.Origin);

        return GetStatsFloat("cooldown");
    }

    public int GetStatsInt(string statsName) => int.Parse(tempWeaponStats[statsName].ToString());
    public float GetStatsFloat(string statsName) => Global.ParseFloat(tempWeaponStats[statsName].ToString());

    private void SpawnBullet()
    {
        string bullet = tempWeaponStats["bullet"].ToString();
        var bulletPrefab = GD.Load<PackedScene>("res://objects/guns/bullets/" + bullet + ".tscn");
        Bullet newBullet = bulletPrefab.Instantiate<Bullet>();
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
                newBullet.Rotation.X + rotXDelta,
                newBullet.Rotation.Y + rotYDelta,
                newBullet.Rotation.Z
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

        gunLight = tempWeapon.GetNode<Node3D>("light");
        gunFire = tempWeapon.GetNode<Node3D>("fire");
        gunSmoke = tempWeapon.GetNode<GpuParticles3D>("smoke");
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
        missSound = GD.Load<AudioStreamWav>("res://assets/audio/guns/ShotBeside.wav");
        audiShoot = GetNode<AudioStreamPlayer3D>("../audiShoot");
        rand.Randomize();
    }
}