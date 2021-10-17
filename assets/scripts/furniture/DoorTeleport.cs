using Godot;
using Godot.Collections;

//дверь, перемещающая в отдельную подлокацию
public class DoorTeleport : StaticBody, ISavable
{
    [Export] public bool Closed;
    [Export] private bool Inside;
    [Export] private NodePath newPlacePath;
    [Export] private NodePath oldLocationPath;
    [Export] private NodePath newLocationPath;
    [Export] private NodePath otherDoorPath;

    [Export] private AudioStreamSample openSound;
    [Export] private AudioStreamSample closedSound;
    public DoorTeleport otherDoor { get; private set; }
    AudioStreamPlayer3D audi;
    Spatial newPlace, oldLocation, newLocation;

    private static Player player => Global.Get().player;
    private CheckFall checkFall;

    public override void _Ready()
    {
        audi = GetNode<AudioStreamPlayer3D>("audi");
        newPlace = GetNode<Spatial>(newPlacePath);
        oldLocation = GetNode<Spatial>(oldLocationPath);
        newLocation = GetNode<Spatial>(newLocationPath);
        otherDoor = GetNodeOrNull<DoorTeleport>(otherDoorPath);
        checkFall = GetNodeOrNull<CheckFall>("/root/Main/Scene/terrain/checkFall");
        SetProcess(false);
    }

    public override void _Process(float delta)
    {
        player.Camera.ShowHint("open", false);
        if (!Input.IsActionJustPressed("use")) return;
        Open(player, true);
    }

    public void SoundOpening()
    {
        if (otherDoor?.audi != null)
        {
            otherDoor.audi.Stream = openSound;
            otherDoor.audi.Play();
        }
        if (audi != null)
        {
            audi.Stream = openSound;
            audi.Play();
        }
    }

    public void Open(Spatial character, bool makeVisible)
    {
        if (Closed)
        {
            player.Camera.closedTimer = 1;
            audi.Stream = closedSound;
            audi.Play();
            SetProcess(false);
            return;
        }
        
        SoundOpening();

        character.GlobalTransform = newPlace.GlobalTransform;
        
        if (character is Player_Unicorn unicorn)
        {
            unicorn.teleportInside = Inside;
        }
        
        if (makeVisible)
        {
            oldLocation.Visible = false;
            newLocation.Visible = true;
        }

        if (checkFall == null) return;
        checkFall.tempDoorTeleport = this;
        checkFall.inside = Inside;
        
        player.Camera.HideHint();
    }

    public void _on_body_entered(Node body)
    {
        if (!(body is Player)) return;
        SetProcess(true);
    }

    public void _on_body_exited(Node body)
    {
        if (!(body is Player)) return;
        player?.Camera.HideHint();
        SetProcess(false);
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            {"Closed", Closed}
        };
    }

    public void LoadData(Dictionary data)
    {
        Closed = (bool) data["Closed"];
    }
}
