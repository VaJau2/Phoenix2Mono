using Godot;
using Godot.Collections;

public partial class CloneFlask : Node3D, ISavable
{
    public AnimationPlayer anim { private set; get; }
    public CloneFlaskCamera camera => GetNode<CloneFlaskCamera>("Camera3D");
    public Node3D playerPos => GetNode<Node3D>("player_pos");

    public AudioStreamWav underwater => GD.Load<AudioStreamWav>("res://assets/audio/underwater.wav");
    public AudioStreamWav flaskOpen => GD.Load<AudioStreamWav>("res://assets/audio/futniture/flaskOpen.wav");
    private AudioStreamPlayer3D message => GetNode<AudioStreamPlayer3D>("message");

    private bool wokenUp;

    private Node3D water, glass;
    private bool animateWater;
    private bool animateGlass;

    [Export] private Race cloneRace;

    public override void _Ready()
    {
        anim = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");

        if (anim != null)
        {
            GD.Randomize();
            anim.SpeedScale = (float)GD.RandRange(0.8, 1);
            anim.Play("idle");
        }

        SetRace(cloneRace);
        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        if (animateWater)
        {
            AnimateObjectDown(water, ref animateWater, 0.007f);
        }

        if (animateGlass)
        {
            AnimateObjectDown(glass, ref animateGlass, 0.05f);
        }

        if (!animateGlass && !animateWater)
        {
            SetProcess(false);
        }
    }

    private void AnimateObjectDown(Node3D _object, ref bool animate, float speed = 1f)
    {
        if (!IsInstanceValid(_object))
        {
            animate = false;
            return;
        }

        if (_object.Position.Y > -2)
        {
            _object.Translate(speed * Vector3.Down);
        }
        else
        {
            _object.QueueFree();
            animate = false;
        }
    }

    public void PlayMessage()
    {
        message.Play();
    }
    
    public void AnimateWater()
    {
        water = GetNode<Node3D>("water");
        animateWater = true;
        SetProcess(true);
    }

    public void AnimateGlass()
    {
        wokenUp = true;
        glass = GetNode<Node3D>("flask-glass");
        animateGlass = true;
        SetProcess(true);
    }

    public void PrepareFlaskForPlayer()
    {
        var head = GetNode<MeshInstance3D>("Armature/Skeleton3D/Body002");
        var mane = GetNode<MeshInstance3D>("Armature/Skeleton3D/BoneAttachment3D/Cube");
        var horn = GetNode<MeshInstance3D>("Armature/Skeleton3D/BoneAttachment2/horn");
        var wires = GetNode<MeshInstance3D>("wires");

        head.Layers = 2;
        mane.Layers = 2;
        horn.Layers = 2;
        wires.Visible = false;
    }

    public void DeleteBody()
    {
        var armature = GetNode<Node3D>("Armature");
        armature.QueueFree();

        var light = GetNode<Node3D>("light");
        light.QueueFree();
        
        anim.QueueFree();
    }

    public void SetRace(Race newRace)
    {
        cloneRace = newRace;
        var horn = GetNode<MeshInstance3D>("Armature/Skeleton3D/BoneAttachment2/horn");
        var wingL = GetNode<MeshInstance3D>("Armature/Skeleton3D/WingL");
        var wingR = GetNode<MeshInstance3D>("Armature/Skeleton3D/WingR");

        switch (newRace)
        {
            case Race.Earthpony:
                horn.Visible = wingL.Visible = wingR.Visible = false;
                break;
            case Race.Unicorn:
                horn.Visible = true;
                wingL.Visible = wingR.Visible = false;
                break;
            case Race.Pegasus:
                horn.Visible = false;
                wingL.Visible = wingR.Visible = true;
                break;
        }
    }

    public Race GetRace() => cloneRace;

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            { "wokenUp", wokenUp }
        };
    }

    public void LoadData(Dictionary data)
    {
        wokenUp = System.Convert.ToBoolean(data["wokenUp"]);
        if (!wokenUp) return;
        
        DeleteBody();
        GetNode<MeshInstance3D>("wires").QueueFree();
        GetNode<Node3D>("water").QueueFree();
        GetNode<Node3D>("flask-glass").QueueFree();
    }
}
