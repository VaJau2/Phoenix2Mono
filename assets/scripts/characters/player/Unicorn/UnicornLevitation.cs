using Godot;

//Класс для анимирования левитирующего оружия у единорогов
//Умеет работать с игроком-единорогом класса Player_Unicorn
//или с неписем-единорогом класса Pony
public partial class UnicornLevitation : Node3D
{
    const float HEIGHT_MIN = -0.1f;
    const float HEIGHT_MAX = 0.2f;
    const float SPEED = 0.1f;
    const float CLOSING_SPEED = 10f;

    GpuParticles3D cloud;
    Node3D weaponNode;
    Player_Unicorn player;
    Pony npc;

    bool moveUp;
    bool weaponClose;

    float startXPos, startZPos;

    public override void _Ready()
    {
        Character parent = GetParent<Character>();
        if (parent is Player_Unicorn unicorn)
        {
            player = unicorn;
        }
        else
        {
            npc = parent as Pony;
        }

        cloud = GetNode<GpuParticles3D>("cloud");
        weaponNode = GetNode<Node3D>("weapons");
        startXPos = weaponNode.Position.X;
        startZPos = weaponNode.Position.Z;
    }

    public override void _Process(double delta)
    {
        if (!CheckGunOn())
            return;

        Vector3 weaponPos = weaponNode.Position;
        AnimateUpDown(ref weaponPos, (float)delta);
        UpdateWeaponNode(ref weaponPos, (float)delta);
        weaponNode.Position = weaponPos;

        Vector3 oldRot = Rotation;
        oldRot.X = GetPlayerRotation();
        Rotation = oldRot;
    }

    public void _on_collisionArea_body_entered(PhysicsBody3D body)
    {
        if (body == GetOwnerCharacter()) return;
        if (body.Name.ToString().Contains("shell")) return;
        weaponClose = true;
    }

    public void _on_collisionArea_body_exited(PhysicsBody3D body)
    {
        if (body == GetOwnerCharacter()) return;
        if (body.Name.ToString().Contains("shell")) return;
        weaponClose = false;
    }

    private bool CheckGunOn()
    {
        bool gunOn = false;

        gunOn = IsInstanceValid(player) ? player.Weapons.GunOn : npc.weapons.GunOn;

        cloud.Emitting = gunOn;
        return gunOn;
    }

    private float GetPlayerRotation()
    {
        return IsInstanceValid(player) ? player.RotationHelper.Rotation.X : npc.RotationToVictim;
    }

    private Character GetOwnerCharacter()
    {
        if (IsInstanceValid(player)) return player;
        return npc;
    }

    //анимация движения оружия вверх-вниз
    private void AnimateUpDown(ref Vector3 weaponPos, float delta)
    {
        if (moveUp)
        {
            if (weaponPos.X < HEIGHT_MAX)
            {
                weaponPos.X += SPEED * delta;
            }
            else
            {
                moveUp = false;
            }
        }
        else
        {
            if (weaponPos.X > HEIGHT_MIN)
            {
                weaponPos.X -= SPEED * delta;
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
}