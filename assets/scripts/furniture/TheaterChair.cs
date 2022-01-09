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

    private float tempTimer;
    private int step = 0;

    public override void _Ready()
    {
        SetProcess(false);
        if (!isActive) return;
        otherTrigger = GetNodeOrNull<TriggerBase>(otherTriggerPath);
        strikelyPlace = GetNode<Spatial>("strikelyPlace");
    }
    
    public override void _Process(float delta)
    {
        if (tempTimer > 0)
        {
            tempTimer -= delta;
            return;
        }

        SetProcess(false);
        otherTrigger.SetActive(true);
        step = 0;
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

        SetProcess(true);
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            {"isActive", isActive},
            {"step", step},
            {"tempTimer", tempTimer}
        };
    }

    public void LoadData(Dictionary data)
    {
        if (data.Contains("isActive"))
        {
            isActive = Convert.ToBoolean(data["isActive"]);
        }
        if (data.Contains("tempTimer"))
        {
            tempTimer = Convert.ToSingle(data["tempTimer"]);
        }
        step = Convert.ToInt16(data["step"]);
        if (step > 0)
        {
            SetProcess(true);
        }
    }
}
