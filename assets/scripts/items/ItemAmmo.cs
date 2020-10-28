using Godot;

public class ItemAmmo: ItemBase {

    [Export]
    public WeaponTypes AmmoType;

    [Export]
    public int AmmoCount = 50;

    public override void _Ready()
    {
        base._Ready();
        sound = GD.Load<AudioStreamSample>("res://assets/audio/item/ItemAmmo.wav");
    }

    public void _on_Item_body_entered(Node body) {
        if (body is Player) {
            var player = body as Player;
            var ammo = player.Weapons.weaponStats[AmmoType].ammo;
            var ammoMax = player.Weapons.weaponStats[AmmoType].ammoMax;

            if (ammo >= ammoMax) {
                messages.ShowMessage("notSpace", "items", 1.5f);
                return;
            } 

            ammo += AmmoCount;
            if (ammo > ammoMax) {
                ammo = ammoMax;
            }

            WeaponStats stats = player.Weapons.weaponStats[AmmoType];
            stats.ammo = ammo;
            player.Weapons.weaponStats[AmmoType] = stats;

            var ammoLabel = player.Weapons.ammoLabel;
            if (player.Weapons.TempWeaponType == AmmoType) {
                ammoLabel.Text = ammo.ToString();
            }

            var audi = player.GetAudi(true);
            audi.Stream = sound;
            audi.Play();
            QueueFree();
        }
    }
}