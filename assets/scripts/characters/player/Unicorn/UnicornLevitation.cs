using Godot;
using Godot.Collections;

//Класс для анимирования левитирующего оружия у единорогов
//Умеет работать с игроком-единорогом класса Player_Unicorn
//или с неписем-единорогом класса Pony
public class UnicornLevitation : Spatial, ISavable
{
    private const float PLAYER_HEIGHT_MIN = -0.1f;
    private const float PLAYER_HEIGHT_MAX = 0.2f;
    
    private const float NPC_HEIGHT_MIN = 0.9f;
    private const float NPC_HEIGHT_MAX = 1.3f;
    
    private const float SPEED = 0.1f;
    private const float CLOSING_SPEED = 10f;
    
    private Particles cloud;
    private Spatial weaponNode;
    private Player_Unicorn player;
    private Pony npc;

    private bool moveUp = false;
    private bool weaponClose = false;

    private float startXPos, startZPos, heightMin, heightMax;

    public override void _Ready()
    {
        Character parent = GetParent<Character>();
        if (parent is Player_Unicorn)
        {
            player = parent as Player_Unicorn;
            heightMin = PLAYER_HEIGHT_MIN;
            heightMax = PLAYER_HEIGHT_MAX;
        }
        else
        {
            npc = parent as Pony;
            heightMin = NPC_HEIGHT_MIN;
            heightMax = NPC_HEIGHT_MAX;
        }

        cloud = GetNode<Particles>("cloud");
        weaponNode = GetNode<Spatial>("weapons");
        startXPos = weaponNode.Translation.x;
        startZPos = weaponNode.Translation.z;
    }

    public override void _Process(float delta)
    {
        if (CheckGunOn())
        {
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
        if (body.Name.Contains("shell")) return;
        weaponClose = true;
    }

    public void _on_collisionArea_body_exited(PhysicsBody body)
    {
        if (body == getOwner()) return;
        if (body.Name.Contains("shell")) return;
        weaponClose = false;
    }

    private bool CheckGunOn()
    {
        var gunOn = IsInstanceValid(player) ? player.Weapons.GunOn : npc.weapons.GunOn;
        cloud.Emitting = gunOn;
        return gunOn;
    }

    private float GetPlayerRotation()
    {
        return IsInstanceValid(player) ? player.RotationHelper.Rotation.x : npc.RotationToVictim;
    }

    private Character getOwner()
    {
        if (IsInstanceValid(player)) return player;
        return npc;
    }

    //анимация движения оружия вверх-вниз
    private void AnimateUpDown(ref Vector3 weaponPos, float delta)
    {
        if (moveUp)
        {
            if (weaponPos.y < heightMax)
            {
                weaponPos.y += SPEED * delta;
            }
            else
            {
                moveUp = false;
            }
        }
        else
        {
            if (weaponPos.y > heightMin)
            {
                weaponPos.y -= SPEED * delta;
            }
            else
            {
                moveUp = true;
            }
        }
    }

    //анимация приближения оружия при столкновении со стенами
    private void UpdateWeaponNode(ref Vector3 weaponPos, float delta)
    {
        if (weaponClose)
        {
            if (weaponPos.x > 0)
            {
                weaponPos.x -= CLOSING_SPEED * delta;
            }
            else
            {
                weaponPos.x = 0;
            }

            if (weaponPos.z < 0)
            {
                weaponPos.z += CLOSING_SPEED * delta;
            }
            else
            {
                weaponPos.z = 0;
            }
        }
        else
        {
            if (weaponPos.x < startXPos)
            {
                weaponPos.x += CLOSING_SPEED * delta;
            }
            else
            {
                weaponPos.x = startXPos;
            }

            if (weaponPos.z > startZPos)
            {
                weaponPos.z -= CLOSING_SPEED * delta;
            }
            else
            {
                weaponPos.z = startZPos;
            }
        }
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            {"gunOn", CheckGunOn()}
        };
    }

    public void LoadData(Dictionary data)
    {
        if (data.Count == 0) return;
        
        if (npc != null && (bool) data["gunOn"])
        {
            npc.weapons.SetWeapon(true);
        }
    }
}