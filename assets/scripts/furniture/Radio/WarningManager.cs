using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

public class WarningManager : RadioManager, ISavable
{
    [Export] private string systemName;
    private readonly List<Receiver> receivers = [];

    public WarningMessage Message { get; private set; }
    private readonly List<WarningMessage> messageQueue = [];

    public AudioStream Audio => messagePlayer.Stream;
    public bool IsMessagePlaying => messagePlayer.Playing;
    public float Timer => messagePlayer.GetPlaybackPosition();
    
    private EnemiesManager enemiesManager;

    private AudioStreamPlayer3D messagePlayer;
    private Subtitles subtitles;
    
    [Signal]
    public delegate void StartMessageEvent(AudioStream stream = null);
    
    [Signal]
    public delegate void MessageFinishedEvent();
    
    public override void _Ready()
    {
        foreach (var radio in radioList)
        {
            if (radio is Receiver { OnBaseFrequency: true } receiver)
            {
                receivers.Add(receiver);
            }
        }

        messagePlayer = GetNode<AudioStreamPlayer3D>("Message Player");
        subtitles = GetNode<Subtitles>("/root/Main/Scene/canvas/subtitles");
        subtitles.Connect(nameof(Subtitles.End), this, nameof(OnMessageEnd));
        subtitles.Connect(nameof(Subtitles.ChangePhrase), this, nameof(OnAudioChange));
        
        enemiesManager = GetNodeOrNull<EnemiesManager>("/root/Main/Scene/npc");
        enemiesManager?.Connect(nameof(EnemiesManager.AlarmStarted), this, nameof(OnAlarmStart));
        enemiesManager?.Connect(nameof(EnemiesManager.AlarmEnded), this, nameof(OnAlarmEnd));
        
        base._Ready();
    }

    public void SendMessage(string messageCode, VoiceMessageTrigger trigger = null)
    {
        if (messagePlayer.Playing)
        {
            var message = new WarningMessage(messageCode, trigger);
            messageQueue.Add(message);
        }
        else
        {
            foreach (var receiver in receivers)
            {
                receiver.TuneOut();
            }
            
            StartMessage(messageCode, trigger);
        }
    }
    
    private void StartMessage(string code, VoiceMessageTrigger trigger)
    {
        ChangeRadioPauseMode(PauseModeEnum.Inherit);
        trigger?.Connect();
        
        subtitles.SetTalker(messagePlayer, systemName)
            .LoadSubtitlesFile(code)
            .StartAnimatingText();
        
        Message = new WarningMessage(code, trigger);
        EmitSignal(nameof(StartMessageEvent), messagePlayer.Stream);
    }

    private void OnAudioChange()
    {
        EmitSignal(nameof(StartMessageEvent), messagePlayer.Stream);
    }
    
    private void OnMessageEnd()
    {
        if (messageQueue.Count > 0)
        {
            StartMessage(messageQueue[0].code, messageQueue[0].trigger);
            messageQueue.RemoveAt(0);
        }
        else
        {
            foreach (var receiver in receivers)
            {
                receiver.TuneIn();
            }
            
            Message = null;
            ChangeRadioPauseMode(PauseModeEnum.Process);
            EmitSignal(nameof(MessageFinishedEvent));
        }
    }
    
    // alarm
    private void OnAlarmStart()
    {
        foreach (var receiver in receivers.Where(receiver => receiver.IsOn))
        {
            receiver.SwitchOff(true, false);
            receiver.AddToGroup("Alarm Mode");
        }
    }

    private void OnAlarmEnd()
    {
        foreach (Node node in GetTree().GetNodesInGroup("Alarm Mode"))
        {
            if (node is Receiver receiver) receiver.SwitchOn(true, false);
        }
    }
    
    public Dictionary GetSaveData()
    {
        var saveData = new Dictionary();
        
        if (string.IsNullOrEmpty(Message?.code) || Message?.trigger == null)
        {
            return saveData;
        }
        
        saveData.Add("code", Message?.code);
        saveData.Add("triggerPath", Message?.trigger.GetPath());

        if (messageQueue.Count == 0) return saveData;
        
        saveData.Add("messageQueueCount", messageQueue.Count);
        for (int i = 0; i < messageQueue.Count; i++)
        {
            saveData.Add($"code{i}", messageQueue[i].code);
            saveData.Add($"triggerPath{i}", messageQueue[i].trigger.GetPath());
        }
        
        return saveData;
    }

    public void LoadData(Dictionary data)
    {
        if (!data.Contains("code")) return;

        var code = Convert.ToString(data["code"]);
        
        var triggerPath = Convert.ToString(data["triggerPath"]);
        var trigger = GetNode<VoiceMessageTrigger>(triggerPath);
        trigger.Connect();

        Message = new WarningMessage(code, trigger);
        
        if (!data.Contains("messageQueueCount")) return;
        
        var messageQueueCount = Convert.ToInt32(data["messageQueueCount"]);
        
        for (int i = 0; i < messageQueueCount; i++)
        {
            var tempCode = Convert.ToString(data[$"code{i}"]);
            var tempTriggerPath = Convert.ToString(data[$"triggerPath{i}"]);
            var tempTrigger = GetNode<VoiceMessageTrigger>(tempTriggerPath);
            var message = new WarningMessage(tempCode, tempTrigger);
            messageQueue.Add(message);
        }
    }
}
