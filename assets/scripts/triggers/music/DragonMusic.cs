using System;
using Godot;
using Godot.Collections;

//При активации играет первый трек, затем переключает на другой после его завершения
public partial class DragonMusic : TriggerBase
{
    [Export] private NodePath audiPath;
    [Export] private bool isAudi3D;
    [Export] private AudioStream startTrack;
    [Export] private AudioStream loopTrack;
    [Export] private float volumeSpeed = 0.1f;

    private AudioPlayerCommon audi;
    private bool startTrackPlayed;

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        startTrackPlayed = Convert.ToBoolean(data["startTrackPlayed"]);
        if (IsActive)
        {
            SetActive(true);
        }
    }

    public override Dictionary GetSaveData()
    {
        var saveData = base.GetSaveData();
        saveData["startTrackPlayed"] = startTrackPlayed;
        return saveData;
    }

    public override void _Ready()
    {
        audi = new AudioPlayerCommon(isAudi3D, audiPath, this);
        SetProcess(false);
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        if (IsActive)
        {
            audi.Play(startTrackPlayed ? loopTrack : startTrack);
        }
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (IsActive)
        {
            if (!startTrackPlayed 
                && audi.GetStream == startTrack && !audi.IsPlaying)
            {
                startTrackPlayed = true;
                SetActive(true);
                return;
            }
            
            if (volumeSpeed > 0 && audi.Volume < 2)
            {
                audi.Volume += volumeSpeed;
            }
        }
        else
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
            SetProcess(false);
            OnActivateTrigger();
        }
    }
}
