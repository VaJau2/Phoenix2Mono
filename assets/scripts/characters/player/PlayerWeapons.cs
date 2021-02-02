using Godot;
using Godot.Collections;
using System.Globalization;

public class PlayerWeapons: CollisionShape
{
    Global global;
    const float SHAKE_SPEED = 2f;
    const float SHAKE_DIFF = 0.5f;
    const int ZERO_NUM_KEY = 49;

    public bool GunOn;
    public bool isPistol;

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
    Control shootInterface;

    public Label ammoLabel;
    TextureRect ammoIcon;

    Dictionary tempWeaponStats;

    //--эффекты и анимания выстрела

    Spatial tempWeapon;
    public ItemIcon tempAmmoButton {get; private set;}
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
    AudioStreamSample tryShootSound;
    AudioStreamSample shootSound;

    Node enemiesManager; //TODO - изменить на класс enemiesManager

    bool onetimeShoot = false;

    public void LoadNewWeapon(string weaponCode, Dictionary weaponData)
    {
        //грузим префаб оружия
        string path = "res://objects/guns/prefabs/" + weaponCode + ".tscn";
        PackedScene weaponPrefab = GD.Load<PackedScene>(path);
        //грузим статистику оружия
        tempWeaponStats = weaponData;
        isPistol = weaponData.Contains("isPistol");
        shootSound = GD.Load<AudioStreamSample>("res://assets/audio/guns/shoot/" + weaponCode + ".wav");

        //вытаскиваем родительский нод из игрока
        Spatial tempParent = player.GetWeaponParent(isPistol);
        //отправляем префаб туда
        var weapon = weaponPrefab.Instance();
        tempParent.AddChild(weapon);
        //сохраняем его для будущих перемещений по нодам
        tempWeapon = (Spatial)weapon;
        LoadNewAmmo();
        LoadGunEffects();
        
        shootInterface.Visible = true;
        player.SetWeaponOn(isPistol);
        GunOn = true;
    }
    
    public void ClearWeapon()
    {
        tempWeapon.QueueFree();
        tempWeapon = null;
        shootInterface.Visible = false;
        player.SetWeaponOff();
        GunOn = false;
    }

    public void СheckThirdView() 
    {
        //если сменился вид, моделька перемещается в новый нод
        if(GunOn && isPistol) {
            Spatial oldParent = (Spatial)tempWeapon.GetParent();
            Spatial newParent = player.GetWeaponParent(isPistol);
            
            if (oldParent != newParent) {
                oldParent.RemoveChild(tempWeapon);
                newParent.AddChild(tempWeapon);
            }
        }
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
        shootInterface = GetNode<Control>("/root/Main/Scene/canvas/shootInterface");
        ammoIcon = shootInterface.GetNode<TextureRect>("ammoBack/icon");
        ammoLabel = shootInterface.GetNode<Label>("ammoBack/label");

        gunParticlesPrefab = GD.Load<PackedScene>("res://objects/guns/gunParticles.tscn");
        particlesParent = GetNode("/root/Main/Scene");

        audi = GetNode<AudioStreamPlayer>("../sound/audi_weapons");
        audiShoot = GetNode<AudioStreamPlayer>("../sound/audi_shoot");
        tryShootSound = GD.Load<AudioStreamSample>("res://assets/audio/guns/TryShoot.wav");

        //enemiesManager = player.GetNode("/root/Main/Scene/enemies");
    }

    private void SetAmmoIcon(string ammoType)
    {
        Dictionary itemData = ItemJSON.GetItemData(ammoType);
        string path = "res://assets/textures/interface/icons/items/" + itemData["icon"] + ".png";
        StreamTexture newIcon = GD.Load<StreamTexture>(path);
        ammoIcon.Texture = newIcon;
    }

    private int GetAmmo() => (tempAmmoButton != null) ? tempAmmoButton.GetCount() : 0;

    private void SetAmmo(int newAmmo) 
    {
        tempAmmoButton.SetCount(newAmmo);
        if (newAmmo == 0) {
            tempAmmoButton = null;
        } 
        ammoLabel.Text = newAmmo.ToString();
    }

    public void UpdateAmmoCount()
    {
        ammoLabel.Text = GetAmmo().ToString();
    }

    public bool isTempAmmo(string ammoType)
    {
        if (tempWeaponStats != null) {
            if (tempWeaponStats.Contains("ammoType") 
            && tempWeaponStats["ammoType"].ToString() == ammoType) {
                return true;
            }
        }
        return false;
    }

