using System;
using Godot;
using Godot.Collections;

public class TheaterChair : StaticBody, ISavable, IInteractable
{
    [Export] public bool isActive;
    [Export] private string otherTriggerPath;
    
    private TriggerBase otherTrigger;
    private Spatial strikelyPlace;
    private StaticBody back;

    private Player player => Global.Get().player;
    public bool MayInteract => isActive && !player.IsSitting;
    public string InteractionHintCode => "sit";

    public override void _Ready()
    {
        if (!isActive) return;
        otherTrigger = GetNodeOrNull<TriggerBase>(otherTriggerPath);
        strikelyPlace = GetNode<Spatial>("strikelyPlace");
        back = GetNode<StaticBody>("back");
    }

    public async void Interact(PlayerCamera interactor)
    {
        if (!MayInteract) return;
        
        back.CollisionLayer = 0;
        back.CollisionMask = 0;
        
        interactor.HideInteractionSquare();
        
        player.Velocity = Vector3.Zero;
        player.GlobalTransform = Global.SetNewOrigin(player.GlobalTransform, strikelyPlace.GlobalTransform.origin);
        player.Rotation = new Vector3
        (
            player.Rotation.x,
            strikelyPlace.Rotation.y,
            player.Rotation.z
        );
        
        await Global.Get().ToTimer(0.23f);
        
        player.SitOnChair(true);
        player.Connect(nameof(Character.ChangeMayMove), this, nameof(OnPlayerStandUp));
        
        otherTrigger.SetActive(true);
    }

    public void OnPlayerStandUp()
    {
        if (player.MayMove)
        {
            back.CollisionLayer = 3;
            back.CollisionMask = 3;
            player.Disconnect(nameof(Character.ChangeMayMove), this, nameof(OnPlayerStandUp));
        }
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            {"isActive", isActive},
        };
    }

    public void LoadData(Dictionary data)
    {
        if (data.Contains("isActive"))
        {
            isActive = Convert.ToBoolean(data["isActive"]);
        }
    }
}
