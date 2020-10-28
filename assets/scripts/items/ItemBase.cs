using Godot;

public class ItemBase: Area {

    Spatial mesh;
    protected Messages messages;
    protected AudioStreamSample sound;
    const float ROTATE_SPEED = 2;

    public override void _Ready()
    {
        mesh = GetNode<Spatial>("mesh");
        messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
    }

    public override void _Process(float delta)
    {
        mesh.RotateY(ROTATE_SPEED * delta);
    }
}