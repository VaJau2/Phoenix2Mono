using Godot;

public class HealPotion: ItemBase {

    const int HEALING = 50;

    public override void _Ready()
    {
        base._Ready();
        sound = GD.Load<AudioStreamSample>("res://assets/audio/item/ItemHeal.wav");
    }

    public void _on_HealPotion_body_entered(Node body) 
    {
        if (body is Player) {
            var player = body as Player;
            if (player.Health < player.HealthMax) {
                if (player.Health + HEALING < player.HealthMax) {
                    player.Health += HEALING;
                } else {
                    player.Health = player.HealthMax;
                }
                AudioStreamPlayer audi = player.GetAudi(true);
                audi.Stream = sound;
                audi.Play();
                QueueFree();
            } else {
                messages.ShowMessage("healthEnough", "items", 1.5f);
            }
        }
    }
}