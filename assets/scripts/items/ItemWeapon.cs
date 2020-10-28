using Godot;

public class ItemWeapon: ItemBase {

    [Export]
    public WeaponTypes WeaponType;
    [Export]
    public int AmmoCount;

    public override void _Ready()
    {
        base._Ready();
        sound = GD.Load<AudioStreamSample>("res://assets/audio/item/ItemAmmo.wav");
    }

    public void _on_gun_body_entered(Node body) {
        if (body is Player) {
            var player = body as Player;
            if (player.MayMove) {
                var weapons = player.Weapons;
                if (weapons.weaponStats[WeaponType].have) {
                    var ammo = weapons.weaponStats[WeaponType].ammo;
                    var ammoMax = weapons.weaponStats[WeaponType].ammoMax;

                    if (ammo >= ammoMax) {
                        messages.ShowMessage("notSpace", "items", 1.5f);
                        return;
                    } 

                    ammo += AmmoCount;
                    if (ammo > ammoMax) {
                        ammo = ammoMax;
                    }

                    WeaponStats stats = weapons.weaponStats[WeaponType];
                    stats.ammo = ammo;
                    weapons.weaponStats[WeaponType] = stats;

                    var ammoLabel = weapons.ammoLabel;
                    if (weapons.TempWeaponType == WeaponType) {
                        ammoLabel.Text = ammo.ToString();
                    }
                } 
                else //not have weapon
                {
                    WeaponStats stats = weapons.weaponStats[WeaponType];
                    stats.have = true;
                    weapons.weaponStats[WeaponType] = stats;
                    weapons.changeGun(WeaponType);
                }

                var audi = player.GetAudi(true);
                audi.Stream = sound;
                audi.Play();
                QueueFree();
            }
        }
    }
}