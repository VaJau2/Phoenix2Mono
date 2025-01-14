using Godot;

public static class Vector2Utils
{
    public static Vector2 Lerp(this Vector2 from, Vector2 to, float delta)
    {
        var newVector = from;
        newVector.x = Mathf.Lerp(newVector.x, to.x, delta);
        newVector.y = Mathf.Lerp(newVector.y, to.y, delta);
        return newVector;
    }
}
