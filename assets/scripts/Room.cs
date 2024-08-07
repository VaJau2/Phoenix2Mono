using Godot;
using System.Collections.Generic;
using System.Linq;

public class Room : SaveActive
{
    [Export] private float depth = 100 / 1.5f;
    
    [Export] private List<NodePath> radioPaths;
    private List<RadioBase> radioList = new();
    private RadioManager radioManager;

    [Export] private List<NodePath> activateTriggersPaths = new ();
    public List<TriggerBase> activateTriggers { get; } = new ();
    
    [Export] private bool changeEnvironment;
    [Export] private float backgroundEnergy;
    [Export] private float ambientEnergy;
    private WorldEnvironment skybox;

    private AudioEffectsController audioEffectsController => Global.Get().player?.AudioEffectsController;
    
    private RoomManager roomManager;
    
    public override void _Ready()
    {
        roomManager = GetNode<RoomManager>("/root/Main/Scene/rooms");
        if (Visible) roomManager.CurrentRoom = this;
        
        if (radioPaths?.Count > 0)
        {
            foreach (var radio in radioPaths.Select(GetNode<RadioBase>))
            {
                radio.depthOfRoom = depth;
                radio.InRoom = true;
                radioList.Add(radio);
            }

            radioManager = GetNode<RadioManager>("/root/Main/Scene/Radio Manager");
        }

        foreach (var trigger in activateTriggersPaths.Select(GetNode<TriggerBase>))
        {
            activateTriggers.Add(trigger);
        }

        skybox = GetNode<WorldEnvironment>("/root/Main/Scene/WorldEnvironment");

        var levelsLoader = GetNode<LevelsLoader>("/root/Main");
        levelsLoader.Connect(nameof(LevelsLoader.SaveDataLoaded), this, nameof(OnSaveDataLoaded));
    }

    private async void OnSaveDataLoaded()
    {
        await ToSignal(GetTree(), "idle_frame");
        
        if (!Visible) return;

        Enter(false);
    }
    
    public void Enter(bool withTriggers = true)
    {
        roomManager.CurrentRoom = this;
        radioManager?.EnterToRoom(radioList);
        audioEffectsController?.AddEffects(Name);
        
        if (changeEnvironment)
        {
            skybox.Environment.BackgroundEnergy = backgroundEnergy;
            skybox.Environment.AmbientLightEnergy = ambientEnergy;
        }
        
        if (!withTriggers || activateTriggers.Count == 0) return;
        foreach (var trigger in activateTriggers)
        {
            trigger.SetActive(true);
        }
    }

    public void Exit(bool withTriggers = true)
    {
        roomManager.CurrentRoom = null;
        radioManager?.ExitFromRoom(radioList);
        audioEffectsController?.RemoveEffects(Name);
        
        if (!withTriggers || activateTriggers.Count == 0) return;
        foreach (var trigger in activateTriggers)
        {
            trigger.SetActive(false);
        }
    }
}
