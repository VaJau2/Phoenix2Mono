using Godot;

public class PegasusSmash : Area
{
    private Player_Pegasus player;
    private AudioStreamSample hitSound;

    public override void _Ready()
    {
        player = GetParent<Player_Pegasus>();
        hitSound = GD.Load<AudioStreamSample>("res://assets/audio/flying/PegasusHit.wav");
    }

    public void _on_smasharea_body_entered(Node body)
    {
        if (body is Player) return;
        if (!player.MaySmash || player.GetSpeed() <= 6) return;

        var tempDamage = (int)player.GetSpeed() * 3;

        switch (body)
        {
            case FurnDoor door when player.IsFlyingFast:
                door.TrySmashOpen(tempDamage);
                return;
            case Character victim:
                victim.TakeDamage(player, tempDamage);
                break;
            default:
                player.TakeDamage(player, tempDamage / 6);
                player.wingsAudi.Stop();
                player.IsFlying = false;
                break;
        }
        
        var audiHitted = player.GetAudi(true);
        audiHitted.Stream = hitSound;
        audiHitted.Play();
    }
}