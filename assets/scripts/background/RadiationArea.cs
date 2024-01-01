using Godot;

public class RadiationArea : Area
{
    private const float RADIATION_TIMER = 0.06f;
    
    private Player tempPlayer;
    private float tempTimer;

    public override void _Process(float delta)
    {
        if (tempPlayer == null) return;
        
        if (tempTimer > 0)
        {
            tempTimer -= delta;
            return;
        }
        
        tempPlayer.Radiation.IncreaseRadiation();
        tempTimer = RADIATION_TIMER;
    }

    private void _on_radiation_body_entered(Node body)
    {
        if (!(body is Player player)) return;
        tempPlayer = player;
        tempPlayer.Radiation.StartSounding();
    }

    private void _on_radiation_body_exited(Node body)
    {
        if (body != tempPlayer) return;
        tempPlayer.Radiation.StopSounding();
        tempPlayer = null;
    }
}
