using Godot;

//дверь, перемещающая в отдельную подлокацию
public class DoorTeleport : StaticBody
{
    [Export] private bool Closed;
    [Export] private bool Inside;
    [Export] private NodePath newPlacePath;
    [Export] private NodePath oldLocationPath;
    [Export] private NodePath newLocationPath;
    [Export] private NodePath otherDoorPath;

    [Export] private AudioStreamSample openSound;
    private DoorTeleport otherDoor;
    AudioStreamPlayer3D audi;
    Spatial newPlace, oldLocation, newLocation;

    private static Player player => Global.Get().player;

    public override void _Ready()
    {
        audi = GetNode<AudioStreamPlayer3D>("audi");
        newPlace = GetNode<Spatial>(newPlacePath);
        oldLocation = GetNode<Spatial>(oldLocationPath);
        newLocation = GetNode<Spatial>(newLocationPath);
        otherDoor = GetNodeOrNull<DoorTeleport>(otherDoorPath);
        SetProcess(false);
    }

    public override void _Process(float delta)
    {
        player.Camera.ShowHint("open", false);
        if (!Input.IsActionJustPressed("use")) return;
        Open(player, true);
        player.Camera.HideHint();
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
}
