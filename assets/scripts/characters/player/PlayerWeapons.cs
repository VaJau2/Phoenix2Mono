using Godot;
using Godot.Collections;

public partial class PlayerWeapons : CollisionShape3D
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
    public ItemIcon tempAmmoButton { get; private set; }
    Node3D tempWeapon;
    AnimationPlayer gunAnim;
    Node3D gunLight;
    Node3D gunFire;
    GpuParticles3D gunSmoke;
    PackedScene gunParticlesPrefab;
    Node particlesParent;
    WeaponShellSpawner shellSpawner;

    float tempShake = 0;
    bool shakeUp = true;
    float cooldown = 0.6f;

    //--звуки-------------
    AudioStreamPlayer audiShoot;
    AudioStreamWav tryShootSound;
    AudioStreamWav shootSound;

    EnemiesManager enemiesManager;

    bool onetimeShoot = false;

    public void LoadNewWeapon(string weaponCode, Dictionary weaponData)
    {
        //грузим префаб оружия
        string path = "res://objects/guns/prefabs/" + weaponCode + ".tscn";
        PackedScene weaponPrefab = GD.Load<PackedScene>(path);

        //грузим статистику оружия
        tempWeaponStats = weaponData;
        isPistol = weaponData.ContainsKey("isPistol");
        Disabled = !isPistol || global.playerRace == Race.Unicorn;
        shootSound = GD.Load<AudioStreamWav>("res://assets/audio/guns/shoot/" + weaponCode + ".wav");

        //вытаскиваем родительский нод из игрока
        Node3D tempParent = player.GetWeaponParent(isPistol);

        //отправляем префаб туда
        var weapon = weaponPrefab.Instantiate();
        tempParent.AddChild(weapon);

        //сохраняем его для будущих перемещений по нодам
        tempWeapon = (Node3D)weapon;
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
        if (!IsInstanceValid(tempWeapon)) return;
        cooldown = 0;
        Disabled = true;
        tempWeapon.QueueFree();
        tempWeapon = null;
        shootInterface.Visible = false;
        point.SetInteractionVariant(InteractionVariant.Point);
        player.SetWeaponOff();
        GunOn = false;
        
        player.EmitSignal(Player.SignalName.ClearWeaponBind);
    }

    public void СheckThirdView()
    {
        //если сменился вид, моделька перемещается в новый нод
        if (GunOn && isPistol)
        {
            Node3D oldParent = (Node3D)tempWeapon.GetParent();
            Node3D newParent = player.GetWeaponParent(isPistol);

            if (oldParent != newParent)
            {
                oldParent.RemoveChild(tempWeapon);
                newParent.AddChild(tempWeapon);
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
        tryShootSound = GD.Load<AudioStreamWav>("res://assets/audio/guns/TryShoot.wav");

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
        CompressedTexture2D newIcon = GD.Load<CompressedTexture2D>(path);
        ammoIcon.SetTexture(newIcon);
    }

    private int GetAmmo() => tempAmmoButton?.GetCount() ?? 0;

    private void SetAmmo(int newAmmo)
    {
        tempAmmoButton.SetCount(newAmmo);
        if (newAmmo == 0)
        {
            tempAmmoButton = null;
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
            if (tempWeaponStats.ContainsKey("ammoType") && tempWeaponStats["ammoType"].ToString() == ammoType)
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
            tempAmmoButton = player.Inventory.ammoButtons[ammoType];
        }
        else
        {
            tempAmmoButton = null;
        }

        SetAmmoIcon(ammoType);
        UpdateAmmoCount();
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

    private Vector3 SetRotX(Vector3 origin, float newRotX)
    {
        origin.X = newRotX;
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
                    Camera3D camera = player.GetViewport().GetCamera3D();
                    camera.RotationDegrees = SetRotX(
                        camera.RotationDegrees,
                        camera.RotationDegrees.X + SHAKE_SPEED
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
                    Camera3D camera = player.GetViewport().GetCamera3D();
                    camera.RotationDegrees = SetRotX(
                        camera.RotationDegrees,
                        camera.RotationDegrees.X - SHAKE_SPEED * SHAKE_DIFF
                    );
                }
                else
                {
                    shakeUp = true;
                    shakingProcess = false;
                }
            }

            await player.ToSignal(player.GetTree(), "process_frame");
        }
    }

    public int GetStatsInt(string statsName) => int.Parse(tempWeaponStats[statsName].ToString());
    public float GetStatsFloat(string statsName) => Global.ParseFloat(tempWeaponStats[statsName].ToString());

    private string HandleVictim(Node3D victim, int shapeID = 0)
    {
        string name = null;
        switch (victim)
        {
            case Character character:
            {
                var characterName = character.Name.ToString();
                if (characterName.Contains("target") ||
                    characterName.Contains("roboEye") ||
                    characterName.Contains("MrHandy"))
                {
                    name = "black";
                }
                else
                {
                    name = "blood";
                }

                character.CheckShotgunShot(tempWeaponStats.ContainsKey("isShotgun"));
                player.MakeDamage(character, shapeID);
                ShowCrossHitted(shapeID != 0);
                break;
            }
            case StaticBody3D body:
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

    private void SpawnBullet()
    {
        string bullet = tempWeaponStats["bullet"].ToString();
        var bulletPrefab = GD.Load<PackedScene>("res://objects/guns/bullets/" + bullet + ".tscn");
        Bullet newBullet = bulletPrefab.Instantiate<Bullet>();
        newBullet.Damage = player.GetDamage();
        newBullet.Shooter = player;
        newBullet.Timer = GetStatsFloat("shootDistance");

        GetNode("/root/Main/Scene").AddChild(newBullet);
        newBullet.GlobalTransform = gunFire.GlobalTransform;
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
            player.EmitSignal(Player.SignalName.FireWithWeapon);
            ammo -= 1;
            SetAmmo(ammo);

            cooldown = GetStatsFloat("cooldown");
            audiShoot.Stream = shootSound;
            audiShoot.Play();
            gunAnim?.Play("shoot");

            player.Body.Head.CloseEyes();
            var tempDistance = GetStatsInt("shootDistance");
            Dictionary armorProps = player.Inventory.GetArmorProps();
            if (armorProps.TryGetValue("shootDistPlus", out var distPlus))
            {
                tempDistance += int.Parse(distPlus.ToString());
            }

            var tempRay = player.Camera3D.UseRay(tempDistance);

            await global.ToTimer(0.05f);

            //обрабатываем попадания
            if (isPistol || player.MayMove)
            {
                if (tempWeaponStats.ContainsKey("bullet"))
                {
                    SpawnBullet();
                }
                else
                {
                    if (tempWeaponStats.ContainsKey("isShotgun"))
                    {
                        player.Impulse = player.RotationHelper.GlobalTransform.Basis.Z / 2;
                    }

                    tempRay.ForceRaycastUpdate();
                    var obj = (Node3D)tempRay.GetCollider();
                    if (obj != null)
                    {
                        var gunParticles = gunParticlesPrefab.Instantiate<Node3D>();
                        particlesParent.AddChild(gunParticles);
                        gunParticles.GlobalTransform = Global.SetNewOrigin(
                            gunParticles.GlobalTransform,
                            tempRay.GetCollisionPoint()
                        );
                        var shapeId = tempRay.GetColliderShape();
                        var matName = HandleVictim(obj, shapeId);
                        gunParticles.Call(
                            "_startEmitting",
                            tempRay.GetCollisionNormal(),
                            matName
                        );
                    }
                }
            }

            player.Camera3D.ReturnRayBack();

            SetGunEffects(true);
            ShakeCameraUp();
            await global.ToTimer(0.06f);
            SetGunEffects(false);

            if (!tempWeaponStats.ContainsKey("isSilence"))
            {
                enemiesManager.LoudShoot(GetStatsInt("shootDistance") * 0.8f, player.GlobalTransform.Origin);
            }

            onetimeShoot = false;
        }
    }

    public override void _Process(double delta)
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

                if (Input.IsMouseButtonPressed(MouseButton.Left) && cooldown <= 0)
                {
                    if (!onetimeShoot) HandleShoot();
                }
            }

            if (cooldown > 0) cooldown -= (float)delta;
        }
    }
}