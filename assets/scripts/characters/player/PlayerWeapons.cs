using Godot;
using System.Collections.Generic;
using System;

public class PlayerWeapons 
{
    Global global;
    const float SHAKE_SPEED = 4;
    const float SHAKE_DIFF = 0.5f;
    const int ZERO_NUM_KEY = 49;

    //--статистика------
    public bool GunOn = false;
    public WeaponTypes TempWeapon = WeaponTypes.None;

    public Dictionary<WeaponTypes, WeaponStats> weaponStats;

    Player player;
    //внутри player:
    //body
    //camera
    //head -> body.head

    RayCast rayFirst;
    RayCast rayThird;
    CollisionShape gunShape; //бывший collision
    RayCast rayShotgun;
    ShotgunArea shotgunArea;

    //---интерфейс-------
    Control askGet;
    Label label;
    Control shootInterface;

    Label ammoLabel;
    Sprite ammoIcon;
    GunIcons weaponIcons;


    //--модельки-------
    Dictionary<string, Spatial> weaponModels;
    Dictionary<string, Spatial> weaponThirdModels;

    //--эффекты и анимания выстрела
    AnimationPlayer gunAnim;
    Spatial gunLight;
    Spatial gunFire;
    Particles gunSmoke;
    PackedScene gunParticlesPrefab;
    Node particlesParent;

    float tempShake = 0;
    bool shakeUp = true;
    float cooldown = 0.6f;
    float changeWeaponCooldown = 0f;

    //--звуки-------------
    AudioStreamPlayer audi;
    AudioStreamPlayer audiShoot;

    Dictionary<string, AudioStreamSample> sounds;

    Node enemiesManager; //TODO - изменить на класс enemiesManager

    bool onetimeShoot = false;
    bool onetimeVisible = false;
    bool bagEnabled = false;
    

    private void loadModels() 
    {
        string head1Path = "rotation_helper/camera/weapons/";
        string bagPath = "player_body/Armature/Skeleton/BoneAttachment 2/shotgunBag/";
        weaponModels = new Dictionary<string, Spatial>()
        {
            {"pistol_on", player.GetNode<Spatial>(head1Path + "pistol")},
            {"pistol_off", player.GetNode<Spatial>(bagPath + "pistol")},

            {"shotgun_on", player.GetNode<Spatial>(bagPath + "shotgun")},

            {"revolver_on", player.GetNode<Spatial>(head1Path + "revolver")},
            {"revolver_off", player.GetNode<Spatial>(bagPath + "revolver")},

            {"sniper_on", player.GetNode<Spatial>(bagPath + "rifle")},
        };

        string head3Path = "player_body/Armature/Skeleton/BoneAttachment/weapons/";
        weaponThirdModels = new Dictionary<string, Spatial>() 
        {
            {"pistol_on", player.GetNode<Spatial>(head3Path + "pistol")},
            {"revolver_on", player.GetNode<Spatial>(head3Path + "revolver")}
        };
    }  

    private void loadStats()
    {
        WeaponStats pistolStats = new WeaponStats();
        pistolStats.silent = true;
        pistolStats.ammo = 50;
        pistolStats.ammoMax = 100;
        pistolStats.damage = 20;
        pistolStats.distance = 80;
        pistolStats.cooldown = 0.7f;
        pistolStats.recoil = 4f;
        pistolStats.icon = 40;

        WeaponStats shotgunStats = new WeaponStats();
        shotgunStats.ammo = 5;
        shotgunStats.ammoMax = 60;
        shotgunStats.damage = 110;
        shotgunStats.distance = 40;
        shotgunStats.cooldown = 2f;
        shotgunStats.recoil = 2.5f;
        shotgunStats.icon = 80;

        WeaponStats revolverStats = new WeaponStats();
        revolverStats.ammo = 20;
        revolverStats.ammoMax = 100;
        revolverStats.damage = 20;
        revolverStats.distance = 80;
        revolverStats.cooldown = 0.5f;
        revolverStats.recoil = 3.0f;
        revolverStats.icon = 40;

        WeaponStats sniperStats = new WeaponStats();
        sniperStats.ammoMax = 5;
        sniperStats.ammoMax = 15;
        sniperStats.damage = 70;
        sniperStats.distance = 200;
        sniperStats.cooldown = 1.6f;
        sniperStats.recoil = 1.5f;
        sniperStats.icon = 120;

        weaponStats = new Dictionary<WeaponTypes, WeaponStats>()
        {
            {WeaponTypes.Pistol, pistolStats},
            {WeaponTypes.Shotgun, shotgunStats},
            {WeaponTypes.Revolver, revolverStats},
            {WeaponTypes.Sniper, sniperStats}
        };
    }

