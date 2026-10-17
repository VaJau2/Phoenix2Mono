using Godot;

public partial class PlayerDeathManager : Node
{
    public bool PermanentDeath = true;
    public TriggerBase OnDeathTrigger;
    
    private ColorRect blackScreen;
    private LevelsLoader levelsLoader;
    
    [Signal]
    public delegate void CloneDieEventHandler();
    
    public override void _Ready()
    {
        levelsLoader = GetNode<LevelsLoader>("/root/Main");
        blackScreen = GetNode<ColorRect>("/root/Main/Scene/canvas/black");
    }
    
    public async void OnPlayerDeath()
    {
        while (blackScreen.Color.A < 1)
        {
            //затухание всей музыки на уровне
            foreach (Node node in GetTree().GetNodesInGroup("unpaused_sound"))
            {
                if (node is AudioStreamPlayer { VolumeDb: > -20f } musicAudi)
                {
                    musicAudi.VolumeDb -= 0.5f;
                }
            }

            var temp = blackScreen.Color;
            temp.A += 0.01f;
            blackScreen.Color = temp;
            await Global.Get().ToTimer(0.04f, this);
        }
        
        if (PermanentDeath)
        {
            if (OnDeathTrigger != null) OnDeathTrigger._on_activate_trigger();
            else levelsLoader.ShowDeathMenu();
        }
        else
        {
            EmitSignal(SignalName.CloneDie);
        }
    }
}