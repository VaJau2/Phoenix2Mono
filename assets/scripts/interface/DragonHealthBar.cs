using Godot;

public class DragonHealthBar : Control
{
    private NPC dragon;
    private ProgressBar healthBar;
    
    public override void _Ready()
    {
        healthBar = GetNode<ProgressBar>("ProgressBar");
        
        MenuBase.LoadColorForChildren(this);
    }

    public void OnDragonSpawned(NPC spawnedDragon)
    {
        dragon = spawnedDragon;
        dragon.Connect(nameof(Character.TakenDamage), this, nameof(OnTakeDamage));
        dragon.Connect(nameof(NPC.IsDying), this, nameof(OnDying));
        Visible = true;
    }

    private void OnTakeDamage()
    {
        Visible = dragon.Health > 0;
        healthBar.Value = dragon.Health;
    }

    private void OnDying()
    {
        Visible = false;
    }
}
