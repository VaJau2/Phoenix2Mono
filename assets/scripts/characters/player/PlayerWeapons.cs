using Godot;
using Godot.Collections;

public class PlayerWeapons : CollisionShape
{
    Global global;
    const float SHAKE_SPEED = 2f;
    const float SHAKE_DIFF = 0.5f;
    const int ZERO_NUM_KEY = 49;

    public bool GunOn;
    public bool isPistol;

    Player player
    {
        get => GetParent<Player>();
    }
    //внутри player:
    //body
    //camera
    //head -> body.head

    //---интерфейс-------
    InteractionPointManager point;
    Control shootInterface;

    public Label ammoLabel;
    IconWithShadow ammoIcon;
    TextureRect crossHitted;

    Dictionary tempWeaponStats;

    //--эффекты и анимания выстрела
    public ItemIcon TempAmmoButton { get; private set; }
    public Spatial TempWeapon { get; private set; }
    
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
        TempWeapon = (Spatial)weapon;
        LoadNewAmmo();
        LoadGunEffects();

        shootInterface.Visible = true;
        point.SetInteractionVariant(InteractionVariant.Cross);
        player.SetWeaponOn(isPistol);
        GunOn = true;
    }

    // Просто очищает модельку оружия
    // использовать его как метод для снятия всего оружия нельзя
    public void ClearWeapon()
    {
        if (!IsInstanceValid(TempWeapon)) return;
        cooldown = 0;
        Disabled = true;
        TempWeapon.QueueFree();
        TempWeapon = null;
        shootInterface.Visible = false;
        point.SetInteractionVariant(InteractionVariant.Point);
        player.SetWeaponOff();
        GunOn = false;
        
        player.EmitSignal(nameof(Player.ClearWeaponBindSignal));
    }

    public void СheckThirdView()
    {
        //если сменился вид, моделька перемещается в новый нод
        if (GunOn && isPistol)
        {
            Spatial oldParent = (Spatial)TempWeapon.GetParent();
            Spatial newParent = player.GetWeaponParent(isPistol);

            if (oldParent != newParent)
            {
                oldParent.RemoveChild(TempWeapon);
                newParent.AddChild(TempWeapon);
            }
        }
    }

    public override void _Ready()
    {
        global = Global.Get();

        point = GetNode<InteractionPointManager>("/root/Main/Scene/canvas/pointManager");
        shootInterface = GetNode<Control>("/root/Main/Scene/canvas/shootInterface");
        ammoIcon = shootInterface.GetNode<IconWithShadow>("ammoBack/icon");
        ammoLabel = shootInterface.GetNode<Label>("ammoBack/label");
        crossHitted = shootInterface.GetNode<TextureRect>("hitted");

        gunParticlesPrefab = GD.Load<PackedScene>("res://objects/guns/gunParticles.tscn");
        particlesParent = GetNode("/root/Main/Scene");

        audiShoot = GetNode<AudioStreamPlayer>("../sound/audi_shoot");
        tryShootSound = GD.Load<AudioStreamSample>("res://assets/audio/guns/TryShoot.wav");

        enemiesManager = player.GetNode<EnemiesManager>("/root/Main/Scene/npc");
    }

    public async void ShowCrossHitted(bool head)
    {
        crossHitted.Modulate = head ? Colors.Red : Colors.White;
        if (crossHitted.Visible)
        {
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
        ammoIcon.SetTexture(newIcon);
    }

    private int GetAmmo() => TempAmmoButton?.GetCount() ?? 0;

    private void SetAmmo(int newAmmo)
    {
        TempAmmoButton.SetCount(newAmmo);
        if (newAmmo == 0)
        {
            TempAmmoButton = null;
        }

        ammoLabel.Text = newAmmo.ToString();
    }

    public void UpdateAmmoCount()
    {
        ammoLabel.Text = GetAmmo().ToString();
    }

    public bool IsTempAmmo(string ammoType)
    {
        if (tempWeaponStats != null)
        {
            if (tempWeaponStats.Contains("ammoType") && tempWeaponStats["ammoType"].ToString() == ammoType)
            {
                return true;
            }
        }

        return false;
    }

    public void LoadNewAmmo()
    {
        string ammoType = tempWeaponStats["ammoType"].ToString();
        if (player.Inventory.ammoButtons.ContainsKey(ammoType))
        {
            TempAmmoButton = player.Inventory.ammoButtons[ammoType];
        }
        else
        {
            TempAmmoButton = null;
        }

        SetAmmoIcon(ammoType);
        UpdateAmmoCount();
    }

    private void LoadGunEffects()
    {
        if (TempWeapon.HasNode("anim"))
        {
            gunAnim = TempWeapon.GetNode<AnimationPlayer>("anim");
        }
        else
        {
            gunAnim = null;
        }

        gunLight = TempWeapon.GetNode<Spatial>("light");
        gunFire = TempWeapon.GetNode<Spatial>("fire");
        gunSmoke = TempWeapon.GetNode<Particles>("smoke");
        shellSpawner = TempWeapon.GetNodeOrNull<WeaponShellSpawner>("shells");
    }

    private Vector3 SetRotX(Vector3 origin, float newRotX)
    {
        origin.x = newRotX;
        return origin;
    }

    private async void ShakeCameraUp()
    {
        bool shakingProcess = true;
        while (shakingProcess)
        {
            if (shakeUp)
            {
                float recoil = player.BaseRecoil + GetStatsFloat("recoil");
                if (tempShake < recoil)
                {
                    tempShake += SHAKE_SPEED;
                    Camera camera = player.GetViewport().GetCamera();
                    camera.RotationDegrees = SetRotX(
                        camera.RotationDegrees,
                        camera.RotationDegrees.x + SHAKE_SPEED
                    );
                }
                else
                {
                    shakeUp = false;
                }
            }
            else
            {
                if (tempShake > 0)
                {
                    var diff = SHAKE_SPEED * SHAKE_DIFF;
                    tempShake -= diff;
                    Camera camera = player.GetViewport().GetCamera();
                    camera.RotationDegrees = SetRotX(
                        camera.RotationDegrees,
                        camera.RotationDegrees.x - SHAKE_SPEED * SHAKE_DIFF
                    );
                }
                else
                {
                    shakeUp = true;
                    shakingProcess = false;
                }
            }

            await player.ToSignal(player.GetTree(), "idle_frame");
        }
    }

    public int GetStatsInt(string statsName) => int.Parse(tempWeaponStats[statsName].ToString());
    public float GetStatsFloat(string statsName) => Global.ParseFloat(tempWeaponStats[statsName].ToString());

    private string HandleVictim(Spatial victim, int shapeID = 0)
    {
        string name = null;
        switch (victim)
        {
            case Character character:
            {
                if (character.Name.Contains("target") ||
                    character.Name.Contains("roboEye") ||
                    character.Name.Contains("MrHandy"))
                {
                    name = "black";
                }
                else
                {
                    name = "blood";
                }

                character.CheckShotgunShot(tempWeaponStats.Contains("isShotgun"));
                player.MakeDamage(character, shapeID);
                ShowCrossHitted(shapeID != 0);
                break;
            }
            case StaticBody body:
            {
                if (body.PhysicsMaterialOverride != null)
                {
                    name = MatNames.GetMatName(body.PhysicsMaterialOverride.Friction);

                    if (body is BreakableObject obj)
                    {
                        obj.Brake(player.GetDamage());
                    }
                }

                break;
            }
        }

        return name;
    }
    
    private void SpawnBullet() => SpawnBullet(Vector3.Zero);

    private void SpawnBullet(Vector3 target)
    {
        string bullet = tempWeaponStats["bullet"].ToString();
        var bulletPrefab = GD.Load<PackedScene>("res://objects/guns/bullets/" + bullet + ".tscn");
        Bullet newBullet = (Bullet)bulletPrefab.Instance();
        newBullet.Damage = player.GetDamage();
        newBullet.Shooter = player;
        newBullet.Timer = GetStatsFloat("shootDistance");

        GetNode("/root/Main/Scene").AddChild(newBullet);
        newBullet.GlobalTransform = gunFire.GlobalTransform;

        if (target != Vector3.Zero)
        {
            newBullet.LookAt(target, Vector3.Forward);
        }
    }

    private void SetGunEffects(bool on)
    {
        if (!IsInstanceValid(gunLight) || !IsInstanceValid(gunFire) || !IsInstanceValid(gunSmoke)) return;

        gunLight.Visible = on;
        gunFire.Visible = on;
        
        if (!on) return;
        
        gunSmoke.Restart();
        shellSpawner?.StartSpawning();
    }

    private async void HandleShoot()
    {
        onetimeShoot = true;
        int ammo = GetAmmo();

        if (ammo == 0)
        {
            audiShoot.Stream = tryShootSound;
            audiShoot.Play();
            onetimeShoot = false;
        }
        else
        {
            player.EmitSignal(nameof(Player.FireWithWeapon));
            ammo -= 1;
            SetAmmo(ammo);

            cooldown = GetStatsFloat("cooldown");
            audiShoot.Stream = shootSound;
            audiShoot.Play();
            if (gunAnim != null)
            {
                gunAnim.Play("shoot");
            }

            player.Body.Head.CloseEyes();
            var tempDistance = GetStatsInt("shootDistance");
            Dictionary armorProps = player.Inventory.GetArmorProps();
            if (armorProps.Contains("shootDistPlus"))
            {
                tempDistance += int.Parse(armorProps["shootDistPlus"].ToString());
            }

            var tempRay = player.Camera.UseRay(tempDistance);

            await global.ToTimer(0.05f);

            //обрабатываем попадания
            if (isPistol || player.MayMove)
            {
                if (tempWeaponStats.Contains("isShotgun"))
                {
                    player.impulse = player.RotationHelper.GlobalTransform.basis.z / 2;
                }

                tempRay.ForceRaycastUpdate();
                var obj = (Spatial)tempRay.GetCollider();
                if (obj != null)
                {
                    var gunParticles = (Spatial)gunParticlesPrefab.Instance();
                    particlesParent.AddChild(gunParticles);
                    gunParticles.GlobalTransform = Global.SetNewOrigin(
                        gunParticles.GlobalTransform,
                        tempRay.GetCollisionPoint()
                    );
                    var shapeId = tempRay.GetColliderShape();

                    var matName = "box";
                    
                    if (tempWeaponStats.Contains("bullet"))
                    {
                        SpawnBullet(tempRay.GetCollisionPoint());
                    }
                    else
                    {
                        matName = HandleVictim(obj, shapeId);
                    }
                    
                    gunParticles.Call(
                        "_startEmitting",
                        tempRay.GetCollisionNormal(),
                        matName
                    );
                }
                else if (tempWeaponStats.Contains("bullet"))
                {
                    SpawnBullet();
                }
            }

            player.Camera.ReturnRayBack();

            SetGunEffects(true);
            ShakeCameraUp();
            await global.ToTimer(0.06f);
            SetGunEffects(false);

            if (!tempWeaponStats.Contains("isSilence"))
            {
                enemiesManager.LoudShoot(GetStatsInt("shootDistance") * 0.8f, player.GlobalTransform.origin);
            }

            onetimeShoot = false;
            
            player.StealthBoy?.SetOff(false);
        }
    }

    public override void _Process(float delta)
    {
        if (!global.paused && player.Health > 0 && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            if (GunOn)
            {
                //вращаем коллизию пистолета вместе с пистолетом
                if (isPistol)
                {
                    RotationDegrees = player.RotationHelper.RotationDegrees;
                }

                if (Input.IsMouseButtonPressed(1) && cooldown <= 0)
                {
                    if (!onetimeShoot) HandleShoot();
                }
            }

            if (cooldown > 0) cooldown -= delta;
        }
    }
}