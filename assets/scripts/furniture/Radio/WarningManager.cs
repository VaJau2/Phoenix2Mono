using System.Collections.Generic;
using System.Linq;
using Godot;

public class WarningManager : RadioManager
{
    [Export] private string systemName;
    private List<Receiver> receivers = new ();

    public Message message { get; private set; }
    private List<Message> messagesList = new ();
    
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

    public struct Message(string _code, IVoiceMessage _trigger)
    {
        public string code = _code;
        public AudioStream audio;
        public IVoiceMessage trigger = _trigger;

        public void SetAudio(AudioStream _audio)
        {
            audio = _audio;
        }

        public void Clear()
        {
            code = null;
            audio = null;
            trigger = null;
        }
    }
    
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
        if (messagePlayer.Playing)
        {
            var msg = new Message(messageCode, trigger);
            messagesList.Add(msg);
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

    private void OnAudioChange()
    {
        EmitSignal(nameof(StartMessageEvent), messagePlayer.Stream);
    }
    
    private void OnMessageEnd()
    {
        if (messagesList.Count > 0)
        {
            StartMessage(messagesList[0].code, messagesList[0].trigger);
            messagesList.RemoveAt(0);
        }
        else
        {
            foreach (var receiver in receivers)
            {
                receiver.TuneIn();
            }

            message.Clear();
            EmitSignal(nameof(MessageFinishedEvent));
        }
    }

    private void StartMessage(string code, IVoiceMessage trigger)
    {
        trigger?.Connect();

        message = new Message(code, trigger);
        
        subtitles.SetTalker(messagePlayer, systemName)
            .LoadSubtitlesFile(code)
            .StartAnimatingText();
        
        message.SetAudio(messagePlayer.Stream);
        EmitSignal(nameof(StartMessageEvent), messagePlayer.Stream);
    }
    
    // alarm
    private void OnAlarmStart()
    {
        foreach (var receiver in receivers.Where(receiver => receiver.IsOn))
        {
            receiver.SwitchOff();
            receiver.AddToGroup("Alarm Mode");
        }
    }

    private void OnAlarmEnd()
    {
        foreach (Node node in GetTree().GetNodesInGroup("Alarm Mode"))
        {
            if (node is Receiver receiver) receiver.SwitchOn();
        }
    }
}