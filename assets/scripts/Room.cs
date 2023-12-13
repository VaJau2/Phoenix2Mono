using Godot;
using System.Collections.Generic;

public class Room : SaveActive
{
    [Export] private float depth = 100 / 1.5f;
    
    [Export] private List<NodePath> radioPaths;
    private List<RadioBase> radioList = new();
    private RadioManager radioManager;

    [Export] private List<NodePath> activateTriggersPaths = new ();
    private List<TriggerBase> activateTriggers = new ();
    
    [Export] private bool changeEnvironment;
    [Export] private float backgroundEnergy;
    [Export] private float ambientEnergy;
    private WorldEnvironment skybox;
    
    private AudioEffectsController audioEffectController;
    
    public override void _Ready()
    {
        if (radioPaths?.Count > 0)
        {
            foreach (var radioPath in radioPaths)
            {
                var radio = GetNode<RadioBase>(radioPath);
                radio.depthOfRoom = depth;
                radio.InRoom = true;
                radioList.Add(radio);
            }

            radioManager = GetNode<RadioManager>("/root/Main/Scene/Radio Manager");
        }

        foreach (var path in activateTriggersPaths)
        {
            var trigger = GetNode<TriggerBase>(path);
            activateTriggers.Add(trigger);
        }

        skybox = GetNode<WorldEnvironment>("/root/Main/Scene/WorldEnvironment");

        var levelsLoader = GetNode<LevelsLoader>("/root/Main");
        levelsLoader.Connect(nameof(LevelsLoader.SaveDataLoaded), this, nameof(OnSaveDataLoaded));
    }

    private async void OnSaveDataLoaded()
    {
        await ToSignal(GetTree(), "idle_frame");
        
        audioEffectController = GetNode<AudioEffectsController>("/root/Main/Scene/Player/audioEffectsController");

        if (!Visible) return;

        if (changeEnvironment)
        {
            skybox.Environment.BackgroundEnergy = backgroundEnergy;
            skybox.Environment.AmbientLightEnergy = ambientEnergy;
        }
        
        audioEffectController.AddEffects(Name);
    }
    
    public void Enter()
    {
        if (radioManager != null)
        {
            radioManager.EnterToRoom(radioList);
            radioManager.currentRoom = GetPath();
        }

        foreach (var trigger in activateTriggers)
        {
            trigger.SetActive(true);
        }

        if (changeEnvironment)
        {
            skybox.Environment.BackgroundEnergy = backgroundEnergy;
            skybox.Environment.AmbientLightEnergy = ambientEnergy;
        }
        
        audioEffectController.AddEffects(Name);
    }

    public void Exit()
    {
        if (radioManager != null)
        {
            radioManager.ExitFromRoom(radioList);
            radioManager.currentRoom = null;
        }

        foreach (var trigger in activateTriggers)
        {
            trigger.SetActive(false);
        }

        audioEffectController.RemoveEffects(Name);
    }
}
