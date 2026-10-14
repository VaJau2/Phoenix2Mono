using System;
using Godot;
using Godot.Collections;

public partial class NpcWeapons : Node, ISavable
{
    [Export] public string WeaponCode = "";
    [Export] public NodePath PistolWeaponParentPath;
    [Export] public NodePath GunWeaponParentPath;
    
    public bool GunOn;
    public bool IsPistol;
    
    private NPC npc;
    
    private Dictionary tempWeaponStats;
    private Node3D tempWeapon;
    private AnimationPlayer gunAnim;
    private Node3D gunLight;
    private Node3D gunFire;
    private GpuParticles3D gunSmoke;
    private PackedScene gunParticlesPrefab;
    private WeaponShellSpawner shellSpawner;

    //--звуки-------------
    private AudioStreamPlayer3D audiShoot;
    private AudioStreamWav shootSound;
    private AudioStreamWav missSound;

    private RandomNumberGenerator rand = new();
    private Node3D PistolWeaponParent;
    private Node3D GunWeaponParent;

    private float shootCooldown;

    public bool HasWeapon => !string.IsNullOrEmpty(WeaponCode);
    
    [Signal]
    public delegate void ShootingEventHandler();
    
    public override void _Ready()
    {
        npc = GetParent<NPC>();
        gunParticlesPrefab = GD.Load<PackedScene>("res://objects/guns/gunParticles.tscn");
        missSound = GD.Load<AudioStreamWav>("res://assets/audio/guns/ShotBeside.wav");
        audiShoot = GetNode<AudioStreamPlayer3D>("../audiShoot");
        PistolWeaponParent = GetNodeOrNull<Node3D>(PistolWeaponParentPath);
        GunWeaponParent = GetNodeOrNull<Node3D>(GunWeaponParentPath);
        rand.Randomize();

        LoadWeapon(WeaponCode);
    }

    public override void _Process(double delta)
    {
        if (shootCooldown > 0)
        {
            shootCooldown -= delta;
        }
    }

    public void SetWeaponCode(string newWeaponCode)
    {
        WeaponCode = newWeaponCode;
    }

    public void LoadWeapon(string newWeaponCode)
    {
        if (string.IsNullOrEmpty(WeaponCode))
        {
            tempWeapon?.QueueFree();
            tempWeapon = null;
            SetWeaponOn(false);
            return;
        }

        WeaponCode = newWeaponCode;

        //грузим префаб оружия
        string path = "res://objects/guns/prefabs/" + newWeaponCode + ".tscn";
        PackedScene weaponPrefab = GD.Load<PackedScene>(path);

        //грузим статистику оружия
        Dictionary weaponData = ItemJSON.GetItemData(newWeaponCode);
        tempWeaponStats = weaponData;
        IsPistol = weaponData.Contains("isPistol");
        shootSound = GD.Load<AudioStreamWav>("res://assets/audio/guns/shoot/" + newWeaponCode + ".wav");
        npc.BaseDamage = GetStatsInt("damage");

        //вытаскиваем родительский нод из непися
        Node3D tempParent = GetWeaponParent();

        //отправляем префаб туда
        var weapon = weaponPrefab.Instantiate();
        tempParent.AddChild(weapon);

        tempWeapon = (Node3D) weapon;
        LoadGunEffects();

        SetWeaponOn(false);
    }

    public void SetWeaponOn(bool on)
    {
        GunOn = on;
        if (IsInstanceValid(tempWeapon))
        {
            tempWeapon.Visible = on;
        }
        
        CheckMagicParticles(on);
    }

    private Node3D GetWeaponParent()
    {
        return IsPistol ? PistolWeaponParent : GunWeaponParent;
    }

    private void CheckMagicParticles(bool on)
    {
        var particles = npc.GetNodeOrNull<GpuParticles3D>("Armature/Skeleton/BoneAttachment/HeadPos/Particles");
        if (particles == null) return;
        particles.Emitting = on;
    }

    public async void SpawnPickableItem()
    {
        if (string.IsNullOrEmpty(WeaponCode)) return;
        
        string path = "res://objects/guns/items/" + WeaponCode + ".tscn";
        if (!ResourceLoader.Exists(path)) return;
        PackedScene itemPrefab = GD.Load<PackedScene>(path);
        var item = (Node3D)itemPrefab.Instantiate();
        item.Name = "Created_" + npc.Name + "s_" + item.Name;
        var itemsParent = GetNode<Node>("/root/Main/Scene");
        itemsParent.AddChild(item);
        
        var tempParent = GetWeaponParent();
        item.GlobalTransform = Global.SetNewOrigin(item.GlobalTransform, tempParent.GlobalTransform.Origin);
        item.GlobalRotation = tempParent.GlobalRotation;
    }

    public void MakeShoot(float victimDistance)
    {
        if (shootCooldown > 0) return;
        
        audiShoot.Stream = shootSound;
        audiShoot.Play();
        gunAnim?.Play("shoot");

        var eyes = npc.GetNodeOrNull<NpcFace>("Armature/Skeleton/Body");
        eyes?.CloseEyes();
        
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
                Vector3 shootPos = victim.GlobalTransform.Origin + Vector3.Up;
                var gunParticles = (GunParticles) gunParticlesPrefab.Instantiate();
                GetNode("/root/Main/Scene").AddChild(gunParticles);
                gunParticles.GlobalTransform = Global.SetNewOrigin(
                    gunParticles.GlobalTransform,
                    shootPos
                );
                
                gunParticles.StartEmitting(
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

        shootCooldown += GetShootCooldown();

        if (tempWeaponStats.Contains("isSilence")) return;
        
        EnemiesManager enemiesManager = npc.GetParent() as EnemiesManager;
        enemiesManager?.LoudShoot(GetStatsInt("shootDistance") * 0.75f, npc);
    }
    
    public float GetStatsFloat(string statsName) => Global.ParseFloat(tempWeaponStats[statsName].ToString());
    
    private int GetStatsInt(string statsName) => int.Parse(tempWeaponStats[statsName].ToString());
    
    private float GetShootCooldown()
    {
        return tempWeapon == null ? 1f : GetStatsFloat("cooldown");
    }

    private void SpawnBullet()
    {
        string bullet = tempWeaponStats["bullet"].ToString();
        var bulletPrefab = GD.Load<PackedScene>("res://objects/guns/bullets/" + bullet + ".tscn");
        Bullet newBullet = (Bullet) bulletPrefab.Instantiate();
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

    public Dictionary GetSaveData()
    {
        var saveData = new Dictionary();
        saveData["weaponCode"] = WeaponCode;
        saveData["shootCooldown"] = shootCooldown;
        return saveData;
    }

    public void LoadData(Dictionary data)
    {
        if (!data.Contains("weaponCode")) return;
        
        if (data["weaponCode"] != null)
        {
            LoadWeapon(data["weaponCode"].ToString());
        }

        shootCooldown = Convert.ToSingle(data["shootCooldown"]);
    }
}
