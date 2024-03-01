using Godot;
using Godot.Collections;

//дверь, перемещающая в отдельную подлокацию
public partial class DoorTeleport : StaticBody3D, ISavable, IInteractable
{
    [Export] public bool Closed;
    [Export] private bool Inside;
    [Export] private NodePath newPlacePath;
    [Export] private NodePath oldLocationPath;
    [Export] private NodePath newLocationPath;
    [Export] private NodePath otherDoorPath;

    [Export] private AudioStreamWav openSound;
    [Export] private AudioStreamWav closedSound;

    Room oldRoom;
    Room newRoom;

    public DoorTeleport otherDoor { get; private set; }
    AudioStreamPlayer3D audi;
    Node3D newPlace, oldLocation, newLocation;

    private static Player player => Global.Get().player;
    private CheckFall checkFall;

    public bool MayInteract => true;
    public string InteractionHintCode => "open";
    

    public override void _Ready()
    {
        oldRoom = GetNodeOrNull<Room>(oldLocationPath);
        newRoom = GetNodeOrNull<Room>(newLocationPath);

        audi = GetNode<AudioStreamPlayer3D>("audi");
        newPlace = GetNode<Node3D>(newPlacePath);
        oldLocation = GetNode<Node3D>(oldLocationPath);
        newLocation = GetNode<Node3D>(newLocationPath);
        otherDoor = GetNodeOrNull<DoorTeleport>(otherDoorPath);
        checkFall = GetNodeOrNull<CheckFall>("/root/Main/Scene/terrain/checkFall");
                
        SetProcess(false);
    }
    
    public void Interact(PlayerCamera interactor)
    {
        Open(player, true);
    }

    public void SoundOpening()
    {
        if (IsInstanceValid(otherDoor) && IsInstanceValid(otherDoor.audi))
        {
            otherDoor.audi.Stream = openSound;
            otherDoor.audi.Play();
        }
        
        if (IsInstanceValid(audi))
        {
            audi.Stream = openSound;
            audi.Play();
        }
    }

    public void Open(Node3D character, bool makeVisible, bool soundOpening = true)
    {
        if (Closed)
        {
            player.Camera3D.closedTimer = 1;
            audi.Stream = closedSound;
            audi.Play();
            SetProcess(false);
            return;
        }

        if (soundOpening)
        {
            SoundOpening();
        }

        if (character != null)
        {
            character.GlobalTransform = newPlace.GlobalTransform;
        
            if (character is Player_Unicorn unicorn)
            {
                unicorn.teleportInside = Inside;
            } 
        }

        if (makeVisible)
        {
            oldLocation.Visible = false;
            newLocation.Visible = true;
        }

        if (checkFall == null) return;
        checkFall.tempDoorTeleport = this;
        checkFall.inside = Inside;

        player.Camera3D.HideHint();

        oldRoom?.Exit();
        newRoom?.Enter();
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
