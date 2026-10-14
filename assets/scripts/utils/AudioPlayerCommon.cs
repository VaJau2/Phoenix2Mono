using System;
using Godot;
using Object = Godot.Object;

//общий класс для работы с 2д аудио и 3д аудио (потому что разрабы годота так не умеют)
public class AudioPlayerCommon
{
    private AudioStreamPlayer player;
    private AudioStreamPlayer3D player3D;
    private bool audio3D;

    public AudioPlayerCommon(Node audioPlayerToDefine)
    {
        if (audioPlayerToDefine is not (AudioStreamPlayer or AudioStreamPlayer3D))
        {
            throw new ArgumentException("Trying to define audio player from not player node!");
        }
        
        audio3D = audioPlayerToDefine is AudioStreamPlayer3D;
        
        if (audio3D)
        {
            player3D = audioPlayerToDefine as AudioStreamPlayer3D;
        }
        else
        {
            player = audioPlayerToDefine as AudioStreamPlayer;
        }
    }
    
    public AudioPlayerCommon(bool audio3D, NodePath audioPath, Node objToGetNode)
    {
        this.audio3D = audio3D;
        if (audio3D)
        {
            player3D = objToGetNode.GetNode<AudioStreamPlayer3D>(audioPath);
        }
        else
        {
            player = objToGetNode.GetNode<AudioStreamPlayer>(audioPath);
        }
    }

    public void Connect(string signal, Object target, string method)
    {
        if (audio3D)
        {
            player3D.Connect(signal, target, method);
        }
        else
        {
            player.Connect(signal, target, method);
        }
    }

    public void Play(AudioStream sample = null)
    {
        if (sample != null)
        {
            if (audio3D)
            {
                player3D.Stream = sample;
            }
            else
            {
                player.Stream = sample;
            }
        }
        
        if (audio3D)
        {
            player3D.Play();
        }
        else
        {
            player.Play();
        }
    }

    public void Stop()
    {
        if (audio3D)
        {
            player3D.Stop();
        }
        else
        {
            player.Stop();
        }
    }

    public float Volume
    {
        get => audio3D ? player3D.UnitDb : player.VolumeDb;
        set
        {
            if (audio3D)
            {
                player3D.UnitDb = value;
            }
            else
            {
                player.VolumeDb = value;
            }
        }
    }

    public bool IsPlaying => audio3D ? player3D.Playing : player.Playing;

    public AudioStream Stream
    {
        get => audio3D ? player3D.Stream : player.Stream;
        set
        {
            if (audio3D)
            {
                player3D.Stream = value;
            }
            else
            {
                player.Stream = value;
            }
        }
    }

    public float PlayTime
    {
        get => audio3D ? player3D.GetPlaybackPosition() : player.GetPlaybackPosition();
        set
        {
            if (audio3D)
            {
                player3D.Seek(value);
            }
            else
            {
                player.Seek(value);
            }
        }
    }

    public float PitchScale
    {
        get => audio3D ? player3D.PitchScale : player.PitchScale;
        set
        {
            if (audio3D)
            {
                player3D.PitchScale = value;
            }
            else
            {
                player.PitchScale = value;
            }
        }
    }
}