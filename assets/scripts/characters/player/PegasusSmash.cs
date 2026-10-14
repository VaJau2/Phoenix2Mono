using Godot;

public partial class PegasusSmash : Area3D
{
    private Player_Pegasus player;
    private AudioStreamWAV hitSound;

    public override void _Ready()
    {
        player = GetParent<Player_Pegasus>();
        hitSound = GD.Load<AudioStreamWAV>("res://assets/audio/flying/PegasusHit.wav");
    }

    public void _on_smasharea_body_entered(Node body)
    {
        if (body is Player) return;
        if (!player.MaySmash || player.GetVelocity() <= 3) return;

        var tempDamage = (int)player.GetVelocity() * 3;

        switch (body)
        {
            case FurnDoor door when player.IsFlyingFast:
                door.TrySmashOpen(tempDamage);
                return;
            case Character victim:
                victim.TakeDamage(player, tempDamage);
                break;
            default:
                player.TakeDamage(player, tempDamage / 5);
                player.wingsAudi.Stop();
                player.IsFlying = false;
                break;
        }
        
        var audiHitted = player.GetAudi(true);
        audiHitted.Stream = hitSound;
        audiHitted.Play();
    }
}