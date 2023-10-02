using Godot;

public class ManaEffect: Effect 
{
    const float MANA_SPEED_DELTA = 0.5f;
    private Player_Unicorn player;
    private int manaCount = 2;
    private float cooldown = 0;

    public ManaEffect() 
    {
        if (Global.Get().playerRace == Race.Unicorn) {
            maxTime = 60;
        } else {
            maxTime = 0;
        }
        badEffect = false;
    }

    public override void SetOn(EffectHandler handler)
    {
        if (Global.Get().playerRace == Race.Unicorn) {
            player = Global.Get().player as Player_Unicorn;
        } 

        iconName = "manaPotion";
        base.SetOn(handler);
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