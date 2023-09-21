using Godot;

public class PlayerRadiation
{
    private const int INCREASE_SPEED = 1;
    private const string RADIATION_COUNTER_ITEM = "radiationCounter";

    private Player player;
    private AudioPlayerCommon audi;
    private int radiationLevel;
    private bool stoppedSounding;

    public PlayerRadiation(Player player)
    {
        this.player = player;
        audi = new AudioPlayerCommon(false, new NodePath("sound/audi_radiation"), player);
    }
    
    private bool IgnoreRadiation
    {
        get
        {
            var armorProps = player.inventory.GetArmorProps();
            if (!armorProps.Contains("ignoreRadiation")) return false;
            
            var ignoreValue = int.Parse(armorProps["ignoreRadiation"].ToString());
            return ignoreValue == 1;
        }
    }

    public void IncreaseRadiation()
    {
        if (IgnoreRadiation) return;
        
        radiationLevel += INCREASE_SPEED;

        if (player.Health + radiationLevel > player.HealthMax)
        {
            player.DecreaseHealth(INCREASE_SPEED);
        }
        
        if (player.Health < 1)
        {
            player.TakeDamage(player, 1);
        }
    }

    public void StartSounding()
    {
        if (!player.inventory.HasItem(RADIATION_COUNTER_ITEM)) return;
        stoppedSounding = false;
        audi.Play();
    }

    public void StopSounding()
    {
        audi.Stop();
    }

    public int GetRadiationLevel()
        => radiationLevel;

    public void CheckDropRadiationCounter(string itemCode)
    {
        if (itemCode != RADIATION_COUNTER_ITEM || !audi.IsPlaying) return;

        stoppedSounding = true;
        StopSounding();
    }

    public void CheckTakeRadiationCounter()
    {
        if (stoppedSounding)
        {
            StartSounding();
        }
    }
}
