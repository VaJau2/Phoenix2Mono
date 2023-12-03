using Godot;
using System.Collections.Generic;

public class Room : SaveActive
{
    [Export] float depth = 100 / 1.5f;

    RadioController radioController;
    [Export] List<NodePath> radioPaths;
    List<RadioBase> radioList = new();

    private AudioEffectsController audioEffectController;
    
    public override void _Ready()
    {
        if (radioPaths?.Count > 0)
        {
            foreach (NodePath radioPath in radioPaths)
            {
                RadioBase radio = GetNode<RadioBase>(radioPath);
                radio.depthOfRoom = depth;
                radio.inRoom = true;
                radioList.Add(radio);
            }

            radioController = GetNodeOrNull<RadioController>("/root/Main/Scene/RadioController");
        }

        var levelsLoader = GetNode<LevelsLoader>("/root/Main");
        levelsLoader.Connect(nameof(LevelsLoader.SaveDataLoaded), this, nameof(OnSaveDataLoaded));
    }

    private async void OnSaveDataLoaded()
    {
        await ToSignal(GetTree(), "idle_frame");
        
        audioEffectController = GetNode<AudioEffectsController>("/root/Main/Scene/Player/audioEffectsController");
        if (Visible) audioEffectController.AddEffects(Name);
    }
    
    public void Enter()
    {
        if (radioController != null)
        {
            radioController.EnterToRoom(radioList);
            radioController.currentRoom = GetPath();
        }
        
        audioEffectController?.AddEffects(Name);
    }

    public void Exit()
    {
        if (radioController != null)
        {
            radioController.ExitFromRoom(radioList);
            radioController.currentRoom = null;
        }
        
        audioEffectController?.RemoveEffects(Name);
    }
}
