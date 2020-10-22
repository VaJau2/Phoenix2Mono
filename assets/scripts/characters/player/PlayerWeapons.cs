using Godot;
using System.Collections.Generic;

public class PlayerWeapons 
{
    //TODO
    //создать скрипт для Area, назвать shotgunArea и переписать соотв-й код
    //создать здесь ссылку для него
    //найти в оригинальном коде ссылку на него и писать все это дело дальше

    const float SHAKE_SPEED = 4;
    const float SHAKE_DIFF = 0.5f;

    //--статистика------
    public WeaponTypes TempWeapon = WeaponTypes.None;

    public Dictionary<WeaponTypes, WeaponStats> weaponStats;

    Player player;
    RayCast rayFirst;
    RayCast rayThird;
    CollisionShape gunShape;
    RayCast rayShotgun;

    public void LoadStats()
    {
        WeaponStats pistolStats = new WeaponStats();
        pistolStats.ammo = 50;
        pistolStats.ammoMax = 100;
        pistolStats.damage = 20;
        pistolStats.distance = 80;
        pistolStats.cooldown = 0.7f;
        pistolStats.recoil = 4f;

        WeaponStats shotgunStats = new WeaponStats();
        shotgunStats.ammo = 5;
        shotgunStats.ammoMax = 60;
        shotgunStats.damage = 110;
        shotgunStats.distance = 40;
        shotgunStats.cooldown = 2f;
        shotgunStats.recoil = 2.5f;

        WeaponStats revolverStats = new WeaponStats();
        revolverStats.ammo = 20;
        revolverStats.ammoMax = 100;
        revolverStats.damage = 20;
        revolverStats.distance = 80;
        revolverStats.cooldown = 0.5f;
        revolverStats.recoil = 3.0f;

        WeaponStats sniperStats = new WeaponStats();
        sniperStats.ammoMax = 5;
        sniperStats.ammoMax = 15;
        sniperStats.damage = 70;
        sniperStats.distance = 200;
        sniperStats.cooldown = 1.6f;
        sniperStats.recoil = 1.5f;

        weaponStats = new Dictionary<WeaponTypes, WeaponStats>()
        {
            {WeaponTypes.Pistol, pistolStats},
            {WeaponTypes.Shotgun, shotgunStats},
            {WeaponTypes.Revolver, revolverStats},
            {WeaponTypes.Sniper, sniperStats}
        };
    }

    public PlayerWeapons(Player player) 
    {
        this.player = player;

        rayFirst = player.GetNode<RayCast>("rotation_helper/camera/ray");
        rayThird = player.GetNode<RayCast>("rotation_helper_third/camera/ray");
        rayFirst.AddException(player);
        rayThird.AddException(player);

        gunShape = player.GetNode<CollisionShape>("gun_shape");
        rayShotgun = player.GetNode<RayCast>("player_body/shotgunRay");

        LoadStats();
    }

    public bool IsNotRifle {
        get {
            return (TempWeapon == WeaponTypes.Pistol ||
                    TempWeapon == WeaponTypes.Revolver ||
                    TempWeapon == WeaponTypes.None);
        }
    }

    public RayCast EnableHeadRay(float distance)
    {
        var tempRay = rayFirst;
        if(player.ThirdView) 
        {
            tempRay = rayThird;
        }
        tempRay.CastTo = new Vector3(0,0,-distance);
        tempRay.Enabled = true;
        return tempRay;
    }
}

public enum WeaponTypes
{
    Pistol,
    Shotgun,
    Revolver,
    Sniper,
    None
};

public struct WeaponStats
{
    public bool have;
    public int ammo;
    public int ammoMax;
    public int damage;
    public int distance;
    public float cooldown;
    public float recoil;
}