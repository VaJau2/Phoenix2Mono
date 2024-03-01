using Godot;

public partial class PegasusSmash : Area3D
{
    private Player_Pegasus player;
    private AudioStreamWav hitSound;

    public override void _Ready()
    {
        player = GetParent<Player_Pegasus>();
        hitSound = GD.Load<AudioStreamWav>("res://assets/audio/flying/PegasusHit.wav");
    }

    public void _on_smasharea_body_entered(Node body) 
    {
        if (body is Player) return;

        
        if(player.MaySmash && player.GetVelocity() > 6) {
            var audiHitted = player.GetAudi(true);
            audiHitted.Stream = hitSound;
            audiHitted.Play();

            if (body is Character) {
                var victim = body as Character;
                victim.TakeDamage(player, player.GetVelocity() * 3);
            } else {
                player.TakeDamage(player, player.GetVelocity() / 2);
                player.wingsAudi.Stop();
                player.IsFlying = false;
            }
        }
    }
}
