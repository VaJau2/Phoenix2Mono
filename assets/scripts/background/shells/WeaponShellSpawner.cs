using Godot;

public class WeaponShellSpawner : Spatial
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

    public override void _Process(float delta)
    {
        if (timer > 0)
        {
            timer -= delta;
        }
        else
        {
            Spawn();
            SetProcess(false);
        }
    }

    private void Spawn()
    {
        if (shellPrefab.Instance() is WeaponShell shell)
        {
            shell.Name = "Created_" + shell.Name;
            GetNode<Node>("/root/Main/Scene").AddChild(shell);
            shell.GlobalTransform = Global.SetNewOrigin(shell.GlobalTransform, GlobalTransform.origin);
            shell.Rotation = GlobalTransform.basis.GetEuler();

            var dir = shell.Transform.basis.Xform(forceDirection);
            shell.AddCentralForce(dir * 950);
        }
    }
}
