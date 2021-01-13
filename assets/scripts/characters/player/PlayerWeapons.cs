using Godot;
using System.Collections.Generic;
using System;

public class PlayerWeapons: CollisionShape
{
    Global global;
    const float SHAKE_SPEED = 4;
    const float SHAKE_DIFF = 0.5f;
    const int ZERO_NUM_KEY = 49;

    //--статистика------
    public bool GunOn = false;
    public WeaponTypes TempWeaponType = WeaponTypes.None;

    public Dictionary<WeaponTypes, WeaponStats> weaponStats;

    Player player {get => GetParent<Player>();}
    //внутри player:
    //body
    //camera
    //head -> body.head

    RayCast rayFirst;
    RayCast rayThird;
    RayCast rayShotgun;
    ShotgunArea shotgunArea;

    //---интерфейс-------
    Control askGet;
    Label label;
    Control shootInterface;

    public Label ammoLabel;
    Sprite ammoIcon;
    GunIcons weaponIcons;


    //--модельки-------
    Dictionary<string, Spatial> weaponParents;
    Dictionary<string, Transform> weaponPositions = new Dictionary<string, Transform>();
    Dictionary<string, PackedScene> weaponPrefabs = new Dictionary<string, PackedScene>();
    Dictionary<string, PackedScene> weaponMirrorPrefabs = new Dictionary<string, PackedScene>();

    //--эффекты и анимания выстрела

    Spatial tempWeapon;
    Spatial tempMirrorWeapon;
    string tempWeaponKey = "";
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
    bool bagEnabled = true; //для будущего прятания сумки
    