    public void LoadNewAmmo()
    {
        string ammoType = tempWeaponStats["ammoType"].ToString();
        if (player.inventory.ammoButtons.ContainsKey(ammoType)) {
            tempAmmoButton = player.inventory.ammoButtons[ammoType];
        } else {
            tempAmmoButton = null;
        }
        
        SetAmmoIcon(ammoType);
        UpdateAmmoCount();
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

    private Vector3 SetRotX(Vector3 origin, float newRotX) 
    {
        origin.x = newRotX;
        return origin;
    }

    private async void shakeCameraUp() {
        bool shakingProcess = true;
        while(shakingProcess) {
            if (shakeUp) {
                float recoil = player.BaseRecoil + GetStatsFloat("recoil");
                if (tempShake < recoil) {
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

    private async void СheckVisible(Array<Spatial> victims) 
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

    public int GetStatsInt(string statsName) => int.Parse(tempWeaponStats[statsName].ToString());
    public float GetStatsFloat(string statsName) => float.Parse(tempWeaponStats[statsName].ToString(), CultureInfo.InvariantCulture);

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
        GD.Print("add in weapons.cs in 262 enemy's impulse pls");
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
            player.MakeDamage(character, shapeID);
        } else if (victim is StaticBody) {
            var body = victim as StaticBody;
            name = MatNames.GetMatName(body.PhysicsMaterialOverride.Friction);
            if (victim is BreakableObject) {
                var obj = victim as BreakableObject;
                obj.Brake(player.GetDamage());
            }
        }

        return name;
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
        int ammo = GetAmmo();

        if (ammo == 0) {
            audiShoot.Stream = tryShootSound;
            audiShoot.Play();
            onetimeShoot = false;
        } else {
            ammo -= 1;
            SetAmmo(ammo);

            cooldown = GetStatsFloat("cooldown");
            audiShoot.Stream = shootSound;
            audiShoot.Play();
            if (gunAnim != null) {
                gunAnim.Play("shoot");
            }

            player.Body.Head.CloseEyes();
            var tempDistance = GetStatsInt("shootDistance");
            Dictionary armorProps = player.inventory.GetArmorProps();
            if (armorProps.Contains("shootDistPlus")) {
                tempDistance += int.Parse(armorProps["shootDistPlus"].ToString());
            }
            var tempRay = EnableHeadRay(tempDistance);

            await player.ToSignal(player.GetTree(),"idle_frame");

            //обрабатываем попадания
            if (isPistol || player.MayMove) {
                if (tempWeaponStats.Contains("isShotgun")) {
                    player.impulse = player.RotationHelper.GlobalTransform.basis.z / 2;
                    var objs = shotgunArea.objectsInside;
                    СheckVisible(objs);
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

            await global.ToTimer(0.05f);

            gunLight.Visible = true;
            gunSmoke.Restart();
            gunFire.Visible = true;
            shakeCameraUp();
            await global.ToTimer(0.05f);
            gunFire.Visible = false;
            gunLight.Visible = false;

            tempRay.Enabled = false;

            if (!tempWeaponStats.Contains("isSilence")) {
                GD.Print("alarmed, but didn't have manager");
                GD.Print("go to PlayerWeapons.cs - 372 to fix");
            }

            onetimeShoot = false;
        }
    }

    public override void _Process(float delta)
    {
        if (!global.paused && player.Health > 0 && Input.GetMouseMode() == Input.MouseMode.Captured) 
        {
            if (GunOn) {
                //вращаем коллизию пистолета вместе с пистолетом
                if (isPistol) {
                    RotationDegrees = player.RotationHelper.RotationDegrees;
                } //иначе вращаем область дробовика за камерой
                else {
                    Vector3 newRot = shotgunArea.RotationDegrees;
                    newRot.x = player.RotationHelper.RotationDegrees.x;
                    shotgunArea.RotationDegrees = newRot;
                }

                if (Input.IsMouseButtonPressed(1) && cooldown <= 0) {
                    if (!onetimeShoot) {
                        handleShoot();
                    }
                }
            }

            if (cooldown > 0) {
                cooldown -= delta;
            }

            if (changeWeaponCooldown > 0) {
                changeWeaponCooldown -= delta;
            }
        }
    }
}