    private AudioStreamSample loadSound(string path) 
    {
        return GD.Load<AudioStreamSample>(path);
    }

    private void loadSounds() 
    {
        string gunsPath = "res://assets/audio/guns/";
        sounds = new Dictionary<string, AudioStreamSample>() 
        {
            {"GunOn", loadSound(gunsPath + "GunOn.wav")},
            {"GunOff", loadSound(gunsPath + "GunOff.wav")},
            {"TryShoot", loadSound(gunsPath + "TryShoot.wav")},

            {"pistol_shoot", loadSound(gunsPath + "PistolShoot.wav")},
            {"shotgun_shoot", loadSound(gunsPath + "ShotgunShoot.wav")},
            {"revolver_shoot", loadSound(gunsPath + "RevolverShoot.wav")},
            {"sniper_shoot", loadSound(gunsPath + "SniperShoot.wav")}
        };
    }

    public PlayerWeapons(Player player) 
    {
        global = Global.Get();
        this.player = player;

        rayFirst = player.GetNode<RayCast>("rotation_helper/camera/ray");
        rayThird = player.GetNode<RayCast>("rotation_helper_third/camera/ray");
        rayFirst.AddException(player);
        rayThird.AddException(player);

        gunShape = player.GetNode<CollisionShape>("gun_shape");
        rayShotgun = player.GetNode<RayCast>("player_body/shotgunRay");
        shotgunArea = player.GetNode<ShotgunArea>("player_body/shotgunArea");
        askGet = player.GetNode<Control>("/root/Main/Scene/canvas/openBack");
        label = askGet.GetNode<Label>("label");
        shootInterface = player.GetNode<Control>("/root/Main/Scene/canvas/shootInterface");
        ammoIcon = shootInterface.GetNode<Sprite>("ammoBack/Sprite2");
        ammoLabel = shootInterface.GetNode<Label>("ammoBack/label");
        weaponIcons = shootInterface.GetNode<GunIcons>("gunIcons");

        gunParticlesPrefab = GD.Load<PackedScene>("res://objects/guns/gunParticles.tscn");
        particlesParent = player.GetNode("/root/Main/Scene");

        audi = player.GetNode<AudioStreamPlayer>("sound/audi_weapons");
        audiShoot = player.GetNode<AudioStreamPlayer>("sound/audi_shoot");

        //enemiesManager = player.GetNode("/root/Main/Scene/enemies");

        loadModels();
        loadStats();
        loadSounds();

        if (player.StartWeapons.Count > 0) {
            ChangeBagVisible(true);

            foreach(WeaponTypes type in player.StartWeapons) {
                WeaponStats stats = weaponStats[type];
                stats.have = true;
                weaponStats[type] = stats;
            }
            changeGun(player.StartWeapons[0]);
        }
    }

    private void loadGunEffects() 
    {
        var tempWeaponDict = weaponModels;

        string tempType = WeaponType.ToString(TempWeapon);
        bool haveThirdModel = weaponThirdModels.ContainsKey(tempType + "_on");
        if (player.ThirdView && haveThirdModel) {
            tempWeaponDict = weaponThirdModels;
        }
        var weapon = tempWeaponDict[tempType + "_on"];

        gunAnim = weapon.GetNode<AnimationPlayer>("anim");
        gunLight = weapon.GetNode<Spatial>("light");
        gunFire = weapon.GetNode<Spatial>("fire");
        gunSmoke = weapon.GetNode<Particles>("smoke");
    }

    public bool IsNotRifle {
        get {
            return (TempWeapon == WeaponTypes.Pistol ||
                    TempWeapon == WeaponTypes.Revolver ||
                    TempWeapon == WeaponTypes.None);
        }
    }

    private Vector3 SetRotX(Vector3 origin, float newRotX) 
    {
        origin.x = newRotX;
        return origin;
    }

