using Godot;
using Godot.Collections;
using System.Globalization;

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

    //--звуки-------------
    AudioStreamPlayer audi;
    AudioStreamPlayer audiShoot;
    AudioStreamSample shootSound;

    private bool GunOn;
    private bool isPistol;

    public void LoadWeapon(NPC npc, string weaponCode)
    {
        this.npc = npc;

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

        tempWeapon = (Spatial)weapon;
        LoadGunEffects();

        SetWeapon(false);
    }

    public void SetWeapon(bool on)
    {
        GunOn = on;
        if (tempWeapon != null) tempWeapon.Visible = on;
    }

    public void MakeShoot()
    {
        audiShoot.Stream = shootSound;
        audiShoot.Play();
        if (gunAnim != null) {
            gunAnim.Play("shoot");
        }
        npc.head.CloseEyes();
        var tempDistance = GetStatsInt("shootDistance");

        if (tempWeaponStats.Contains("bullet")) {
            SpawnBullet();
        } else {
            var victim = npc.tempVictim;
            Vector3 shootPos = victim.GlobalTransform.origin + Vector3.Up * 2f;

            var gunParticles = (Spatial)gunParticlesPrefab.Instance();
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
    }

    public int GetStatsInt(string statsName) => int.Parse(tempWeaponStats[statsName].ToString());
    public float GetStatsFloat(string statsName) => float.Parse(tempWeaponStats[statsName].ToString(), CultureInfo.InvariantCulture);

    private void SpawnBullet()
    {
        string bullet = tempWeaponStats["bullet"].ToString();
        var bulletPrefab = GD.Load<PackedScene>("res://objects/guns/bullets/" + bullet + ".tscn");
        Bullet newBullet = (Bullet)bulletPrefab.Instance();
        newBullet.Damage = npc.GetDamage();
        newBullet.Shooter = npc;
        
        GetNode("/root/Main/Scene").AddChild(newBullet);
        newBullet.GlobalTransform = gunFire.GlobalTransform;
    }

    private void LoadGunEffects() 
    {
        if (tempWeapon.HasNode("anim")) {
            gunAnim = tempWeapon.GetNode<AnimationPlayer>("anim");
        } else {
            gunAnim = null;
        }
        
        gunLight = tempWeapon.GetNode<Spatial>("light");
        gunFire = tempWeapon.GetNode<Spatial>("fire");
        gunSmoke = tempWeapon.GetNode<Particles>("smoke");
    }
}
