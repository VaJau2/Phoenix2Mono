using Godot;

public class PlayerDeathManager : Node
{
    public bool permanentDeath = true;
    
    private ColorRect blackScreen;
    private LevelsLoader levelsLoader;
    private Global global = Global.Get();
    
    [Signal]
    public delegate void PlayerDie();
    
    [Signal]
    public delegate void CloneDie();
    
    public override void _Ready()
    {
        levelsLoader = GetNode<LevelsLoader>("/root/Main");
        blackScreen = GetNode<ColorRect>("/root/Main/Scene/canvas/black");
    }
    
    public async void OnPlayerDeath()
    {
        while (blackScreen.Color.a < 1)
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
            temp.a += 0.01f;
            blackScreen.Color = temp;
            await global.ToTimer(0.04f);
        }
        
        if (permanentDeath)
        {
            levelsLoader.ShowDeathMenu();
            EmitSignal(nameof(PlayerDie));
        }
        else
        {
            EmitSignal(nameof(PlayerDie));
            EmitSignal(nameof(CloneDie));
        }
    }
}