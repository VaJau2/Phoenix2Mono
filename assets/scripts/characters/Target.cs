using Godot;

public class Target : Character
{
    public const int TARGET_HEALTH = 30;
        
    [Export] public bool StartLie; 
    
    private AnimationPlayer anim;

    public override void _Ready()
    {
        base._Ready();
        anim = GetNode<AnimationPlayer>("anim");
        SetHealth(StartLie ? 0 : TARGET_HEALTH);
    }
    
    public void SetHealth(int newHealth)
    {
        SetStartHealth(newHealth);
        anim.Play(newHealth > 0 ? "Rise" : "Lie");
    }
    
    public void Off()
    {
        if (Health <= 0) return;
        SetStartHealth(0);
        anim.Play("Die");
    }
    
    public override void TakeDamage(Character damager, int damage, int shapeID = 0)
    {
        if (Health <= 0) return;
        if (shapeID != 0) {
            damage = (int)(damage * 1.5f);
        }
        base.TakeDamage(damager, damage, shapeID);
        if (Health <= 0)
        {
            anim.Play("Die");
        }
    }
}
