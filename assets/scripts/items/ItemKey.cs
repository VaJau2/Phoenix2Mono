using Godot;

public class ItemKey: ItemBase {

    [Export]
    public string KeyName;
    [Export]
    public string KeyPickTextLink;

    public override void _Ready()
    {
        base._Ready();
        sound = GD.Load<AudioStreamSample>("res://assets/audio/item/ItemAmmo.wav");
    }


    public void _on_Item_body_entered(Node body)
    {
        if (body is Player) {
            var player = body as Player;
            if (player.MayMove) {
                if (messages != null && KeyPickTextLink != null) {
                    messages.ShowMessage(KeyPickTextLink, "items", 2f);
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