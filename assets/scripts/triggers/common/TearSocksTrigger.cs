using System;
using Godot;

public partial class TearSocksTrigger : Node
{
    private const float MAX_DISTANCE = 40f;

    [Export] private AudioStream tearSound;

    private SaveNode saveNode;
    private Subtitles subtitles;
    
    private Player player => Global.Get().player;
    private NPC assistantRarity;
    
    public override void _Ready()
    {
        assistantRarity = GetNodeOrNull<NPC>("/root/Main/Scene/npc/assistantRarity");
        subtitles = GetNode<Subtitles>("/root/Main/Scene/canvas/subtitles");
        saveNode =  GetNode<SaveNode>("/root/Main/SaveNode");
        
        var levelsLoader = GetNode<LevelsLoader>("/root/Main");
        levelsLoader.Connect(LevelsLoader.SignalName.SaveDataLoaded, new Callable(this, nameof(OnSaveDataLoaded)));
    }
    
    private async void OnSaveDataLoaded()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        
        if (saveNode.SavedVariables.Contains("tornSocks"))
        {
            var tornSocks = Convert.ToBoolean(saveNode.SavedVariables["tornSocks"]);
            
            if (tornSocks)
            {
                Delete();
                return;
            }
        }
        
        player.Connect(Player.SignalName.WearItem, new Callable(this, nameof(_on_player_wear_item)));
    }

    public void _on_player_wear_item(string itemCode)
    {
        if (itemCode != "stealthArmor") return;

        var audioPlayer = player.GetAudi(true);
        audioPlayer.Connect("finished", this, nameof(Comment));
        audioPlayer.Stream = tearSound;
        audioPlayer.Play();
    }

    private void Comment()
    {
        if (assistantRarity != null 
            && player.GlobalPosition.DistanceTo(assistantRarity.GlobalPosition) < MAX_DISTANCE)
        {
            subtitles.SetTalker(assistantRarity)
                .LoadSubtitlesFile("tearSocks")
                .StartAnimatingText();
        }
        else
        {
            subtitles.SetTalker(player.GetAudi(true), "strikely")
                .LoadSubtitlesFile("tearSocks")
                .StartAnimatingText();
        }
        
        saveNode.SavedVariables["tornSocks"] = true;
        Delete();
    }

    private void Delete()
    {
        Global.AddDeletedObject(this);
        QueueFree();
    }
}
