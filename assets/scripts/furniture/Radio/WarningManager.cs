using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

public class WarningManager : RadioManager, ISavable
{
    [Export] private string systemName;
    private readonly List<Receiver> receivers = [];

    public WarningMessage message { get; private set; }
    
    public bool IsMessagePlaying => messagePlayer.Playing;
    public float timer => messagePlayer.GetPlaybackPosition();
    
    private EnemiesManager enemiesManager;
    private AudioStream alarmSound;
    private bool isAlarmPlaying;

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
        
        // alarm
        alarmSound = (AudioStream)GD.Load("res://assets/audio/background/alarm.wav");
        
        enemiesManager = GetNodeOrNull<EnemiesManager>("/root/Main/Scene/npc");
        enemiesManager?.Connect(nameof(EnemiesManager.AlarmStarted), this, nameof(OnAlarmStart));
        enemiesManager?.Connect(nameof(EnemiesManager.AlarmEnded), this, nameof(OnAlarmEnd));
        
        base._Ready();
    }

    public void SendMessage(string messageCode, IVoiceMessage trigger = null)
    {
        if (messagePlayer.Playing) return;
        
        foreach (var receiver in receivers)
        {
            receiver.TuneOut();
        }
            
        StartMessage(messageCode, trigger);
    }
    
    private void StartMessage(string code, IVoiceMessage trigger)
    {
        ChangeRadioPauseMode(PauseModeEnum.Inherit);
        trigger?.Connect();
        
        subtitles.SetTalker(messagePlayer, systemName)
            .LoadSubtitlesFile(code)
            .StartAnimatingText();
        
        message = new WarningMessage(code, messagePlayer.Stream, trigger);
        EmitSignal(nameof(StartMessageEvent), messagePlayer.Stream);
    }

    private void OnAudioChange()
    {
        EmitSignal(nameof(StartMessageEvent), messagePlayer.Stream);
    }
    
    private void OnMessageEnd()
    {
        foreach (var receiver in receivers)
        {
            receiver.TuneIn();
        }

        message.Clear();
        ChangeRadioPauseMode(PauseModeEnum.Process);
        EmitSignal(nameof(MessageFinishedEvent));
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
        if (message.trigger is not VoiceMessageTrigger trigger)
        {
            return new Dictionary();
        }
        
        return new Dictionary()
        {
            {"code", message.code},
            {"triggerPath", trigger.GetPath()},
            {"audioPath", message.audio.ResourcePath}
        };
    }

    public void LoadData(Dictionary data)
    {
        var code = Convert.ToString(data["code"]);
        
        if (string.IsNullOrEmpty(code)) return;
        
        var triggerPath = Convert.ToString(data["triggerPath"]);
        var trigger = GetNode<IVoiceMessage>(triggerPath);
        
        var audioPath = Convert.ToString(data["audioPath"]);
        var audio = GD.Load<AudioStream>(audioPath);

        message = new WarningMessage(code, audio, trigger);
    }
}