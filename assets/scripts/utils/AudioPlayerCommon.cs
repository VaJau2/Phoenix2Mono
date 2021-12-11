using Godot;

//общий класс для работы с 2д аудио и 3д аудио (потому что разрабы годота так не умеют)
public class AudioPlayerCommon
{
    private AudioStreamPlayer player;
    private AudioStreamPlayer3D player3D;
    private bool audio3D;
    
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

    public AudioStream GetStream => audio3D ? player3D.Stream : player.Stream;
}