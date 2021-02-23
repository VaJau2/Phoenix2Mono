using Godot;

//Класс для анимирования левитирующего оружия у единорогов
//Умеет работать с игроком-единорогом класса Player_Unicorn
//или с неписем-единорогом класса Pony
public class UnicornLevitation : Spatial
{   
    const float HEIGHT_MIN = 2.5f;
    const float HEIGHT_MAX = 2.8f;
    const float SPEED = 0.002f;
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
            AnimateUpDown();
            UpdateWeaponNode(delta);

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
        else return player;
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
            if (weaponPos.x < startXPos) {
                weaponPos.x += CLOSING_SPEED * delta; 
            }
            if (weaponPos.z > startZPos) {
                weaponPos.z -= CLOSING_SPEED * delta; 
            }
        }
        weaponNode.Translation = weaponPos;
    }
}
