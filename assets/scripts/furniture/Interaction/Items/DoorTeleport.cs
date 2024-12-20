using Godot;
using Godot.Collections;

//дверь, перемещающая в отдельную подлокацию
public class DoorTeleport : StaticBody, ISavable, IInteractable
{
    [Export] public bool Closed;
    [Export] private bool Inside;
    [Export] private NodePath newPlacePath;
    [Export] private NodePath oldLocationPath;
    [Export] private NodePath newLocationPath;
    [Export] private NodePath otherDoorPath;

    [Export] private AudioStreamSample openSound;
    [Export] private AudioStreamSample closedSound;

    private Room oldRoom;
    private Room newRoom;

    public DoorTeleport otherDoor { get; private set; }
    private AudioStreamPlayer3D audi;
    private Spatial newPlace;
    private Spatial oldLocation;
    private Spatial newLocation;

    private static Player player => Global.Get().player;
    private CheckFall checkFall;

    public bool MayInteract => true;
    public string InteractionHintCode => "open";
    

    public override void _Ready()
    {
        if (oldLocationPath != null)
        {
            oldLocation = GetNode<Spatial>(oldLocationPath);
            oldRoom = GetNodeOrNull<Room>(oldLocationPath);
        }

        if (newLocationPath != null)
        {
            newLocation = GetNode<Spatial>(newLocationPath);
            newRoom = GetNodeOrNull<Room>(newLocationPath);
        }
        
        audi = GetNode<AudioStreamPlayer3D>("audi");
        newPlace = GetNode<Spatial>(newPlacePath);
        otherDoor = GetNodeOrNull<DoorTeleport>(otherDoorPath);
        checkFall = GetNodeOrNull<CheckFall>("/root/Main/Scene/terrain/checkFall");
                
        SetProcess(false);
    }
    
    public void Interact(PlayerCamera interactor)
    {
        Open(player);
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

    public void Open(Spatial character, bool soundOpening = true)
    {
        player.Camera.HideHint();
        
        if (Closed)
        {
            player.Camera.closedTimer = 1;
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

        if (oldLocation != null)
        {
            oldLocation.Visible = false;
        }

        if (newLocation != null)
        {
            newLocation.Visible = true;
        }

        if (checkFall == null) return;
        checkFall.tempDoorTeleport = this;
        checkFall.inside = Inside;

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
