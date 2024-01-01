using System.Collections.Generic;
using System.Linq;
using Godot;

public class WarningManager : RadioManager
{
    private List<Receiver> receivers = new ();
    
    public AudioStream message => messagePlayer.Stream;
    private List<Message> messagesList = new ();
    
    public bool IsMessagePlaying => messagePlayer.Playing;
    public float timer => messagePlayer.GetPlaybackPosition();
    
    private EnemiesManager enemiesManager;
    private AudioStream alarmSound;
    private bool isAlarmPlaying;

    private AudioStreamPlayer3D messagePlayer;
    
    [Signal]
    public delegate void SendMessageEvent(AudioStream stream = null);
    
    [Signal]
    public delegate void MessageFinishedEvent();

    private struct Message
    {
        public AudioStream stream;
        public IVoiceMessage trigger;

        public Message(AudioStream _stream, IVoiceMessage _trigger)
        {
            stream = _stream;
            trigger = _trigger;
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
        messagePlayer.Connect("finished", this, nameof(OnMessageEnd));
        
        // alarm
        alarmSound = (AudioStream)GD.Load("res://assets/audio/background/alarm.wav");
        
        enemiesManager = GetNodeOrNull<EnemiesManager>("/root/Main/Scene/npc");
        enemiesManager?.Connect(nameof(EnemiesManager.AlarmStarted), this, nameof(OnAlarmStart));
        enemiesManager?.Connect(nameof(EnemiesManager.AlarmEnded), this, nameof(OnAlarmEnd));
        
        base._Ready();
    }

    public void SendMessage(AudioStream stream, IVoiceMessage trigger)
    {
        if (messagePlayer.Playing)
        {
            var message = new Message(stream, trigger);
            messagesList.Add(message);
        }
        else
        {
            foreach (var receiver in receivers)
            {
                receiver.TuneOut();
            }
            
            messagePlayer.Stream = stream;
            trigger.Connect();
            messagePlayer.Play();
            
            EmitSignal(nameof(SendMessageEvent), message);
        }
    }

    private void OnMessageEnd()
    {
        if (messagesList.Count > 0)
        {
            messagePlayer.Stream = messagesList[0].stream;
            messagesList[0].trigger.Connect();
            messagesList.RemoveAt(0);
            messagePlayer.Play();
            EmitSignal(nameof(SendMessageEvent), message);
        }
        else
        {
            foreach (var receiver in receivers)
            {
                receiver.TuneIn();
            }
            
            EmitSignal(nameof(MessageFinishedEvent));
        }
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