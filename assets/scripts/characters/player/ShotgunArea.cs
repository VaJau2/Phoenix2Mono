using Godot;
using System.Collections.Generic;

public class ShotgunArea: Area {
    public List<Spatial> objectsInside = new List<Spatial>();

    public void _on_shotgunArea_body_entered(Node body) {
        if (body is Player) return;

        if (body is Character || body is BreakableObject) {
            objectsInside.Add(body as Spatial);
        }
    }

    public void _on_shotgunArea_body_exited(Node body) {
        if (body is Player) return;
        
        if (body is Character || body is BreakableObject) {
            objectsInside.Remove(body as Spatial);
        }
    }
}