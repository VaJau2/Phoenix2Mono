using Godot;

public class ManaEffect: Effect 
{
    const float MANA_SPEED_DELTA = 0.5f;
    private Player_Unicorn player;
    private int manaCount = 2;
    private float cooldown = 0;

    public ManaEffect() 
    {
        maxTime = 60;
        badEffect = false;
    }

    public override void SetOn(EffectHandler handler)
    {
        if (Global.Get().playerRace == Race.Unicorn) {
            player = Global.Get().player as Player_Unicorn;
            iconName = "mana-potion";
            base.SetOn(handler);
        } else {
            maxTime = 0;
        }
    }

    public override bool Count(float delta)
    {
        if (Global.Get().playerRace == Race.Unicorn) {
            if (cooldown > 0) {
                cooldown -= delta;
            } else {
                if (player.Mana < Player_Unicorn.MANA_MAX)
                {
                    player.Mana += manaCount;
                }
                cooldown = MANA_SPEED_DELTA;
            }
        }
        
        
        return base.Count(delta);
    }
}