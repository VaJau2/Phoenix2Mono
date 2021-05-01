using Godot;

//дверь, перемещающая в отдельную подлокацию
public class DoorTeleport : StaticBody
{
    [Export] private NodePath newPlacePath;
    [Export] private NodePath oldLocationPath;
    [Export] private NodePath newLocationPath;

    [Export] private AudioStreamSample openSound;
    AudioStreamPlayer3D audi;
    Spatial newPlace, oldLocation, newLocation;

    private static Player player => Global.Get().player;

    public override void _Ready()
    {
        audi = GetNode<AudioStreamPlayer3D>("audi");
        newPlace = GetNode<Spatial>(newPlacePath);
        oldLocation = GetNode<Spatial>(oldLocationPath);
        newLocation = GetNode<Spatial>(newLocationPath);
        SetProcess(false);
    }

    public override void _Process(float delta)
    {
        player.Camera.ShowHint("open", false);
        if (!Input.IsActionJustPressed("use")) return;
        Open(player, true);
        player.Camera.HideHint();
    }

    public void Open(Spatial character, bool makeVisible)
    {
        if (audi != null)
        {
            audi.Stream = openSound;
            audi.Play();
        }

        character.GlobalTransform = newPlace.GlobalTransform;
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
        player.Camera.HideHint();
        SetProcess(false);
    }
}
