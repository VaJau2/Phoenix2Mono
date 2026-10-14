using Godot;
using Godot.Collections;

//Работает с двумя плеерами - боевым и не-боевым
//При запуске действует в зависимости от активности
//Если активен, врубает первый "боевой" трек
//Если неактивен, вырубает его и, если есть второй "спокойный" трек, врубает его
public class MusicTwoPlayersTrigger : TriggerBase
{
    [Export] public NodePath audi1Path;
    [Export] public NodePath audi2Path;
    [Export] public bool isAudi3D;
    [Export] private float volume1Speed = 0.1f;
    [Export] private float volume2Speed = 0.05f;
    [Export] private float volumeMax = 2f;
    [Export] private float volumeMin = -20f;

    private AudioPlayerCommon audi1, audi2;
    
    public override void _Ready()
    {
        SetProcess(false);
        audi1 = new AudioPlayerCommon(isAudi3D, audi1Path, this);
        audi2 = new AudioPlayerCommon(isAudi3D, audi2Path, this);
        if (IsActive)
        {
            _on_activate_trigger();
        }
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        if (IsActive)
        {
            _on_activate_trigger();
        }
    }
    
    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        SetProcess(true);
    }

    private bool UpdateTrackVolume(AudioPlayerCommon riseAudi, AudioPlayerCommon fallAudi, float speed)
    {
        if (!riseAudi.IsPlaying)
        {
            riseAudi.Play();
        }

        if (riseAudi.Volume < volumeMax)
        {
            riseAudi.Volume += speed;
        }

        if (fallAudi.Volume > volumeMin)
        {
            fallAudi.Volume -= speed;
        }
        
        if (fallAudi.Volume <= volumeMin)
        {
            fallAudi.Stop();
        }

        return riseAudi.Volume >= volumeMax && fallAudi.Volume <= volumeMin;
    }

    public override void _Process(float delta)
    {
        //если активен = играет боевой трек (audi2)
        //если неактивен = играет небоевой трек (audi1)
        if (IsActive)
        {
            if (!UpdateTrackVolume(audi2, audi1, volume1Speed))
            {
                return;
            }
        }
        else
        {
            if (!UpdateTrackVolume(audi1, audi2, volume2Speed))
            {
                return;
            }
        }
        
        SetProcess(false);
        _on_activate_trigger();
    }
}
