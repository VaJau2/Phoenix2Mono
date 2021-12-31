using Godot;

//Класс для анимирования левитирующего оружия у единорогов
//Умеет работать с игроком-единорогом класса Player_Unicorn
//или с неписем-единорогом класса Pony
public class UnicornLevitation : Spatial
{   
    const float HEIGHT_MIN = -0.1f;
    const float HEIGHT_MAX = 0.2f;
    const float SPEED = 0.1f;
    const float CLOSING_SPEED = 10f;

    Particles cloud;
    Spatial weaponNode;
    Player_Unicorn player;
    Pony npc;

    bool moveUp = false;
    bool weaponClose = false;

    float startXPos, startZPos;

    public override void _Ready()
    {
        Character parent = GetParent<Character>();
        if (parent is Player_Unicorn) {
            player = parent as Player_Unicorn;
        } else {
            npc = parent as Pony;
        }
        
        cloud = GetNode<Particles>("cloud");
        weaponNode = GetNode<Spatial>("weapons");
        startXPos = weaponNode.Translation.x;
        startZPos = weaponNode.Translation.z;
    }

    public override void _Process(float delta)
    {
        if (CheckGunOn()) {
            Vector3 weaponPos = weaponNode.Translation;
            AnimateUpDown(ref weaponPos, delta);
            UpdateWeaponNode(ref weaponPos, delta);
            weaponNode.Translation = weaponPos;

            Vector3 oldRot = Rotation;
            oldRot.x = GetPlayerRotation();
            Rotation = oldRot;
        } 
    }

    public void _on_collisionArea_body_entered(PhysicsBody body)
    {
        if (body == getOwner()) return;
        weaponClose = true;
    }

    public void _on_collisionArea_body_exited(PhysicsBody body)
    {
        if (body == getOwner()) return;
        weaponClose = false;
    }

    private bool CheckGunOn()
    {
        bool gunOn = false;

        if (player != null) {
            gunOn = player.Weapons.GunOn;
        } else {
            gunOn = npc.weapons.GunOn;
        }

        cloud.Emitting = gunOn;
        return gunOn;
    }

    private float GetPlayerRotation()
    {
        if (player != null) {
            return player.RotationHelper.Rotation.x;
        } else {
            return npc.RotationToVictim;
        }
    }   

    private Character getOwner()
    {
        if (player == null) return npc;
        return player;
    }

    //анимация движения оружия вверх-вниз
    private void AnimateUpDown(ref Vector3 weaponPos, float delta)
    {
        if (moveUp) {
            if (weaponPos.y < HEIGHT_MAX)
            {
                weaponPos.y += SPEED * delta;
            }
            else
            {
                moveUp = false;
            }
        } else {
            if (weaponPos.y > HEIGHT_MIN)
            {
                weaponPos.y -= SPEED * delta;
            }
            else
            {
                moveUp = true;
            }
        }
        // GD.Print(weaponPos.y);
    }

    //анимация приближения оружия при столкновении со стенами
    private void UpdateWeaponNode(ref Vector3 weaponPos, float delta)
    {
        if (weaponClose) {
            if (weaponPos.x > 0) {
                weaponPos.x -= CLOSING_SPEED * delta; 
            }
            else
            {
                weaponPos.x = 0;
            }
            
            if (weaponPos.z < 0) {
                weaponPos.z += CLOSING_SPEED * delta; 
            }
            else
            {
                weaponPos.z = 0;
            }
        } else {
            if (weaponPos.x < startXPos) {
                weaponPos.x += CLOSING_SPEED * delta; 
            }
            else
            {
                weaponPos.x = startXPos;
            }
            
            if (weaponPos.z > startZPos) {
                weaponPos.z -= CLOSING_SPEED * delta; 
            }
            else
            {
                weaponPos.z = startZPos;
            }
        }
    }
}
