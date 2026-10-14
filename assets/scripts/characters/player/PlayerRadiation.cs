using Godot;

public class PlayerRadiation
{
    private const int INCREASE_SPEED = 1;
    private const string RADIATION_COUNTER_ITEM = "radiationCounter";

    private int radLevel;
    private bool stoppedSounding;
    
    private Player player;
    private AudioPlayerCommon audi;

    public PlayerRadiation(Player player)
    {
        this.player = player;
        audi = new AudioPlayerCommon(false, new NodePath("sound/audi_radiation"), player);
    }
    
    private bool IgnoreRadiation
    {
        get
        {
            var armorProps = player.Inventory.GetArmorProps();
            if (!armorProps.Contains("ignoreRadiation")) return false;
            
            var ignoreValue = bool.Parse(armorProps["ignoreRadiation"].ToString());
            return ignoreValue;
        }
    }

    public void SetRadLevel(int _radLevel)
    {
        if (_radLevel >= player.HealthMax)
        {
            radLevel = player.HealthMax;
            player.DecreaseHealth(player.HealthMax);
            player.TakeDamage(player, 1);
        }
        else
        {
            radLevel = _radLevel;
        }
    }
    
    public int GetRadLevel()
    {
        return radLevel;
    }
    
    public void IncreaseRadiation()
    {
        if (IgnoreRadiation) return;
        
        radLevel += INCREASE_SPEED;

        if (player.Health + radLevel > player.HealthMax)
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
        if (!player.Inventory.HasItem(RADIATION_COUNTER_ITEM)) return;
        stoppedSounding = false;
        audi.Play();
    }

    public void StopSounding()
    {
        audi.Stop();
    }

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
