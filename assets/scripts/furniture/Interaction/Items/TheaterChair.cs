using System;
using Godot;
using Godot.Collections;

public partial class TheaterChair : StaticBody3D, ISavable, IInteractable
{
    [Export] public bool isActive;
    [Export] private string otherTriggerPath;
    
    private TriggerBase otherTrigger;
    private Node3D strikelyPlace;
    private StaticBody3D back;

    private Player player => Global.Get().player;
    public bool MayInteract => isActive && !player.IsSitting;
    public string InteractionHintCode => "sit";

    public override void _Ready()
    {
        if (!isActive) return;
        otherTrigger = GetNodeOrNull<TriggerBase>(otherTriggerPath);
        strikelyPlace = GetNode<Node3D>("strikelyPlace");
        back = GetNode<StaticBody3D>("back");
    }

    public void Interact(PlayerCamera interactor)
    {
        if (!MayInteract) return;
        
        back.CollisionLayer = 0;
        back.CollisionMask = 0;
        
        interactor.HideInteractionSquare();
        
        player.Velocity = Vector3.Zero;
        player.GlobalTranslation = strikelyPlace.GlobalTranslation;
        player.Rotation = new Vector3
        (
            player.Rotation.x,
            strikelyPlace.Rotation.y,
            player.Rotation.z
        );
        
        player.SitOnChair(true);
        player.Connect(nameof(Character.ChangeMayMove), new Callable(this, nameof(OnPlayerStandUp)));
        
        otherTrigger.SetActive(true);
    }

    public void OnPlayerStandUp()
    {
        if (player.MayMove)
        {
            back.CollisionLayer = 3;
            back.CollisionMask = 3;
            player.Disconnect(nameof(Character.ChangeMayMove), new Callable(this, nameof(OnPlayerStandUp)));
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
