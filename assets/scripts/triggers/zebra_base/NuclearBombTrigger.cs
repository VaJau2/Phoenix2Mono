using Godot;
using Godot.Collections;

public partial class NuclearBombTrigger : Node3D
{
    [Export] private Array<AudioStreamWAV> explosionSounds;
    [Export] private float timer;

    public bool isExploding;
    private Node3D rocket;
    private bool rockerCame;
    private float speedY;

    public override void _Ready()
    {
        base._Ready();
        rocket = GetNode<Node3D>("rocket");
        SetProcess(false);
    }

    public void Explode()
    {
        isExploding = true;
        rocket.Visible = true;
        SetProcess(true);
    }

    private void UpdateRocket()
    {
        var rocketTempPlace = rocket.GlobalTransform.origin;
        rocketTempPlace.y = GlobalTransform.origin.y;
        var dist2D = rocketTempPlace.DistanceTo(GlobalTransform.origin);
        if (dist2D > 310)
        {
            rocket.Position = new Vector3(
                rocket.Position.x - 0.6f,
                rocket.Position.y,
                rocket.Position.z
            );
        } 
        else if (dist2D > 4)
        {
            if (speedY < 0.21f)
            {
                speedY += 0.004f;
            }

            rocket.Position = new Vector3(
                rocket.Position.x - (0.6f - speedY),
                rocket.Position.y - speedY,
                rocket.Position.z)
            ;
            rocket.RotationDegrees = new Vector3(
                speedY * -150f,
                rocket.RotationDegrees.y,
                rocket.RotationDegrees.z
            );
        }
        else
        {
            rocket.QueueFree();
            rockerCame = true;
        }
    }

    public override void _Process(float delta)
    {
        if (!isExploding) return;
        
        if (timer > 0)
        {
            timer -= delta;
        }
        else if (!rockerCame)
        {
            UpdateRocket();
        }
        else
        {
            GetNode<Node3D>("light").Visible = true;
            GetNode<Node3D>("sprite").Visible = true;
            GetNode<AnimationPlayer>("anim").Play("fire");
            var audi = GetNode<AudioStreamPlayer3D>("audi");
            var rand = new RandomNumberGenerator();
            rand.Randomize();
            audi.Stream = explosionSounds[rand.RandiRange(0, explosionSounds.Count - 1)];
            audi.Play();
            isExploding = false;
        }
    }
}
