using Godot;

public partial class WeaponShellSpawner : Node3D
{
    [Export]
    public PackedScene shellPrefab;
    [Export]
    public float spawnDelay;
    [Export]
    public Vector3 forceDirection;

    double timer;

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
        if (shellPrefab.Instantiate() is not WeaponShell shell) 
            return;
        
        shell.Name = "Created_" + shell.Name;
        GetNode<Node>("/root/Main/Scene").AddChild(shell);
        shell.GlobalTransform = Global.SetNewOrigin(shell.GlobalTransform, GlobalTransform.Origin);
        shell.Rotation = GlobalTransform.Basis.GetEuler();

        var dir = shell.Transform.Basis * forceDirection;
        shell.AddConstantCentralForce(dir * 950);
    }
}
