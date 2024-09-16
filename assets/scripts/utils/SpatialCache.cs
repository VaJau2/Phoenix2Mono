using Godot;

public class SpatialCache(Vector3 pos, Vector3 rot)
{
    public Vector3 Pos { get; private set; } = pos;
    public Vector3 Rot { get; private set; } = rot;
}