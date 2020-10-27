using Godot;
using Godot.Collections;

public class GunIcons: Control {
    const float MIN_Y_POS = -36;
    const float SPEEED_Y = 120;

    Global global;
    Dictionary<WeaponTypes, Sprite> icons = new Dictionary<WeaponTypes, Sprite>();

    float showTimer = 0;

    public override void _Ready()
    {
        global = Global.Get();
        icons.Add(WeaponTypes.Pistol, GetNode<Sprite>("pistol"));
        icons.Add(WeaponTypes.Shotgun, GetNode<Sprite>("shotgun"));
        icons.Add(WeaponTypes.Revolver, GetNode<Sprite>("revolver"));
        icons.Add(WeaponTypes.Sniper, GetNode<Sprite>("sniper"));
    }

    public void ChangeWeapon(PlayerWeapons weapons, WeaponTypes newWeapon) {
        var weaponStats = weapons.weaponStats;
        foreach(WeaponTypes type in icons.Keys) {
            if (weaponStats[type].have) {
                icons[type].Visible = true;
                icons[type].Modulate = Colors.White;
            } else {
                icons[type].Visible = false;
            }
        }
        icons[newWeapon].Modulate = Colors.Yellow;
        showTimer += 2f;
    }

    public override void _Process(float delta)
    {
        if (showTimer > 0) {
            showTimer -= delta;
            if (RectPosition.y < 0) {
                Vector2 newPos = RectPosition;
                newPos.y += SPEEED_Y * delta;
                RectPosition = newPos;
            }
        } else {
            if (RectPosition.y > MIN_Y_POS) {
                Vector2 newPos = RectPosition;
                newPos.y -= SPEEED_Y * delta;
                RectPosition = newPos;
            }
        }
    }
}