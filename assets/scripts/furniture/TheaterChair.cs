using Godot;

public class TheaterChair : StaticBody
{
    [Export] public bool isActive;
    [Export] private string otherTriggerPath;
    [Export] private float triggerTimer;
    private TriggerBase otherTrigger;
    private Spatial strikelyPlace;

    public override void _Ready()
    {
        if (isActive)
        {
            otherTrigger = GetNode<TriggerBase>(otherTriggerPath);
            strikelyPlace = GetNode<Spatial>("strikelyPlace");
        }
    }

    public async void Sit(Player player)
    {
        player.SitOnChair(true);
        player.GlobalTransform = Global.setNewOrigin(player.GlobalTransform, strikelyPlace.GlobalTransform.origin);
        player.Rotation = new Vector3(
            player.Rotation.x,
            strikelyPlace.Rotation.y,
            player.Rotation.z
        );

        await Global.Get().ToTimer(triggerTimer);
        otherTrigger.SetActive(true);
    }
}
