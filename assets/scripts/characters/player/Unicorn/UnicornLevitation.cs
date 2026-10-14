using Godot;
using Godot.Collections;

//Класс для анимирования левитирующего оружия у единорогов
//Умеет работать с игроком-единорогом класса Player_Unicorn
//или с неписем-единорогом класса Pony
public partial class UnicornLevitation : Node3D, ISavable
{
    private const float PLAYER_HEIGHT_MIN = -0.1f;
    private const float PLAYER_HEIGHT_MAX = 0.2f;
    
    private const float NPC_HEIGHT_MIN = 0.9f;
    private const float NPC_HEIGHT_MAX = 1.3f;
    
    private const float SPEED = 0.1f;
    private const float CLOSING_SPEED = 10f;
    
    private GpuParticles3D cloud;
    private Node3D weaponNode;
    private Player_Unicorn player;
    private NPC npc;

    private bool moveUp;
    private bool weaponClose;

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
            npc = parent as NPC;
            heightMin = NPC_HEIGHT_MIN;
            heightMax = NPC_HEIGHT_MAX;
        }

        cloud = GetNode<GpuParticles3D>("cloud");
        weaponNode = GetNode<Node3D>("weapons");
        startXPos = weaponNode.Position.X;
        startZPos = weaponNode.Position.Z;
    }

    public override void _Process(double delta)
    {
        if (CheckGunOn())
        {
            Vector3 weaponPos = weaponNode.Position;
            AnimateUpDown(ref weaponPos, delta);
            UpdateWeaponNode(ref weaponPos, delta);
            weaponNode.Position = weaponPos;

            Vector3 oldRot = Rotation;
            oldRot.X = GetPlayerRotation();
            Rotation = oldRot;
        }
    }
    
    private bool CheckGunOn()
    {
        var gunOn = IsInstanceValid(player) ? player.Weapons.GunOn : npc.Weapons.GunOn;
        cloud.Emitting = gunOn;
        return gunOn;
    }

    public void _on_collisionArea_body_entered(PhysicsBody3D body)
    {
        if (body == GetCharacterOwner()) return;
        if (body.Name.Contains("shell")) return;
        weaponClose = true;
    }

    public void _on_collisionArea_body_exited(PhysicsBody3D body)
    {
        if (body == GetCharacterOwner()) return;
        if (body.Name.Contains("shell")) return;
        weaponClose = false;
    }

    private Character GetCharacterOwner()
    {
        if (IsInstanceValid(player)) return player;
        return npc;
    }

    private float GetPlayerRotation()
    {
        return IsInstanceValid(player) ? player.RotationHelper.Rotation.X : GetRotationToVictim();
    }

    private float GetRotationToVictim()
    {
        if (npc.tempVictim == null) return 0;
        var npcForward = -npc.GlobalTransform.Basis.Z;
        var npcDir = GetDirToTarget(npc.tempVictim);
        return npcForward.AngleTo(npcDir);
    }
    
    private Vector3 GetDirToTarget(Node3D target)
    {
        Vector3 targetDirPos = target.GlobalPosition;
        targetDirPos.Y = npc.GlobalPosition.Y;
        return targetDirPos - npc.GlobalPosition;
    }

    //анимация движения оружия вверх-вниз
    private void AnimateUpDown(ref Vector3 weaponPos, float delta)
    {
        if (moveUp)
        {
            if (weaponPos.Y < heightMax)
            {
                weaponPos.Y += SPEED * delta;
            }
            else
            {
                moveUp = false;
            }
        }
        else
        {
            if (weaponPos.Y > heightMin)
            {
                weaponPos.Y -= SPEED * delta;
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
            if (weaponPos.X > 0)
            {
                weaponPos.X -= CLOSING_SPEED * delta;
            }
            else
            {
                weaponPos.X = 0;
            }

            if (weaponPos.Z < 0)
            {
                weaponPos.Z += CLOSING_SPEED * delta;
            }
            else
            {
                weaponPos.Z = 0;
            }
        }
        else
        {
            if (weaponPos.X < startXPos)
            {
                weaponPos.X += CLOSING_SPEED * delta;
            }
            else
            {
                weaponPos.X = startXPos;
            }

            if (weaponPos.Z > startZPos)
            {
                weaponPos.Z -= CLOSING_SPEED * delta;
            }
            else
            {
                weaponPos.Z = startZPos;
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
            npc.Weapons.SetWeaponOn(true);
        }
    }
}