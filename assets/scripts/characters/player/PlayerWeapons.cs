using Godot;
using Godot.Collections;

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

    //---интерфейс-------
    Control shootInterface;

    public Label ammoLabel;
    TextureRect ammoIcon;
    TextureRect crossHitted;

    Dictionary tempWeaponStats;

    //--эффекты и анимания выстрела

    public ItemIcon tempAmmoButton {get; private set;}
    Spatial tempWeapon;
    AnimationPlayer gunAnim;
    Spatial gunLight;
    Spatial gunFire;
    Particles gunSmoke;
    PackedScene gunParticlesPrefab;
    Node particlesParent;
    WeaponShellSpawner shellSpawner;

    float tempShake = 0;
    bool shakeUp = true;
    float cooldown = 0.6f;

    //--звуки-------------
    AudioStreamPlayer audiShoot;
    AudioStreamSample tryShootSound;
    AudioStreamSample shootSound;

    EnemiesManager enemiesManager;

    bool onetimeShoot = false;

    public void LoadNewWeapon(string weaponCode, Dictionary weaponData)
    {
        //грузим префаб оружия
        string path = "res://objects/guns/prefabs/" + weaponCode + ".tscn";
        PackedScene weaponPrefab = GD.Load<PackedScene>(path);

        //грузим статистику оружия
        tempWeaponStats = weaponData;
        isPistol = weaponData.Contains("isPistol");
        Disabled = !isPistol || global.playerRace == Race.Unicorn;
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
        if (!IsInstanceValid(tempWeapon)) return;
        cooldown = 0;
        Disabled = true;
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

        shootInterface = GetNode<Control>("/root/Main/Scene/canvas/shootInterface");
        ammoIcon = shootInterface.GetNode<TextureRect>("ammoBack/icon");
        ammoLabel = shootInterface.GetNode<Label>("ammoBack/label");
        crossHitted = shootInterface.GetNode<TextureRect>("cross/hitted");

        gunParticlesPrefab = GD.Load<PackedScene>("res://objects/guns/gunParticles.tscn");
        particlesParent = GetNode("/root/Main/Scene");

        audiShoot = GetNode<AudioStreamPlayer>("../sound/audi_shoot");
        tryShootSound = GD.Load<AudioStreamSample>("res://assets/audio/guns/TryShoot.wav");

        enemiesManager = player.GetNode<EnemiesManager>("/root/Main/Scene/npc");
    }

    public async void ShowCrossHitted(bool head)
    {
        crossHitted.Modulate = head? Colors.Red : Colors.White;
        if (crossHitted.Visible) {
            return;
        }
        crossHitted.Visible = true;
        await global.ToTimer(0.2f);
        crossHitted.Visible = false;
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
        shellSpawner = tempWeapon.GetNodeOrNull<WeaponShellSpawner>("shells");
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

    public int GetStatsInt(string statsName) => int.Parse(tempWeaponStats[statsName].ToString());
    public float GetStatsFloat(string statsName) => Global.ParseFloat(tempWeaponStats[statsName].ToString());

    private string handleVictim(Spatial victim, int shapeID = 0)
    {
        string name = null;
        switch (victim)
        {
            case Character character:
            {
                if (character.Name.Contains("target") ||
                    character.Name.Contains("roboEye") ||
                    character.Name.Contains("MrHandy")) {
                    name = "black";
                } else {
                    name = "blood";
                }

                character.CheckShotgunShot(tempWeaponStats.Contains("isShotgun"));
                player.MakeDamage(character, shapeID);
                ShowCrossHitted(shapeID != 0);
                break;
            }
            case StaticBody body:
            {
                if (body.PhysicsMaterialOverride != null) {
                    name = MatNames.GetMatName(body.PhysicsMaterialOverride.Friction);
                    
                    if (body is BreakableObject obj) {
                        obj.Brake(player.GetDamage());
                    }
                }

                break;
            }
        }
        
        return name;
    }  

    private void SpawnBullet()
    {
        string bullet = tempWeaponStats["bullet"].ToString();
        var bulletPrefab = GD.Load<PackedScene>("res://objects/guns/bullets/" + bullet + ".tscn");
        Bullet newBullet = (Bullet)bulletPrefab.Instance();
        newBullet.Damage = player.GetDamage();
        newBullet.Shooter = player;
        newBullet.Timer = GetStatsFloat("shootDistance");
        
        GetNode("/root/Main/Scene").AddChild(newBullet);
        newBullet.GlobalTransform = gunFire.GlobalTransform;
    }

    private void SetGunEffects(bool on)
    {
        if (!IsInstanceValid(gunLight)
            || !IsInstanceValid(gunFire)
            || !IsInstanceValid(gunSmoke))
        {
            return;
        }
        
        gunLight.Visible = on;
        gunFire.Visible = on;
        if (on)
        {
            gunSmoke.Restart();
            shellSpawner?.StartSpawning();
        }
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

            var tempRay = player.Camera.UseRay(tempDistance);

            await global.ToTimer(0.05f);

            //обрабатываем попадания
            if (isPistol || player.MayMove) {
                if (tempWeaponStats.Contains("bullet")) {
                    SpawnBullet();
                } else {
                    if (tempWeaponStats.Contains("isShotgun")) {
                        player.impulse = player.RotationHelper.GlobalTransform.basis.z / 2;
                    } 

                    tempRay.ForceRaycastUpdate();
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
                        gunParticles.Call(
                            "_startEmitting", 
                            tempRay.GetCollisionNormal(), 
                            matName
                        );
                    }
                }
            }
            player.Camera.ReturnRayBack();

            SetGunEffects(true);
            shakeCameraUp();
            await global.ToTimer(0.06f);
            SetGunEffects(false);

            if (!tempWeaponStats.Contains("isSilence")) {
                enemiesManager.LoudShoot(GetStatsInt("shootDistance") * 0.8f, player.GlobalTransform.origin);
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
        }
    }
}
