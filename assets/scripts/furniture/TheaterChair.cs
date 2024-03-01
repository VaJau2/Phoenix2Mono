using System;
using Godot;
using Godot.Collections;

public partial class TheaterChair : StaticBody3D, ISavable, IInteractable
{
    [Export] public bool isActive;
    [Export] private string otherTriggerPath;
    [Export] private float triggerTimer;
    private TriggerBase otherTrigger;
    private Node3D strikelyPlace;

    private float tempTimer;
    private int step;

    private Player player => Global.Get().player;
    public bool MayInteract => isActive && !player.IsSitting;
    public string InteractionHintCode => "sit";

    public override void _Ready()
    {
        SetProcess(false);
        if (!isActive) return;
        otherTrigger = GetNodeOrNull<TriggerBase>(otherTriggerPath);
        strikelyPlace = GetNode<Node3D>("strikelyPlace");
    }
    
    public override void _Process(double delta)
    {
        if (tempTimer > 0)
        {
            tempTimer -= (float)delta;
            return;
        }

        SetProcess(false);
        otherTrigger.SetActive(true);
        step = 0;
    }

    public void Interact(PlayerCamera interactor)
    {
        interactor.HideInteractionSquare();
        
        if (step == 0)
        {
            player.SitOnChair(true);
            player.GlobalTransform = Global.SetNewOrigin(player.GlobalTransform, strikelyPlace.GlobalTransform.Origin);
            player.Rotation = new Vector3(
                player.Rotation.X,
                strikelyPlace.Rotation.Y,
                player.Rotation.Z
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
        if (data.TryGetValue("isActive", out var newActive))
        {
            isActive = newActive.AsBool();
        }
        if (data.TryGetValue("tempTimer", out var newTimer))
        {
            tempTimer = newTimer.AsSingle();
        }
        
        step = Convert.ToInt16(data["step"]);
        if (step > 0)
        {
            SetProcess(true);
        }
    }
}