    private void setGunOn(bool newGunOn, bool collisionOn = true) {
        GunOn = newGunOn;
        if (collisionOn) {
            gunShape.Disabled = !newGunOn;
            gunShape.RotationDegrees = Vector3.Zero;
        } else {
            gunShape.Disabled = true;
        }
        shootInterface.Visible = newGunOn;

        if (GunOn) {
            if (IsNotRifle) {
                player.BodyFollowsCamera = false;
            } else {
                player.BodyFollowsCamera = true;
                shotgunArea.RotationDegrees = SetRotX(
                    shotgunArea.RotationDegrees,
                    player.RotationHelper.RotationDegrees.x
                );
            }
        } else {
            player.BodyFollowsCamera = false;
        }
    }

    private async void shakeCameraUp() {
        bool shakingProcess = true;
        while(shakingProcess) {
            if (shakeUp) {
                if (tempShake < weaponStats[TempWeapon].recoil) {
                    tempShake += SHAKE_SPEED;
                    player.Camera.RotationDegrees = SetRotX(
                        player.Camera.RotationDegrees,
                        player.Camera.RotationDegrees.x + SHAKE_SPEED
                    );
                } else {
                    shakeUp = false;
                }
            } else {
                if (tempShake > 0) {
                    var diff = SHAKE_SPEED * SHAKE_DIFF;
                    tempShake -= diff;
                    player.Camera.RotationDegrees = SetRotX(
                        player.Camera.RotationDegrees,
                        player.Camera.RotationDegrees.x - SHAKE_SPEED * SHAKE_DIFF
                    );
                } else {
                    shakeUp = true;
                    shakingProcess = false;
                }
            }
            await player.ToSignal(player.GetTree(), "idle_frame");
        }
    }

    private async void checkVisible(List<Spatial> victims) 
    {
        foreach(Spatial victim in victims) {
            var wr = WeakRef.WeakRef(victim);
            if (wr.GetRef() != null) {
                var victPos = victim.GlobalTransform.origin;
                if (victim is Character) {
                    victPos.y += 1;
                }
                var dir = victPos - rayShotgun.GlobalTransform.origin;
                Transform newTransf = rayShotgun.GlobalTransform;
                newTransf.basis = new Basis(Vector3.Zero);
                rayShotgun.GlobalTransform = newTransf;
                rayShotgun.CastTo = dir;
                await player.ToSignal(player.GetTree(), "idle_frame");
                if (rayShotgun.GetCollider() == victim) {
                    handleVictim(victim);
                    GD.Print("add in weapons.cs in 313 enemy's impulse pls");
                } else {
                    if(!rayShotgun.IsColliding() && (victim is BreakableObject)) {
                        handleVictim(victim);
                    }
                }
            }
        }   
    }

    private void gunPartsEmit(Spatial gunParts, Vector3 point, string material) 
    {
        gunParts.Call("_startEmitting", point, material);
    }

    private void spawnBlood(Character victim, Vector3 impulse, Vector3 dir) 
    {
        Spatial gunParts = gunParticlesPrefab.Instance() as Spatial;
        particlesParent.AddChild(gunParts);
        gunParts.GlobalTransform = Global.setNewOrigin(
            gunParts.GlobalTransform,
            rayShotgun.GetCollisionPoint()
        );
        gunPartsEmit(gunParts, rayShotgun.GetCollisionNormal(), "blood");

        dir = dir.Normalized();
        dir.y = 0;
        GD.Print("add in weapons.cs in 340 enemy's impulse pls");
    }

    private string handleVictim(Spatial victim, int shapeID = 0)
    {
        string name = null;
        if(victim is Character) {
            if (victim.Name.Contains("target") ||
                victim.Name.Contains("roboEye") ||
                victim.Name.Contains("MrHandy")) {
                    name = "black";
                } else {
                    name = "blood";
                }
            var character = victim as Character;
            player.MakeDamage(character, weaponStats[TempWeapon].damage, shapeID);
        } else if (victim is StaticBody) {
            var body = victim as StaticBody;
            name = MatNames.GetMatName(body.PhysicsMaterialOverride.Friction);
            if (victim is BreakableObject) {
                var obj = victim as BreakableObject;
                obj.Brake(weaponStats[TempWeapon].damage);
            }
        }

        return name;
    }  

    private void activateGunOnModel(bool on) 
    {
        string tempType = WeaponType.ToString(TempWeapon);
        bool haveThirdModel = weaponThirdModels.ContainsKey(tempType + "_on");

        if (player.ThirdView && haveThirdModel) {
            weaponThirdModels[tempType + "_on"].Visible = on;
            gunShape.Disabled = on; 
        } else {
            weaponModels[tempType + "_on"].Visible = on;
            if (!IsNotRifle) {
                gunShape.Disabled = !on;
            }
        }
        if (on) {
            loadGunEffects();
        }
    }

