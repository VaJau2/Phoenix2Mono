using Godot;
using System;

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

        
        if(player.MaySmash) {
            var audiHitted = player.GetAudi(true);
            audiHitted.Stream = hitSound;
            audiHitted.Play();

            if (body is Character) {
                var victim = body as Character;
                victim.TakeDamage(player, (int)player.GetSpeed() * 3);
            } else {
                player.TakeDamage(player, (int)player.GetSpeed() / 2);
                player.wingsAudi.Stop();
                player.IsFlying = false;
            }
        }
    }
}
