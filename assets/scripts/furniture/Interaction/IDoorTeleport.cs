using Godot;

public interface IDoorTeleport
{ 
    Spatial TeleportPos { get; }

    Vector3 GlobalTranslation { get; }
}
