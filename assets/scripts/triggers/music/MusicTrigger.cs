using Godot;
using Godot.Collections;

//Работает с одним "боевым" треком или с одним "спокойным" и одним "боевым" треком
//При запуске действует в зависимости от активности
//Если активен, врубает первый "боевой" трек
//Если неактивен, вырубает его и, если есть второй "спокойный" трек, врубает его
public partial class MusicTrigger: TriggerBase
{
    [Export] public NodePath audiPath;
    [Export] public AudioStream track;
    [Export] public AudioStream otherTrack;
    [Export] private bool Audio3d;
    [Export] private float volumeSpeed = 0.1f;

    private AudioPlayerCommon audi;

    public override void _Ready()
    {
        SetProcess(false);
        audi = new AudioPlayerCommon(Audio3d, audiPath, this);
        if (IsActive)
        {
            SetActive(true);
        }
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        if (IsActive || otherTrack != null)
        {
            SetActive(true);
        }
    }
    
    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        //если не активен, заглушаем первый трек
        //если есть второй трек, врубаем его
        if (!IsActive)
        {
            if (audi.GetStream != otherTrack && audi.IsPlaying)
            {
                if (volumeSpeed > 0)
                {
                    if (audi.Volume > -8f)
                    {
                        audi.Volume -= volumeSpeed;
                        return;
                    }
                } 
                audi.Stop();
            }

            if (otherTrack != null)
            {
                if (!audi.IsPlaying)
                {
                    audi.Play(otherTrack);
                }

                if (volumeSpeed > 0)
                {
                    if (audi.Volume < 2)
                    {
                        audi.Volume += volumeSpeed;
                        return;
                    }
                }
            }
        }
        //если активен, заглушаем второй трек
        //врубаем первый трек
        else
        {
            if (audi.GetStream != track && audi.IsPlaying)
            {
                if (volumeSpeed > 0)
                {
                    if (audi.Volume > -8f)
                    {
                        audi.Volume -= volumeSpeed;
                        return;
                    }
                } 
                audi.Stop();
            }
            
            if (!audi.IsPlaying)
            {
                audi.Play(track);
            }

            if (volumeSpeed > 0)
            {
                if (audi.Volume < 2)
                {
                    audi.Volume += volumeSpeed;
                    return;
                }
            }
            
        }

        SetProcess(false);
        OnActivateTrigger();
    }
}