    private void loadModels() 
    {
        //вытаскиваем все модельки оружия
        string head1Path = "../rotation_helper/camera/weapons/";
        string bagPath = "../player_body/Armature/Skeleton/BoneAttachment 2/shotgunBag/";
        var weaponModels = new Dictionary<string, Spatial>()
        {
            {"pistol_on", GetNode<Spatial>(head1Path + "pistol")},
            {"pistol_off", GetNode<Spatial>(bagPath + "pistol")},

            {"shotgun_on", GetNode<Spatial>(bagPath + "shotgun")},

            {"revolver_on", GetNode<Spatial>(head1Path + "revolver")},
            {"revolver_off", GetNode<Spatial>(bagPath + "revolver")},

            {"sniper_on", GetNode<Spatial>(bagPath + "rifle")},
        };

        string head3Path = "../player_body/Armature/Skeleton/BoneAttachment/weapons/";
        var weaponThirdModels = new Dictionary<string, Spatial>() 
        {
            {"pistol_on", GetNode<Spatial>(head3Path + "pistol")},
            {"revolver_on", GetNode<Spatial>(head3Path + "revolver")}
        };

        //грузим parent-объекты для спавнящегося оружия
        weaponParents = new Dictionary<string, Spatial>()
        {
            {"head1", GetNode<Spatial>(head1Path)},
            {"head3", GetNode<Spatial>(head3Path)},
            {"bag", GetNode<Spatial>(bagPath)},
        };

        //запоминаем их локальную позицию и удаляем их
        foreach(string weaponName in weaponModels.Keys) {
            weaponPositions.Add(weaponName, weaponModels[weaponName].Transform);
            weaponModels[weaponName].QueueFree();
        }

        foreach(string weaponName in weaponThirdModels.Keys) {
            weaponPositions.Add("third_" + weaponName, weaponThirdModels[weaponName].Transform);
            weaponThirdModels[weaponName].QueueFree();
        }

        //загружаем префабы моделек
        string prefabPath = "res://objects/guns/";
        weaponPrefabs = new Dictionary<string, PackedScene>()
        {
            {"pistol", GD.Load<PackedScene>(prefabPath + "pistol.tscn")},
            {"revolver", GD.Load<PackedScene>(prefabPath + "revolver.tscn")},
            {"sniper", GD.Load<PackedScene>(prefabPath + "rifle.tscn")},
            {"shotgun", GD.Load<PackedScene>(prefabPath + "shotgun.tscn")}
        };
        
        weaponMirrorPrefabs = new Dictionary<string, PackedScene>()
        {
            {"pistol", GD.Load<PackedScene>(prefabPath + "pistolMirror.tscn")},
            {"revolver", GD.Load<PackedScene>(prefabPath + "revolverMirror.tscn")},
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
        pistolStats.iconSize = 40;

        WeaponStats shotgunStats = new WeaponStats();
        shotgunStats.ammo = 5;
        shotgunStats.ammoMax = 60;
        shotgunStats.damage = 110;
        shotgunStats.distance = 40;
        shotgunStats.cooldown = 2f;
        shotgunStats.recoil = 2.5f;
        shotgunStats.iconSize = 80;

        WeaponStats revolverStats = new WeaponStats();
        revolverStats.ammo = 20;
        revolverStats.ammoMax = 100;
        revolverStats.damage = 20;
        revolverStats.distance = 80;
        revolverStats.cooldown = 0.5f;
        revolverStats.recoil = 3.0f;
        revolverStats.iconSize = 40;

        WeaponStats sniperStats = new WeaponStats();
        sniperStats.ammo = 5;
        sniperStats.ammoMax = 15;
        sniperStats.damage = 70;
        sniperStats.distance = 200;
        sniperStats.cooldown = 1.6f;
        sniperStats.recoil = 1.5f;
        sniperStats.iconSize = 120;

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

    public override void _Ready()
    {
        global = Global.Get();

        rayFirst = GetNode<RayCast>("../rotation_helper/camera/ray");
        rayThird = GetNode<RayCast>("../rotation_helper_third/camera/ray");
        rayFirst.AddException(player);
        rayThird.AddException(player);

        rayShotgun = GetNode<RayCast>("../player_body/shotgunRay");
        shotgunArea = GetNode<ShotgunArea>("../player_body/shotgunArea");
        askGet = GetNode<Control>("/root/Main/Scene/canvas/openBack");
        label = askGet.GetNode<Label>("label");
        shootInterface = GetNode<Control>("/root/Main/Scene/canvas/shootInterface");
        ammoIcon = shootInterface.GetNode<Sprite>("ammoBack/Sprite2");
        ammoLabel = shootInterface.GetNode<Label>("ammoBack/label");
        weaponIcons = shootInterface.GetNode<GunIcons>("gunIcons");

        gunParticlesPrefab = GD.Load<PackedScene>("res://objects/guns/gunParticles.tscn");
        particlesParent = GetNode("/root/Main/Scene");

        audi = GetNode<AudioStreamPlayer>("../sound/audi_weapons");
        audiShoot = GetNode<AudioStreamPlayer>("../sound/audi_shoot");

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
            changeGun(player.StartWeapons[0], true);
        }
    }

    private void loadGunEffects() 
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

    private void setWeaponModel(WeaponTypes newType) 
    {
        if (TempWeaponType == WeaponTypes.None && 
            newType == WeaponTypes.None) {
            return;
        }

        //проверяем, не включена ли уже эта моделька
        var key = "";
        bool isPistol = IsNotRifle;
        bool loadMirror = false;
        Spatial parent = null;

        if (GunOn && isPistol) {
            if (player.ThirdView) {
                key += "third_";
                parent = weaponParents["head3"];
            } else {
                parent = weaponParents["head1"];
                loadMirror = true;
            }
        } else {
            parent = weaponParents["bag"];
        }
        
        string weaponName = WeaponType.ToString(newType);
        key += weaponName;

        if (!GunOn && isPistol) {
            key += "_off";
        } else {
            key += "_on";
        }
        
        if (tempWeaponKey != key) {
            //удаляем предыдущую модельку
            if (tempWeapon != null) {
                tempWeapon.QueueFree();
            }
            if (tempMirrorWeapon != null) {
                tempMirrorWeapon.QueueFree();
            }

            if (newType == WeaponTypes.None) {
                tempWeapon = null;
                return;
            }

            //спавним новую модельку, ставим на её позицию
            var newWeapon = (Spatial)weaponPrefabs[weaponName].Instance();
            parent.AddChild(newWeapon);
            newWeapon.Transform = weaponPositions[key];

            if (loadMirror) {
                var newMirrorWeapon = (Spatial)weaponMirrorPrefabs[weaponName].Instance();
                weaponParents["head3"].AddChild(newMirrorWeapon);
                newMirrorWeapon.Transform = weaponPositions["third_" + weaponName + "_on"];
                tempMirrorWeapon = newMirrorWeapon;
            }
            
            //запоминаем её
            tempWeapon = newWeapon;
            tempWeaponKey = key;
            loadGunEffects();
        }
    }

    public bool IsNotRifle {
        get {
            return (TempWeaponType == WeaponTypes.Pistol ||
                    TempWeaponType == WeaponTypes.Revolver ||
                    TempWeaponType == WeaponTypes.None);
        }
    }

    private Vector3 SetRotX(Vector3 origin, float newRotX) 
    {
        origin.x = newRotX;
        return origin;
    }

    private void setGunOn(bool newGunOn) {
        GunOn = newGunOn;
        var isPistol = IsNotRifle;

        if (isPistol) {
            Disabled = !newGunOn;
            RotationDegrees = Vector3.Zero;
        } else {
            Disabled = true;
        }
        shootInterface.Visible = newGunOn;

        if (GunOn) {
            if (isPistol) {
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
                if (tempShake < weaponStats[TempWeaponType].recoil) {
                    tempShake += SHAKE_SPEED;
                    Camera camera = player.GetViewport().GetCamera();
                    camera.RotationDegrees = SetRotX(
                        camera.RotationDegrees,
                        camera.RotationDegrees.x + SHAKE_SPEED
                    );
                } else {
                    shakeUp = false;
                }
            } else {
                if (tempShake > 0) {
                    var diff = SHAKE_SPEED * SHAKE_DIFF;
                    tempShake -= diff;
                    Camera camera = player.GetViewport().GetCamera();
                    camera.RotationDegrees = SetRotX(
                        camera.RotationDegrees,
                        camera.RotationDegrees.x - SHAKE_SPEED * SHAKE_DIFF
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
            var wr = WeakRef(victim);
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
                    if (victim is Character) {
                        var chr = victim as Character;
                        spawnBlood(chr, player.impulse * -4, dir);
                    }
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
            player.MakeDamage(character, weaponStats[TempWeaponType].damage, shapeID);
        } else if (victim is StaticBody) {
            var body = victim as StaticBody;
            name = MatNames.GetMatName(body.PhysicsMaterialOverride.Friction);
            if (victim is BreakableObject) {
                var obj = victim as BreakableObject;
                obj.Brake(weaponStats[TempWeaponType].damage);
            }
        }

        return name;
    }  

    public void checkThirdView() 
    {
        //если сменился вид, новая моделька подключится вместо старой
        setWeaponModel(TempWeaponType);
    }

    private void ChangeBagVisible(bool visible)
    {
        bagEnabled = visible;
        var bug = player.GetNode<Spatial>("player_body/Armature/Skeleton/BoneAttachment 2/shotgunBag");
        bug.Visible = visible;
    }

    public void changeGun(WeaponTypes newType, bool atStart = false) 
    {
        //если у игрока до этого не было оружия
        if (bagEnabled) {
            ChangeBagVisible(true);
        }

        //меняем тип оружия и модельку
        TempWeaponType = newType;

        if (!atStart) {
            setGunOn(true);
        }

        setWeaponModel(TempWeaponType);

        shotgunArea.Monitoring = (newType == WeaponTypes.Shotgun);
        
        ammoLabel.Text = weaponStats[newType].ammo.ToString();
        ammoIcon.RegionRect = new Rect2(weaponStats[newType].iconSize, 0, 40, 40);

        if (!atStart) {
            audi.Stream = sounds["GunOn"];
            audi.Play();

            weaponIcons.ChangeWeapon(this, newType);
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

    private async void handleShoot() {
        onetimeShoot = true;
        WeaponStats tempStats = weaponStats[TempWeaponType];

        if (tempStats.ammo == 0) {
            audiShoot.Stream = sounds["TryShoot"];
            audiShoot.Play();
            onetimeShoot = false;
        } else {
            tempStats.ammo -= 1;
            ammoLabel.Text = tempStats.ammo.ToString();
            cooldown = tempStats.cooldown;
            var tempWeapon = WeaponType.ToString(TempWeaponType);
            audiShoot.Stream = sounds[tempWeapon + "_shoot"];
            audiShoot.Play();
            if (gunAnim != null) {
                gunAnim.Play("shoot");
            }

            //player.Body.Head.CloseEyes();
            var tempDistance = tempStats.distance;
            if (player.clothCode == "stealth_armor") {
                tempDistance += 15;
            }
            var tempRay = EnableHeadRay(tempDistance);

            await player.ToSignal(player.GetTree(),"idle_frame");

            //обрабатываем попадания
            if (IsNotRifle || player.MayMove) {
                if (TempWeaponType == WeaponTypes.Shotgun) {
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
                GD.Print("go to PlayerWeapons.cs - 574 to fix");
            }

            onetimeShoot = false;
        }
    }

    private void checkInputKey(int key, WeaponTypes type) 
    {
        if (Input.IsKeyPressed(key)) {
            if (TempWeaponType != type && weaponStats[type].have) {
                changeGun(type);
            }
        }
    }

    public override void _Process(float delta)
    {
        if (!global.paused && player.Health > 0) 
        {
            if (TempWeaponType != WeaponTypes.None) {
                if (GunOn) {
                    //вращаем коллизию пистолета вместе с пистолетом
                    if (IsNotRifle) {
                        RotationDegrees = player.RotationHelper.RotationDegrees;
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
                if (weaponStats[TempWeaponType].have) {
                    if (IsNotRifle) {
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
                                    setWeaponModel(TempWeaponType);

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
    public int iconSize;
}