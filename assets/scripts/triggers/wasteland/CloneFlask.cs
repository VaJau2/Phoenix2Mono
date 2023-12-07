using Godot;
using Godot.Collections;

public class CloneFlask : Spatial, ISavable
{
    public AnimationPlayer anim => GetNode<AnimationPlayer>("AnimationPlayer");
    public CloneFlaskCamera camera => GetNode<CloneFlaskCamera>("Camera");
    public Spatial playerPos => GetNode<Spatial>("player_pos");
    private AudioStreamPlayer3D audi => GetNode<AudioStreamPlayer3D>("audi");

    private bool wokenUp;

    Spatial water, glass;
    private bool animateWater;
    private bool animateGlass;

    [Export] private Race cloneRace;

    public override void _Ready()
    {
        anim.Play("idle");
        SetRace(cloneRace);
        SetProcess(false);
    }

    public override void _Process(float delta)
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

    private void AnimateObjectDown(Spatial _object, ref bool animate, float speed = 1f)
    {
        if (!IsInstanceValid(_object))
        {
            animate = false;
            return;
        }

        if (_object.Translation.y > -2)
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
        audi.Play();
    }
    
    public void AnimateWater()
    {
        water = GetNode<Spatial>("water");
        animateWater = true;
        SetProcess(true);
    }

    public void AnimateGlass()
    {
        wokenUp = true;
        glass = GetNode<Spatial>("flas-glass");
        animateGlass = true;
        SetProcess(true);
    }

    public void PrepareFlaskForPlayer()
    {
        var head = GetNode<MeshInstance>("Armature/Skeleton/Body002");
        var mane = GetNode<MeshInstance>("Armature/Skeleton/BoneAttachment/Cube");
        var horn = GetNode<MeshInstance>("Armature/Skeleton/BoneAttachment2/horn");
        var wires = GetNode<MeshInstance>("wires");

        head.Layers = 2;
        mane.Layers = 2;
        horn.Layers = 2;
        wires.Visible = false;
    }

    public void DeleteBody()
    {
        GetNode<Spatial>("Armature").QueueFree();
        anim.QueueFree();
    }

    public void SetRace(Race newRace)
    {
        cloneRace = newRace;
        var horn = GetNode<MeshInstance>("Armature/Skeleton/BoneAttachment2/horn");
        var wingL = GetNode<MeshInstance>("Armature/Skeleton/WingL");
        var wingR = GetNode<MeshInstance>("Armature/Skeleton/WingR");

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
        if (wokenUp)
        {
            DeleteBody();
            GetNode<MeshInstance>("wires").QueueFree();
            GetNode<Spatial>("water").QueueFree();
            GetNode<Spatial>("flas-glass").QueueFree();
        }
    }
}
