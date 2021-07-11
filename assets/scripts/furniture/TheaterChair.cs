using System;
using Godot;
using Godot.Collections;

public class TheaterChair : StaticBody, ISavable
{
    [Export] public bool isActive;
    [Export] private string otherTriggerPath;
    [Export] private float triggerTimer;
    private TriggerBase otherTrigger;
    private Spatial strikelyPlace;

    private SavableTimers timers;
    private int step = 0;

    public override void _Ready()
    {
        timers = GetNode<SavableTimers>("/root/Main/Scene/timers");
        
        if (isActive)
        {
            otherTrigger = GetNode<TriggerBase>(otherTriggerPath);
            strikelyPlace = GetNode<Spatial>("strikelyPlace");
        }
    }

    public void Sit(Player player)
    {
        if (step == 0)
        {
            player.SitOnChair(true);
            player.GlobalTransform = Global.setNewOrigin(player.GlobalTransform, strikelyPlace.GlobalTransform.origin);
            player.Rotation = new Vector3(
                player.Rotation.x,
                strikelyPlace.Rotation.y,
                player.Rotation.z
            );
            step = 1;
        }

        WaitAndSetTriggers();
    }

    private async void WaitAndSetTriggers()
    {
        if (step == 1)
        {
            while (timers.CheckTimer(Name + "_timer", triggerTimer))
            {
                await ToSignal(GetTree(), "idle_frame");
            }
            step = 2;
        }

        if (step == 2)
        {
            otherTrigger.SetActive(true);
        }

        step = 0;
    }
    
    
    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            {"step", step}
        };
    }

    public void LoadData(Dictionary data)
    {
        step = Convert.ToInt16(data["step"]);
        if (step > 0)
        {
            WaitAndSetTriggers();
        }
    }
}