    private void checkThirdView(bool thirdOn) 
    {
        string tempType = WeaponType.ToString(TempWeapon);
        bool haveThirdModel = weaponThirdModels.ContainsKey(tempType + "_on");

        if (GunOn && haveThirdModel) {
            weaponThirdModels[tempType + "_on"].Visible = thirdOn;
            weaponModels[tempType + "on"].Visible = !thirdOn;
            gunShape.Disabled = thirdOn;
            loadGunEffects();
        }
    }

    private void disactivateGunModel()
    {
        activateGunOnModel(false);
        if (IsNotRifle) {
            string tempType = WeaponType.ToString(TempWeapon);
            weaponModels[tempType + "_off"].Visible = false;
        } 
    }

    private void ChangeBagVisible(bool visible)
    {
        bagEnabled = visible;
        var bug = player.GetNode<Spatial>("player_body/Armature/Skeleton/BoneAttachment 2/shotgunBag");
        bug.Visible = visible;
    }

    private void changeGun(WeaponTypes newType) 
    {
        //если у игрока до этого не было оружия
        if (!bagEnabled) {
            ChangeBagVisible(true);
        }

        //вырубаем предыдущую модельку
        if (TempWeapon != WeaponTypes.None) {
            disactivateGunModel();
        }

        TempWeapon = newType;

        //врубаем новые модельки
        if (IsNotRifle) {
            string tempType = WeaponType.ToString(TempWeapon);
            weaponModels[tempType + "_off"].Visible = !GunOn;
            activateGunOnModel(GunOn);
            player.BodyFollowsCamera = false;
        } else {
            activateGunOnModel(true);
            setGunOn(true, false);
        }

        shotgunArea.Monitoring = (TempWeapon == WeaponTypes.Shotgun);
        
        ammoLabel.Text = weaponStats[TempWeapon].ammo.ToString();
        ammoIcon.RegionRect = new Rect2(weaponStats[TempWeapon].icon, 0, 40, 40);
        loadGunEffects();
        audi.Stream = sounds["GunOn"];
        audi.Play();
        cooldown = 0;

        weaponIcons.ChangeWeapon(this, TempWeapon);
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

    private async void handleShoot() {
        onetimeShoot = true;
        WeaponStats tempStats = weaponStats[TempWeapon];

        if (tempStats.ammo == 0) {
            audiShoot.Stream = sounds["TryShoot"];
            audiShoot.Play();
            onetimeShoot = true;
        } else {
            tempStats.ammo -= 1;
            ammoLabel.Text = tempStats.ammo.ToString();
            cooldown = tempStats.cooldown;
            var tempWeapon = WeaponType.ToString(TempWeapon);
            audiShoot.Stream = sounds[tempWeapon + "_shoot"];
            audiShoot.Play();
            if (gunAnim != null) {
                gunAnim.Play("shoot");
            }

            player.Body.Head.CloseEyes();
            var tempDistance = tempStats.distance;
            if (player.equipment["have_bandage"]) {
                tempDistance += 15;
            }
            var tempRay = EnableHeadRay(tempDistance);

            await player.ToSignal(player.GetTree(),"idle_frame");

            //обрабатываем попадания
            if (IsNotRifle || player.MayMove) {
                if (TempWeapon == WeaponTypes.Shotgun) {
                    player.impulse = player.RotationHelper.GlobalTransform.basis.z / 2;
                    var objs = shotgunArea.objectsInside;
                    checkVisible(objs);
                } else {
                    var obj = (Spatial)tempRay.GetCollider();
                    if (obj != null) {
                        var gunParticles = (Spatial)gunParticlesPrefab.Instance();
                        particlesParent.AddChild(gunParticles);
                        gunParticles.GlobalTransform = Global.setNewOrigin(
                            gunParticles.GlobalTransform,
                            tempRay.GetCollisionPoint()
                        );
                        var shapeId = tempRay.GetColliderShape();
                        var matName = handleVictim(obj, shapeId);
                        gunPartsEmit(
                            gunParticles,
                            tempRay.GetCollisionNormal(),
                            matName
                        );
                    }
                }
            }

            await global.ToTimer(0.1f);
            //Здесь была еще одна отдача с дробовика, но я честно хз зачем

            gunLight.Visible = true;
            gunSmoke.Restart();
            gunFire.Visible = true;
            shakeCameraUp();
            await global.ToTimer(0.05f);
            gunFire.Visible = false;
            await global.ToTimer(0.1f);
            gunLight.Visible = false;

            tempRay.Enabled = false;

            if (!tempStats.silent) {
                GD.Print("alarmed, but didn't have manager");
                GD.Print("go to PlayerWeapons.cs - 530 to fix");
            }

            onetimeShoot = false;
        }
    }

    private void checkInputKey(int key, WeaponTypes type) 
    {
        if (Input.IsKeyPressed(key)) {
            if (TempWeapon != type && weaponStats[type].have) {
                changeGun(type);
            }
        }
    }

    public void Update(float delta) {
        if (!global.paused && player.Health > 0) 
        {
            if (TempWeapon != WeaponTypes.None) {
                if (GunOn) {
                    //вращаем коллизию пистолета вместе с пистолетом
                    if (IsNotRifle) {
                        gunShape.RotationDegrees = player.RotationHelper.RotationDegrees;
                    } //иначе вращаем область дробовика за камерой
                    else {
                        Vector3 newRot = shotgunArea.RotationDegrees;
                        newRot.x = player.RotationHelper.RotationDegrees.x;
                        shotgunArea.RotationDegrees = newRot;
                    }

                    if (Input.IsMouseButtonPressed(1) && cooldown <=0) {
                        if (!onetimeShoot) {
                            handleShoot();
                        }
                    }
                }

                //обработка доставания оружия
                if (weaponStats[TempWeapon].have) {
                    if (IsNotRifle) {
                       // GD.Print(player.RotationHelper.RotationDegrees.x);
                        if (player.Body.bodyRot > 80 &&
                            player.Body.bodyRot < 130 &&
                            player.RotationHelper.RotationDegrees.x < -40) {
                                var actions = InputMap.GetActionList("getGun");
                                var keyAction = actions[0] as InputEventKey;
                                var key = OS.GetScancodeString(keyAction.Scancode);

                                if (GunOn) {
                                    label.Text = key + InterfaceLang.GetLang("inGame","cameraHints","putGun");
                                } else {
                                    label.Text = key + InterfaceLang.GetLang("inGame","cameraHints","takeGun");
                                }

                                askGet.Visible = true;
                                onetimeVisible = true;
                                if (Input.IsActionJustPressed("getGun")) {
                                    setGunOn(!GunOn);

                                    activateGunOnModel(GunOn);
                                    if (IsNotRifle) {
                                        string tempWeapon = WeaponType.ToString(TempWeapon);
                                        weaponModels[tempWeapon + "_off"].Visible = !GunOn;
                                    }

                                    if (!GunOn) {
                                        audi.Stream = sounds["GunOff"];
                                    } else {
                                        audi.Stream = sounds["GunOn"];
                                    }
                                    audi.Play();
                                }
                            } else {
                                if (onetimeVisible) {
                                    onetimeVisible = false;
                                    askGet.Visible = false;
                                }
                            }
                    }
                }
            }

            if (cooldown > 0) {
                cooldown -= delta;
            }

            //обработка смены оружия (проходим циклом по всем типам)
            int typeI = 0;
            foreach(WeaponTypes tempType in Enum.GetValues(typeof(WeaponTypes))) {
                int key = ZERO_NUM_KEY + typeI;
                checkInputKey(key, tempType);
                typeI++;
            }

            if (changeWeaponCooldown > 0) {
                changeWeaponCooldown -= delta;
            }
        }
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

public static class WeaponType {
    public static string ToString(WeaponTypes type) {
        switch(type) {
            case WeaponTypes.Pistol:
                return "pistol";
            case WeaponTypes.Shotgun:
                return "shotgun";
            case WeaponTypes.Revolver:
                return "revolver";
            case WeaponTypes.Sniper:
                return "sniper";
        }
        GD.PrintErr("someone tried to get none type name");
        return "";
    }

    public static WeaponTypes ToType(int number) {
        switch (number) {
            case 0:
                return WeaponTypes.Pistol;
            case 1:
                return WeaponTypes.Shotgun;
            case 2:
                return WeaponTypes.Revolver;
            case 3:
                return WeaponTypes.Sniper;
            default:
                return WeaponTypes.None;
        }
    }
}

public struct WeaponStats
{
    public bool have;
    public bool silent;
    public int ammo;
    public int ammoMax;
    public int damage;
    public int distance;
    public float cooldown;
    public float recoil;
    public int icon;
}