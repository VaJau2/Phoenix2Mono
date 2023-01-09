using Godot;
using Godot.Collections;
using System.Collections.Generic;

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

    RadioController radioController;
    [Export] List<NodePath> radioPaths = new List<NodePath>();
    List<Radio> radioList = new List<Radio>();

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

        radioController = GetNodeOrNull<RadioController>("/root/Main/Scene/RadioController");
        foreach (NodePath tempPath in radioPaths)
        {
            Radio radio = GetNode<Radio>(tempPath);
            radio.inRoom = true;
            radioList.Add(radio);
        }

        SetProcess(false);
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

    public void Open(Spatial character, bool makeVisible, bool soundOpening = true)
    {
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

        if (makeVisible)
        {
            oldLocation.Visible = false;
            newLocation.Visible = true;
        }

        if (checkFall == null) return;
        checkFall.tempDoorTeleport = this;
        checkFall.inside = Inside;

        player.Camera.HideHint();

        if (radioController == null) return;
        if (Inside) radioController.EnterToRoom(radioList);
        else radioController.ExitFromRoom(radioList);
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
