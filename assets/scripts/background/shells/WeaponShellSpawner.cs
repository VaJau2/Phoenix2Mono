using Godot;

public partial class WeaponShellSpawner : Node3D
{
    [Export]
    public PackedScene shellPrefab;
    [Export]
    public float spawnDelay;
    [Export]
    public Vector3 forceDirection;

    float timer;

    public void StartSpawning()
    {
        timer = spawnDelay;
        SetProcess(true);
    }

    public override void _Ready()
    {
        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        if (timer > 0)
        {
            timer -= (float)delta;
        }
        else
        {
            Spawn();
            SetProcess(false);
        }
    }

    private void Spawn()
    {
        if (shellPrefab.Instantiate() is WeaponShell shell)
        {
            shell.Name = "Created_" + shell.Name;
            //гильза появляется внутри коллизии стрелка — без исключения движение персонажа
            //выталкивает его из гильзы (в Godot 4 игрока сдвигало вбок при каждом выстреле)
            var shooter = GetShooter();
            if (shooter != null)
            {
                shell.AddCollisionExceptionWith(shooter);
            }
            GetNode<Node>("/root/Main/Scene").AddChild(shell);
            shell.GlobalTransform = Global.SetNewOrigin(shell.GlobalTransform, GlobalTransform.Origin);
            shell.Rotation = GlobalTransform.Basis.GetEuler();

            var dir = shell.Transform.Basis * forceDirection;
            shell.ApplyCentralForce(dir * 950);
        }
    }

    private Character GetShooter()
    {
        for (var node = GetParent(); node != null; node = node.GetParent())
        {
            if (node is Character character) return character;
        }

        return null;
    }
}
