using Godot;

public class UnicornLevitation : Particles
{   
    const float HEIGHT_MIN = 2.3f;
    const float HEIGHT_MAX = 2.5f;
    const float SPEED = 0.002f;
    const float CLOSING_SPEED = 10f;

    Spatial rotationHelper;
    Spatial weaponNode;
    Player player;

    float tempHeight;
    bool moveUp = false;
    bool weaponClose = false;

    public override void _Ready()
    {
        player = GetParent<Player>();
        rotationHelper = GetNode<Spatial>("rotationHelper");
        weaponNode = GetNode<Spatial>("rotationHelper/weapons");
    }

    public override void _Process(float delta)
    {
        Emitting = player.Weapons.GunOn;
        if (player.Weapons.GunOn) {
            AnimateUpDown();
            UpdateWeaponNode(delta);

            Vector3 oldRot = rotationHelper.Rotation;
            oldRot.x = player.RotationHelper.Rotation.x;
            rotationHelper.Rotation = oldRot;
        }
    }

    public void _on_collisionArea_body_entered(PhysicsBody body)
    {
        if (body is Player) return;
        weaponClose = true;
    }

    public void _on_collisionArea_body_exited(PhysicsBody body)
    {
        if (body is Player) return;
        weaponClose = false;
    }

    private void AnimateUpDown()
    {
        if (moveUp) {
            if(Translation.y < HEIGHT_MAX)
                Translate(Vector3.Up * SPEED);
            else
                moveUp = false;
        } else {
            if(Translation.y > HEIGHT_MIN)
                Translate(Vector3.Down * SPEED);
            else
                moveUp = true;
        }
    }

    private void UpdateWeaponNode(float delta)
    {
        Vector3 weaponPos = weaponNode.Translation;
        if (weaponClose) {
            if (weaponPos.x > 0) {
                weaponPos.x -= CLOSING_SPEED * delta; 
            }
            if (weaponPos.z < 0) {
                weaponPos.z += CLOSING_SPEED * delta; 
            }
        } else {
            if (weaponPos.x < 0.43f) {
                weaponPos.x += CLOSING_SPEED * delta; 
            }
            if (weaponPos.z > -2.2f) {
                weaponPos.z -= CLOSING_SPEED * delta; 
            }
        }
        weaponNode.Translation = weaponPos;
    }
}
