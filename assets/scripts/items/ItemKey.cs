using Godot;

public class ItemKey: Area {
    const float RotateSpeed = 2;

    [Export]
    public string KeyName;
    [Export]
    public string KeyPickTextLink;

    Spatial mesh;
    Messages messages;
    AudioStreamSample sound;

    public override void _Ready()
    {
        mesh = GetNode<Spatial>("mesh");
        messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
        sound = GD.Load<AudioStreamSample>("res://assets/audio/item/ItemAmmo.wav");
    }

    public override void _Process(float delta)
    {
        mesh.RotateY(RotateSpeed * delta);
    }

    public void _on_Item_body_entered(Node body)
    {
        if (body is Player) {
            var player = body as Player;
            if (player.MayMove) {
                if (messages != null && KeyPickTextLink != null) {
                    messages.ShowMessage(KeyPickTextLink, "keys", 2f);
                }
            }
            player.HaveKeys.Add(KeyName);

            var audi = player.GetAudi(true);
            audi.Stream = sound;
            audi.Play();
            QueueFree();
        }
    }
}