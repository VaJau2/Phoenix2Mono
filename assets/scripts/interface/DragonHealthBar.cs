using Godot;
using Godot.Collections;

public partial class DragonHealthBar : Control, ISavable
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
        dragon.Connect(Character.SignalName.TakenDamage, new Callable(this, nameof(OnTakeDamage)));
        dragon.Connect(Character.SignalName.DieEvent, new Callable(this, nameof(OnDying)));
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

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            { "dragonPath", dragon?.GetPath() ?? "" }
        };
    }

    public void LoadData(Dictionary data)
    {
        var dragonPath = data["dragonPath"].ToString();
        if (!string.IsNullOrEmpty(dragonPath))
        {
            dragon = GetNode<NPC>(dragonPath);
        }
    }
}
