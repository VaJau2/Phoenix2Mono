using Godot;

public interface IDoorTeleport
{ 
    Node3D TeleportPos { get; }

    Vector3 GlobalPosition { get; }
}
